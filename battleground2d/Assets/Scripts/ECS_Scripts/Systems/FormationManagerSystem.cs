// Calculates all formation positions once per frame
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;
[UpdateBefore(typeof(FormationCombatSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
public class FormationManagerSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _unitQuery;
    /// <summary>
    /// Holds a runtime mapping of FormationGroupEntity → UnitEntities
    /// Built each frame by FormationManagerSystem
    /// Read by systems like FormationCollisionSystem, FormationIntegritySystem, etc.
    /// </summary>
    public NativeMultiHashMap<Entity, Entity> _groupToUnits;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        _unitQuery = GetEntityQuery(typeof(FormationComponent));

        // Create a singleton to hold our cached mapping
        //Entity cacheEntity = EntityManager.CreateEntity(typeof(FormationRuntimeCache));
        //EntityManager.SetName(cacheEntity, "FormationRuntimeCache");
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        //get unit entites and formation components
        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob);
        var formationComponents = _unitQuery.ToComponentDataArray<FormationComponent>(Allocator.TempJob);

        // ---------------------------------------------------------
        // STEP 1: Build mapping of group → unit indices
        // ---------------------------------------------------------
        var groupToUnitIndices = new NativeMultiHashMap<Entity, int>(unitEntities.Length, Allocator.TempJob);
        var processedGroups = new NativeHashSet<Entity>(256, Allocator.TempJob);

        //build map group -> unit indices
        for (int i = 0; i < formationComponents.Length; i++)
        {
            var groupEnity = formationComponents[i].FormationGroupEntity.GetValueOrDefault(Entity.Null);
            if (groupEnity != Entity.Null)
                groupToUnitIndices.Add(groupEnity, i);
        }

        // ---------------------------------------------------------
        // STEP 2: Prepare the runtime cache for other systems
        // ---------------------------------------------------------
        //var cacheEntity = GetSingletonEntity<FormationRuntimeCache>();

        //// Clean up last frame's cache (we'll rebuild it each frame)
        //if (EntityManager.HasComponent<FormationRuntimeCache>(cacheEntity))
        //{
        //    var oldCache = EntityManager.GetComponentData<FormationRuntimeCache>(cacheEntity);
        //    if (oldCache.GroupToUnits.IsCreated)
        //        oldCache.GroupToUnits.Dispose();
        //}
        // Create a new mapping for this frame
        if (_groupToUnits.IsCreated)
        {
            if (_groupToUnits.Capacity < unitEntities.Length)
            {
                _groupToUnits.Dispose();
                _groupToUnits = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length * 2, Allocator.Persistent);
            }
            else
            {
                _groupToUnits.Clear();
            }
        }
        else
        {
            // Not created yet, create new
            _groupToUnits = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length * 2, Allocator.Persistent);
        }
        var groupToUnits = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length, Allocator.Persistent);


        // ---------------------------------------------------------
        // STEP 3: Iterate over each formation group
        // ---------------------------------------------------------
        var entityManager = EntityManager;
        var groupKeys = groupToUnitIndices.GetKeyArray(Allocator.TempJob);

        //update units in each group
        foreach (var groupEntity in groupKeys)
        {
            if (!processedGroups.Add(groupEntity)) continue;

            if (!entityManager.Exists(groupEntity)) continue;
            if (!entityManager.HasComponent<FormationGroupComponent>(groupEntity)) continue;

            var groupData = entityManager.GetComponentData<FormationGroupComponent>(groupEntity);
            //gather units in this group
            var unitIndices = new NativeList<int>(Allocator.TempJob);
            NativeMultiHashMapIterator<Entity> it;
            int idx;
            if (groupToUnitIndices.TryGetFirstValue(groupEntity, out idx, out it)) 
            {
                do { unitIndices.Add(idx);  
                } while (groupToUnitIndices.TryGetNextValue(out idx, ref it));
            }

            // 🔹 Sort deterministically by entity index so units keep stable positions
            //unitIndices.Sort(new EntityIndexComparer
            //{
            //    UnitEntities = unitEntities
            //});

            int unitCount = unitIndices.Length;
            if (unitCount == 0) 
            {
                unitIndices.Dispose(); 
                continue; 
            }

            var unitEntitiesForGroup = new NativeArray<Entity>(unitCount, Allocator.TempJob);
            var newPositions = new NativeArray<float2>(unitCount, Allocator.TempJob);
            var updatedFormations = new NativeArray<FormationComponent>(unitCount, Allocator.TempJob);

            FormationGenerator.GeneratePhalanxFomationForJob(newPositions, groupData.UnitsPerRow, groupData.UnitSpacing, groupData.AnchorPosition);

            // Assume newPositions is NativeArray<float2> of unit positions calculated by your formation generator
            float2 minPos = newPositions[0];
            float2 maxPos = newPositions[0];

            for (int i = 1; i < newPositions.Length; i++)
            {
                var pos = newPositions[i];
                minPos = math.min(minPos, pos);
                maxPos = math.max(maxPos, pos);
            }

            float unitRadius = .125f; // or fixed size depending on your unit scale
            // Expand bounds by unit radius so units fit inside AABB
            minPos -= new float2(unitRadius, unitRadius);
            maxPos += new float2(unitRadius, unitRadius);
            groupData.BoundsMin = minPos;
            groupData.BoundsMax = maxPos;
            entityManager.SetComponentData(groupEntity, groupData);


            for (int i = 0; i < unitCount; i++)
            {
                int unitIndex = unitIndices[i];
                unitEntitiesForGroup[i] = unitEntities[unitIndex];
                updatedFormations[i] = formationComponents[unitIndex]; //Copy existing data!
            }

            //assign update positions using parallel job
            var applyJob = new ApplyFormationPositionJob 
            {
                Entities = unitEntitiesForGroup,
                UpdatedFormations = updatedFormations,
                NewPositions = newPositions,
                ECB = ecb
            };
            Dependency = applyJob.Schedule(unitCount, 64, Dependency);

            //clean up
            unitIndices.Dispose();
            //unitEntitiesForGroup.Dispose();
            //newPositions.Dispose();
            //updatedFormations.Dispose();

        }


        // ---------------------------------------------------------
        // STEP 4: Store the cache so other systems can read it
        // ---------------------------------------------------------
        _groupToUnits = groupToUnits;

        // ---------------------------------------------------------
        // STEP 5: Dispose temp data and finalize
        // ---------------------------------------------------------
        unitEntities.Dispose();
        formationComponents.Dispose();
        groupToUnitIndices.Dispose();
        processedGroups.Dispose();
        groupKeys.Dispose();

        _ecbSystem.AddJobHandleForProducer(Dependency);


    }


    public struct EntityIndexComparer : IComparer<int>
    {
        [ReadOnly] public NativeArray<Entity> UnitEntities;

        public int Compare(int a, int b)
        {
            var ea = UnitEntities[a];
            var eb = UnitEntities[b];
            if (ea.Index < eb.Index) return -1;
            if (ea.Index > eb.Index) return 1;
            return ea.Version.CompareTo(eb.Version);
        }
    }
    [BurstCompile]
    private struct ApplyFormationPositionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> Entities;
        public NativeArray<FormationComponent> UpdatedFormations;
        [ReadOnly] public NativeArray<float2> NewPositions;

        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(int index)
        {
            if (Entities[index] == Entity.Null) return;
            var formation = UpdatedFormations[index];
            formation.FormationPosition = NewPositions[formation.SlotIndex];
            ECB.SetComponent(index, Entities[index], formation);
        }
    }
}

