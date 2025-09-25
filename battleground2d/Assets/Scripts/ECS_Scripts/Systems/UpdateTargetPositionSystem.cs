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
        _query = GetEntityQuery(typeof(HasTarget), typeof(Translation), typeof(FindTargetCommandTag));
    }

    protected override void OnUpdate()
    {
        var targetTranslationLookup = GetComponentDataFromEntity<Translation>(true);

        var job = new UpdateTargetPositionJob
        {
            TargetTranslationLookup = targetTranslationLookup,
            HasTargetTypeHandle = GetComponentTypeHandle<HasTarget>(false),
            EntityTypeHandle = GetEntityTypeHandle()
        };

        Dependency = job.ScheduleParallel(_query, Dependency);
    }

    [BurstCompile]
    private struct UpdateTargetPositionJob : IJobChunk
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> TargetTranslationLookup;
        public ComponentTypeHandle<HasTarget> HasTargetTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var hasTargetComponents = chunk.GetNativeArray(HasTargetTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var hasTarget = hasTargetComponents[i];

                if (hasTarget.Type == HasTarget.TargetType.Entity &&
                    TargetTranslationLookup.HasComponent(hasTarget.TargetEntity))
                {
                    var targetTranslation = TargetTranslationLookup[hasTarget.TargetEntity];
                    hasTarget.TargetPosition = targetTranslation.Value.xy;

                    hasTargetComponents[i] = hasTarget; // Write updated struct back
                }
            }
        }
    }
}