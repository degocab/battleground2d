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
        //float moveX = 0f;
        //float moveY = 0f;
        //bool isRunnning = false;

        //if (Input.GetKey(KeyCode.W)) moveY = 1f;
        //if (Input.GetKey(KeyCode.S)) moveY = -1f;
        //if (Input.GetKey(KeyCode.A)) moveX = -1f;
        //if (Input.GetKey(KeyCode.D)) moveX = 1f;
        //if (Input.GetKey(KeyCode.LeftShift)) isRunnning = true;

        //// Get mouse position for aiming
        //Vector3 mousePosition = Input.mousePosition;
        //mousePosition.z = -Camera.main.transform.position.z;
        //Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
        //float2 worldMousePosFloat = new float2(worldMousePos.x, worldMousePos.y);

        //// We need to get the commander's position first
        //float2 commanderPosition = float2.zero;

        //// Calculate movement penalty based on angle between movement and aim
        ////if (foundCommander)
        //if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        //{
        //    float2 aimDirection = worldMousePosFloat - commanderPosition;
        //    aimDirection = math.normalize(aimDirection);

        //    float2 moveDirection = new float2(moveX, moveY);
        //    float moveMagnitude = math.length(moveDirection);

        //    if (moveMagnitude > 0)
        //    {
        //        moveDirection = math.normalize(moveDirection);

        //        // Calculate dot product to get angle between movement and aim
        //        float dotProduct = math.dot(moveDirection, aimDirection);

        //        // Apply speed multipliers based on direction
        //        float speedMultiplier = 1.0f;

        //        if (dotProduct > 0.8f)
        //        {
        //            speedMultiplier = 1.0f; // Moving forward (full speed)
        //        }
        //        else if (dotProduct > 0.3f)
        //        {
        //            speedMultiplier = 0.8f; // Moving somewhat sideways
        //        }
        //        else if (dotProduct > -0.3f)
        //        {
        //            speedMultiplier = 0.6f; // Moving mostly sideways
        //        }
        //        else
        //        {
        //            speedMultiplier = 0.4f; // Moving backwards (slowest)
        //        }

        //        // Apply the speed penalty
        //        moveX *= speedMultiplier;
        //        moveY *= speedMultiplier;
        //    }
        //}

        //// This job sets the desired velocity based on input or AI for commander.
        //var inputJobHandle = Entities
        //    .WithName("SetCommanderVelocity")
        //    .WithAll<CommanderComponent>()
        //    .ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
        //    {
        //        movementSpeedComponent.velocity = new float3(moveX, moveY, 0);
        //        movementSpeedComponent.isRunnning = isRunnning;
        //        movementSpeedComponent.isPlayerControlled = true;
        //    }).ScheduleParallel(Dependency);

        // ... rest of your existing jobs (speedJobHandle, animationJobHandle, etc.)

        // -- JOB 2: Apply Speed Modifiers (Burst Parallel) --
        // This job processes EVERY moving entity to finalize its desired velocity.
        // Randomize movement speed
        float minRange = 1f;
        float maxRange = 1.125f;
        //float minRange = 1.75f;
        //float maxRange = 1.875f;
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

        // -- JOB 3: Update Animation State (Burst Parallel) --
        // This could also be a separate system after Physics.
        // Get direction for animation
        var animationJobHandle = Entities
     .WithName("UpdateAnimationFromVelocity")
   .ForEach((ref Translation transform, ref MovementSpeedComponent movementSpeedComponent, ref AnimationComponent animationComponent, in CombatState combatState) =>
   {
       float2 velocity = movementSpeedComponent.velocity.xy;
       //if (!movementSpeedComponent.isPlayerControlled) 
       //{
       //    movementSpeedComponent.aimDirection = movementSpeedComponent.velocity.xy;
       //}
       //update view direction
       if (/* movementSpeedComponent.isPlayerControlled &&*/ (combatState.CurrentState == CombatState.State.Attacking || combatState.CurrentState == CombatState.State.Defending))
       {



           if (movementSpeedComponent.isPlayerControlled)
           {
               float2 viewDirection = (movementSpeedComponent.aimDirection - transform.Value.xy);
               CombatUtils.SetAnimationDirection(ref animationComponent, viewDirection);
           }
           //else
           //{

           //    //movementSpeedComponent.aimDirection = velocity;// velocity;
           //    //CombatUtils.SetAnimationDirection(ref animationComponent, movementSpeedComponent.aimDirection);

           //}



           //if (math.abs(viewDirection.x) > math.abs(viewDirection.y))
           //{
           //    if (viewDirection.x > 0)
           //    {
           //        animationComponent.Direction = EntitySpawner.Direction.Right;
           //        animationComponent.animationWidthOffset = 1;
           //    }
           //    else
           //    {
           //        animationComponent.Direction = EntitySpawner.Direction.Left;
           //        animationComponent.animationWidthOffset = 2;
           //    }
           //}
           //else
           //{
           //    if (viewDirection.y > 0)
           //    {
           //        animationComponent.Direction = EntitySpawner.Direction.Up;
           //        animationComponent.animationWidthOffset = 3;
           //    }
           //    else
           //    {
           //        animationComponent.Direction = EntitySpawner.Direction.Down;
           //        animationComponent.animationWidthOffset = 4;
           //    }
           //}

       }
       else
       {
           if (math.lengthsq(velocity) > 0.0001f) // Check if moving
           {
               CombatUtils.SetAnimationDirection(ref animationComponent, velocity);
               //compare abs values to deterimine dominant axis
               //if (math.abs(velocity.x) > math.abs(velocity.y))
               //{
               //    if (velocity.x > 0)
               //    {
               //        animationComponent.Direction = EntitySpawner.Direction.Right;
               //        animationComponent.animationWidthOffset = 1;
               //    }
               //    else
               //    {
               //        animationComponent.Direction = EntitySpawner.Direction.Left;
               //        animationComponent.animationWidthOffset = 2;
               //    }
               //}
               //else
               //{
               //    if (velocity.y > 0)
               //    {
               //        animationComponent.Direction = EntitySpawner.Direction.Up;
               //        animationComponent.animationWidthOffset = 3;
               //    }
               //    else
               //    {
               //        animationComponent.Direction = EntitySpawner.Direction.Down;
               //        animationComponent.animationWidthOffset = 4;
               //    }
               //}
           }
       }



       animationComponent.prevDirection = animationComponent.Direction;
   }).ScheduleParallel(speedJobHandle);



        // !!! REMOVE Dependency.Complete() !!! 
        // Let the scheduler handle it. Your CollisionSystem should use this Dependency.

        var restrictMovemenJobHandle = Entities
                .WithName("RestrictMovementByStates")
                .ForEach((ref MovementSpeedComponent movementSpeedComponent, ref AnimationComponent animationComponent, in AttackComponent attackComponent, in CombatState combatState) =>
                {

                    switch (animationComponent.AnimationType)
                    {
                        case EntitySpawner.AnimationType.Attack:
                        case EntitySpawner.AnimationType.TakeDamage:
                            //if (!attackComponent.isTakingDamage)
                            if (combatState.CurrentState != CombatState.State.TakingDamage)
                            {
                                movementSpeedComponent.velocity = float3.zero;
                            }
                            break;

                        default:
                            //dont do nothing
                            break;
                    }

                }).ScheduleParallel(animationJobHandle);


        // Set the final dependency for the next system
        Dependency = restrictMovemenJobHandle;
    }

}