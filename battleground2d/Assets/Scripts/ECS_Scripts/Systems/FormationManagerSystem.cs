using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

// This system manages formations of units, updating their positions and handling group-related logic.
[UpdateBefore(typeof(FormationCombatSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
public partial class FormationManagerSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem; // Command buffer system for deferred entity commands.
    private EntityQuery _unitQuery; // Query to retrieve all units with FormationComponent.
    private EntityQuery _unitGroupQuery; // Query to retrieve all formation groups.

    [NativeDisableParallelForRestriction]
    private NativeMultiHashMap<Entity, Entity> _groupToUnitsMap; // Maps formation groups to their respective units.

    [NativeDisableParallelForRestriction]
    private NativeHashMap<Entity, float2> _groupAveragePositions; // Stores average positions of units in each group.

    [NativeDisableParallelForRestriction]
    private NativeHashMap<Entity, FormationGroupComponent> _formationGroupMap; // Maps formation groups to their components.

    protected override void OnCreate()
    {
        // Initialize the command buffer system and entity queries.
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        _unitQuery = GetEntityQuery(
            ComponentType.ReadWrite<FormationComponent>(),
            ComponentType.Exclude<DeadTagComponent>() // Exclude dead units.
        );
        _unitGroupQuery = GetEntityQuery(typeof(FormationGroupComponent));

        // Initialize the hash map for group average positions.
        _groupAveragePositions = new NativeHashMap<Entity, float2>(256, Allocator.Persistent);
        _formationGroupMap = new NativeHashMap<Entity, FormationGroupComponent>(256, Allocator.Persistent);
        _groupToUnitsMap = new NativeMultiHashMap<Entity, Entity>(256, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        // Dispose of the hash map for group average positions if it was created.
        if (_groupAveragePositions.IsCreated)
            _groupAveragePositions.Dispose();
        if (_formationGroupMap.IsCreated)
            _formationGroupMap.Dispose();
        if (_groupToUnitsMap.IsCreated)
            _groupToUnitsMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter(); // Command buffer for parallel jobs.

        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob); // Retrieve all unit entities.
        var translations = GetComponentDataFromEntity<Translation>(true); // Retrieve Translation components for position data.
        var unitFormations = GetComponentDataFromEntity<FormationComponent>(false); // Retrieve Translation components for position data.
        var groupCount = _unitGroupQuery.CalculateEntityCount(); // Count the number of formation groups.
        _groupToUnitsMap = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length * 2, Allocator.Persistent);
        _formationGroupMap = new NativeHashMap<Entity, FormationGroupComponent>(groupCount * 2, Allocator.Persistent);
        // Initialize or clear the native maps used for group-to-unit mapping and formation group data.
        InitializeOrClearNativeMaps(unitEntities.Length, groupCount);

        // Populate the formation group map and group-to-units map.
        PopulateFormationGroupMap();
        PopulateGroupToUnitsMap(unitEntities);

        // Calculate the number of units in each group.
        var groupToUnitCountMap = GetCountsPerKey(_groupToUnitsMap, Allocator.TempJob);

        // Update the bounds of each formation group based on unit positions.
        UpdateFormationGroupBounds(groupToUnitCountMap, translations, unitFormations);

        // Update the positions of units in their formations.
        UpdateFormationComponents(groupToUnitCountMap);

        //// Update the bounds of each formation group based on unit positions.
        //UpdateFormationGroupBounds(groupToUnitCountMap, translations, unitFormations);

        // Schedule a job to remove or add colliders for groups based on their status.
        var removeGroupCollidersJobHandle = RemoveGroupColliders(ecb);
        Dependency = JobHandle.CombineDependencies(Dependency, removeGroupCollidersJobHandle);

        // Add the job handle to the command buffer system.
        _ecbSystem.AddJobHandleForProducer(Dependency);

        // Dispose of temporary data used during the update.
        DisposeTemporaryData(unitEntities);
        groupToUnitCountMap.Dispose();
        _groupToUnitsMap.Dispose();
        _formationGroupMap.Dispose();
    }

    private void InitializeOrClearNativeMaps(int unitCount, int groupCount)
    {
        // Initialize or clear the group-to-units map.
        if (!_groupToUnitsMap.IsCreated)
            _groupToUnitsMap = new NativeMultiHashMap<Entity, Entity>(unitCount * 2, Allocator.Persistent);
        else
            _groupToUnitsMap.Clear();

        // Initialize or clear the formation group map.
        if (!_formationGroupMap.IsCreated)
            _formationGroupMap = new NativeHashMap<Entity, FormationGroupComponent>(groupCount * 2, Allocator.Persistent);
        else
            _formationGroupMap.Clear();
    }

    private void PopulateFormationGroupMap()
    {
        var formationGroupWriter = _formationGroupMap.AsParallelWriter();
        // Populate the formation group map with all entities that have a FormationGroupComponent.
        Entities
            .WithNone<DeadTagComponent>()
            .WithAll<FormationGroupComponent>()
            .ForEach((Entity entity, ref FormationGroupComponent formationGroupComponent) =>
            {
                formationGroupWriter.TryAdd(entity, formationGroupComponent);
            }).WithBurst().ScheduleParallel(Dependency).Complete();
    }

    private void PopulateGroupToUnitsMap(NativeArray<Entity> unitEntities)
    {
        var groupToUnitsWriter = _groupToUnitsMap.AsParallelWriter();
        // Map each unit to its formation group if it has one.
        Entities
            .WithNone<DeadTagComponent>()
            .WithAll<FormationComponent>()
            .ForEach((Entity entity, ref FormationComponent formationComponent) =>
            {
                if (formationComponent.FormationGroupEntity.HasValue)
                {
                    groupToUnitsWriter.Add(formationComponent.FormationGroupEntity.Value, entity);
                }
            }).WithBurst().ScheduleParallel(Dependency).Complete();
    }

    private void UpdateFormationComponents(NativeHashMap<Entity, int> groupToUnitCountMap)
    {
        var formationGroupMapTemp = _formationGroupMap;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var parallelEcb = ecb.AsParallelWriter();

        Entities
            .WithNone<DeadTagComponent>()
            .WithAll<FormationComponent>()
            .WithReadOnly(groupToUnitCountMap)
            .WithReadOnly(formationGroupMapTemp)
            .ForEach((Entity entity, int entityInQueryIndex,
                      ref FormationComponent formationComponent,
                      ref CommandData command,
                      ref HasTarget hasTarget) =>
            {
                // Formation "weight" / mass depending on type
                formationComponent.FormationWeight =
                    (formationComponent.FormationType == FormationType.Phalanx)
                        ? 3.0f
                        : 1.0f;

                if (!formationComponent.FormationGroupEntity.HasValue)
                    return;

                // Look up this unit's formation group
                if (!formationGroupMapTemp.TryGetValue(formationComponent.FormationGroupEntity.Value, out var formationGroup))
                    return;

                // Always propagate the group's current command down to the unit
                command.Command = formationGroup.CurrentCommand;

                // Optional: number of units in this group for slot layout
                groupToUnitCountMap.TryGetValue(formationComponent.FormationGroupEntity.Value, out var groupValueCount);

                switch (formationComponent.Status)
                {
                    case FormationStatus.Hold:
                        {
                            // In Hold: recompute slot positions tightly around the anchor
                            if (groupValueCount > 0)
                            {
                                formationComponent.FormationPosition = CalculatePhalanxPosition(
                                formationComponent.SlotIndex,
                                groupValueCount,
                                formationGroup.UnitsPerRow,
                                formationGroup.UnitSpacing,
                                formationGroup.AnchorPosition
                            );
                                formationComponent.Direction = formationGroup.FormationFacingDirection;
                            }

                            // We *don't* force HasTarget here unless you want the unit
                            // to explicitly walk back into its slot when idle.
                            // If you do, keep it very gentle and only when no entity target:
                            if (hasTarget.TargetEntity == Entity.Null &&
                            hasTarget.Type == HasTarget.TargetType.Position)
                            {
                                hasTarget.TargetPosition = formationComponent.FormationPosition;
                            }

                            // Don't touch HasTarget.TargetEntity – let Combat/Target systems assign it.
                            break;
                        }

                    case FormationStatus.Engaged:
                        {
                            // Engaged: combat systems are in charge of HasTarget and CombatState.
                            // Do NOT overwrite HasTarget here, or you'll fight the CombatSystem.
                            //
                            // You can still update Direction if you want the formation to "face" something:
                            // formationComponent.Direction = formationGroup.FormationFacingDirection;

                            // If you ever want to debug distance-to-anchor, you can do it here,
                            // but with no writes to HasTarget or CombatState.
                            break;
                        }

                    case FormationStatus.Disengaging:
                        {
                            // Disengaging: formation/retreat logic owns movement.
                            // Force units to move toward the group's anchor/retreat point.

                            // Optionally recompute FormationPosition using the *new* AnchorPosition,
                            // so everyone reforms into a proper block at the retreat location.
                            if (groupValueCount > 0)
                            {
                                formationComponent.FormationPosition = CalculatePhalanxPosition(
                                formationComponent.SlotIndex,
                                groupValueCount,
                                formationGroup.UnitsPerRow,
                                formationGroup.UnitSpacing,
                                formationGroup.AnchorPosition
                            );
                                formationComponent.Direction = formationGroup.FormationFacingDirection;
                            }

                            hasTarget.Type = HasTarget.TargetType.Position;
                            hasTarget.TargetEntity = Entity.Null;
                            hasTarget.TargetPosition = formationComponent.FormationPosition;

                            // While disengaging we never want FindTarget to re-fire
                            // and reacquire melee targets.
                            if (HasComponent<FindTargetCommandTag>(entity))
                            {
                                parallelEcb.RemoveComponent<FindTargetCommandTag>(entityInQueryIndex, entity);
                            }

                            break;
                        }

                    default:
                        {
                            // Other statuses (Moving, Routing, etc.) can be treated like a very loose Hold,
                            // or left untouched depending on how you extend FormationStatus.

                            // For now, we do nothing special here to avoid surprising interactions.
                            break;
                        }
                }
            }).WithBurst().ScheduleParallel(Dependency).Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }


    private void UpdateFormationGroupBounds(NativeHashMap<Entity, int> groupToUnitCountMap, ComponentDataFromEntity<Translation> translations, ComponentDataFromEntity<FormationComponent> unitFormations)
    {


        // Update the bounds of each formation group based on the positions of its units.
        Entities
            .WithNone<DeadTagComponent>()
            .WithAll<FormationGroupComponent>()
            .WithReadOnly(groupToUnitCountMap)
            .WithReadOnly(_groupToUnitsMap)
            .ForEach((Entity groupEntity, ref FormationGroupComponent formationGroupComponent) =>
            {


                if (groupToUnitCountMap.TryGetValue(groupEntity, out var groupValueCount))
                {
                    var unitEntitiesList = GetValuesForKey(_groupToUnitsMap, groupEntity, Allocator.TempJob);
                    if (formationGroupComponent.PriorGroupCount != unitEntitiesList.Length)
                    {
                        //re index slots
                        formationGroupComponent.ReIndexSlots = true;
                        formationGroupComponent.PriorGroupCount = unitEntitiesList.Length;
                        ////foreach (var unitEntity in unitEntitiesList)
                        //for (int i = 0; i < unitEntitiesList.Length; i++)
                        //{
                        //    var unitEntity = unitEntitiesList[i];
                        //    if (unitFormations.HasComponent(unitEntity))
                        //    {
                        //        var unitFormation = unitFormations[unitEntity];
                        //        unitFormation.SlotIndex = i;
                        //        unitFormations[unitEntity] = unitFormation;
                        //    }
                        //}
                    }

                    formationGroupComponent.CurrentUnitAveragePosition = CalculateAveragePositionForGroup(unitEntitiesList, translations);

                    var bounds = CalculateFormationBounds(unitEntitiesList, translations, formationGroupComponent.UnitsPerRow,
                        formationGroupComponent.UnitSpacing, formationGroupComponent.CurrentUnitAveragePosition);

                    formationGroupComponent.BoundsMin = bounds.Min;
                    formationGroupComponent.BoundsMax = bounds.Max;
                    unitEntitiesList.Dispose();
                }
            }).WithoutBurst().Run();




        var groupEntities = _unitGroupQuery.ToEntityArray(Allocator.TempJob);

        var reindexJob = new ReindexFormationsPerGroupJob
        {
            GroupEntities = groupEntities,
            GroupToUnits = _groupToUnitsMap, // your cached map
            Formations = GetComponentDataFromEntity<FormationComponent>(false),
            Groups = GetComponentDataFromEntity<FormationGroupComponent>(false),
            DeadTags = GetComponentDataFromEntity<DeadTagComponent>(true)
        };

        Dependency = reindexJob.Schedule(groupEntities.Length, 1, Dependency);
        Dependency.Complete();

        groupEntities.Dispose();

    }

    private JobHandle RemoveGroupColliders(EntityCommandBuffer.ParallelWriter ecb)
    {
        // Remove or add colliders for units based on their collider status.
        var jobHandle = Entities.WithNone<DeadTagComponent>()
            .WithAll<FormationComponent, CollidableTag>()
            .WithBurst()
            .ForEach((Entity entity, int entityInQueryIndex, ref FormationComponent formation) =>
            {
                if (formation.ColliderStatus == FormationColliderStatus.Group)
                {
                    ecb.RemoveComponent<CollidableTag>(entityInQueryIndex, entity);
                }
                else
                {
                    ecb.AddComponent<CollidableTag>(entityInQueryIndex, entity);
                }
            }).ScheduleParallel(Dependency);

        return jobHandle;
    }

    private void DisposeTemporaryData(NativeArray<Entity> unitEntities)
    {
        // Dispose of temporary data used during the update.
        Dependency = unitEntities.Dispose(Dependency);
        Dependency.Complete();
    }

    private static float2 CalculateAveragePositionForGroup(NativeList<Entity> unitEntities, ComponentDataFromEntity<Translation> translations)
    {
        // Calculate the average position of all units in a group.
        float2 sum = float2.zero;
        int validUnitCount = 0;

        foreach (var unitEntity in unitEntities)
        {
            if (translations.HasComponent(unitEntity))
            {
                var translation = translations[unitEntity];
                sum += new float2(translation.Value.x, translation.Value.y);
                validUnitCount++;
            }
        }

        return validUnitCount > 0 ? sum / validUnitCount : float2.zero;
    }

    private static FormationBounds CalculateFormationBounds(NativeList<Entity> unitEntities, ComponentDataFromEntity<Translation> translations, int unitsPerRow, float spacing, float2 anchor)
    {
        // Calculate the bounds of a formation based on the positions of its units.
        if (unitEntities.Length == 0 || unitsPerRow <= 0)
            return new FormationBounds { Min = anchor, Max = anchor };

        float2 min = new float2(float.MaxValue, float.MaxValue);
        float2 max = new float2(float.MinValue, float.MinValue);

        foreach (var unitEntity in unitEntities)
        {
            if (translations.HasComponent(unitEntity))
            {
                var translation = translations[unitEntity];
                float2 position = new float2(translation.Value.x, translation.Value.y);

                min = math.min(min, position);
                max = math.max(max, position);
            }
        }

        // Add padding for unit radius.
        float unitRadius = 0.125f;
        min -= new float2(unitRadius, unitRadius);
        max += new float2(unitRadius, unitRadius);

        return new FormationBounds { Min = min, Max = max };
    }

    [BurstCompile]
    public static float2 CalculatePhalanxPosition(int unitIndex, int totalUnits, int unitsPerRow, float spacing, float2 anchor)
    {
        // Calculate the position of a unit in a phalanx formation.  
        if (unitsPerRow <= 0)
            unitsPerRow = 16;
        int row = unitIndex / unitsPerRow;
        int col = unitIndex % unitsPerRow;

        int rowCount = (totalUnits + unitsPerRow - 1) / unitsPerRow;
        int columnsThisRow = math.min(unitsPerRow, totalUnits - row * unitsPerRow);

        float formationWidth = (columnsThisRow - 1) * spacing;
        float offsetX = col * spacing - formationWidth * 0.5f;

        float formationHeight = (rowCount - 1) * spacing;
        float offsetY = -row * spacing + formationHeight * 0.5f;

        // Ensure rows with fewer columns do not drift by clamping offsets.  
        if (columnsThisRow < unitsPerRow)
        {
            offsetX = math.clamp(offsetX, -formationWidth * 0.5f, formationWidth * 0.5f);
        }

        return anchor + new float2(offsetX, offsetY);
    }

    public static NativeHashMap<Entity, int> GetCountsPerKey(NativeMultiHashMap<Entity, Entity> groupToUnits, Allocator allocator)
    {
        // Count the number of units in each group.
        if (groupToUnits.Count() == 0)
        {
            Debug.Log($"No units found for GetCountsPerKey({groupToUnits}, {allocator})");
        }

        var counts = new NativeHashMap<Entity, int>(groupToUnits.Count(), allocator);

        foreach (var kv in groupToUnits)
        {
            counts[kv.Key] = counts.TryGetValue(kv.Key, out var current) ? current + 1 : 1;
        }

        return counts;
    }

    public static NativeList<Entity> GetValuesForKey(NativeMultiHashMap<Entity, Entity> map, Entity key, Allocator allocator)
    {
        // Retrieve all values (units) for a given key (group).
        var values = new NativeList<Entity>(allocator);

        if (map.TryGetFirstValue(key, out var value, out var it))
        {
            do
            {
                values.Add(value);
            } while (map.TryGetNextValue(out value, ref it));
        }

        return values;
    }

    private struct FormationBounds
    {
        public float2 Min; // Minimum corner of the formation bounds.
        public float2 Max; // Maximum corner of the formation bounds.
    }
    public struct SlotEntry
    {
        public int OldSlot;
        public Entity Entity;
    }


    [BurstCompile]
    public struct ReindexFormationsPerGroupJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> GroupEntities;
        [ReadOnly] public NativeMultiHashMap<Entity, Entity> GroupToUnits;

        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<FormationComponent> Formations;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<FormationGroupComponent> Groups;

        [ReadOnly] public ComponentDataFromEntity<DeadTagComponent> DeadTags;

        public void Execute(int index)
        {
            var groupEntity = GroupEntities[index];
            var group = Groups[groupEntity];
            if (!group.ReIndexSlots)
                return;

            var aliveUnits = new NativeList<SlotEntry>(Allocator.Temp);

            if (GroupToUnits.TryGetFirstValue(groupEntity, out var unitEntity, out var iterator))
            {
                do
                {
                    if (DeadTags.HasComponent(unitEntity) || !Formations.HasComponent(unitEntity))
                        continue;

                    var formation = Formations[unitEntity];
                    aliveUnits.Add(new SlotEntry { OldSlot = formation.SlotIndex, Entity = unitEntity });
                }
                while (GroupToUnits.TryGetNextValue(out unitEntity, ref iterator));
            }

            if (aliveUnits.Length == 0)
            {
                aliveUnits.Dispose();
                return;
            }

            aliveUnits.Sort(new SlotComparer());

            for (int i = 0; i < aliveUnits.Length; i++)
            {
                var formation = Formations[aliveUnits[i].Entity];
                formation.SlotIndex = i;
                Formations[aliveUnits[i].Entity] = formation;
            }

            aliveUnits.Dispose();
        }

        private struct SlotEntry
        {
            public int OldSlot;
            public Entity Entity;
        }

        private struct SlotComparer : IComparer<SlotEntry>
        {
            public int Compare(SlotEntry a, SlotEntry b)
            {
                return a.OldSlot.CompareTo(b.OldSlot);
            }
        }
    }


}

