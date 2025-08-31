using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AttackResolutionSystem))]
[UpdateBefore(typeof(DeathSystem))]
[BurstCompile]
public partial class ApplyDamageSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("ApplyDamage")
            .WithAll<DamageComponent>()
            .ForEach((Entity entity, int entityInQueryIndex,
            ref AttackComponent attackComponent,
                     ref HealthComponent health,
                     in DamageComponent damage) =>
            {
                health.Health -= damage.Value;

                attackComponent.isTakingDamage = true;
                ecb.RemoveComponent<DamageComponent>(entityInQueryIndex, entity);

                if (health.Health <= 0)
                {
                    health.isDying = true;
                    health.timeRemaining = health.deathAnimationDuration;
                }

            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}