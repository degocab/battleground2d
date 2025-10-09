using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(AttackResolutionSystem))]
[UpdateAfter(typeof(TargetReevaluationSystem))]
[BurstCompile]
public partial class CombatSystem : SystemBase
{
    private EntityQuery _combatQuery;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private Unity.Mathematics.Random _random;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        // Create query for entities that can engage in combat
        _combatQuery = GetEntityQuery(
            ComponentType.ReadWrite<CombatState>(),
            ComponentType.ReadWrite<AttackComponent>(),
            ComponentType.ReadWrite<AttackCooldownComponent>(),
            ComponentType.ReadWrite<AnimationComponent>(),
            ComponentType.ReadWrite<DefenseComponent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<HasTarget>(),
            ComponentType.Exclude<CommanderComponent>()
        );
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        float deltaTime = Time.DeltaTime;
        float currentTime = (float)Time.ElapsedTime;
        //_random.NextUInt();
        // Reset cooldowns first
        Entities
            .WithName("ResetCoolDowns")
            .ForEach((
                ref AttackComponent attackComponent,
                ref AttackCooldownComponent cooldown,
                ref DefenseComponent defenseComponent) =>
            {
                if (cooldown.attackCoolTimeRemaining > 0)
                    cooldown.attackCoolTimeRemaining -= deltaTime;
                if (cooldown.takingDmgTimeRemaining > 0)
                    cooldown.takingDmgTimeRemaining -= deltaTime;
                if (attackComponent.AttackRateRemaining > 0)
                    attackComponent.AttackRateRemaining -= deltaTime;
                if (defenseComponent.BlockDuration > 0)
                    defenseComponent.BlockDuration -= deltaTime;
                if (attackComponent.DefendCooldownRemaining > 0)
                    attackComponent.DefendCooldownRemaining -= deltaTime;
            }).ScheduleParallel();

        // Get the ComponentDataFromEntity for translations
        ComponentDataFromEntity<Translation> translationFromEntity = GetComponentDataFromEntity<Translation>(true);

