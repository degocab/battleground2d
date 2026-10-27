using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ApplyDamageSystem))]
[UpdateBefore(typeof(UnitMoveToTargetSystem))]
public partial class DeathSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _ecbSystem;
    private double _nextDeathSummary;
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
                        .WithAll<DeadTagComponent>() // Only process entities that are marked as dead
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


                    //freeze the final frame after animation is done
                    if (animation.AnimationType == EntitySpawner.AnimationType.Die &&
                        animation.CurrentFrame == animation.FrameCount - 1)
                    {
    //                    Debug.Log(
    //$"CORPSE {entity}: frame={animation.CurrentFrame}, " +
    //$"direction={animation.animationWidthOffset}, " +
    //$"row={animation.animationHeightOffset}, uv={animation.uv}");
                        animation.isFrozen = true;
                    }

                    //if (health.timeRemaining <= 0) //wait for death animation to finaish?
                    //{
                    //    ecb.DestroyEntity(entityInQueryIndex, entity);
                    //}
                }

            }).WithoutBurst().Run();//.ScheduleParallel();





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
        //            ecb.RemoveComponent<FormationSlotGoal>(entityInQueryIndex, entity);
        //        }

        //    }).ScheduleParallel();


        _ecbSystem.AddJobHandleForProducer(Dependency);
    }


}
public struct DeadTagComponent : IComponentData
{
    // This is a tag component, no data needed
}