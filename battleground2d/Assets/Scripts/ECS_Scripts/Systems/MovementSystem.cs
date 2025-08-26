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
[UpdateBefore(typeof(CollisionDetectionSystem))]
[UpdateAfter(typeof(QuadrantSystem))]
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
        float moveX = 0f;
        float moveY = 0f;
        bool isRunnning = false;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) isRunnning = true;


        // This job sets the desired velocity based on input or AI for commander.
        // For most units, it might be set by an AI system, not input.
        var inputJobHandle = Entities
            .WithName("SetCommanderVelocity")
          .WithAll<CommanderComponent>() // Remove to let all units update
          .ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
          {
              //movementSpeedComponent.moveX = moveX;
              //movementSpeedComponent.moveY = moveY;
              movementSpeedComponent.velocity = new float3(moveX, moveY, 0);
              movementSpeedComponent.isRunnning = isRunnning;
          }).ScheduleParallel(Dependency);

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
                  movementSpeedComponent.randomSpeed = 2f;//random.NextFloat(minRange, maxRange);
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
          }).ScheduleParallel(inputJobHandle);

        // -- JOB 3: Update Animation State (Burst Parallel) --
        // This could also be a separate system after Physics.
        // Get direction for animation
               var animationJobHandle = Entities
            .WithName("UpdateAnimationFromVelocity")
          .ForEach((ref MovementSpeedComponent movementSpeedComponent, ref AnimationComponent animationComponent) =>
          {
              float2 velocity = movementSpeedComponent.velocity.xy;

              if (math.lengthsq(velocity) > 0.0001f) // Check if moving
              {
                  //compare abs values to deterimine dominant axis
                  if (math.abs(velocity.x) > math.abs(velocity.y))
                  {
                      if (velocity.x > 0)
                      {
                          animationComponent.Direction = EntitySpawner.Direction.Right;
                          animationComponent.animationWidthOffset = 1;
                      }
                      else
                      {
                          animationComponent.Direction = EntitySpawner.Direction.Left;
                          animationComponent.animationWidthOffset = 2;
                      }
                  }
                  else
                  {
                      if (velocity.y > 0)
                      {
                          animationComponent.Direction = EntitySpawner.Direction.Up;
                          animationComponent.animationWidthOffset = 3;
                      }
                      else
                      {
                          animationComponent.Direction = EntitySpawner.Direction.Down;
                          animationComponent.animationWidthOffset = 4;
                      }
                  }
              }

              animationComponent.prevDirection = animationComponent.Direction;
          }).ScheduleParallel(speedJobHandle);

        // Set the final dependency for the next system
        Dependency = animationJobHandle;

        // !!! REMOVE Dependency.Complete() !!! 
        // Let the scheduler handle it. Your CollisionSystem should use this Dependency.


    }

}