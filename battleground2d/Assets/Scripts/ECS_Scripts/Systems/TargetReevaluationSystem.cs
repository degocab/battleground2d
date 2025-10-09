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
        //Debug.Log($"{currentTime} < {_nextReevaluationTime}");
        if (currentTime < _nextReevaluationTime)
            return;

        _nextReevaluationTime = currentTime + ReevaluationInterval;



        // Use EntityCommandBuffer for structural changes instead of EntityManager
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var random = new Unity.Mathematics.Random((uint)(currentTime * 1000));

        var r = random.NextFloat();
        if (!(r < .8f))
            return;
        //Debug.Log(r);
        // Option 1: Using Entities.ForEach with proper Burst compatibility
        Entities
            .WithName("ReevaluateTargets")
            .WithAll<HasTarget>()
            .WithNone<CommanderComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, ref HasTarget hasTarget) =>
            {
                if (r < 0.8f && hasTarget.Type == HasTarget.TargetType.Entity)
                {
                    ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();
        Entities
            .WithName("ReevaluateTargets2")
            .WithAll<HasTarget>()
            .WithNone<CommanderComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, ref HasTarget hasTarget) =>
            {


                if (r < 0.8f && hasTarget.Type == HasTarget.TargetType.Entity)
                {
                    ecb.AddComponent<HasTarget>(entityInQueryIndex, entity);

                }
            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);

        // Option 2: Alternative approach using IJobChunk (more performant)


        //if (!(_reevaluationQuery.CalculateEntityCount() > 0))
        //    return;
        //var reevaluateJob = new ReevaluateTargetsJob
        //{
        //    ECB = ecb,
        //    RandomSeed = (uint)(currentTime * 1000),
        //    EntityTypeHandle = GetEntityTypeHandle()
        //};

        //Dependency = reevaluateJob.ScheduleParallel(_reevaluationQuery, Dependency);
        //_ecbSystem.AddJobHandleForProducer(Dependency);

    }

    // Option 2: Burst-compiled job version (recommended for performance)
    [BurstCompile]
    private struct ReevaluateTargetsJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint RandomSeed;

        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var random = new Unity.Mathematics.Random(RandomSeed + (uint)chunkIndex);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                //todo: fiund out how this affects target finding
                var r = random.NextFloat();
                if (r < 4f)
                {
                    ECB.AddComponent<FindTargetCommandTag>(firstEntityIndex + chunkIndex, entities[i]);
                }
            }
        }
    }
}

