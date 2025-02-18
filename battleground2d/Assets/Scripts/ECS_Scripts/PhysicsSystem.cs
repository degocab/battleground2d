using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
[BurstCompile]
[DisableAutoCreation]
public class PhysicsSystem : SystemBase
{

    private EntityQuery unitQuery;
    private EntityCommandBufferSystem ecbSystem;

    // Reference to the GridSystem for getting nearby units
    private GridSystem gridSystem;

    protected override void OnCreate()
    {
        // Initialize the grid system


        // Define the query to get all units
        unitQuery = GetEntityQuery(
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadWrite<PhysicsPosition>(),
            ComponentType.ReadOnly<GridID>(),
            ComponentType.ReadOnly<PhysicsRadius>()
        );

        ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }


    // OnUpdate is the main system entry point for each frame
    protected override void OnUpdate()
    {



        // Get DeltaTime for frame updates
        float deltaTime = Time.DeltaTime;


        // Update physics (velocity, position) of all entities
        Entities.ForEach((ref Translation translation, ref PhysicsPosition position, ref PhysicsVelocity velocity, ref PhysicsForce force, in PhysicsRadius radius) =>
        {
            // Apply force to update velocity (F = ma -> v = v0 + a * t)
            velocity.Value += force.Value * deltaTime;
            velocity.Value.z = 0;
            // Update position based on velocity (p = p0 + vt)
            position.Value += velocity.Value * deltaTime;
            position.Value.z = 0;
            translation.Value += position.Value;
            // Reset force after applying it to prevent it from accumulating
            force.Value = float3.zero;
            

        }).ScheduleParallel();




        NativeArray<Entity> unitEntities = unitQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<PhysicsPosition> positions = unitQuery.ToComponentDataArray<PhysicsPosition>(Allocator.TempJob);
        NativeArray<PhysicsRadius> radii = unitQuery.ToComponentDataArray<PhysicsRadius>(Allocator.TempJob);
        NativeArray<Translation> translations = unitQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<GridID> gridData = unitQuery.ToComponentDataArray<GridID>(Allocator.TempJob);


        // Collect all entities with PhysicsPosition and PhysicsRadius components
        //NativeArray<Entity> entities = GetEntityQuery(typeof(PhysicsPosition), typeof(PhysicsRadius)).ToEntityArray(Allocator.TempJob);
        //NativeArray<PhysicsPosition> positions = GetComponentDataFromEntity<PhysicsPosition>(true); // Read-only
        //NativeArray<PhysicsRadius> radii = GetComponentDataFromEntity<PhysicsRadius>(true); // Read-only
        //NativeArray<Translation> translations = GetComponentDataFromEntity<Translation>();// Read-only

        //// Create a job to update the physics for each entity
        //Entities.ForEach((ref PositionComponent position, ref PhysicsVelocity velocity, ref PhysicsForce force, in PhysicsRadius radius) =>
        //{
        //    // Apply force to update velocity (F = ma -> v = v0 + a * t)
        //    velocity.Value += force.Value * deltaTime;

        //    // Update position based on velocity (p = p0 + vt)
        //    position.value += velocity.Value * deltaTime;

        //    // Reset force after applying it to prevent it from accumulating
        //    force.Value = float3.zero;

        //}).ScheduleParallel();

        //// Optionally handle collisions after positions and velocities are updated
        //HandleCollisions(entities, positions, radii, translations);

        var job = new UnitCollisionJob
        {
            positions = positions,
            unitEntities = unitEntities,
            radii = radii,
            gridData = gridData,
            translations = translations,
            ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter(),

            deltaTime = Time.DeltaTime
        };

        // Schedule the job
        JobHandle jobHandle = job.Schedule(unitQuery.CalculateEntityCount(), 256, Dependency);
        jobHandle.Complete();

        // Dispose of the NativeArrays after job completion
        unitEntities.Dispose();
        positions.Dispose();
        radii.Dispose();
        translations.Dispose();

        ecbSystem.AddJobHandleForProducer(jobHandle);
    }
}

// Handle entity collisions
//private void HandleCollisions(NativeArray<Entity> entities, ComponentDataFromEntity<PhysicsPosition> positions, ComponentDataFromEntity<PhysicsRadius> radii, ComponentDataFromEntity<Translation> translations)
//{
//    // Process each entity for collisions with every other entity
//    for (int i = 0; i < entities.Length; i++)
//    {
//        var entityA = entities[i];
//        var positionA = positions[entityA];
//        var radiusA = radii[entityA];

