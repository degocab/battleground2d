using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TargetReevaluationSystem))]
[UpdateBefore(typeof(ApplyDamageSystem))]
[BurstCompile]
public partial class CombatSystem : SystemBase
{
    private EntityQuery _combatQuery;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        // Create query for entities that can engage in combat
        _combatQuery = GetEntityQuery(
            ComponentType.ReadWrite<CombatState>(),
            ComponentType.ReadWrite<AttackComponent>(),
            ComponentType.ReadWrite<AttackCooldownComponent>(),
            ComponentType.ReadWrite<AnimationComponent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<HasTarget>()

            , ComponentType.Exclude<CommanderComponent>()
        );
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = Time.DeltaTime;
        float currentTime = (float)Time.ElapsedTime;

        // Get the ComponentDataFromEntity for translations
        ComponentDataFromEntity<Translation> translationFromEntity = GetComponentDataFromEntity<Translation>(true);

        var combatJob = new AutonomousCombatJob
        {
            DeltaTime = deltaTime,
            CurrentTime = currentTime,
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter(),
            TranslationFromEntity = translationFromEntity,
            EntityTypeHandle = GetEntityTypeHandle(),
            // Get type handles
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            AttackTypeHandle = GetComponentTypeHandle<AttackComponent>(false),
            CooldownTypeHandle = GetComponentTypeHandle<AttackCooldownComponent>(false),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(false),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            HasTargetTypeHandle = GetComponentTypeHandle<HasTarget>(true)
        };

        Dependency = combatJob.ScheduleParallel(_combatQuery, Dependency);
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private struct AutonomousCombatJob : IJobChunk
    {
        public float DeltaTime;
        public float CurrentTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

        public ComponentTypeHandle<CombatState> CombatStateTypeHandle;
        public ComponentTypeHandle<AttackComponent> AttackTypeHandle;
        public ComponentTypeHandle<AttackCooldownComponent> CooldownTypeHandle;
        public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<HasTarget> HasTargetTypeHandle;
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            // Get arrays for all components
            var combatStates = chunk.GetNativeArray(CombatStateTypeHandle);
            var attacks = chunk.GetNativeArray(AttackTypeHandle);
            var cooldowns = chunk.GetNativeArray(CooldownTypeHandle);
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var hasTargets = chunk.GetNativeArray(HasTargetTypeHandle);

            // Get entity array for adding components
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var combatState = combatStates[i];
                var attack = attacks[i];
                var cooldown = cooldowns[i];
                var animation = animations[i];
                var translation = translations[i];
                var hasTarget = hasTargets[i];
                var entity = entities[i];
                attack.isTakingDamage = false;
                // State machine logic
                switch (combatState.CurrentState)
                {
                    case CombatState.State.Idle:
                        if (hasTarget.TargetEntity != Entity.Null)
                        {
                            combatState.CurrentState = CombatState.State.SeekingTarget;
                            combatState.TargetEntity = hasTarget.TargetEntity;
                            combatState.StateTimer = 0f;
                        }
                        break;

                    case CombatState.State.SeekingTarget:
                        combatState.StateTimer += DeltaTime;

                        // Timeout for seeking (optional)
                        if (combatState.StateTimer > 10f) // 10 second timeout
                        {
                            combatState.CurrentState = CombatState.State.Idle;
                            combatState.TargetEntity = Entity.Null;
                            break;
                        }

                        if (combatState.TargetEntity == Entity.Null ||
                            !TranslationFromEntity.HasComponent(combatState.TargetEntity))
                        {
                            combatState.CurrentState = CombatState.State.Idle;
                            break;
                        }

                        float3 targetPos = TranslationFromEntity[combatState.TargetEntity].Value;
                        float distance = math.distance(translation.Value, targetPos);

                        if (distance <= attack.Range)
                        {
                            combatState.CurrentState = CombatState.State.Attacking;
                            combatState.StateTimer = 0f;
                        }
                        break;

                    case CombatState.State.Attacking:
                        combatState.StateTimer += DeltaTime;
                        // Check if target is still valid BEFORE trying to attack
                        if (combatState.TargetEntity == Entity.Null ||
                            !TranslationFromEntity.HasComponent(combatState.TargetEntity))
                        {
                            combatState.CurrentState = CombatState.State.SeekingTarget;
                            combatState.TargetEntity = Entity.Null;
                            break;
                        }
                        // Check if we can attack based on cooldown
                        if (CurrentTime - attack.LastAttackTime >= 1f / attack.AttackRate)
                        {
                            // Execute attack


                            // Apply damage to target
                            if (combatState.TargetEntity != Entity.Null
                                && TranslationFromEntity.HasComponent(combatState.TargetEntity)
                                )
                            {


                                attack.LastAttackTime = CurrentTime;
                                animation.AnimationType = EntitySpawner.AnimationType.Attack;
                                attack.isAttacking = true;



                                // Create a projectile or attack hitbox instead of immediate damage
                                ECB.AddComponent(chunkIndex, entity, new AttackEventComponent
                                {
                                    TargetEntity = combatState.TargetEntity,
                                    Damage = attack.Damage,
                                    SourceEntity = entity,
                                    AttackTime = CurrentTime,
                                    AttackDuration = 0.2f // Time for attack to land
                                });

                                //ECB.AddComponent(chunkIndex, combatState.TargetEntity, new DamageComponent
                                //{
                                //    Value = attack.Damage,
                                //    SourceEntity = entity
                                //});
                            }
                        }

                        // Check if target is still valid or if we timed out
                        if (combatState.TargetEntity == Entity.Null ||
                            !TranslationFromEntity.HasComponent(combatState.TargetEntity) ||
                            combatState.StateTimer > 30f) // 30 second combat timeout
                        {
                            combatState.CurrentState = CombatState.State.Idle;
                            combatState.TargetEntity = Entity.Null;
                        }
                        break;
                    case CombatState.State.TakingDamage:
                        attack.isTakingDamage = true;
                        break;
                }

                // Write back modified components
                combatStates[i] = combatState;
                attacks[i] = attack;
                animations[i] = animation;
            }
        }
    }
}




