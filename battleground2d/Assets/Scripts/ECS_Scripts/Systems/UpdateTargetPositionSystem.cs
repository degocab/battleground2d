using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FindTargetSystem))]
[UpdateBefore(typeof(MovementSystem))]
public class UpdateTargetPositionSystem : SystemBase
{
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _query = GetEntityQuery(typeof(HasTarget), typeof(LocalTransform), typeof(FindTargetCommandTag));
    }

    protected override void OnUpdate()
    {
        var targetTransformLookup = GetComponentLookup<LocalTransform>(true);

        var job = new UpdateTargetPositionJob
        {
            TargetTransformLookup = targetTransformLookup,
            HasTargetTypeHandle = GetComponentTypeHandle<HasTarget>(false),
            EntityTypeHandle = GetEntityTypeHandle()
        };

        Dependency = job.ScheduleParallel(_query, Dependency);
    }

    [BurstCompile]
    private struct UpdateTargetPositionJob : IJobChunk
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TargetTransformLookup;
        public ComponentTypeHandle<HasTarget> HasTargetTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            //var entities = chunk.GetNativeArray(EntityTypeHandle);
            //var hasTargetComponents = chunk.GetNativeArray(ref HasTargetTypeHandle);

            //for (int i = 0; i < chunk.Count; i++)
            //{
            //    var hasTarget = hasTargetComponents[i];

            //    if (hasTarget.Type == HasTarget.TargetType.Entity &&
            //        TargetTransformLookup.HasComponent(hasTarget.TargetEntity))
            //    {
            //        var targetTransform = TargetTransformLookup[hasTarget.TargetEntity];
            //        hasTarget.TargetPosition = targetTransform.Position.xy;

            //        hasTargetComponents[i] = hasTarget; // Write updated struct back
            //    }
            //}
        }
    }
}