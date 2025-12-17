using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(CombatSystem))]
[UpdateAfter(typeof(FindTargetSystem))]
public partial class TargetReevaluationSystem : SystemBase
{
    private float _nextReevaluationTime;
    private const float ReevaluationInterval = 2f;
    private EntityQuery _reevaluationQuery;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _reevaluationQuery = GetEntityQuery(
            ComponentType.ReadWrite<HasTarget>(),
            ComponentType.Exclude<CommanderComponent>()
        );

        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate(_reevaluationQuery);
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        float currentTime = (float)Time.ElapsedTime;
        if (currentTime < _nextReevaluationTime)
            return;

        _nextReevaluationTime = currentTime + ReevaluationInterval;

        var random = new Unity.Mathematics.Random((uint)(currentTime * 1000));
        var r = random.NextFloat();

        if (!(r < 0.8f))
            return;

        // Use the efficient job approach
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var reevaluateJob = new ReevaluateTargetsJob
        {
            ECB = ecb,
            RandomSeed = (uint)(currentTime * 1000),
            EntityTypeHandle = GetEntityTypeHandle(),
            HasTargetTypeHandle = GetComponentTypeHandle<HasTarget>(true)
        };

        Dependency = reevaluateJob.ScheduleParallel(_reevaluationQuery, Dependency);
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private struct ReevaluateTargetsJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint RandomSeed;

        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<HasTarget> HasTargetTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var random = new Unity.Mathematics.Random(RandomSeed + (uint)chunkIndex);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var hasTargets = chunk.GetNativeArray(HasTargetTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                // Only process entities with Entity targets (not Position targets)
                if (hasTargets[i].Type == HasTarget.TargetType.Entity)
                {
                    var r = random.NextFloat();
                    if (r < 0.8f)
                    {
                        int entityInQueryIndex = firstEntityIndex + i;
                        ECB.RemoveComponent<HasTarget>(entityInQueryIndex, entities[i]);
                        // Optionally add FindTargetTag if you want them to find new targets immediately
                        ECB.AddComponent<FindTargetTag>(entityInQueryIndex, entities[i]);
                    }
                }
            }
        }
    }
}