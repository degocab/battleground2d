using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(AttackResolutionSystem))]
[UpdateAfter(typeof(TargetReevaluationSystem))]
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

        var combatJob = new CombatJob
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
    private struct CombatJob : IJobChunk
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
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

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
                //attack.isTakingDamage = false;
                attack.isDefending = false;

                // State machine logic
                switch (combatState.CurrentState)
                {
                    case CombatState.State.Idle:
                        HandleIdleState(ref combatState, hasTarget);
                        break;

                    case CombatState.State.SeekingTarget:
                        HandleSeekingState(ref combatState, attack, translation, hasTarget, DeltaTime, TranslationFromEntity);

                        break;

                    case CombatState.State.Attacking:
                        HandleAttackingState(ref combatState, ref attack, ref animation, entity, chunkIndex,
                                           translation, CurrentTime, DeltaTime, TranslationFromEntity, ECB);

                        break;
                    case CombatState.State.TakingDamage:
                        attack.isTakingDamage = true;
                        break;
                    case CombatState.State.Defending:
                        attack.isDefending = true;
                        Debug.Log("Defending");
                        // Reduce incoming damage by 50%
                        // Chance to parry and counter-attack
                        // Prevent movement while defending
                        break;
                }

                // Write back modified components
                combatStates[i] = combatState;
                attacks[i] = attack;
                animations[i] = animation;
            }
        }

        private void HandleAttackingState(ref CombatState combatState, ref AttackComponent attack, ref AnimationComponent animation, Entity entity, int chunkIndex, Translation translation, float currentTime, float deltaTime, ComponentDataFromEntity<Translation> translationFromEntity, EntityCommandBuffer.ParallelWriter eCB)
        {
            combatState.StateTimer += DeltaTime;
            // Check if target is still valid BEFORE trying to attack
            if (!CombatUtils.IsTargetValid(combatState.TargetEntity, TranslationFromEntity))
            {
                combatState.CurrentState = CombatState.State.SeekingTarget;
                combatState.TargetEntity = Entity.Null;
                return;
            }
            //Debug.Log($"currentTime - lastattacktime: {CurrentTime - attack.LastAttackTime}");
            //Debug.Log($"atk Rate({attack.AttackRate})|{CurrentTime - attack.LastAttackTime} >= {1f / attack.AttackRate}]");
            // Check if we can attack based on cooldown
            if (CurrentTime - attack.LastAttackTime >= 1f / attack.AttackRate)
            {
                
                // Apply damage to target
                if (CombatUtils.IsTargetValid(combatState.TargetEntity, TranslationFromEntity))
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
                }
            }
            else{
                combatState.CurrentState = CombatState.State.Defending;

            }

        }

        private void HandleSeekingState(ref CombatState combatState, AttackComponent attack, Translation translation, HasTarget hasTarget, float deltaTime, ComponentDataFromEntity<Translation> translationFromEntity)
        {
            combatState.StateTimer += DeltaTime;

            if (combatState.StateTimer > 10f || !CombatUtils.IsTargetValid(combatState.TargetEntity, translationFromEntity))
            {
                combatState.CurrentState = CombatState.State.Idle;
                combatState.TargetEntity = Entity.Null;
                return;
            }

            float3 targetPos = TranslationFromEntity[combatState.TargetEntity].Value;
            if (CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range))
            {
                combatState.CurrentState = CombatState.State.Attacking;
                combatState.StateTimer = 0f;
            }
        }

        private void HandleIdleState(ref CombatState combatState, HasTarget hasTarget)
        {
            if (hasTarget.TargetEntity != Entity.Null)
            {
                combatState.CurrentState = CombatState.State.SeekingTarget;
                combatState.TargetEntity = hasTarget.TargetEntity;
                combatState.StateTimer = 0f;
            }
        }
    }
}







