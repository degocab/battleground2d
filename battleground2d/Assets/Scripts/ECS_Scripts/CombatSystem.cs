using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(MovementSystem))]
public class CombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        bool attack = false;
        bool defend = false;
        //if (Input.GetKeyDown(KeyCode.Space)) // Detect spacebar press only
        if (Input.GetMouseButtonDown(0)) // Detect spacebar press only
            attack = true;

        if (Input.GetMouseButton(1)) // Detect spacebar press only
            defend = true;
        else
            defend = false;

        bool takeDamage = false;
        if (Input.GetKeyDown(KeyCode.T)) // Detect spacebar press only
            takeDamage = true;
        bool isDying = false;
        if (Input.GetKeyDown(KeyCode.Y)) // Detect spacebar press only
            isDying = true;

        Entities.ForEach((ref Entity entity, ref Translation translation, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref AnimationComponent animationComponent, ref HealthComponent healthComponent) =>
        {


            if (takeDamage)
            {
                if (!attackComponent.isTakingDamage)
                {
                    attackComponent.isTakingDamage = true;
                    attackCooldown.timeRemaining = attackCooldown.takeDamageCooldownDuration;
                    healthComponent.health -= 50f;

                    if (healthComponent.health <= 0)
                    {
                        healthComponent.isDying = true;
                    }
                }
            }
            else
            {
                if (attack)
                {
                    if (!attackComponent.isAttacking) //dont reset until we are done
                    {
                        attackComponent.isAttacking = true;
                        //animationComponent.animationType = EntitySpawner.AnimationType.Attack;
                        //EntitySpawner.UpdateAnimationFields(ref animationComponent);
                        attackCooldown.timeRemaining = attackCooldown.cooldownDuration; // Set the cooldown duration 
                    }
                }
                if (defend)
                {
                    attackComponent.isDefending = true;
                }
                else
                    attackComponent.isDefending = false;
            }

        }).ScheduleParallel();




        Entities
        .ForEach((ref AnimationComponent animationComponent, ref MovementSpeedComponent movementSpeedComponent, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref HealthComponent healthComponent, in Entity entity) =>
        {


            if (animationComponent.isFrozen)
            {
                return;
            }

            if (healthComponent.isDying)
            {
                if (healthComponent.timeRemaining == healthComponent.deathAnimationDuration) //on attack trigger?
                {
                    animationComponent.animationType = EntitySpawner.AnimationType.Die;
                }
                if (healthComponent.timeRemaining > 0f)
                {
                    healthComponent.timeRemaining -= deltaTime; // Reduce cooldown
                }
                else
                {
                    if (animationComponent.currentFrame == animationComponent.frameCount - 1)
                    {
                        //animationComponent.finishAnimation = false; // Reset finish flag after animation is done
                        //attackComponent.isTakingDamage = false; // Reset finish flag after animation is done
                        animationComponent.isFrozen = true;
                    }

                }
            }
            else if (attackComponent.isTakingDamage)
            {
                if (attackCooldown.timeRemaining == attackCooldown.takeDamageCooldownDuration) //on attack trigger?
                {
                    animationComponent.animationType = EntitySpawner.AnimationType.TakeDamage;
                }
                if (attackCooldown.timeRemaining > 0f)
                {
                    attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
                }
                else
                {
                    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
                    attackComponent.isTakingDamage = false; // Reset finish flag after animation is done
                }
            }
            else if (attackComponent.isAttacking)
            {
                if (attackCooldown.timeRemaining == attackCooldown.cooldownDuration) //on attack trigger?
                {
                    animationComponent.animationType = EntitySpawner.AnimationType.Attack;
                }
                if (attackCooldown.timeRemaining > 0f)
                {
                    attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
                }
                else
                {
                    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
                    attackComponent.isAttacking = false; // Reset finish flag after animation is done
                }
            }
            else if (attackComponent.isDefending)
            {
                animationComponent.animationType = EntitySpawner.AnimationType.Defend;
            }
            else
            {
                if (movementSpeedComponent.moveX == 0f && movementSpeedComponent.moveY == 0f && movementSpeedComponent.isKnockedBack == false) //not moving
                {
                    animationComponent.animationType = EntitySpawner.AnimationType.Idle;
                    //EntitySpawner.UpdateAnimationFields(ref animationComponent);
                    movementSpeedComponent.randomSpeed = 0f;
                }
                else
                {
                    if (movementSpeedComponent.isRunnning)
                    {
                        animationComponent.animationType = EntitySpawner.AnimationType.Run;
                    }
                    else
                    {
                        animationComponent.animationType = EntitySpawner.AnimationType.Walk;
                    }
                }

            }

            if (animationComponent.prevAnimationType != animationComponent.animationType)
            {
                //if (animationComponent.animationType == EntitySpawner.AnimationType.Idle)
                //{
                //    EntitySpawner.UpdateAnimationFields(ref animationComponent);
                //}
                //else //(animationComponent.animationType == EntitySpawner.AnimationType.Run)
                //{

                Unity.Mathematics.Random walkRandom = new Unity.Mathematics.Random((uint)entity.Index);
                Unity.Mathematics.Random runRandom = new Unity.Mathematics.Random((uint)entity.Index * 1000);
                EntitySpawner.UpdateAnimationFields(ref animationComponent, walkRandom, runRandom);
                //}
                animationComponent.prevAnimationType = animationComponent.animationType;
            }

        }).WithBurst().ScheduleParallel();
    }
}
