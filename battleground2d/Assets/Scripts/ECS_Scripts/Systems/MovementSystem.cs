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
        var deltaTime = Time.DeltaTime;
        float moveX = 0f;
        float moveY = 0f;
        bool isRunnning = false;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) isRunnning = true;

        // Update movement x and y
        Entities
          .WithAll<CommanderComponent>() // Remove to let all units update
          .ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
          {
              movementSpeedComponent.moveX = moveX;
              movementSpeedComponent.moveY = moveY;
              movementSpeedComponent.isRunnning = isRunnning;
          }).ScheduleParallel();

        // EntityQuery for getting all the entities with position and collision bounds
        EntityQuery query = GetEntityQuery(
          ComponentType.ReadOnly<Translation>(),
          ComponentType.ReadOnly<Unit>(),
          ComponentType.ReadOnly<GridID>(),
          ComponentType.ReadWrite<MovementSpeedComponent>(),
          ComponentType.ReadOnly<AnimationComponent>()
        );

        int count = query.CalculateEntityCount();

        // Collect positions and collision bounds in NativeArrays for collision detection
        NativeArray<Translation> positions = new NativeArray<Translation>(count, Unity.Collections.Allocator.TempJob);
        positions = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<float3> updatedPositions = new NativeArray<float3>(count, Unity.Collections.Allocator.TempJob); // To store updated positions

        Entities
          .ForEach((ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, ref TargetPositionComponent targetLocationComponent) =>
          {
              //float3 targetPosition = targetLocationComponent.targetPosition;
              //if (movementSpeedComponent.isBlocked == false)
              //{
              //    float3 direction = targetPosition - translation.Value;
              //    direction.z = 0;
              //    if (math.length(direction) < 0.1f)
              //    {
              //        movementSpeedComponent.direction = float3.zero;
              //        movementSpeedComponent.Value = float3.zero;

              //        // Increment frame counter for the unit
              //        movementSpeedComponent.frameCounter++;

              //        // If we've waited for 2-3 frames, move the unit and update target position
              //        if (movementSpeedComponent.frameCounter >= 300) // Wait for 3 frames (or you can use 2)
              //        {
              //            // Move the unit by changing the target position
              //            float newX = translation.Value.x;
              //            newX -= 50f;
              //            targetPosition = new float3(newX, translation.Value.y, 0f);
              //            targetLocationComponent.targetPosition = targetPosition;

              //            // Reset the frame counter after moving
              //            movementSpeedComponent.frameCounter = 0;

              //            // Recalculate direction after updating target position
              //            direction = targetPosition - translation.Value;
              //        }
              //    }
              //    else
              //    {
              //        direction = math.normalize(direction);
              //        movementSpeedComponent.direction = direction;
              //    }

              //    movementSpeedComponent.moveX = direction.x;
              //    movementSpeedComponent.moveY = direction.y;
              //}

              float3 direction = float3.zero;
              movementSpeedComponent.direction = direction;
              movementSpeedComponent.Value = float3.zero;
              movementSpeedComponent.moveX = direction.x;
              movementSpeedComponent.moveY = direction.y;

              movementSpeedComponent.isRunnning = true;
          }).WithBurst().ScheduleParallel();

        // Randomize movement speed
        float minRange = 1f;
        float maxRange = 1.125f;
        Entities
          .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity, in Entity entity) =>
          {
              if (velocity.isRunnning)
              {
                  Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);
                  velocity.randomSpeed = random.NextFloat(minRange, maxRange);
              }
              else
              {
                  //Unity.Mathematics.Random random2 = new Unity.Mathematics.Random((uint)entity.Index);
                  //velocity.randomSpeed = random2.NextFloat(.5f, .6f);
                  velocity.randomSpeed = .5f;
              }

              float3 vel = new float3(velocity.moveX, velocity.moveY, 0) * velocity.randomSpeed;
              vel.z = 0;
              velocity.Value = vel;
          }).WithBurst().ScheduleParallel();
        NativeArray<MovementSpeedComponent> velocities = new NativeArray<MovementSpeedComponent>(count, Unity.Collections.Allocator.TempJob); // To store updated positions
        velocities = query.ToComponentDataArray<MovementSpeedComponent>(Allocator.TempJob);

        // Get direction for animation
        Entities
          .ForEach((ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
          {
              if (velocity.Value.x > 0)
              {
                  animationComponent.direction = EntitySpawner.Direction.Right;
                  animationComponent.animationWidthOffset = 1;
              }
              else if (velocity.Value.x < 0)
              {
                  animationComponent.direction = EntitySpawner.Direction.Left;
                  animationComponent.animationWidthOffset = 2;
              }
              else if (velocity.Value.y > 0)
              {
                  animationComponent.direction = EntitySpawner.Direction.Up;
                  animationComponent.animationWidthOffset = 3;
              }
              else if (velocity.Value.y < 0)
              {
                  animationComponent.direction = EntitySpawner.Direction.Down;
                  animationComponent.animationWidthOffset = 4;
              }
              else
              {
                  if (!animationComponent.finishAnimation)
                  {
                      animationComponent.direction = animationComponent.prevDirection;
                  }
              }
              animationComponent.prevDirection = animationComponent.direction;
          }).WithBurst().ScheduleParallel();

        NativeArray<AnimationComponent> anims = new NativeArray<AnimationComponent>(count, Unity.Collections.Allocator.TempJob); // To store updated positions
        anims = query.ToComponentDataArray<AnimationComponent>(Allocator.TempJob);

        NativeArray<GridID> grids = new NativeArray<GridID>(count, Unity.Collections.Allocator.TempJob); // To store updated positions
        grids = query.ToComponentDataArray<GridID>(Allocator.TempJob);



        // Apply the updated positions to the entities
        //Entities
        //  .ForEach((int entityInQueryIndex, ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent movementSpeedComponent, ref Unit unit, ref CollisionBounds collisionBounds, ref AnimationComponent animationComponent) =>
        //  {
        //      // Apply the updated position from the collision job
        //      position.value = updatedPositions[entityInQueryIndex]; // Just use index here as a placeholder for actual entity lookup
        //      //translation.Value = position.value;
        //  }).WithBurst().ScheduleParallel();

        // Complete and dispose of NativeArrays
        Dependency.Complete(); // Ensure job is completed before disposal
        positions.Dispose();
        //radii.Dispose();
        updatedPositions.Dispose();
        velocities.Dispose();
        anims.Dispose();
        grids.Dispose();
    }

}