using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(UnitMoveToTargetSystem))]
[UpdateAfter(typeof(DeathSystem))]
[BurstCompile]
public partial class MovementGoalResolverSystem : SystemBase
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
            .WithName("MovementGoalResolverJob")
            .WithAll<HasTarget>()
            .WithAll<CombatTarget>()
            .WithNone<PlayerInputComponent>()
            .WithNone<RestrictMovementTag>()
            .ForEach((Entity entity, int entityInQueryIndex, ref MovementGoal movementGoal, in Translation translation, in MovementSpeedComponent movementSpeed, in HasTarget hasTarget, in CombatState combatState, in CombatTarget combatTarget
            ) =>
            {

                //decide which movement goal to use for final movement calc in unitmovetostarget system
                //if there a combat target 
                //  go to target
                //if there is a hastarget
                //  go to this second
                if (combatTarget.isActive) //follow combat target first
                {
                    movementGoal.Position = combatTarget.TargetPosition;
                }
                else// if (hasTarget.isActive)// follow all other adfter for now
                {
                    movementGoal.Position = hasTarget.TargetPosition;
                }


            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

public struct MovementGoal : IComponentData
{
    public float2 Position;
}

