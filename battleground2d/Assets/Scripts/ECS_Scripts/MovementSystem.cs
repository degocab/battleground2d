using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
//[UpdateBefore(typeof(UnitCollisionSystem))]
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
        //NativeArray<float> radii = new NativeArray<float>(count, Unity.Collections.Allocator.TempJob);
        NativeArray<float3> updatedPositions = new NativeArray<float3>(count, Unity.Collections.Allocator.TempJob); // To store updated positions

        //var collectJob = new CollectPositionsJob
        //{
        //    positions = positions
        //    //radii = radii
        //};

        //// Schedule the job to collect positions and radii in parallel
        //JobHandle collectJobHandle = collectJob.Schedule(count, 64); // Use a batch size for parallel processing
        //collectJobHandle.Complete();


        Entities
.ForEach((ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, ref TargetPositionComponent targetLocationComponent) =>
{
    float3 targetPosition = targetLocationComponent.targetPosition;
    if (movementSpeedComponent.isBlocked == false)
    {
        float3 direction = targetPosition - translation.Value;
        direction.z = 0;
        if (math.length(direction) < 0.1f)
        {
            //float newX = translation.Value.x;
            //newX -= 50f;
            //targetPosition = new float3(newX, translation.Value.y, 0f);
            //targetLocationComponent.targetPosition = targetPosition;
            //direction = targetPosition - translation.Value;
            ////direction = float3.zero;
            ///
            movementSpeedComponent.direction = float3.zero;
            movementSpeedComponent.value = float3.zero;

            // Increment frame counter for the unit
            movementSpeedComponent.frameCounter++;

            // If we've waited for 2-3 frames, move the unit and update target position
            if (movementSpeedComponent.frameCounter >= 300)  // Wait for 3 frames (or you can use 2)
            {
                // Move the unit by changing the target position
                float newX = translation.Value.x;
                newX -= 50f;
                targetPosition = new float3(newX, translation.Value.y, 0f);
                targetLocationComponent.targetPosition = targetPosition;

                // Reset the frame counter after moving
                movementSpeedComponent.frameCounter = 0;

                // Recalculate direction after updating target position
                direction = targetPosition - translation.Value;
            }
        }
        else
        {
            direction = math.normalize(direction);
            movementSpeedComponent.direction = direction;
        }


        //movementSpeedComponent.normalizedDirection = math.normalize(direction);
        movementSpeedComponent.moveX = direction.x;
        movementSpeedComponent.moveY = direction.y;
    }
    //movementSpeedComponent.isRunnning = isRunnning;
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
                velocity.value = vel;
            }).WithBurst().ScheduleParallel();
        NativeArray<MovementSpeedComponent> velocities = new NativeArray<MovementSpeedComponent>(count, Unity.Collections.Allocator.TempJob); // To store updated positions
        velocities = query.ToComponentDataArray<MovementSpeedComponent>(Allocator.TempJob);




        // Get direction for animation
        Entities
            .ForEach((ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
            {
                if (velocity.value.x > 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Right;
                    animationComponent.animationWidthOffset = 1;
                }
                else if (velocity.value.x < 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Left;
                    animationComponent.animationWidthOffset = 2;
                }
                else if (velocity.value.y > 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Up;
                    animationComponent.animationWidthOffset = 3;
                }
                else if (velocity.value.y < 0)
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

        // Actual movement and collision check
        var collisionJob = new CollisionCheckJob
        {
            positions = positions,
            grids = grids,
            updatedPositions = updatedPositions, // Passing the array to store updated positions
            velocities = velocities,
            anims = anims,
            deltaTime = deltaTime
        };

        JobHandle collisionJobHandle = collisionJob.Schedule(count, 64);
        collisionJobHandle.Complete();

        // Apply the updated positions to the entities
        Entities
            .ForEach((int entityInQueryIndex, ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent movementSpeedComponent, ref Unit unit, ref CollisionBounds collisionBounds, ref AnimationComponent animationComponent) =>
            {
                // Apply the updated position from the collision job
                position.value = updatedPositions[entityInQueryIndex]; // Just use index here as a placeholder for actual entity lookup
                translation.Value = position.value;
            }).WithBurst().ScheduleParallel();

        // Complete and dispose of NativeArrays
        Dependency.Complete(); // Ensure job is completed before disposal
        positions.Dispose();
        //radii.Dispose();
        updatedPositions.Dispose();
        velocities.Dispose();
        anims.Dispose();
        grids.Dispose();
    }

    // Job for collecting positions and radii
    [BurstCompile]
    struct CollectPositionsJob : IJobParallelFor
    {
        public NativeArray<Translation> positions;
        public NativeArray<float> radii;

        public void Execute(int index)
        {
            // Access the position and collision bounds for each entity
            // This will need to be scheduled outside of the main system as a job
            // Assign values to the NativeArrays
            //positions[index] = position; // Replace with actual data
            //radii[index] = .125f; // Replace with actual data
        }
    }




    [BurstCompile]
    struct CollisionCheckJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Translation> positions;
        public NativeArray<float3> updatedPositions; // New NativeArray to store updated positions
        public float deltaTime;
        public NativeArray<MovementSpeedComponent> velocities;
        public NativeArray<AnimationComponent> anims;
        [ReadOnly]
        public NativeArray<GridID> grids;

        public void Execute(int index)
        {
            // Perform the collision check for each entity here
            float3 position = positions[index].Value;
            bool isColliding = false;
            bool isSurrounded = true;  // Assume it's surrounded until proven otherwise
            MovementSpeedComponent movementSpeedComponent = velocities[index];
            AnimationComponent anim = anims[index];
            GridID grid = grids[index];
            float3 collidingPosition = position;
            float dist = 0;
            float boundingBoxSize = 0.125f; // You can adjust this based on unit size


            bool up = false;
            bool down = false;
            bool left = false;
            bool right = false;
            bool upRight = false;
            bool downRight = false;
            bool upLeft = false;
            bool downLeft = false;




            // Define all the directions to check: up, down, left, right, and the diagonals
            NativeArray<float3> directions = new NativeArray<float3>(8, Allocator.Temp); // Using NativeArray

            // Define all the directions to check: up, down, left, right, and the diagonals
            directions[0] = new float3(0, .125f, 0);  // Up
            directions[1] = new float3(0, -.125f, 0); // Down
            directions[2] = new float3(-.125f, 0, 0); // Left
            directions[3] = new float3(.125f, 0, 0);  // Right
            directions[4] = new float3(.125f, .125f, 0);  // Up-right
            directions[5] = new float3(.125f, -.125f, 0); // Down-right
            directions[6] = new float3(-.125f, .125f, 0); // Up-left
            directions[7] = new float3(-.125f, -.125f, 0); // Down-left

            // Loop through the other positions to detect collision
            for (int i = 0; i < positions.Length; i++)
            {

                if (index != i) // Skip self collision
                {
                    GridID gridToCheck = grids[index];
                    if (grid.value != gridToCheck.value)
                    {
                        continue;  // Skip if entities are too far apart (early exit)
                    }

                    collidingPosition = positions[i].Value;

                    // Perform early exit with bounding box check
                    dist = math.distancesq(position, collidingPosition);
                    if (dist > math.pow(boundingBoxSize * 2, 2))
                    {
                        continue;  // Skip if entities are too far apart (early exit)
                    }

                    if (dist < .12f)
                    {
                        // Check if the colliding unit is in one of the 8 surrounding directions
                        for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
                        {
                            float3 direction = directions[dirIndex];
                            float3 directionToCheck = position + direction;

                            // Check if the other unit is in this direction
                            if (math.distancesq(directionToCheck, collidingPosition) < math.pow(boundingBoxSize, 2))
                            {
                                // Mark this direction as blocked (this could represent collision)
                                isSurrounded &= false;  // If we find a collision in one of the 8 directions, we don't assume it's surrounded
                                isColliding = true;

                                if(dirIndex == 0) up = true;
                                if(dirIndex == 1) down = true;
                                if(dirIndex == 2) left = true;
                                if(dirIndex == 3) right = true;
                                if(dirIndex == 4) upRight = true;
                                if(dirIndex == 5) downRight = true;
                                if(dirIndex == 6) upLeft = true;
                                if(dirIndex == 7) downLeft = true;
                            }
                        }
                    }
                }
            }

            float3 movementDirection = movementSpeedComponent.value;

            // Handle collision logic (e.g., stop movement, adjust velocity)
            if (isSurrounded)
            {
                // If surrounded, stop movement (set velocity to zero)
                movementSpeedComponent.value = float3.zero;
            }
            else if (isColliding)
            {

                //where is the collision,
                if (up) //up
                {
                    movementDirection.y = -.125f;
                }
                if (down) //Down
                {
                    movementDirection.y = .125f;
                }
                if (left) //Left
                {
                    movementDirection.x = .125f;
                }
                if (right) //Right
                {
                    movementDirection.x = -.125f;
                }
                if (upRight) //Up-right
                {
                    movementDirection.y = -.125f;
                    movementDirection.x = -.125f;
                }
                if (downRight) //Down-right
                {
                    movementDirection.y = .125f;
                    movementDirection.x = -.125f;
                }
                if (upLeft) //Up-left
                {
                    movementDirection.y = -.125f;
                    movementDirection.x = .125f;
                }
                if (downLeft) //Down-left
                {
                    movementDirection.y = .125f;
                    movementDirection.x = .125f;
                }


            }
            else
            {
                // No collision, proceed with normal movement
            }
            movementDirection.z = 0;
            updatedPositions[index] = position + movementDirection * deltaTime;
            // Dispose the NativeArray after usage
            directions.Dispose();
        }
    }



    // Job for collision check
    [BurstCompile]
    struct CollisionCheckJob2 : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Translation> positions;
        //public NativeArray<float> radii;
        public NativeArray<float3> updatedPositions; // New NativeArray to store updated positions
        public float deltaTime;
        public NativeArray<MovementSpeedComponent> velocities;
        public NativeArray<AnimationComponent> anims;

        public void Execute(int index)
        {
            // Perform the collision check for each entity here
            float3 position = positions[index].Value;
            //float radius = radii[index];
            bool isColliding = false;
            MovementSpeedComponent movementSpeedComponent = velocities[index];
            AnimationComponent anim = anims[index];
            float3 collidingPosition = position;
            float dist = 0;
            float boundingBoxSize = 0.125f; // You can adjust this based on unit size

            // Loop through the other positions to detect collision
            for (int i = 0; i < positions.Length; i++)
            {

                collidingPosition = positions[i].Value;
                if (index != i) // Skip self collision
                {
                    // Perform early exit with bounding box check
                    if (math.distancesq(position, collidingPosition) > math.pow(boundingBoxSize * 2, 2))
                    {
                        continue;  // Skip if entities are too far apart (early exit)
                    }

                    dist = math.distancesq(position, collidingPosition);

                    if (dist < .2f)
                    {

                        // Define all the directions to check: up, down, left, right, and the diagonals
                        float3[] directions = new float3[]
                        {
                    new float3(0, 1, 0),  // Up
                    new float3(0, -1, 0), // Down
                    new float3(-1, 0, 0), // Left
                    new float3(1, 0, 0),  // Right
                    new float3(1, 1, 0),  // Up-right
                    new float3(1, -1, 0), // Down-right
                    new float3(-1, 1, 0), // Up-left
                    new float3(-1, -1, 0) // Down-left
                        };

                        isColliding = true;
                        break;
                    }
                }
            }
            float3 movementDirection = movementSpeedComponent.value;

            // Handle collision logic (e.g., stop movement, adjust velocity)
            if (isColliding)
            {

                // Calculate the direction of the collision (normalized vector)
                float3 collisionDirection = math.normalize(position - collidingPosition);

                // Get the velocity vector (movement direction)
                // If movement is primarily in the X direction (left/right)
                if (math.abs(collisionDirection.x) > math.abs(collisionDirection.y))
                {
                    // If moving right (positive X), allow movement left (negative X)
                    if (anim.direction == EntitySpawner.Direction.Right) // Moving right
                    {
                        movementDirection.x = -.125f;  // Stop movement in the right direction
                    }
                    // If moving left (negative X), allow movement right (positive X)
                    else if (anim.direction == EntitySpawner.Direction.Left) // Moving left
                    {
                        movementDirection.x = .125f;  // Stop movement in the left direction
                    }
                    else
                    {
                        //// No collision, move the entity based on velocity
                    }
                }
                // If movement is primarily in the Y direction (up/down)
                else if (math.abs(collisionDirection.y) > math.abs(collisionDirection.x))
                {
                    // If moving up (positive Y), allow movement down (negative Y)
                    if (anim.direction == EntitySpawner.Direction.Up) // Moving up
                    {
                        movementDirection.y = -.125f;  // Stop movement in the upward direction
                    }
                    // If moving down (negative Y), allow movement up (positive Y)
                    else if (anim.direction == EntitySpawner.Direction.Down) // Moving down
                    {
                        movementDirection.y = .125f;  // Stop movement in the downward direction
                    }
                    else
                    {
                        //// No collision, move the entity based on velocity
                    }
                }
                else
                {
                    // If moving up (positive Y), allow movement down (negative Y)
                    if (anim.direction == EntitySpawner.Direction.Up) // Moving up
                    {
                        movementDirection.y = -.125f;  // Stop movement in the upward direction

                        if (movementDirection.x > 0)
                            movementDirection.x = -.125f;  // Stop movement in the upward direction
                        else
                            movementDirection.x = .125f;  // Stop movement in the upward direction
                    }
                    // If moving down (negative Y), allow movement up (positive Y)
                    else if (anim.direction == EntitySpawner.Direction.Down) // Moving down
                    {
                        movementDirection.y = .125f;  // Stop movement in the downward direction
                        if (movementDirection.x > 0)
                            movementDirection.x = -.125f;  // Stop movement in the upward direction
                        else
                            movementDirection.x = .125f;  // Stop movement in the downward direction
                    }
                    else if (anim.direction == EntitySpawner.Direction.Right) // Moving right
                    {
                        movementDirection.x = -.125f;  // Stop movement in the right direction
                                                      // Stop movement in the right direction
                        if (movementDirection.x > 0)
                            movementDirection.y = -.125f;  // Stop movement in the upward direction
                        else
                            movementDirection.y = .125f;  // Stop movement in the downward direction
                    }
                    // If moving left (negative X), allow movement right (positive X)
                    else //if (anim.direction == EntitySpawner.Direction.Left) // Moving left
                    {
                        movementDirection.x = .125f;  // Stop movement in the left direction
                                                     // Stop movement in the left direction
                        if (movementDirection.x > 0)
                            movementDirection.y = -.125f;  // Stop movement in the upward direction
                        else
                            movementDirection.y = .125f; // Stop movement in the downward directi
                    }
                }

                // Apply the adjusted movement direction (which now avoids the collision direction)
                //updatedPositions[index] = position + movementDirection * deltaTime;
            }
            //else
            //{
            //    // No collision, move the entity based on velocity
            //    float3 newPosition = position + movementDirection * deltaTime; // Normal movement
            //    updatedPositions[index] = newPosition; // Update to new position
            //}



            updatedPositions[index] = position + movementDirection * deltaTime;
        }
    }
}
