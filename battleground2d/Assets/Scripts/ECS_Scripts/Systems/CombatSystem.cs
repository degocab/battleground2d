using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateBefore(typeof(PhysicsSystem))]
//[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
//public class CombatSystem : SystemBase
//{
//    protected override void OnUpdate()
//    {
//        var deltaTime = Time.DeltaTime;
//        bool attack = false;
//        bool defend = false;
//        //if (Input.GetKeyDown(KeyCode.Space)) // Detect spacebar press only
//        if (Input.GetMouseButtonDown(0)) // Detect spacebar press only
//            attack = true;

//        if (Input.GetMouseButton(1)) // Detect spacebar press only
//            defend = true;
//        else
//            defend = false;

//        bool takeDamage = false;
//        if (Input.GetKeyDown(KeyCode.T)) // Detect spacebar press only
//            takeDamage = true;
//        bool isDying = false;
//        if (Input.GetKeyDown(KeyCode.Y)) // Detect spacebar press only
//            isDying = true;

//        Entities.ForEach((ref Entity entity, ref Translation translation, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref AnimationComponent animationComponent, ref HealthComponent healthComponent) =>
//        {


//            if (takeDamage)
//            {
//                if (!attackComponent.isTakingDamage)
//                {
//                    attackComponent.isTakingDamage = true;
//                    attackCooldown.timeRemaining = attackCooldown.takeDamageCooldownDuration;
//                    healthComponent.health -= 50f;

//                    if (healthComponent.health <= 0)
//                    {
//                        healthComponent.isDying = true;
//                    }
//                }
//            }
//            else
//            {
//                if (attack)
//                {
//                    if (!attackComponent.isAttacking) //dont reset until we are done
//                    {
//                        attackComponent.isAttacking = true;
//                        //animationComponent.animationType = EntitySpawner.AnimationType.Attack;
//                        //EntitySpawner.UpdateAnimationFields(ref animationComponent);
//                        attackCooldown.timeRemaining = attackCooldown.cooldownDuration; // Set the cooldown duration 
//                    }
//                }
//                if (defend)
//                {
//                    attackComponent.isDefending = true;
//                }
//                else
//                    attackComponent.isDefending = false;
//            }

//        }).ScheduleParallel();




//        Entities
//        .ForEach((ref AnimationComponent animationComponent, ref MovementSpeedComponent movementSpeedComponent, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref HealthComponent healthComponent, in Entity entity) =>
//        {


//            if (animationComponent.isFrozen)
//            {
//                return;
//            }

//            if (healthComponent.isDying)
//            {
//                if (healthComponent.timeRemaining == healthComponent.deathAnimationDuration) //on attack trigger?
//                {
//                    animationComponent.animationType = EntitySpawner.AnimationType.Die;
//                }
//                if (healthComponent.timeRemaining > 0f)
//                {
//                    healthComponent.timeRemaining -= deltaTime; // Reduce cooldown
//                }
//                else
//                {
//                    if (animationComponent.currentFrame == animationComponent.frameCount - 1)
//                    {
//                        //animationComponent.finishAnimation = false; // Reset finish flag after animation is done
//                        //attackComponent.isTakingDamage = false; // Reset finish flag after animation is done
//                        animationComponent.isFrozen = true;
//                    }

//                }
//            }
//            else if (attackComponent.isTakingDamage)
//            {
//                if (attackCooldown.timeRemaining == attackCooldown.takeDamageCooldownDuration) //on attack trigger?
//                {
//                    animationComponent.animationType = EntitySpawner.AnimationType.TakeDamage;
//                }
//                if (attackCooldown.timeRemaining > 0f)
//                {
//                    attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
//                }
//                else
//                {
//                    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
//                    attackComponent.isTakingDamage = false; // Reset finish flag after animation is done
//                }
//            }
//            else if (attackComponent.isAttacking)
//            {
//                if (attackCooldown.timeRemaining == attackCooldown.cooldownDuration) //on attack trigger?
//                {
//                    animationComponent.animationType = EntitySpawner.AnimationType.Attack;
//                }
//                if (attackCooldown.timeRemaining > 0f)
//                {
//                    attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
//                }
//                else
//                {
//                    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
//                    attackComponent.isAttacking = false; // Reset finish flag after animation is done
//                }
//            }
//            else if (attackComponent.isDefending)
//            {
//                animationComponent.animationType = EntitySpawner.AnimationType.Defend;
//            }
//            else
//            {
//                if (movementSpeedComponent.velocity.x == 0f && movementSpeedComponent.velocity.y == 0f
//                            //&& movementSpeedComponent.isBlocked == false
//                            //&& movementSpeedComponent.isKnockedBack == false
//                            ) //not moving
//                {
//                    animationComponent.animationType = EntitySpawner.AnimationType.Idle;
//                    //EntitySpawner.UpdateAnimationFields(ref animationComponent);
//                    movementSpeedComponent.randomSpeed = 0f;
//                }
//                else
//                {
//                    if (movementSpeedComponent.isRunnning)
//                    {
//                        animationComponent.animationType = EntitySpawner.AnimationType.Run;
//                    }
//                    else
//                    {
//                        animationComponent.animationType = EntitySpawner.AnimationType.Walk;
//                    }
//                }

//            }

