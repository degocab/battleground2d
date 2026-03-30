using System;

using System.Reflection;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateBefore(typeof(CollisionQuadrantSystem))]
[UpdateAfter(typeof(UnitMoveToTargetSystem))]
[BurstCompile]
public class MovementSystem : SystemBase
{
    public static EntitySpawner entitySpawner;

    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var deltaTime = Time.DeltaTime;

        float minRange = 1f;
        float maxRange = 1.125f;
        var speedJobHandle = Entities.WithName("ApplyRandomSpeed")
          .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent movementSpeedComponent, in Entity entity) =>
          {
              if (movementSpeedComponent.isRunnning)
              {
                  Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);
                  movementSpeedComponent.randomSpeed = 1.25f;//random.NextFloat(minRange, maxRange);
              }
              else
              {
                  //Unity.Mathematics.Random random2 = new Unity.Mathematics.Random((uint)entity.Index);
                  //velocity.randomSpeed = random2.NextFloat(.5f, .6f);
                  movementSpeedComponent.randomSpeed = .5f;
              }

              float3 vel = new float3(movementSpeedComponent.velocity.x, movementSpeedComponent.velocity.y, 0) * movementSpeedComponent.randomSpeed;
              vel.z = 0;
              movementSpeedComponent.velocity = vel;
          }).ScheduleParallel(Dependency);

        var animationJobHandle = Entities
     .WithName("UpdateAnimationFromVelocity")
   .ForEach((ref Translation transform, ref MovementSpeedComponent movementSpeedComponent, ref AnimationComponent animationComponent, in CombatState combatState) =>
   {
       float2 velocity = movementSpeedComponent.velocity.xy;
       //update view direction
       if (/* movementSpeedComponent.isPlayerControlled &&*/ (combatState.CurrentState == CombatState.State.Attacking || combatState.CurrentState == CombatState.State.Defending))
       {
           if (movementSpeedComponent.isPlayerControlled)
           {
               float2 viewDirection = (movementSpeedComponent.aimDirection - transform.Value.xy);
               CombatUtils.SetAnimationDirection(ref animationComponent, viewDirection);
           }
       }
       else
       {
           if (math.lengthsq(velocity) > 0.0001f) // Check if moving
           {
               CombatUtils.SetAnimationDirection(ref animationComponent, velocity);
           }
       }



       animationComponent.prevDirection = animationComponent.Direction;
   }).ScheduleParallel(speedJobHandle);

        var restrictMovemenJobHandle = Entities
                .WithName("RestrictMovementByStates")
                .ForEach((ref MovementSpeedComponent movementSpeedComponent, ref AnimationComponent animationComponent, in AttackComponent attackComponent, in CombatState combatState, in MovementStatus movementStatus) =>
                {
                    if (animationComponent.AnimationType == EntitySpawner.AnimationType.Attack
                    || animationComponent.AnimationType == EntitySpawner.AnimationType.TakeDamage
                    || movementStatus.CurrentStatus == MovementStatus.Status.ReachedDestination)
                        movementSpeedComponent.velocity = float3.zero;
                    //switch (animationComponent.AnimationType)
                    //{
                    //    case EntitySpawner.AnimationType.Attack:
                    //    case EntitySpawner.AnimationType.TakeDamage:
                    //        //if (!attackComponent.isTakingDamage)
                    //        if (combatState.CurrentState != CombatState.State.TakingDamage)
                    //        {
                    //            movementSpeedComponent.velocity = float3.zero;
                    //        }
                    //        break;

                    //    default:
                    //        //dont do nothing
                    //        break;
                    //}

                }).ScheduleParallel(animationJobHandle);


        // Set the final dependency for the next system
        Dependency = restrictMovemenJobHandle;
    }

}