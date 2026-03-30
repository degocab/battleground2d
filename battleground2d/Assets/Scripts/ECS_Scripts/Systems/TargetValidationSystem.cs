    using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FindTargetSystem))]
    [UpdateAfter(typeof(ProcessOrderSystem))]
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
            .WithAll<CombatTarget>()
            .ForEach((Entity entity, int entityInQueryIndex, ref CombatTarget combatTarget) =>
            {
                if (
                    combatTarget.TargetEntity != Entity.Null &&
                    !translationFromEntity.HasComponent(combatTarget.TargetEntity))
                {
                    ecb.AddComponent<FindTargetTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<CombatTarget>(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
    }