        var combatJob = new CombatJob
        {
            DeltaTime = deltaTime,
            CurrentTime = currentTime,
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter(),
            TranslationFromEntity = translationFromEntity,
            EntityTypeHandle = GetEntityTypeHandle(),
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            AttackTypeHandle = GetComponentTypeHandle<AttackComponent>(false),
            CooldownTypeHandle = GetComponentTypeHandle<AttackCooldownComponent>(false),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(false),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            HasTargetTypeHandle = GetComponentTypeHandle<HasTarget>(true),
            DefenseTypeHandle = GetComponentTypeHandle<DefenseComponent>(false),
            Random = new Unity.Mathematics.Random((uint)(Time.ElapsedTime * 1000))
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
        public ComponentTypeHandle<DefenseComponent> DefenseTypeHandle;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<HasTarget> HasTargetTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public Unity.Mathematics.Random Random;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var combatStates = chunk.GetNativeArray(CombatStateTypeHandle);
            var attacks = chunk.GetNativeArray(AttackTypeHandle);
            var cooldowns = chunk.GetNativeArray(CooldownTypeHandle);
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var hasTargets = chunk.GetNativeArray(HasTargetTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var defenses = chunk.GetNativeArray(DefenseTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var combatState = combatStates[i];
                var attack = attacks[i];
                var cooldown = cooldowns[i];
                var animation = animations[i];
                var translation = translations[i];
                var hasTarget = hasTargets[i];
                var entity = entities[i];
                var defense = defenses[i];

                // Reset attack flags at start of each frame
                attack.isAttacking = false;
                attack.isDefending = false;
                //attack.isTakingDamage = false;
                defense.IsBlocking = false;

                // State machine logic
                switch (combatState.CurrentState)
                {
                    case CombatState.State.Idle:
                    default:
                        //Debug.Log($"AI is idle");
                        HandleIdleState(ref combatState, ref animation, hasTarget);
                        break;

                    case CombatState.State.SeekingTarget:
                        //Debug.Log($"AI is SeekingTarget");

                        HandleSeekingState(ref combatState, ref animation, ref attack, translation, hasTarget);
                        break;

                    case CombatState.State.Attacking:
                        //Debug.Log($"AI is attacking");
                        HandleAttackingState(ref combatState, ref attack, ref cooldown, ref animation,
                                           entity, chunkIndex, translation, hasTarget, ref defense);
                        break;

                    case CombatState.State.TakingDamage:
                        //Debug.Log($"AI is TakingDamage");
                        //attack.isTakingDamage = true;
                        //animation.AnimationType = EntitySpawner.AnimationType.TakeDamage;
                        break;

                    case CombatState.State.Defending:
                        //Debug.Log($"AI is Defending");

                        HandleDefendingState(ref combatState, ref attack, ref animation, translation, hasTarget);
                        break;

                    case CombatState.State.Blocking:
                        //Debug.Log($"AI is Blocking");

                        if (defense.BlockDuration <= 0f)
                        {
                            defense.IsBlocking = false;

                            // Transition back to appropriate state after blocking ends
                            if (hasTarget.TargetEntity != Entity.Null &&
                                CombatUtils.IsTargetValid(hasTarget.TargetEntity, TranslationFromEntity))
                            {
                                // Still have valid target - go back to attacking
                                combatState.CurrentState = CombatState.State.Attacking;
                            }
                            else
                            {
                                // No valid target - go to idle
                                combatState.CurrentState = CombatState.State.Idle;
                            }
                            //Debug.Log("ai should NOOOTTTT be blocking");
                        }
                        else
                        {
                            combatState.CurrentState = CombatState.State.Blocking;
                            //Debug.Log("ai should still be blocking");
                            // Optionally, you could check if we should counter-attack or do something while blocking
                        }
                        break;
                }

                // Write back modified components
                combatStates[i] = combatState;
                attacks[i] = attack;
                animations[i] = animation;
                cooldowns[i] = cooldown;
                defenses[i] = defense;
            }
        }

        private void HandleAttackingState(ref CombatState combatState, ref AttackComponent attack,
                                        ref AttackCooldownComponent cooldown, ref AnimationComponent animation,
                                        Entity entity, int chunkIndex, Translation translation, HasTarget hasTarget, ref DefenseComponent defense)
        {
            combatState.StateTimer += DeltaTime;

            if (defense.IsBlocking)  // You'll need to pass defense as a parameter
            {
                return; // Stay in attacking state but don't process attack logic while blocking
            }

            // Check if target is still valid
            if (!CombatUtils.IsTargetValid(hasTarget.TargetEntity, TranslationFromEntity))
            {
                TransitionToSeeking(ref combatState, ref animation);
                return;
            }

            float3 targetPos = TranslationFromEntity[hasTarget.TargetEntity].Value;
            bool inRange = CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range);

            // Check cooldown states
            bool animationReady = cooldown.attackCoolTimeRemaining <= 0f;
            bool attackReady = attack.AttackRateRemaining <= 0f;
            bool canAttack = animationReady && attackReady;
            bool waitingOnAttackRateCD = !attackReady && animationReady;

            if (canAttack && inRange)
            {
                // Perform attack
                attack.AttackRateRemaining = attack.AttackRate;
                attack.isAttacking = true;
                attack.isDefending = false;
                animation.AnimationType = EntitySpawner.AnimationType.Attack;
                animation.finishAnimation = true;
                cooldown.attackCoolTimeRemaining = cooldown.attackCoolDownDuration;

                ECB.AddComponent(chunkIndex, entity, new AttackEventComponent
                {
                    TargetEntity = hasTarget.TargetEntity,
                    Damage = attack.Damage,
                    SourceEntity = entity,
                    AttackTime = CurrentTime,
                    AttackDuration = 0.2f
                });
            }
            else if (!inRange)
            {
                // Target is out of range - go seek it
                TransitionToSeeking(ref combatState, ref animation);
            }
            else if (waitingOnAttackRateCD && inRange)
            {
                // On attack cooldown but still in range - decide whether to defend or stay vulnerable
                if (ShouldDefend(ref attack))
                {
                    // Choose to defend - become invulnerable but can't attack
                    combatState.CurrentState = CombatState.State.Defending;
                    attack.isDefending = true;
                    animation.AnimationType = EntitySpawner.AnimationType.Defend;
                    attack.DefendCooldownRemaining = attack.DefendDuration;
                }
                else
                {
                    // Choose NOT to defend - stay in attacking state but vulnerable
                    // This allows the enemy to hit you while you're waiting for attack cooldown
                    animation.AnimationType = EntitySpawner.AnimationType.Idle;
                    attack.isDefending = false;
                }
            }
            else
            {
                // Waiting for animation cooldown but can still attack soon
                animation.AnimationType = EntitySpawner.AnimationType.Idle;
            }

            // Timeout safety
            if (combatState.StateTimer > 30f)
            {
                TransitionToSeeking(ref combatState, ref animation);
            }
        }