//        for (int j = i + 1; j < entities.Length; j++) // Avoid checking the same pairs twice
//        {
//            var entityB = entities[j];
//            var positionB = positions[entityB];
//            var radiusB = radii[entityB];

//            float distance = math.distance(positionA.Value, positionB.Value);
//            if (distance < radiusA.Value + radiusB.Value)
//            {
//                // Handle collision between entity A and entity B
//                float overlap = (radiusA.Value + radiusB.Value) - distance;
//                float3 direction = math.normalize(positionB.Value - positionA.Value);
//                float3 separation = direction * overlap;

//                // Apply the separation to both entities
//                positions[entityA] = new PhysicsPosition { Value = positionA.Value - separation * 0.5f };
//                positions[entityB] = new PhysicsPosition { Value = positionB.Value + separation * 0.5f };
//                // Update the actual positions in Translation components (apply after physics update)
//                translations[entityA] = new Translation { Value = positions[entityA].Value };
//                translations[entityB] = new Translation { Value = positions[entityB].Value };
//            }
//        }
//    }

//    // Dispose the arrays after use
//    entities.Dispose();
//}
[BurstCompile]
struct UnitCollisionJob : IJobParallelFor
{
    // Input data
    [NativeDisableParallelForRestriction]
    public NativeArray<PhysicsPosition> positions;
    [ReadOnly] public NativeArray<Entity> unitEntities;
    [ReadOnly] public NativeArray<PhysicsRadius> radii;
    [ReadOnly] public NativeArray<GridID> gridData;
    [NativeDisableParallelForRestriction]
    public NativeArray<Translation> translations;
    public EntityCommandBuffer.ParallelWriter ecb;

    public float deltaTime;

    public void Execute(int index)
    {
        //if (index >= positions.Length || index >= unitEntities.Length || index >= positionComponents.Length)
        //{
        //    return;
        //}

        //var currentUnitTranslation = positions[index];
        //var currentEntity = unitEntities[index];
        //var collisionBound = collisionBounds[index];
        //var movementSpeedComponent = movementData[index];
        //var currentGridID = gridData[index].value; // Get the grid_id for the current unit


        // Process each entity for collisions with every other entity

        if (index >= positions.Length || index >= unitEntities.Length || index >= positions.Length)
        {
            return;
        }
        var positionA = positions[index];
        var radiusA = radii[index];
        var currentGridID = gridData[index].value; // Get the grid_id for the current unit
        PhysicsPosition phyiscsToApplyOnIndex = positions[index]; // Get the grid_id for the current unit
        Translation translationToApplyOnIndex = translations[index]; // Get the grid_id for the current unit

        for (int otherIndex = 0; otherIndex < unitEntities.Length; otherIndex++)
        {
            if (otherIndex == index) continue; // Skip comparing the unit against itself
            var otherGridID = gridData[otherIndex].value; // Get the grid_id for the other unit
                                                          // Skip if the grid IDs don't match
            if (currentGridID != otherGridID)
            {
                continue; // Skip the collision check if grid IDs don't match
            }
            PhysicsPosition phyiscsToApplyOnOtheIndex = positions[otherIndex]; // Get the grid_id for the current unit
            Translation translationToApplyOnOtheIndex = translations[otherIndex]; // Get the grid_id for the current unit

            var positionB = positions[otherIndex];
            var radiusB = radii[otherIndex];

            float distance = math.distance(positionA.Value, positionB.Value);
            if (distance < radiusA.Value + radiusB.Value)
            {
                // Handle collision between entity A and entity B
                float overlap = (radiusA.Value + radiusB.Value) - distance;
                float3 direction = math.normalize(positionB.Value - positionA.Value);
                float3 separation = direction * overlap;

                phyiscsToApplyOnIndex = new PhysicsPosition { Value = positionA.Value - separation * 0.5f };
                phyiscsToApplyOnOtheIndex = new PhysicsPosition { Value = positionB.Value + separation * 0.5f };

                //// Apply the separation to both entities
                //positions[index] = new PhysicsPosition { Value = positionA.Value - separation * 0.5f };
                //positions[otherIndex] = new PhysicsPosition { Value = positionB.Value + separation * 0.5f };

                // Update the actual positions in Translation components (apply after physics update)
                //translations[index] = new Translation { Value = positions[index].Value };
                //translations[otherIndex] = new Translation { Value = positions[otherIndex].Value };
                ecb.SetComponent(index, unitEntities[index], phyiscsToApplyOnIndex);
                ecb.SetComponent(otherIndex, unitEntities[otherIndex], phyiscsToApplyOnOtheIndex);

                //ecb.SetComponent(index, unitEntities[index], translationToApplyOnIndex);
                //ecb.SetComponent(otherIndex, unitEntities[otherIndex], translationToApplyOnOtheIndex);
            }
        }



        // Get the grid_id of the other unit for comparison
        //for (int otherIndex = 0; otherIndex < positions.Length; otherIndex++)
        //{
        //    if (otherIndex == index) continue; // Skip comparing the unit against itself

        //    var otherUnitTranslation = positions[otherIndex];
        //    var otherGridID = gridData[otherIndex].value; // Get the grid_id for the other unit

        //    // Skip if the grid IDs don't match
        //    if (currentGridID != otherGridID)
        //    {
        //        continue; // Skip the collision check if grid IDs don't match
        //    }


        //    // Check X and Y distance first to quickly eliminate unnecessary distance calculations
        //    if (math.abs(currentUnitTranslation.Value.x - otherUnitTranslation.Value.x) > collisionBound.radius * 2f)
        //        continue;  // Skip if units are too far apart on X axis
        //    if (math.abs(currentUnitTranslation.Value.y - otherUnitTranslation.Value.y) > collisionBound.radius * 2f)
        //        continue;  // Skip if units are too far apart on Y axis

        //    // Now you can proceed with the distance-based collision check
        //    float distance = math.distance(currentUnitTranslation.Value, otherUnitTranslation.Value);

        //    if (distance < collisionBound.radius) // If collision occurs
        //    {
        //        // Handle the collision logic
        //        //HandleCollision(unit, ref movementSpeedComponent, ref positionComponent, ref currentUnitTranslation, unitPositions[otherIndex], collisionBound, ecb, index);...
        //        float3 direction = currentUnitTranslation.Value - otherUnitTranslation.Value;
        //        movementSpeedComponent.knockedBackDirection = direction;
        //        movementSpeedComponent.isBlocked = false;
        //        movementSpeedComponent.isKnockedBack = true;
        //        ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);
        //    }
        //    else
        //    {
        //        movementSpeedComponent.isBlocked = false;
        //        movementSpeedComponent.isKnockedBack = false;
        //        ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);

        //    }
        //}
    }

