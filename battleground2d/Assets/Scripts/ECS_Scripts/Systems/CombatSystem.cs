using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
            ComponentType.ReadWrite<MovementSpeedComponent>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<HasTarget>(),
            ComponentType.Exclude<CommanderComponent>()
        );
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        //_random.NextUInt();
        // Reset cooldowns first
        foreach (var (attackComponent, cooldown, defenseComponent, healthComponent) in 
            SystemAPI.Query<RefRW<AttackComponent>, RefRW<AttackCooldownComponent>, RefRW<DefenseComponent>, RefRW<HealthComponent>>())
        {
            if (cooldown.ValueRO.attackCoolTimeRemaining > 0)
                cooldown.ValueRW.attackCoolTimeRemaining -= deltaTime;
            if (cooldown.ValueRO.takingDmgTimeRemaining > 0)
                cooldown.ValueRW.takingDmgTimeRemaining -= deltaTime;
            if (attackComponent.ValueRO.AttackRateRemaining > 0)
                attackComponent.ValueRW.AttackRateRemaining -= deltaTime;
            if (defenseComponent.ValueRO.BlockDuration > 0)
                defenseComponent.ValueRW.BlockDuration -= deltaTime;
            if (attackComponent.ValueRO.DefendCooldownRemaining > 0)
                attackComponent.ValueRW.DefendCooldownRemaining -= deltaTime;
            if (healthComponent.ValueRO.timeRemaining > 0)
                healthComponent.ValueRW.timeRemaining -= deltaTime;
        }

        // Get the ComponentDataFromEntity for transforms
        ComponentDataFromEntity<LocalTransform> transformFromEntity = GetComponentDataFromEntity<LocalTransform>(true);

        var combatJob = new CombatJob
        {
            DeltaTime = deltaTime,
            CurrentTime = currentTime,
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter(),
            TransformFromEntity = transformFromEntity,
            EntityTypeHandle = GetEntityTypeHandle(),
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            AttackTypeHandle = GetComponentTypeHandle<AttackComponent>(false),
            CooldownTypeHandle = GetComponentTypeHandle<AttackCooldownComponent>(false),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(false),
            MovementSpeedTypeHandle = GetComponentTypeHandle<MovementSpeedComponent>(false),
            TransformTypeHandle = GetComponentTypeHandle<LocalTransform>(true),
            HasTargetTypeHandle = GetComponentTypeHandle<HasTarget>(true),
            DefenseTypeHandle = GetComponentTypeHandle<DefenseComponent>(false),
            Random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000))
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
        [ReadOnly] public ComponentDataFromEntity<LocalTransform> TransformFromEntity;

        public ComponentTypeHandle<CombatState> CombatStateTypeHandle;
        public ComponentTypeHandle<AttackComponent> AttackTypeHandle;
        public ComponentTypeHandle<AttackCooldownComponent> CooldownTypeHandle;
        public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;
        public ComponentTypeHandle<DefenseComponent> DefenseTypeHandle;
        public ComponentTypeHandle<MovementSpeedComponent> MovementSpeedTypeHandle;
        [ReadOnly] public ComponentTypeHandle<LocalTransform> TransformTypeHandle;
        [ReadOnly] public ComponentTypeHandle<HasTarget> HasTargetTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public Unity.Mathematics.Random Random;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var combatStates = chunk.GetNativeArray(ref CombatStateTypeHandle);
            var attacks = chunk.GetNativeArray(ref AttackTypeHandle);
            var cooldowns = chunk.GetNativeArray(ref CooldownTypeHandle);
            var animations = chunk.GetNativeArray(ref AnimationTypeHandle);
            var transforms = chunk.GetNativeArray(ref TransformTypeHandle);
            var hasTargets = chunk.GetNativeArray(ref HasTargetTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var defenses = chunk.GetNativeArray(ref DefenseTypeHandle);
            var movementSpeeds = chunk.GetNativeArray(MovementSpeedTypeHandle);

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
                var movementSpeed = movementSpeeds[i];

                // State machine logic
                switch (combatState.CurrentState)
                {
                    case CombatState.State.Idle:
                    default:
                        HandleIdleState(ref combatState, ref animation, hasTarget);
                        break;
                    case CombatState.State.SeekingTarget:
                        HandleSeekingState(ref combatState, ref animation, ref attack, translation, hasTarget);
                        break;

                    case CombatState.State.Attacking:
                        HandleAttackingState(ref combatState, ref attack, ref cooldown, ref animation,
                                           entity, chunkIndex, translation, hasTarget, ref defense, ref movementSpeed);
                        break;

                    case CombatState.State.TakingDamage:
                        break;
                    case CombatState.State.Dying:
                        break;

                    case CombatState.State.Defending:
                        HandleDefendingState(ref combatState, ref attack, ref animation, translation, hasTarget, ref movementSpeed);
                        break;

                    case CombatState.State.Blocking:

                        if (defense.BlockDuration <= 0f)
                        {
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
                        }
                        else
                        {
                            combatState.CurrentState = CombatState.State.Blocking;
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
                                        Entity entity, int chunkIndex, Translation translation, HasTarget hasTarget, ref DefenseComponent defense, ref MovementSpeedComponent movementSpeed)
        {
            combatState.StateTimer += DeltaTime;

            //if (defense.IsBlocking)  // You'll need to pass defense as a parameter
            if (combatState.CurrentState == CombatState.State.Defending)  // You'll need to pass defense as a parameter
                return; // Stay in attacking state but don't process attack logic while blocking

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
                animation.finishAnimation = true;
                cooldown.attackCoolTimeRemaining = cooldown.attackCoolDownDuration;

                ECB.AddComponent(chunkIndex, entity, new AttackEventComponent
                {
                    TargetEntity = hasTarget.TargetEntity,
                    Damage = attack.Damage,
                    SourceEntity = entity,
                    AttackTime = CurrentTime,
                    AttackDuration = 0.2f
                    , AttackerDirection = animation.Direction
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
                if (ShouldDefend(ref attack, animation))
                {
                    // Choose to defend - become invulnerable but can't attack
                    combatState.CurrentState = CombatState.State.Defending;
                    //animation.AnimationType = EntitySpawner.AnimationType.Defend;
                    attack.DefendCooldownRemaining = attack.DefendDuration;
                }
                else
                {
                    // Choose NOT to defend - stay in attacking state but vulnerable
                    // This allows the enemy to hit you while you're waiting for attack cooldown
                    animation.AnimationType = EntitySpawner.AnimationType.Idle;
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
                                        ref AnimationComponent animation, Translation translation, HasTarget hasTarget, ref MovementSpeedComponent movementSpeed)
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
            }
            else
            {
                // Continue defending while on cooldown
                combatState.CurrentState = CombatState.State.Defending;
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

        private bool ShouldDefend(ref AttackComponent attack, AnimationComponent animation)
        {
            float baseDefendChance = animation.UnitType == EntitySpawner.UnitType.Default ? .1f : 1f;

            // Generate random value and check against defend chance
            float randomValue = Random.NextFloat(0f,1f);
            bool shouldDefend = randomValue < baseDefendChance;

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