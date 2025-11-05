using System;
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
        _groupAveragePositions = new NativeHashMap<Entity, float2>(64, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        // Dispose of the hash map for group average positions if it was created.
        if (_groupAveragePositions.IsCreated)
            _groupAveragePositions.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter(); // Command buffer for parallel jobs.

        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob); // Retrieve all unit entities.
        var translations = GetComponentDataFromEntity<Translation>(true); // Retrieve Translation components for position data.
        var groupCount = _unitGroupQuery.CalculateEntityCount(); // Count the number of formation groups.

        // Initialize or clear the native maps used for group-to-unit mapping and formation group data.
        InitializeOrClearNativeMaps(unitEntities.Length, groupCount);

        // Populate the formation group map and group-to-units map.
        PopulateFormationGroupMap();
        PopulateGroupToUnitsMap(unitEntities);

        // Calculate the number of units in each group.
        var groupToUnitCountMap = GetCountsPerKey(_groupToUnitsMap, Allocator.TempJob);

        // Update the positions of units in their formations.
        UpdateFormationComponents(groupToUnitCountMap);

        // Update the bounds of each formation group based on unit positions.
        UpdateFormationGroupBounds(groupToUnitCountMap, translations);

        // Schedule a job to remove or add colliders for groups based on their status.
        var removeGroupCollidersJobHandle = RemoveGroupColliders(ecb);
        Dependency = JobHandle.CombineDependencies(Dependency, removeGroupCollidersJobHandle);

        // Add the job handle to the command buffer system.
        _ecbSystem.AddJobHandleForProducer(Dependency);

        // Dispose of temporary data used during the update.
        DisposeTemporaryData(unitEntities);
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
        // Update the formation position of each unit based on its group and slot index.
        Entities
            .WithAll<FormationComponent>()
            .WithReadOnly(groupToUnitCountMap)
            .WithReadOnly(formationGroupMapTemp)
            .ForEach((Entity entity, ref FormationComponent formationComponent) =>
            {
                if (groupToUnitCountMap.TryGetValue(formationComponent.FormationGroupEntity.Value, out var groupValueCount) &&
                    formationGroupMapTemp.TryGetValue(formationComponent.FormationGroupEntity.Value, out var formationGroup))
                {
                    formationComponent.FormationPosition = CalculatePhalanxPosition(
                        formationComponent.SlotIndex,
                        groupValueCount,
                        formationGroup.UnitsPerRow,
                        formationGroup.UnitSpacing,
                        formationGroup.AnchorPosition
                    );
                }
            }).WithBurst().ScheduleParallel(Dependency).Complete();
    }

    private void UpdateFormationGroupBounds(NativeHashMap<Entity, int> groupToUnitCountMap, ComponentDataFromEntity<Translation> translations)
    {
        // Update the bounds of each formation group based on the positions of its units.
        Entities
            .WithAll<FormationGroupComponent>()
            .WithReadOnly(groupToUnitCountMap)
            .WithReadOnly(_groupToUnitsMap)
            .ForEach((Entity groupEntity, ref FormationGroupComponent formationGroupComponent) =>
            {
                if (groupToUnitCountMap.TryGetValue(groupEntity, out var groupValueCount))
                {
                    var unitEntitiesList = GetValuesForKey(_groupToUnitsMap, groupEntity, Allocator.TempJob);
                    formationGroupComponent.CurrentUnitAveragePosition = CalculateAveragePositionForGroup(unitEntitiesList, translations);

                    var bounds = CalculateFormationBounds(groupValueCount, formationGroupComponent.UnitsPerRow,
                        formationGroupComponent.UnitSpacing, formationGroupComponent.CurrentUnitAveragePosition);

                    formationGroupComponent.BoundsMin = bounds.Min;
                    formationGroupComponent.BoundsMax = bounds.Max;
                }
            }).WithoutBurst().Run();
    }

    private JobHandle RemoveGroupColliders(EntityCommandBuffer.ParallelWriter ecb)
    {
        // Remove or add colliders for units based on their collider status.
        var jobHandle = Entities
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

    private static FormationBounds CalculateFormationBounds(int unitCount, int unitsPerRow, float spacing, float2 anchor)
    {
        // Calculate the bounds of a formation based on the number of units, rows, and spacing.
        if (unitCount == 0 || unitsPerRow <= 0)
            return new FormationBounds { Min = anchor, Max = anchor };

        int totalRows = (unitCount + unitsPerRow - 1) / unitsPerRow;
        float formationWidth = (math.min(unitsPerRow, unitCount) - 1) * spacing;
        float formationHeight = (totalRows - 1) * spacing;

        float2 min = new float2(anchor.x - formationWidth * 0.5f, anchor.y - formationHeight * 0.5f);
        float2 max = new float2(anchor.x + formationWidth * 0.5f, anchor.y + formationHeight * 0.5f);

        float unitRadius = 0.125f; // Add padding for unit radius.
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
        int col = unitIndex - row * unitsPerRow;

        int rowCount = (totalUnits + unitsPerRow - 1) / unitsPerRow;
        int columnsThisRow = math.min(unitsPerRow, totalUnits - row * unitsPerRow);

        float formationWidth = (columnsThisRow - 1) * spacing;
        float offsetX = col * spacing - formationWidth * 0.5f;

        float formationHeight = (rowCount - 1) * spacing;
        float offsetY = row * -spacing + formationHeight * 0.5f;

        return anchor + new float2(offsetX, offsetY);
    }

    public static NativeHashMap<Entity, int> GetCountsPerKey(NativeMultiHashMap<Entity, Entity> groupToUnits, Allocator allocator)
    {
        // Count the number of units in each group.
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
}

