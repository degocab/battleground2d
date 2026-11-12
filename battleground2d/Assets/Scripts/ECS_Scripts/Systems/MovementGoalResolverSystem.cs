using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

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

        ComponentDataFromEntity<FormationGroupComponent> formationGroups =
GetComponentDataFromEntity<FormationGroupComponent>(true);

        var combatTargets = GetComponentDataFromEntity<CombatTarget>(true);

        var behaviors =
    GetComponentDataFromEntity<FormationBehaviorComponent>(true);
        Entities
            .WithName("MovementGoalResolverJob")
            .WithAll<FormationSlotGoal>()
            //.WithAll<CombatTarget>()
            .WithNone<PlayerInputComponent>()
            .WithNone<RestrictMovementTag>()
            .WithReadOnly(behaviors)
            .WithReadOnly(formationGroups)
            .WithReadOnly(combatTargets)
            .ForEach((Entity entity, ref MovementGoal movementGoal, in MovementSpeedComponent movementSpeed, in FormationSlotGoal formationSlotGoal, in CombatState combatState, in OrderData orderData, in Translation tran, in FormationComponent formation
            ) =>
            {
                bool allowPursuit = true;
                Entity groupEntity = Entity.Null;
                if (formation.FormationGroupEntity.HasValue)
                {
                    groupEntity = formation.FormationGroupEntity.Value;

                    if (behaviors.HasComponent(groupEntity))
                        allowPursuit = behaviors[groupEntity].AllowPursuit;
                }
                //decide which movement goal to use for final movement calc in unitmovetostarget system
                //if there a combat target 
                //  go to target
                //if there is a hastarget
                ////  go to this second
                if (orderData.CurrentOrder == OrderType.Defend || orderData.CurrentOrder == OrderType.MoveTo || orderData.CurrentOrder == OrderType.MoveDirectionalRange)
                {


                    movementGoal.Position = formationSlotGoal.TargetPosition;

                }
                else if (combatState.CurrentState == CombatState.State.Waiting)
                {
                    movementGoal.Position = tran.Value.xy; //stay in place until combat starts
                }
                else
                {
                    //if (combatState.CurrentState == CombatState.State.SeekingTarget) //follow combat target first
                    //{
                    if (formationGroups.HasComponent(groupEntity) && formationGroups[groupEntity].HoldFormation)
                    {
                        movementGoal.Position = formationSlotGoal.TargetPosition;
                    }
                    else if (allowPursuit &&
                    combatTargets.HasComponent(entity) &&
                             combatTargets[entity].TargetEntity != Entity.Null)
                    {
                        movementGoal.Position = combatTargets[entity].TargetPosition;
                    }
                    else
                    {
                        movementGoal.Position = tran.Value.xy;
                    }
                    //}
                    //else
                    //{

                    //}
                }

                //else// if (formationSlotGoal.isActive)// follow all other adfter for now
                //{
                //    movementGoal.Position = formationSlotGoal.TargetPosition;
                //}


            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

public struct MovementGoal : IComponentData
{
    public float2 Position;
}

