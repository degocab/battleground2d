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



        Entities
   .WithName("ResetCoolDowns")
   .ForEach((Entity entity,
            ref AnimationComponent animationComponent,
            ref AttackComponent attackComponent,
            ref AttackCooldownComponent cooldown,
            ref CombatState combatState,
            ref DefenseComponent defenseComponent,
            in MovementSpeedComponent movement,
            in HealthComponent health) =>
   {
       if (cooldown.attackCoolTimeRemaining > 0)
           cooldown.attackCoolTimeRemaining -= deltaTime;
       if (cooldown.takingDmgTimeRemaining > 0)
           cooldown.takingDmgTimeRemaining -= deltaTime;
       if (attackComponent.AttackRateRemaining > 0)
           attackComponent.AttackRateRemaining -= deltaTime;
       if (defenseComponent.BlockDuration > 0)
       {
           defenseComponent.BlockDuration -= deltaTime;

       }
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
                attack.isTakingDamage = false;
                attack.isDefending = false;
                attack.isAttacking = false;

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
                        HandleAttackingState(ref cooldown, ref combatState, ref attack, ref animation, entity, chunkIndex,
                                           translation, CurrentTime, DeltaTime, TranslationFromEntity, ECB);

                        break;
                    case CombatState.State.TakingDamage:

                        attack.isTakingDamage = true;
                        break;
                    case CombatState.State.Defending:
                        attack.isDefending = true;
                        // Reduce incoming damage by 50%
                        // Chance to parry and counter-attack
                        // Prevent movement while defending
                        break;
                }

                // Write back modified components
                combatStates[i] = combatState;
                attacks[i] = attack;
                animations[i] = animation;
                cooldowns[i] = cooldown;
            }
        }

        private void HandleAttackingState(ref AttackCooldownComponent cooldown, ref CombatState combatState,
                                    ref AttackComponent attack, ref AnimationComponent animation,
                                    Entity entity, int chunkIndex, Translation translation,
                                    float currentTime, float deltaTime,
                                    ComponentDataFromEntity<Translation> translationFromEntity,
                                    EntityCommandBuffer.ParallelWriter ecb)
        {
            combatState.StateTimer += DeltaTime;

            // Target validation
            if (!CombatUtils.IsTargetValid(combatState.TargetEntity, TranslationFromEntity))
            {
                combatState.CurrentState = CombatState.State.SeekingTarget;
                combatState.TargetEntity = Entity.Null;
                return;
            }


            // DEBUG: Log the values at the start
            bool stillAttacking = cooldown.attackCoolTimeRemaining > 0f;
            bool animationReady = cooldown.attackCoolTimeRemaining < 0.001f;
            bool attackReady = attack.AttackRateRemaining < 0.001f;
            bool canAttack = animationReady && attackReady;


            // NEW: Also check if animation is actually in attack state
            bool animationPlayingAttack = animation.AnimationType == EntitySpawner.AnimationType.Attack;


            // AI DECISION: Attack or Defend?
            if (canAttack)
            {

                // AI chooses to attack
                attack.AttackRateRemaining = attack.AttackRate * 2;
                attack.isAttacking = true;
                animation.finishAnimation = true;
                animation.AnimationType = EntitySpawner.AnimationType.Attack;
                cooldown.attackCoolTimeRemaining = .3f;// cooldown.attackCoolDownDuration;

                ecb.AddComponent(chunkIndex, entity, new AttackEventComponent
                {
                    TargetEntity = combatState.TargetEntity,
                    Damage = attack.Damage,
                    SourceEntity = entity,
                    AttackTime = CurrentTime,
                    AttackDuration = 0.2f
                });
            }
            else if (animationPlayingAttack)
            {
                // Animation is still playing, wait for it to finish
                return;
            }
            else
            {
                if (stillAttacking)
                {
                    //Debug.Log("stillattacking");
                }
                else
                {
                    //// AI chooses to defend while waiting
                    combatState.CurrentState = CombatState.State.Defending;
                    attack.isDefending = true;
                }
            }

            // Timeout
            if (combatState.StateTimer > 30f)
            {
                combatState.CurrentState = CombatState.State.Idle;
                combatState.TargetEntity = Entity.Null;
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