public struct AttackEventComponent : IComponentData
{
    public Entity TargetEntity;
    public float Damage;
    public Entity SourceEntity;
    public float AttackTime;
    public float AttackDuration;
}

[UpdateAfter(typeof(CombatSystem))]
[UpdateBefore(typeof(ApplyDamageSystem))]
public partial class AttackResolutionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _attackEventQuery;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _attackEventQuery = GetEntityQuery(ComponentType.ReadWrite<AttackEventComponent>());
    }

    protected override void OnUpdate()
    {
        float currentTime = (float)Time.ElapsedTime;
        var translationFromEntity = GetComponentDataFromEntity<Translation>(true);
        var damageComponentFromEntity = GetComponentDataFromEntity<DamageComponent>(true);
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var attackEventBufferFromEntity = GetBufferFromEntity<AttackEventBuffer>(false);

        Dependency = Entities
            .WithName("AttackResolutionJob")
            .WithReadOnly(translationFromEntity)
            //.WithReadOnly(damageComponentFromEntity)
            //.WithNativeDisableParallelForRestriction(attackEventBufferFromEntity)
            .WithAll<AttackEventComponent>()
            .ForEach((Entity entity, int entityInQueryIndex,
                    ref AttackComponent attack,
                     in AttackEventComponent attackEvent,
                     in Translation translation
                     ) =>
            {
                // Check if target still exists and is in range
                if (translationFromEntity.HasComponent(attackEvent.TargetEntity))
                {
                    float3 targetPos = translationFromEntity[attackEvent.TargetEntity].Value;
                    float distance = math.distance(translation.Value, targetPos);

                    if (distance <= attack.Range)
                    {
                        //if (!damageComponentFromEntity.HasComponent(attackEvent.TargetEntity))
                        //{
                        //ecb.AddComponent(entityInQueryIndex, attackEvent.TargetEntity, new DamageComponent
                        //{
                        //    Value = attackEvent.Damage,
                        //    SourceEntity = attackEvent.SourceEntity
                        //});
                        //}
                        //else
                        //{
                        //    // Optional: if DamageComponent exists, you could do something else here,
                        //    // like accumulate damage or skip.
                        //}


                        //ecb.AddComponent(entityInQueryIndex, attackEvent.TargetEntity, new DamageComponent
                        //{
                        //    Value = attackEvent.Damage,
                        //    SourceEntity = attackEvent.SourceEntity
                        //});






                        
                            // Buffer doesn't exist, add it first then append
                            ecb.AddBuffer<AttackEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                            ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new AttackEventBuffer
                            {
                                Attacker = attackEvent.SourceEntity,
                                Damage = attackEvent.Damage,
                                DamageType = 0
                            });
                        
                    }
                }
                
                ecb.RemoveComponent<AttackEventComponent>(entityInQueryIndex, entity);

            }).ScheduleParallel(Dependency);

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

public struct AttackEventBuffer : IBufferElementData
{
    public Entity Attacker;
    public float Damage;
    public int DamageType; // use int for enum-like simplicity
}