    public void HandleCollision(Unit unit, ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, Translation otherTranslation, CollisionBounds collisionBounds, EntityCommandBuffer.ParallelWriter ecb, int index)
    {
        // Calculate the direction vector between the two entities
        float3 direction = translation.Value - otherTranslation.Value;

        // Calculate the distance between the entities' centers
        //float distance = math.length(direction);

        // Check if the entities are colliding using the distance check (radius-based collision)
        //if (distance < collisionBounds.radius * 2)  // 2 * radius to account for both entities
        //{
        //// Calculate the overlap distance (how much the entities are overlapping)
        //float overlap = (collisionBounds.radius * 2) - distance;
        //movementSpeedComponent.isKnockedBack = true;

        //if (overlap > 0f)
        //{
        // Set the knock-back direction
        movementSpeedComponent.knockedBackDirection = direction;

        // Set the knock-back and block states
        movementSpeedComponent.isKnockedBack = true;
        movementSpeedComponent.isBlocked = false;

        // Apply a simple push-back logic based on the overlap
        //float3 pushBackDirection = math.normalize(direction); // Normalize to avoid scaling effects
        //translation.Value += pushBackDirection * overlap;  // Push the unit back to resolve overlap

        //// Update position after the push-back
        //position.value = translation.Value;
        //movementSpeedComponent.isKnockedBack = false;

        // Optionally, handle sliding or other responses here if needed
        // For example, you could slide the entity along the collision surface by applying a secondary force:
        // float3 slideDirection = math.cross(direction, new float3(0f, 1f, 0f));  // Cross with Y-axis for horizontal sliding
        // slideDirection = math.normalize(slideDirection);  // Normalize to avoid scaling effects
        // translation.Value += slideDirection * overlap * someSlidingFactor;

        // Update position after sliding if necessary
        // position.value = translation.Value;
        //}
        //}
        //else
        //{
        //    // If no collision, reset the blocked and knocked-back states
        //    movementSpeedComponent.isBlocked = false;
        //    movementSpeedComponent.isKnockedBack = false;
        //}

        // Set the updated movement speed component in the EntityCommandBuffer
        ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);

    }

}



public struct PhysicsVelocity : IComponentData
{
    public float3 Value; // 2D velocity vector (x, y)
}

public struct PhysicsRadius : IComponentData
{
    public float Value; // Radius of the circular collider
}

public struct PhysicsForce : IComponentData
{
    public float3 Value; // 2D force vector (x, y)
}

public struct PhysicsPosition : IComponentData
{
    public float3 Value; // 2D position vector (x, y)
}