        private void HandleSeekingState(ref CombatState combatState, ref AnimationComponent animation,
                                      ref AttackComponent attack, Translation translation, HasTarget hasTarget)
        {
            combatState.StateTimer += DeltaTime;

            if (!CombatUtils.IsTargetValid(hasTarget.TargetEntity, TranslationFromEntity))
            {
                TransitionToIdle(ref combatState, ref animation);
                return;
            }

            float3 targetPos = TranslationFromEntity[hasTarget.TargetEntity].Value;
            bool inRange = CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range);

            if (inRange)
            {
                // Target is in range - start attacking
                combatState.CurrentState = CombatState.State.Attacking;
                combatState.StateTimer = 0f;
                animation.AnimationType = EntitySpawner.AnimationType.Idle; // Will be set to attack if can attack immediately
            }
            else
            {
                // Still seeking - walk toward target
                animation.AnimationType = EntitySpawner.AnimationType.Walk;
            }

            // Timeout safety
            if (combatState.StateTimer > 10f)
            {
                TransitionToIdle(ref combatState, ref animation);
            }
        }

        private void HandleDefendingState(ref CombatState combatState, ref AttackComponent attack,
                                        ref AnimationComponent animation, Translation translation, HasTarget hasTarget)
        {
            if (!CombatUtils.IsTargetValid(hasTarget.TargetEntity, TranslationFromEntity))
            {
                TransitionToSeeking(ref combatState, ref animation);
                return;
            }

            float3 targetPos = TranslationFromEntity[hasTarget.TargetEntity].Value;
            bool inRange = CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range);

            if (!inRange)
            {
                // Target moved out of range - seek it
                TransitionToSeeking(ref combatState, ref animation);
            }
            else if (attack.AttackRateRemaining <= 0f)
            {
                // Attack cooldown finished - go back to attacking
                combatState.CurrentState = CombatState.State.Attacking;
                animation.AnimationType = EntitySpawner.AnimationType.Idle;
                attack.isDefending = false;
            }
            else
            {
                // Continue defending while on cooldown
                attack.isDefending = true;
                animation.AnimationType = EntitySpawner.AnimationType.Defend;
            }
        }

        private void HandleIdleState(ref CombatState combatState, ref AnimationComponent animation, HasTarget hasTarget)
        {
            if (hasTarget.TargetEntity != Entity.Null &&
                CombatUtils.IsTargetValid(hasTarget.TargetEntity, TranslationFromEntity))
            {
                combatState.CurrentState = CombatState.State.SeekingTarget;
                combatState.TargetEntity = hasTarget.TargetEntity;
                combatState.StateTimer = 0f;
                animation.AnimationType = EntitySpawner.AnimationType.Walk;
            }
            else
            {
                animation.AnimationType = EntitySpawner.AnimationType.Idle;
            }
        }

        private bool ShouldDefend(ref AttackComponent attack)
        {
            // Base defend chance (30% chance to defend)
            float baseDefendChance = 0.92f;

            // Adjust based on health or other factors if needed
            // if (health.IsLow) baseDefendChance += 0.2f;

            // Generate random value and check against defend chance
            float randomValue = Random.NextFloat();
            bool shouldDefend = randomValue < baseDefendChance;

            // Debug log to see defend decisions (remove in final version)
            // if (shouldDefend) Debug.Log($"AI chose to defend! Random: {randomValue:F2} < {baseDefendChance:F2}");
            // else Debug.Log($"AI chose to stay vulnerable! Random: {randomValue:F2} >= {baseDefendChance:F2}");

            return shouldDefend;
        }

        private void TransitionToSeeking(ref CombatState combatState, ref AnimationComponent animation)
        {
            combatState.CurrentState = CombatState.State.SeekingTarget;
            combatState.StateTimer = 0f;
            animation.AnimationType = EntitySpawner.AnimationType.Walk;
        }

        private void TransitionToIdle(ref CombatState combatState, ref AnimationComponent animation)
        {
            combatState.CurrentState = CombatState.State.Idle;
            combatState.TargetEntity = Entity.Null;
            combatState.StateTimer = 0f;
            animation.AnimationType = EntitySpawner.AnimationType.Idle;
        }
    }
}