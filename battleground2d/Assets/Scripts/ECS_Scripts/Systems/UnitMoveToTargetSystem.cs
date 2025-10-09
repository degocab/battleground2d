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
        _ecbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float reachThreshold = 0.5f;

        // *** THE KEY CHANGE FOR ENTITIES 0.16.0 ***
        // Get a ComponentDataFromEntity for Translation. This is the equivalent of ComponentLookup.
        // The 'true' argument makes it read-only, which is necessary for Burst and parallel jobs.
        //ComponentDataFromEntity<Translation> translationFromEntity = GetComponentDataFromEntity<Translation>(true);

        Entities
            .WithName("UnitMoveToTargetJob")
            .WithAll<HasTarget>()
            .WithNone<PlayerInputComponent>()
            // *** ANOTHER KEY CHANGE: You must explicitly declare your read-only dependency ***
            //.WithReadOnly(translationFromEntity) // This is crucial for safety!
            .ForEach((Entity entity, int entityInQueryIndex, ref Translation translation, ref MovementSpeedComponent movementSpeed, ref HasTarget hasTarget, ref CombatState combatState, ref DefenseComponent defenseComponent, ref CommandData commandData) =>
            {
                float2 targetPos = float2.zero;
                bool targetIsValid = false;

                if (hasTarget.Type == HasTarget.TargetType.Entity)
                {
                    // Check if the target entity exists and has a Translation component
                    //if (translationFromEntity.HasComponent(hasTarget.TargetEntity))
                    if (hasTarget.TargetEntity != Entity.Null)
                    {
                        // Now we can safely get the target's position
                        //targetPos = translationFromEntity[hasTarget.TargetEntity].Value.xy;
                        targetPos = hasTarget.TargetPosition;
                        targetIsValid = true;
                    }
                    else
                    {
                        targetIsValid = false;
                    }
                }
                else // TargetType.Position
                {
                    targetPos = hasTarget.TargetPosition;
                    targetIsValid = true;
                }

                if (targetIsValid)
                {
                    float2 direction = math.normalize(targetPos - translation.Value.xy);
                    //direction.z = 0;
                    movementSpeed.velocity.xy = direction;

                    if (math.distance(translation.Value.xy, targetPos) < reachThreshold)
                    {
                        // Only destroy if it's an entity target and the entity is valid
                        //if (hasTarget.Type == HasTarget.TargetType.Entity && hasTarget.TargetEntity != Entity.Null)
                        //{
                        //    ecb.DestroyEntity(entityInQueryIndex, hasTarget.TargetEntity);
                        //}
                        //ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                        movementSpeed.velocity = float3.zero;
                        if (hasTarget.TargetEntity != Entity.Null )
                        {
                            //combatState.CurrentState = CombatState.State.Attacking;
                            // Only transition to Attacking from non-combat states
                            if (combatState.CurrentState == CombatState.State.Idle ||
                                combatState.CurrentState == CombatState.State.SeekingTarget)
                            {
                                combatState.CurrentState = CombatState.State.Attacking;
                                commandData.Command = CommandType.Attack;
                            }
                            commandData.TargetEntity = hasTarget.TargetEntity;
                            commandData.TargetPosition = targetPos;
                        }
                        //hasTarget.TargetPosition.x = targetPos.x + 10f;
                    }
                }
                else
                {
                    combatState.CurrentState = CombatState.State.Idle;
                    // Target is invalid (entity was destroyed). Cancel the command.
                    ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                    movementSpeed.velocity = float3.zero;
                    commandData.Command = CommandType.FindTarget;
                }

            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}