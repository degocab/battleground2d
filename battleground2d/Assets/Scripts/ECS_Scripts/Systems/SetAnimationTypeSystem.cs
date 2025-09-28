using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystem))] // After movement determines velocity
[UpdateBefore(typeof(TransformSystemGroup))] // After movement determines velocity
[BurstCompile]
public partial class SetAnimationTypeSystem : SystemBase
{
    // bonuys poiunts off ultimate oints off car wash
    // 
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
                     ref CombatState combatState,

                     in MovementSpeedComponent movement,
                     in HealthComponent health) =>
            {
                //// 1. Handle cooldowns and timers first
                //if (cooldown.timeRemaining > 0)
                //{
                //    cooldown.timeRemaining -= deltaTime;
                //}

                // 2. Handle death animation (highest priority)
                if (health.isDying)
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Die;
                    animationComponent.finishAnimation = true;
                    UpdatePreviousAnimationField(entity, ref animationComponent);
                    return; // Death overrides everything else
                }
                //Debug.Log("attackComponent.isTakingDamage: " + attackComponent.isTakingDamage);


                // 2. Handle death animation (highest priority)
                if (attackComponent.isTakingDamage)
                {
                    Debug.Log($"takingDmgTimeRemaining {cooldown.takingDmgTimeRemaining}");
                    if (cooldown.takingDmgTimeRemaining <= 0)
                    {
                        animationComponent.finishAnimation = false;
                        attackComponent.isTakingDamage = false;
                        Debug.Log("setting isTakingDamage to false");
                        return; 
                    }

                    Debug.Log("Is taking damage");
                    animationComponent.AnimationType = EntitySpawner.AnimationType.TakeDamage;
                    combatState.CurrentState = CombatState.State.TakingDamage;
                    UpdatePreviousAnimationField(entity, ref animationComponent);
                    //TODO: experiment how this affects gameplay
                    animationComponent.finishAnimation = true;

                    //Debug.Log("cooldown.takingDmgTimeRemaining: " + cooldown.takingDmgTimeRemaining);
                    //if (cooldown.takingDmgTimeRemaining <= 0)
                    //{
                    //    cooldown.takingDmgTimeRemaining = cooldown.takeDamageCooldownDuration;

                    //    animationComponent.finishAnimation = false;
                    //    attackComponent.isTakingDamage = false;
                    //    Debug.Log("setting isTakingDamage to false");
                    //    //return; // Death overrides everything else
                    //}
                    //else
                    //{
                    //    cooldown.takingDmgTimeRemaining -= deltaTime;

                    //}

                    return;
                }

                // 3. Handle combat animations (medium priority)
                if (combatState.CurrentState == CombatState.State.Attacking && cooldown.attackCoolTimeRemaining > 0f)
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Attack;
                }
                else if (combatState.CurrentState == CombatState.State.Attacking)
                {
                    // Attack state but cooldown finished - reset to idle
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Idle;
                    animationComponent.finishAnimation = false;
                    attackComponent.isAttacking = false;
                }
                else if (combatState.CurrentState == CombatState.State.Defending)
                {
                    animationComponent.AnimationType = EntitySpawner.AnimationType.Defend;
                }
                //else if (combatState.CurrentState == CombatState.State.TakingDamage)
                //{

                //}

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

                UpdatePreviousAnimationField(entity, ref animationComponent);
                //if (cooldown.attackCoolTimeRemaining > 0)
                //{
                //    Debug.Log($"attackCoolTimeRemaining: {cooldown.attackCoolTimeRemaining}");
                //    cooldown.attackCoolTimeRemaining -= deltaTime;
                //    Debug.Log($"attackCoolTimeRemaining: {cooldown.attackCoolTimeRemaining}");
                //}
            })//.WithoutBurst().Run();
            .ScheduleParallel();


        //Entities
        //   .WithName("ResetCoolDowns")
        //   .ForEach((Entity entity,
        //            ref AnimationComponent animationComponent,
        //            ref AttackComponent attackComponent,
        //            ref AttackCooldownComponent cooldown,
        //            ref CombatState combatState,

        //            in MovementSpeedComponent movement,
        //            in HealthComponent health) =>
        //   {
        //       if (cooldown.attackCoolTimeRemaining > 0)
        //           cooldown.attackCoolTimeRemaining -= deltaTime;
        //       if (cooldown.takingDmgTimeRemaining > 0)
        //           cooldown.takingDmgTimeRemaining -= deltaTime;
        //       if (attackComponent.AttackRateRemaining > 0)
        //           attackComponent.AttackRateRemaining -= deltaTime;
        //   }).ScheduleParallel();

    }

    private static void UpdatePreviousAnimationField(Entity entity, ref AnimationComponent animationComponent)
    {
        if (animationComponent.PrevAnimationType != animationComponent.AnimationType)
        {
            Unity.Mathematics.Random walkRandom = new Unity.Mathematics.Random((uint)entity.Index);
            Unity.Mathematics.Random runRandom = new Unity.Mathematics.Random((uint)entity.Index * 1000);
            EntitySpawner.UpdateAnimationFields(ref animationComponent, walkRandom, runRandom);
            animationComponent.PrevAnimationType = animationComponent.AnimationType;
        }

    }

}