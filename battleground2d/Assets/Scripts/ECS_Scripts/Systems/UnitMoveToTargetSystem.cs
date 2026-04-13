using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(MovementSystem))]
[UpdateAfter(typeof(DeathSystem))]
[BurstCompile]
public partial class UnitMoveToTargetSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("UnitMoveToTargetJob")
            .WithAll<MovementGoal>()
            .WithNone<PlayerInputComponent>()
            .WithNone<RestrictMovementTag>()
            .ForEach((Entity entity, int entityInQueryIndex, ref Translation translation, /*ref MovementSpeedComponent movementSpeed,*/ ref MovementGoal movementGoal, ref CombatState combatState, ref DefenseComponent defenseComponent, ref MovementStatus movementStatus
            ) =>
            {
                // RESPECT COMBAT STATE - don't move if defending
                if (combatState.CurrentState == CombatState.State.Attacking || combatState.CurrentState == CombatState.State.TakingDamage ||
                    combatState.CurrentState == CombatState.State.Blocking)
                {
                    //movementSpeed.velocity = float3.zero;
                    movementStatus.CurrentStatus = MovementStatus.Status.Idle;
                    return; // Exit early - don't process movement
                }

                float reachThreshold = .4f;
                float2 targetPos = float2.zero;
                targetPos = movementGoal.Position;
                reachThreshold = 0.01f;// should reach position in formation
                float2 direction = math.normalize(targetPos - translation.Value.xy);
                //movementSpeed.velocity.xy = direction;
                movementStatus.CurrentStatus = MovementStatus.Status.Moving;

                if (math.distance(translation.Value.xy, targetPos) < reachThreshold)
                {
                    //movementSpeed.velocity = float3.zero;
                    //if (movementGoal != Entity.Null) //TODO: change movementGoal.Type == HasTarget.TargetType.Entity?
                    //{
                    //    if (combatState.CurrentState == CombatState.State.Idle ||
                    //        combatState.CurrentState == CombatState.State.SeekingTarget)
                    //    {
                    //        combatState.CurrentState = CombatState.State.Attacking;
                    //    }
                    //}
                    movementStatus.CurrentStatus = MovementStatus.Status.ReachedDestination;
                }

                //if (combatState.CurrentState == CombatState.State.SeekingTarget && math.distancesq(movementGoal.Position, translation.Value.xy) > 1f)
                //{
                //    movementSpeed.isRunnning = true;
                //}
                //else
                //{
                //    movementSpeed.isRunnning = false;
                //}

            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

public struct RestrictMovementTag : IComponentData { }

public struct MovementStatus : IComponentData
{
    public Status CurrentStatus;
    public enum Status
    {
        Idle, Moving, ReachedDestination
    }
}