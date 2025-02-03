using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(CombatSystem))]
[UpdateAfter(typeof(UnitCollisionSystem))]
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

        //update movement x and y
        Entities
        .WithAll<CommanderComponent>()// remove to let all units update
        .ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
        {
            movementSpeedComponent.moveX = moveX;
            movementSpeedComponent.moveY = moveY;
            movementSpeedComponent.isRunnning = isRunnning;
        }).ScheduleParallel();


        //move all units?
        Entities
        .ForEach((ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, ref TargetPositionComponent targetLocationComponent) =>
        {
            float3 targetPosition = targetLocationComponent.targetPosition;
            if (!movementSpeedComponent.isBlocked)
            {
                float3 direction = targetPosition - translation.Value;
                direction.z = 0;
                if (math.length(direction) < 0.1f)
                {
                    float newX = translation.Value.x;
                    newX -= 50f;
                    targetPosition = new float3(newX, translation.Value.y, 0f);
                    targetLocationComponent.targetPosition = targetPosition;
                    direction = targetPosition - translation.Value;
                    //direction = float3.zero;
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


        // Movement speed randomizer
        float minRange = 1f;
        float maxRange = 1.25f;
        Entities
            .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity, in Entity entity) =>
            {
                //if (velocity.randomSpeed == 0f)
                //{
                // Create a random generator, seeded by the entity's index
                if (velocity.isRunnning)
                {
                    // Generate a random float between 1f and 1.25f
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);
                    velocity.randomSpeed = random.NextFloat(minRange, maxRange);
                }
                else
                {
                    Unity.Mathematics.Random random2 = new Unity.Mathematics.Random((uint)entity.Index);
                    velocity.randomSpeed = random2.NextFloat(.5f, .6f);
                    //velocity.randomSpeed = entitySpawner.movementSpeedDebug;
                }

                //}

                float3 vel = (new float3(velocity.moveX, velocity.moveY, 0) * velocity.randomSpeed);
                vel.z = 0;
                velocity.value = vel;
            }).WithBurst().ScheduleParallel();

        //get direction
        Entities
            .ForEach((ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
            {
                if (velocity.value.x > 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Right;
                }
                else if (velocity.value.x < 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Left;
                }
                else if (velocity.value.y > 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Up;
                }
                else if (velocity.value.y < 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Down;
                }
                else
                {
                    // In case of the entity being at the origin (0, 0)
                    if (!animationComponent.finishAnimation)
                    {
                        animationComponent.direction = animationComponent.prevDirection; // Or some default direction 
                    }
                }
                animationComponent.prevDirection = animationComponent.direction;

            }).WithBurst().ScheduleParallel();






        //actual movement system
        Entities
        .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent movementSpeedComponent, ref AnimationComponent animationComponent) =>
        {
            //stop moving on attack
            if (animationComponent.animationType == EntitySpawner.AnimationType.Run || animationComponent.animationType == EntitySpawner.AnimationType.Walk)
            {

                // If the unit is blocked, stop its movement
                if (movementSpeedComponent.isBlocked)
                {
                    return; // Do not move this unit this frame
                }

                // Update position based on velocity (movement calculations)
                position.value += movementSpeedComponent.value * deltaTime;

                // Apply the new position to the entity's translation (moving in the scene)
                translation.Value = position.value;
            }
        }).WithBurst().ScheduleParallel();
        //}).WithoutBurst().Run();

    }
}


public struct Unit : IComponentData
{
    public bool isMounted;  // Flag to indicate if this unit is mounted (e.g., cavalry)
}

public struct CollisionBounds : IComponentData
{
    public float radius;  // The radius for collision checks
}


[UpdateAfter(typeof(CombatSystem))]
[BurstCompile]
public partial class UnitCollisionSystem : SystemBase
{
    // Declare the EntityQuery to get all units
    private EntityQuery unitQuery;

    // Declare an EntityCommandBufferSystem to manage changes to components
    private EntityCommandBufferSystem ecbSystem;

    // Initialize the EntityQuery and EntityCommandBufferSystem in OnCreate
    protected override void OnCreate()
    {
        unitQuery = GetEntityQuery(
            ComponentType.ReadWrite<Translation>(),  // Fetching the position of each unit
            ComponentType.ReadOnly<Unit>(),         // Ensuring we're dealing with units
            ComponentType.ReadOnly<CollisionBounds>(), // Adding collision bounds for each unit
            ComponentType.ReadWrite<PositionComponent>(), // Position data for units
            ComponentType.ReadWrite<MovementSpeedComponent>() // Position data for units
        );

        ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }

    // The main update method where the logic happens
    protected override void OnUpdate()
    {
        // Step 1: Create NativeArrays to store all the units and their positions
        NativeArray<Translation> unitPositions = unitQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Entity> unitEntities = unitQuery.ToEntityArray(Allocator.TempJob); // Get entities for reference comparison.
        NativeArray<CollisionBounds> collisionBounds = unitQuery.ToComponentDataArray<CollisionBounds>(Allocator.TempJob); // Get entities for reference comparison.
        NativeArray<PositionComponent> positionComponents = unitQuery.ToComponentDataArray<PositionComponent>(Allocator.TempJob); // Get entities for reference comparison.
        NativeArray<Unit> unitData = unitQuery.ToComponentDataArray<Unit>(Allocator.TempJob); // Get entities for reference comparison.
        NativeArray<MovementSpeedComponent> movementData = unitQuery.ToComponentDataArray<MovementSpeedComponent>(Allocator.TempJob); // Get entities for reference comparison.

        // Create a job to handle the collision logic
        var job = new UnitCollisionJob
        {
            unitPositions = unitPositions,
            unitEntities = unitEntities,
            collisionBounds = collisionBounds,
            unitData = unitData,
            movementData = movementData,
            positionComponents = positionComponents,
            ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter(),
            deltaTime = Time.DeltaTime // Passing time to the job if needed
        };

        // Step 2: Schedule the job
        JobHandle jobHandle = job.Schedule(unitQuery.CalculateEntityCount(), 256, Dependency);

        // Step 3: Ensure that the job finishes before disposing of the arrays
        jobHandle.Complete();

        // Step 4: Dispose of the NativeArrays after the job is done
        unitPositions.Dispose();
        unitEntities.Dispose();

        // Step 5: Play back changes from the EntityCommandBuffer
        ecbSystem.AddJobHandleForProducer(jobHandle);
    }

    // Job to process unit collisions
    [BurstCompile]
    struct UnitCollisionJob : IJobParallelFor
    {
        // Input data
        [NativeDisableParallelForRestriction]
        public NativeArray<Translation> unitPositions;
        [ReadOnly] public NativeArray<Entity> unitEntities;
        [NativeDisableParallelForRestriction]
        public NativeArray<PositionComponent> positionComponents;
        [ReadOnly] public NativeArray<CollisionBounds> collisionBounds;
        [ReadOnly] public NativeArray<Unit> unitData;
        [NativeDisableParallelForRestriction]
        public NativeArray<MovementSpeedComponent> movementData;
        public EntityCommandBuffer.ParallelWriter ecb; // To handle component changes in parallel
        public float deltaTime;

        // Collision handling logic
        public void Execute(int index)
        {
            if (index >= unitPositions.Length || index >= unitEntities.Length || index >= positionComponents.Length)
            {
                Debug.LogError($"Invalid index {index}. unitPositions.Length: {unitPositions.Length}, unitEntities.Length: {unitEntities.Length}, positionComponents.Length: {positionComponents.Length}");
                return;
            }

            // Access the current unit
            var currentUnitTranslation = unitPositions[index];
            var currentEntity = unitEntities[index];
            var positionComponent = positionComponents[index];
            var unit = unitData[index];
            var collisionBound = collisionBounds[index];
            var movementSpeedComponent = movementData[index];

            // Compare the current unit against all other units
            for (int i = 0; i < unitPositions.Length; i++)
            {
                // Skip comparing the unit against itself
                if (unitEntities[i] == currentEntity)
                    continue;

                // Get the translation for the other unit
                var otherUnitTranslation = unitPositions[i];

                // Check X and Y distance first to quickly eliminate unnecessary distance calculations
                if (math.abs(currentUnitTranslation.Value.x - otherUnitTranslation.Value.x) > collisionBound.radius * 2f)
                    continue;  // Skip if units are too far apart on X axis
                if (math.abs(currentUnitTranslation.Value.y - otherUnitTranslation.Value.y) > collisionBound.radius * 2f)
                    continue;  // Skip if units are too far apart on Y axis
                // Calculate the distance between the units
                float distance = math.distance(currentUnitTranslation.Value, otherUnitTranslation.Value);

                // If the units are within collision range, handle the collision
                if (distance < collisionBound.radius) // Example collision radius check
                {
                    // Handle collision logic here...
                    HandleCollision(unit, ref movementSpeedComponent, ref positionComponent, ref currentUnitTranslation, unitPositions[i], collisionBound, ecb, index);
                }
            }
        }

        // Handle the collision logic to stop movement on collision
        public void HandleCollision(Unit unit, ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, Translation otherTranslation, CollisionBounds collisionBounds, EntityCommandBuffer.ParallelWriter ecb, int index)
        {

            float3 direction = translation.Value - otherTranslation.Value;
            float distance = math.length(direction);

            // Normalize the direction to get the unit vector
            direction = math.normalize(direction);

            // Check if the units are colliding (distance is too small)
            if (distance < collisionBounds.radius)
            {
                // **Push the unit away from the other unit (without blocking its movement)**
                float overlap = (collisionBounds.radius * 2) - distance; // Total overlap (if negative, no overlap)
                if (overlap > 0f)
                {

                    // Apply push-back logic
                    float pushBackStrength = unit.isMounted ? 1.5f : 0.05f;  // Stronger pushback for mounted units, softer for regular units

                    // Apply the push-back by scaling the direction
                    translation.Value += direction * overlap * pushBackStrength;
                    position.value = translation.Value; // Update the position component

                    // Allow the unit to move around the obstacle (i.e., stop blocking its movement)
                    movementSpeedComponent.isBlocked = false;
                }
            }
            else
            {
                // If no collision, allow movement freely
                movementSpeedComponent.isBlocked = false;
            }
            ecb.SetComponent(index, unitEntities[index], movementSpeedComponent); // Set isBlocked to true
            ecb.SetComponent(index, unitEntities[index], translation); // Update Translation
            ecb.SetComponent(index, unitEntities[index], position); // Update PositionComponent
        }
    }
}
