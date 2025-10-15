using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ApplyDamageSystem))]
[UpdateBefore(typeof(UnitMoveToTargetSystem))]
public partial class DeathSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = Time.DeltaTime;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("ProcessDeath")
            .ForEach((Entity entity, int entityInQueryIndex,
                     ref HealthComponent health,
                     ref CombatState combatState,
                     ref AnimationComponent animation) =>
            {
                if (combatState.CurrentState == CombatState.State.Dying)
                {

                    // Add DeadTagComponent if entity doesn't have it yet
                    //if (!HasComponent<DeadTagComponent>(entity))
                    //{
                    //}

                    if (animation.isFrozen)
                    {
                        ecb.RemoveComponent<CollidableTag>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<ECS_CircleCollider2DAuthoring>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<CollisionEvent2D>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<CollisionEvent2D>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<CommandData>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<AttackComponent>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<MovementSpeedComponent>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<TargetComponent>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<Unit>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<QuadrantEntity>(entityInQueryIndex, entity);
                    }

                    //freeze the final frame after animation is done
                    if (animation.FrameCount - 1 == animation.CurrentFrame)
                        animation.isFrozen = true;

                    if (health.timeRemaining <= 0) //wait for death animation to finaish?
                    {
                        ecb.DestroyEntity(entityInQueryIndex, entity);
                    }
                }

            }).ScheduleParallel();

        //clean up frozen animations

        //Entities
        //    .WithName("ProcessDeathComponents")
        //    .WithAll<DeadTagComponent>()
        //    .ForEach((Entity entity, int entityInQueryIndex,
        //             ref HealthComponent health,
        //             ref DeadTagComponent dead,
        //             ref AnimationComponent animation) =>
        //    {

        //        // Add DeadTagComponent if entity doesn't have it yet
        //        //if (!HasComponent<DeadTagComponent>(entity))
        //        //{
        //        //    ecb.AddComponent<DeadTagComponent>(entityInQueryIndex, entity);
        //        //}

        //        //freeze the final frame after animation is done
        //        //if (animation.FrameCount - 1 == animation.CurrentFrame)
        //        //    animation.isFrozen = true;

        //        //if (health.timeRemaining <= 0) //wait for death animation to finaish?
        //        //{
        //        //    ecb.DestroyEntity(entityInQueryIndex, entity);
        //        //}
        //        if (animation.isFrozen)
        //        {
        //            ecb.RemoveComponent<CollidableTag>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<ECS_CircleCollider2DAuthoring>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<CollisionEvent2D>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<CollisionEvent2D>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<CommandData>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<AttackComponent>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<MovementSpeedComponent>(entityInQueryIndex, entity);
        //            ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
        //        }

        //    }).ScheduleParallel();


        _ecbSystem.AddJobHandleForProducer(Dependency);
    }


}
public struct DeadTagComponent : IComponentData
{
    // This is a tag component, no data needed
}