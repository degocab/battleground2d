// Calculates all formation positions once per frame
using System.Collections.Generic;
using System.Linq;
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

    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        _unitQuery = GetEntityQuery(typeof(FormationComponent));
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        //get unit entites and formation components
        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob);
        var formationComponents = _unitQuery.ToComponentDataArray<FormationComponent>(Allocator.TempJob);

        //map group to units

        var groupToUnitIndices = new NativeMultiHashMap<Entity, int>(unitEntities.Length, Allocator.TempJob);
        var processedGroups = new NativeHashSet<Entity>(256, Allocator.TempJob);

        //build map group -> unit indices
        for (int i = 0; i < formationComponents.Length; i++)
        {
            var groupEnity = formationComponents[i].FormationGroupEntity.GetValueOrDefault(Entity.Null);
            if (groupEnity != Entity.Null)
            {
                groupToUnitIndices.Add(groupEnity, i);
            }
        }

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