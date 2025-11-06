using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(MovementSystem))]
[UpdateAfter(typeof(DeathSystem))]
[BurstCompile]
public partial class UnitMoveToTargetSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        // *** THE KEY CHANGE FOR ENTITIES 0.16.0 ***
        // Get a ComponentDataFromEntity for Translation. This is the equivalent of ComponentLookup.
        // The 'true' argument makes it read-only, which is necessary for Burst and parallel jobs.
        //ComponentDataFromEntity<Translation> translationFromEntity = GetComponentLookup<Translation>(true);

        foreach (var (transform, movementSpeed, hasTarget, combatState, defenseComponent, commandData, entity) 
            in SystemAPI.Query<RefRW<LocalTransform>, RefRW<MovementSpeedComponent>, RefRW<HasTarget>, RefRW<CombatState>, RefRO<DefenseComponent>, RefRW<CommandData>>()
                .WithAll<HasTarget>()
                .WithNone<PlayerInputComponent>()
                .WithEntityAccess())
        {
            // RESPECT COMBAT STATE - don't move if defending
            if (combatState.ValueRO.CurrentState == CombatState.State.Attacking ||
                combatState.ValueRO.CurrentState == CombatState.State.Blocking)
            {
                movementSpeed.ValueRW.velocity = float3.zero;
                continue; // Exit early - don't process movement
            }

            //// Only move if in seeking or idle states
            //if (combatState.CurrentState != CombatState.State.SeekingTarget &&
            //    combatState.CurrentState != CombatState.State.Idle)
            //{
            //    movementSpeed.velocity = float3.zero;
            //    return;
            //}


            float reachThreshold = 0.275f;
            float2 targetPos = float2.zero;
            bool targetIsValid = false;
      
            if (hasTarget.ValueRO.Type == HasTarget.TargetType.Entity)
            {
                // Check if the target entity exists and has a Translation component
                //if (translationFromEntity.HasComponent(hasTarget.TargetEntity))
                if (hasTarget.ValueRO.TargetEntity != Entity.Null)
                {
                    // Now we can safely get the target's position
                    //targetPos = translationFromEntity[hasTarget.TargetEntity].Value.xy;
                    targetPos = hasTarget.ValueRO.TargetPosition;
                    targetIsValid = true;
                }
                else
                {
                    targetIsValid = false;
                }
            }
            else // TargetType.Position
            {
                targetPos = hasTarget.ValueRO.TargetPosition;
                targetIsValid = true;
                reachThreshold = 0.01f;// should reach position in formation
            }

            if (targetIsValid)
            {
                float2 direction = math.normalize(targetPos - transform.ValueRO.Position.xy);
                //direction.z = 0;
                movementSpeed.ValueRW.velocity.xy = direction;

               

                if (math.distance(transform.ValueRO.Position.xy, targetPos) < reachThreshold)
                {
                    // Only destroy if it's an entity target and the entity is valid
                    //if (hasTarget.Type == HasTarget.TargetType.Entity && hasTarget.TargetEntity != Entity.Null)
                    //{
                    //    ecb.DestroyEntity(entityInQueryIndex, hasTarget.TargetEntity);
                    //}
                    //ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                    movementSpeed.ValueRW.velocity = float3.zero;
                    if (hasTarget.ValueRO.TargetEntity != Entity.Null) //TODO: change hasTarget.Type == HasTarget.TargetType.Entity?
                    {
                        //combatState.CurrentState = CombatState.State.Attacking;
                        // Only transition to Attacking from non-combat states
                        if (combatState.ValueRO.CurrentState == CombatState.State.Idle ||
                            combatState.ValueRO.CurrentState == CombatState.State.SeekingTarget)
                        {
                            combatState.ValueRW.CurrentState = CombatState.State.Attacking;
                            commandData.ValueRW.Command = CommandType.Attack;
                        }
                        commandData.ValueRW.TargetEntity = hasTarget.ValueRO.TargetEntity;
                        commandData.ValueRW.TargetPosition = targetPos;
                    }
                    //hasTarget.TargetPosition.x = targetPos.x + 10f;
                }
                
            }
            else
            {
                combatState.ValueRW.CurrentState = CombatState.State.Idle;
                // Target is invalid (entity was destroyed). Cancel the command.
                // Note: ECB parallel writer needs an index, using entity index
                // For SystemAPI.Query, we can't easily get entityInQueryIndex, so we use a non-parallel ECB
                // or use a different approach. Let's switch to non-parallel for simplicity
                movementSpeed.ValueRW.velocity = float3.zero;
                commandData.ValueRW.Command = CommandType.FindTarget;
            }

        }

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}