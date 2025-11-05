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
            if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
                return;

            var ecb = _ecbSystem.CreateCommandBuffer();
            var transformFromEntity = GetComponentDataFromEntity<LocalTransform>(true);

            foreach (var (hasTarget, entity) in SystemAPI.Query<RefRW<HasTarget>>().WithAll<HasTarget>().WithEntityAccess())
            {
                if (hasTarget.ValueRO.Type == HasTarget.TargetType.Entity &&
                    hasTarget.ValueRO.TargetEntity != Entity.Null &&
                    !transformFromEntity.HasComponent(hasTarget.ValueRO.TargetEntity))
                {
                    ecb.AddComponent<FindTargetCommandTag>(entity);
                    ecb.RemoveComponent<HasTarget>(entity);
                }
            }
            
            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
