    using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FindTargetSystem))]
    [UpdateAfter(typeof(ProcessCommandSystem))]
    public partial class TargetValidationSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
                return;

            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var translationFromEntity = GetComponentDataFromEntity<Translation>(true);

            Entities
                .WithName("ValidateTargets")
                .WithReadOnly(translationFromEntity)
                .WithAll<HasTarget>()
                .ForEach((Entity entity, int entityInQueryIndex, ref HasTarget hasTarget) =>
                {
                    if (hasTarget.Type == HasTarget.TargetType.Entity &&
                        hasTarget.TargetEntity != Entity.Null &&
                        !translationFromEntity.HasComponent(hasTarget.TargetEntity))
                    {
                        ecb.AddComponent<FindTargetCommandTag>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                    }
                }).ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
