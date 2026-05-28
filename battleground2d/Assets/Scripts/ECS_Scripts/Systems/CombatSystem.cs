using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
            ComponentType.ReadOnly<AnimationComponent>(),
            ComponentType.ReadWrite<DefenseComponent>(),
            //ComponentType.ReadWrite<MovementSpeedComponent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<CombatTarget>(),
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
                ref DefenseComponent defenseComponent,
                ref HealthComponent healthComponent) =>
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
                if (healthComponent.timeRemaining > 0)
                    healthComponent.timeRemaining -= deltaTime;
                if (attackComponent.timeRemaingingToSetAsWaiting > 0)
                    attackComponent.timeRemaingingToSetAsWaiting -= deltaTime;
            }).ScheduleParallel();

        //update defending units
        Entities
            .WithName("UpdateAnyDefendingUnits")
            .WithNone<DeadTagComponent>()
            .ForEach((ref Entity entity, ref CombatState combatState, in OrderData order) => 
            {
                if (order.CurrentOrder == OrderType.Defend)
                {
                    combatState.WantsToDefend = true; // <-- add this bool to CombatState
                }
                else
                {
                    combatState.WantsToDefend = false;
                }
            }).Run();


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
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            //MovementSpeedTypeHandle = GetComponentTypeHandle<MovementSpeedComponent>(false),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            CombatTargetTypeHandle = GetComponentTypeHandle<CombatTarget>(true),
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
        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;
        public ComponentTypeHandle<DefenseComponent> DefenseTypeHandle;
        //public ComponentTypeHandle<MovementSpeedComponent> MovementSpeedTypeHandle;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<CombatTarget> CombatTargetTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public Unity.Mathematics.Random Random;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var combatStates = chunk.GetNativeArray(CombatStateTypeHandle);
            var attacks = chunk.GetNativeArray(AttackTypeHandle);
            var cooldowns = chunk.GetNativeArray(CooldownTypeHandle);
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var combatTargets = chunk.GetNativeArray(CombatTargetTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var defenses = chunk.GetNativeArray(DefenseTypeHandle);
            //var movementSpeeds = chunk.GetNativeArray(MovementSpeedTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var combatState = combatStates[i];
                var attack = attacks[i];
                var cooldown = cooldowns[i];
                var animation = animations[i];
                var translation = translations[i];
                var combatTarget = combatTargets[i];
                var entity = entities[i];
                var defense = defenses[i];
                //var movementSpeed = movementSpeeds[i];

                // State machine logic
                switch (combatState.CurrentState)
                {
                    case CombatState.State.Idle:
                    default:
                        HandleIdleState(ref combatState,  combatTarget);
                        break;
                    case CombatState.State.SeekingTarget:
                        HandleSeekingState(ref combatState,  ref attack, translation, combatTarget);
                        break;

                    case CombatState.State.Attacking:
                        HandleAttackingState(ref combatState, ref attack, ref cooldown, 
                                           entity, chunkIndex, translation, combatTarget, ref defense, animation);
                        break;

                    case CombatState.State.TakingDamage:
                        break;
                    case CombatState.State.Dying:
                        break;

                    case CombatState.State.Defending:
                        HandleDefendingState(ref combatState, ref attack,  translation, combatTarget, DeltaTime);
                        break;

                    case CombatState.State.Blocking:

                        if (defense.BlockDuration <= 0f)
                        {
                            // Transition back to appropriate state after blocking ends
                            if (combatTarget.TargetEntity != Entity.Null &&
                                CombatUtils.IsTargetValid(combatTarget.TargetEntity, TranslationFromEntity))
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
                    case CombatState.State.Waiting:
                        if (attack.timeRemaingingToSetAsWaiting < 0f)
                        {
                            attack.timeRemaingingToSetAsWaiting = SeekingTimeout;
                        }
                        else
                        {

                            // Timeout expired - transition to seeking to try to find the target again
                            if (attack.timeRemaingingToSetAsWaiting < 0.5f && attack.timeRemaingingToSetAsWaiting > 0f)
                                TransitionToSeeking(ref combatState);
                   
                        }
                        break;
                }


                //store previous state so we can dictate what happens on state changes from previous

                combatState.PreviousState = combatState.CurrentState;

                // Write back modified components
                combatStates[i] = combatState;
                attacks[i] = attack;
              
                cooldowns[i] = cooldown;
                defenses[i] = defense;
            }
        }

        private void HandleAttackingState(ref CombatState combatState, ref AttackComponent attack,
                                        ref AttackCooldownComponent cooldown, 
                                        Entity entity, int chunkIndex, Translation translation, CombatTarget combatTarget, ref DefenseComponent defense, AnimationComponent animation)
        {
            combatState.StateTimer += DeltaTime;

            // Check if target is still valid
            if (!CombatUtils.IsTargetValid(combatTarget.TargetEntity, TranslationFromEntity))
            {
                TransitionToSeeking(ref combatState);
                return;
            }

            float3 targetPos = TranslationFromEntity[combatTarget.TargetEntity].Value;
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
                //animation.finishAnimation = true;
                cooldown.attackCoolTimeRemaining = cooldown.attackCoolDownDuration;

                ECB.AddComponent(chunkIndex, entity, new AttackEventComponent
                {
                    TargetEntity = combatTarget.TargetEntity,
                    Damage = attack.Damage,
                    SourceEntity = entity,
                    AttackTime = CurrentTime,
                    AttackDuration = 0.2f
                    ,
                    AttackerDirection = animation.Direction
                });
            }
            else if (!inRange)
            {
                // Target is out of range - go seek it
                TransitionToSeeking(ref combatState);
            }
            else if (waitingOnAttackRateCD && inRange)
            {
                // On attack cooldown but still in range - decide whether to defend or stay vulnerable
                if (ShouldDefend(ref attack, animation))
                {
                    // Choose to defend - become invulnerable but can't attack
                    //combatState.CurrentState = CombatState.State.Defending;
                    ////attack.AnimationType = EntitySpawner.AnimationType.Defend;
                    //attack.DefendCooldownRemaining = attack.DefendDuration;
                    TransitionToDefending(ref combatState, ref attack);
                }
                else
                {
                    // Choose NOT to defend - stay in attacking state but vulnerable
                    // This allows the enemy to hit you while you're waiting for attack cooldown
                    //animation.AnimationType = EntitySpawner.AnimationType.Idle;
                }
            }
            else
            {
                // Waiting for attack cooldown but can still attack soon
                //animation.AnimationType = EntitySpawner.AnimationType.Idle;
            }

            // Timeout safety
            if (combatState.StateTimer > 30f)
            {
                TransitionToSeeking(ref combatState);
            }
        }

        private void TransitionToDefending(ref CombatState combatState, ref AttackComponent attack)
        {
            combatState.CurrentState = CombatState.State.Defending;
            combatState.StateTimer = 0f;
            attack.DefendCooldownRemaining = 5f;// attack.DefendDuration;
        }

        const float SeekingTimeout = 3f;
        const float WaitingTimerExtraDistance = 2f; // tune this
        private void HandleSeekingState(
            ref CombatState combatState,

            ref AttackComponent attack,
            Translation translation,
            CombatTarget combatTarget)
        {
            combatState.StateTimer += DeltaTime;

            if (!CombatUtils.IsTargetValid(combatTarget.TargetEntity, TranslationFromEntity))
            {
                ResetSeekingTimers(ref attack);
                TransitionToIdle(ref combatState);
                return;
            }

            float3 targetPos = TranslationFromEntity[combatTarget.TargetEntity].Value;

            if (CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range))
            {
                TransitionToAttacking(ref combatState,  ref attack);
                return;
            }

            if (ShouldWaitBehindFrontLine(ref attack, translation.Value, targetPos))
            {
                TransitionToWaiting(ref combatState, ref attack);
                return;
            }

            //animation.AnimationType = EntitySpawner.AnimationType.Walk;
        }

        private void HandleDefendingState(
            ref CombatState combatState,
            ref AttackComponent attack,
     
            Translation translation,
            CombatTarget combatTarget,
            //ref MovementSpeedComponent movementSpeed,
            float deltaTime)
        {
            if (!CombatUtils.IsTargetValid(combatTarget.TargetEntity, TranslationFromEntity))
            {
                TransitionToSeeking(ref combatState);
                return;
            }

            float3 targetPos = TranslationFromEntity[combatTarget.TargetEntity].Value;
            bool inRange = CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range);

            if (!inRange)
            {
                TransitionToSeeking(ref combatState);
                return;
            }

            //// Start defend timer ONCE when we enter Defending
            //if (combatState.PreviousState != CombatState.State.Defending)
            //{
            //    combatState.DefendCooldownTimer = 5f; // how long you want to “turtle up”
            //}

            //// Tick it down
            //combatState.DefendCooldownTimer = math.max(0f, combatState.DefendCooldownTimer - deltaTime);

            // When defend window is over AND attack cooldown is done -> attack again
            bool readyToAttack = (attack.AttackRateRemaining <= 0f) && (combatState.DefendCooldownTimer <= 0f);

            if (readyToAttack)
            {
                combatState.CurrentState = CombatState.State.Attacking;
                combatState.StateTimer = 0f;
                // attack.AnimationType = EntitySpawner.AnimationType.Idle; // optional
            }
            else
            {
                //combatState.CurrentState = CombatState.State.Defending;
                TransitionToDefending(ref combatState, ref attack);

                // attack.AnimationType = EntitySpawner.AnimationType.Defend; // if you have it
            }
        }


        private void HandleIdleState(ref CombatState combatState, CombatTarget combatTarget)
        {
            if (combatTarget.TargetEntity != Entity.Null &&
                CombatUtils.IsTargetValid(combatTarget.TargetEntity, TranslationFromEntity))
            {
                combatState.CurrentState = CombatState.State.SeekingTarget;
                combatState.TargetEntity = combatTarget.TargetEntity;
                combatState.StateTimer = 0f;
         
            }
            else
            {
         
            }
        }

        private bool ShouldDefend(ref AttackComponent attack, AnimationComponent animation)
        {
            float baseDefendChance = animation.UnitType == EntitySpawner.UnitType.Ally ? 1f : .1f;

            // Generate random value and check against defend chance
            float randomValue = Random.NextFloat(0f, 1f);
            bool shouldDefend = randomValue < baseDefendChance;

            return shouldDefend;
        }

        private bool ShouldWaitBehindFrontLine(
    ref AttackComponent attack,
    float3 currentPosition,
    float3 targetPosition)
        {
            float distanceToTarget = math.distance(currentPosition, targetPosition);
            float waitCheckDistance = attack.Range * WaitingTimerExtraDistance;

            if (distanceToTarget > waitCheckDistance)
            {
                attack.StuckTimer = 0f;
                attack.LastSeekingDistance = distanceToTarget;
                attack.timeRemaingingToSetAsWaiting = -1f;
                return false;
            }

            bool hasPreviousDistance = attack.LastSeekingDistance > 0f;
            bool isGettingCloser =
                hasPreviousDistance &&
                distanceToTarget < attack.LastSeekingDistance - 0.05f;

            if (isGettingCloser)
            {
                attack.StuckTimer = 0f;
            }
            else
            {
                attack.StuckTimer += DeltaTime;
            }

            attack.LastSeekingDistance = distanceToTarget;

            return attack.StuckTimer >= 2.0f;
        }
        private void TransitionToSeeking(ref CombatState combatState)
        {
            combatState.CurrentState = CombatState.State.SeekingTarget;
            combatState.StateTimer = 0f;
        }

        private void TransitionToIdle(ref CombatState combatState)
        {
            combatState.CurrentState = CombatState.State.Idle;
            combatState.TargetEntity = Entity.Null;
            combatState.StateTimer = 0f;
        }
        private void TransitionToWaiting(ref CombatState combatState, ref AttackComponent attack)
        {
            combatState.CurrentState = CombatState.State.Waiting;
            attack.timeRemaingingToSetAsWaiting = -1f;
        }

        private void TransitionToAttacking(
    ref CombatState combatState,
    ref AttackComponent attack)
        {
            combatState.CurrentState = CombatState.State.Attacking;
            combatState.StateTimer = 0f;

            ResetSeekingTimers(ref attack);
        }

        private void ResetSeekingTimers(ref AttackComponent attack)
        {
            attack.timeRemaingingToSetAsWaiting = -1f;
            attack.StuckTimer = 0f;
            attack.LastSeekingDistance = 999999f;
        }
    }
}