[UpdateAfter(typeof(FormationManagerSystem))]
[UpdateBefore(typeof(CombatSystem))]
public partial class FormationCombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithName("FormationCombatLogic")
            .WithAll<FormationComponent>()
            .WithNone<DeadTagComponent>()
            .ForEach((Entity entity,
                     ref HasTarget hasTarget,
                     ref CombatState combatState,
                     in FormationComponent formation,
                     in Translation translation) =>
            {
                switch (formation.Status)
                {
                    case FormationStatus.Hold:
                    default:
                        HandleHoldFormation(ref hasTarget, ref combatState, formation, translation);
                        break;

                    case FormationStatus.Engaged:
                        HandleEngagedFormation(ref hasTarget, ref combatState, formation, translation);
                        break;

                    case FormationStatus.Broken:
                        // Let normal combat system handle it
                        break;

                    case FormationStatus.Returning:
                        //HandleReturningFormation(ref hasTarget, ref combatState, formation, translation);
                        break;
                }
            }).ScheduleParallel();
    }

    private static void HandleHoldFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                   FormationComponent formation, Translation translation)
    {
        // Override target to formation position
        hasTarget.Type = HasTarget.TargetType.Position;
        hasTarget.TargetPosition = formation.FormationPosition;
        //hasTarget.TargetPosition = new float2(formation.FormationPosition.x, formation.FormationPosition.y);

        // Can still fight from formation position
        if (combatState.CurrentState == CombatState.State.Attacking)
        {
            // Stay in formation but attack nearby enemies
            combatState.CurrentState = CombatState.State.Attacking;
        }
    }

    private static void HandleEngagedFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                      FormationComponent formation, Translation translation)
    {
        // Limited movement - can engage nearby enemies but stay roughly in position
        float maxEngageDistance = 1.5f; // How far from formation position they can move

        if (hasTarget.Type == HasTarget.TargetType.Entity)
        {
            float2 formationPos = formation.FormationPosition;
            float distanceFromFormation = math.distance(translation.Value.xy, formationPos);

            if (distanceFromFormation > maxEngageDistance)
            {
                // Too far - return to formation
                hasTarget.Type = HasTarget.TargetType.Position;
                hasTarget.TargetPosition = formationPos;
            }
        }
    }
}



///// <summary>
///// Holds a runtime mapping of FormationGroupEntity → UnitEntities
///// Built each frame by FormationManagerSystem
///// Read by systems like FormationCollisionSystem, FormationIntegritySystem, etc.
///// </summary>
//public struct FormationRuntimeCache : IComponentData
//{
//    public NativeMultiHashMap<Entity, Entity> GroupToUnits;
//}