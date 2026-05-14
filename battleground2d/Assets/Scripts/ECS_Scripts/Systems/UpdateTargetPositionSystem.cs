using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FindTargetSystem))]
[UpdateBefore(typeof(MovementSystem))]
public class UpdateTargetPositionSystem : SystemBase
{
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _query = GetEntityQuery(typeof(FormationSlotGoal), typeof(Translation), typeof(FindTargetTag));
    }

    protected override void OnUpdate()
    {
        var targetTranslationLookup = GetComponentDataFromEntity<Translation>(true);

        var job = new UpdateTargetPositionJob
        {
            TargetTranslationLookup = targetTranslationLookup,
            FormationSlotGoalTypeHandle = GetComponentTypeHandle<FormationSlotGoal>(false),
            EntityTypeHandle = GetEntityTypeHandle()
        };

        Dependency = job.ScheduleParallel(_query, Dependency);
    }

    [BurstCompile]
    private struct UpdateTargetPositionJob : IJobChunk
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> TargetTranslationLookup;
        public ComponentTypeHandle<FormationSlotGoal> FormationSlotGoalTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            //var entities = chunk.GetNativeArray(EntityTypeHandle);
            //var hasTargetComponents = chunk.GetNativeArray(CombatTargetTypeHandle);

            //for (int i = 0; i < chunk.Count; i++)
            //{
            //    var hasTarget = hasTargetComponents[i];

            //    if (hasTarget.Type == FormationSlotGoal.TargetType.Entity &&
            //        TargetTranslationLookup.HasComponent(hasTarget.TargetEntity))
            //    {
            //        Debug.Log("FormationSlotGoal.TargetPosition updated by UpdateTargetPositionJob");

            //        var targetTranslation = TargetTranslationLookup[hasTarget.TargetEntity];
            //        hasTarget.TargetPosition = targetTranslation.Value.xy;

            //        hasTargetComponents[i] = hasTarget; // Write updated struct back
            //    }
            //}
        }
    }
}