//            if (animationComponent.prevAnimationType != animationComponent.animationType)
//            {
//                //if (animationComponent.animationType == EntitySpawner.AnimationType.Idle)
//                //{
//                //EntitySpawner.UpdateAnimationFields(ref animationComponent);
//                //}
//                //else //(animationComponent.animationType == EntitySpawner.AnimationType.Run)
//                //{

//                Unity.Mathematics.Random walkRandom = new Unity.Mathematics.Random((uint)entity.Index);
//                Unity.Mathematics.Random runRandom = new Unity.Mathematics.Random((uint)entity.Index * 1000);
//                EntitySpawner.UpdateAnimationFields(ref animationComponent, walkRandom, runRandom);
//                //}
//                animationComponent.prevAnimationType = animationComponent.animationType;
//            }

//        }).WithBurst().ScheduleParallel();
//    }
//}


public struct CombatState : IComponentData
{
    public enum State { Idle, SeekingTarget, Attacking, Defending, Fleeing }
    public State CurrentState;
    public Entity TargetEntity;
    public float StateTimer;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FindTargetSystem))]
[UpdateBefore(typeof(MovementSystem))]
[BurstCompile]
public partial class AutonomousCombatSystem : SystemBase
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

                        // Check if we can attack based on cooldown
                        if (CurrentTime - attack.LastAttackTime >= 1f / attack.AttackRate)
                        {
                            // Execute attack
                            attack.LastAttackTime = CurrentTime;
                            animation.AnimationType = EntitySpawner.AnimationType.Attack;

                            // Apply damage to target
                            if (combatState.TargetEntity != Entity.Null &&
                                TranslationFromEntity.HasComponent(combatState.TargetEntity))
                            {
                                ECB.AddComponent(chunkIndex, combatState.TargetEntity, new DamageComponent
                                {
                                    Value = attack.Damage,
                                    SourceEntity = entity
                                });
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
                }

                // Write back modified components
                combatStates[i] = combatState;
                attacks[i] = attack;
                animations[i] = animation;
            }
        }
    }
}
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))] // After movement determines velocity
[UpdateAfter(typeof(AutonomousCombatSystem))] // After combat determines state
[BurstCompile]
public partial class Animation2System : SystemBase
{
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = Time.DeltaTime;

        Entities
            .WithName("UpdateAllAnimations")
            .ForEach((Entity entity,
                     ref AnimationComponent animation,
                     ref AttackCooldownComponent cooldown,
                     in MovementSpeedComponent movement,
                     in CombatState combatState,
                     in HealthComponent health) =>
            {
                // 1. Handle cooldowns and timers first
                if (cooldown.timeRemaining > 0)
                {
                    cooldown.timeRemaining -= deltaTime;
                }

                // 2. Handle death animation (highest priority)
                if (health.isDying)
                {
                    animation.AnimationType = EntitySpawner.AnimationType.Die;
                    animation.finishAnimation = false;
                    return; // Death overrides everything else
                }

                // 3. Handle combat animations (medium priority)
                if (combatState.CurrentState == CombatState.State.Attacking)
                {
                    animation.AnimationType = EntitySpawner.AnimationType.Attack;
                }
                else if (combatState.CurrentState == CombatState.State.Defending)
                {
                    animation.AnimationType = EntitySpawner.AnimationType.Defend;
                }
                else if (cooldown.timeRemaining > 0 && cooldown.timeRemaining == cooldown.takeDamageCooldownDuration)
                {
                    animation.AnimationType = EntitySpawner.AnimationType.TakeDamage;
                }
                // 4. Handle movement animations (lowest priority)
                else if (movement.velocity.x != 0f || movement.velocity.y != 0f)
                {
                    animation.AnimationType = movement.isRunnning ?
                        EntitySpawner.AnimationType.Run :
                        EntitySpawner.AnimationType.Walk;
                }
                else
                {
                    animation.AnimationType = EntitySpawner.AnimationType.Idle;
                }

                // 5. YOUR EXISTING ANIMATION LOGIC (keep what works)
                if (animation.prevAnimationType != animation.AnimationType)
                {
                    Unity.Mathematics.Random walkRandom = new Unity.Mathematics.Random((uint)entity.Index);
                    Unity.Mathematics.Random runRandom = new Unity.Mathematics.Random((uint)entity.Index * 1000);
                    EntitySpawner.UpdateAnimationFields(ref animation, walkRandom, runRandom);
                    animation.prevAnimationType = animation.AnimationType;
                }

            }).ScheduleParallel();
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AutonomousCombatSystem))]
[BurstCompile]
public partial class ApplyDamageSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("ApplyDamage")
            .ForEach((Entity entity, int entityInQueryIndex,
                     ref HealthComponent health,
                     in DamageComponent damage) =>
            {
                health.Health -= damage.Value;
                ecb.RemoveComponent<DamageComponent>(entityInQueryIndex, entity);

                if (health.Health <= 0)
                {
                    health.isDying = true;
                    health.timeRemaining = health.deathAnimationDuration;
                }

            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ApplyDamageSystem))]
public partial class DeathSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = Time.DeltaTime;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("ProcessDeath")
            .ForEach((Entity entity, int entityInQueryIndex,
                     ref HealthComponent health,
                     ref AnimationComponent animation) =>
            {
                if (health.isDying)
                {
                    health.timeRemaining -= deltaTime;
                    animation.AnimationType = EntitySpawner.AnimationType.Die;

                    if (health.timeRemaining <= 0)
                    {
                        ecb.DestroyEntity(entityInQueryIndex, entity);
                    }
                }

            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}