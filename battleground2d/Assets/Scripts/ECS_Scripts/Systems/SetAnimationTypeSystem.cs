using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystem))] // After movement determines velocity
[UpdateBefore(typeof(TransformSystemGroup))] // After movement determines velocity
[BurstCompile]
public partial class SetAnimationTypeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = Time.DeltaTime;

        Entities
            .WithName("UpdateAllAnimations")
            .ForEach((Entity entity,
                     ref AnimationComponent animationComponent,
                     ref AttackComponent attackComponent,
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
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Die;
                    animationComponent.finishAnimation = false;
                    return; // Death overrides everything else
                }


                // 2. Handle death animation (highest priority)
                if (attackComponent.isTakingDamage)
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.TakeDamage;
                    animationComponent.finishAnimation = true;
                    return; // Death overrides everything else
                }

                // 3. Handle combat animations (medium priority)
                if (combatState.CurrentState == CombatState.State.Attacking)
                {
                    if (cooldown.timeRemaining == cooldown.cooldownDuration) //on attack trigger?
                    {
                        animationComponent.AnimationType = EntitySpawner.AnimationType.Attack;
                    }
                    if (cooldown.timeRemaining > 0f)
                    {
                        cooldown.timeRemaining -= deltaTime; // Reduce cooldown
                    }
                    else
                    {
                        animationComponent.finishAnimation = false; // Reset finish flag after animation is done
                        attackComponent.isAttacking = false; // Reset finish flag after animation is done
                    }
                }
                else if (combatState.CurrentState == CombatState.State.Defending)
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Defend;
                }
                else if (cooldown.timeRemaining > 0 && cooldown.timeRemaining == cooldown.takeDamageCooldownDuration)
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.TakeDamage;
                }
                // 4. Handle movement animations (lowest priority)
                else if (movement.velocity.x != 0f || movement.velocity.y != 0f)
                {
                    animationComponent.AnimationType = movement.isRunnning ?
                        EntitySpawner.AnimationType.Run :
                        EntitySpawner.AnimationType.Walk;
                }
                else
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Idle;
                }

                // 5. YOUR EXISTING ANIMATION LOGIC (keep what works)
                if (animationComponent.prevAnimationType != animationComponent.AnimationType)
                {
                    Unity.Mathematics.Random walkRandom = new Unity.Mathematics.Random((uint)entity.Index);
                    Unity.Mathematics.Random runRandom = new Unity.Mathematics.Random((uint)entity.Index * 1000);
                    EntitySpawner.UpdateAnimationFields(ref animationComponent, walkRandom, runRandom);
                    animationComponent.prevAnimationType = animationComponent.AnimationType;
                }

            }).ScheduleParallel();
    }
}