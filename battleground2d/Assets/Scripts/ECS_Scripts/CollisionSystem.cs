using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public struct Unit : IComponentData
{
    public bool isMounted;  // Flag to indicate if this unit is mounted (e.g., cavalry)
}

public struct CollisionBounds : IComponentData
{
    public float radius;  // The radius for collision checks
}

//[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
//[BurstCompile]
//[DisableAutoCreation]
//public partial class UnitCollisionSystem : SystemBase
//{
//    private EntityQuery unitQuery;
//    private EntityCommandBufferSystem ecbSystem;

//    // Reference to the GridSystem for getting nearby units
//    private GridSystem gridSystem;

//    protected override void OnCreate()
//    {
//        // Initialize the grid system


//        // Define the query to get all units
//        unitQuery = GetEntityQuery(
//            ComponentType.ReadWrite<Translation>(),
//            ComponentType.ReadOnly<Unit>(),
//            ComponentType.ReadOnly<GridID>(),
//            ComponentType.ReadOnly<CollisionBounds>(),
//            ComponentType.ReadWrite<PositionComponent>(),
//            ComponentType.ReadWrite<MovementSpeedComponent>()
//        );

//        ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
//    }

//    protected override void OnUpdate()
//    {
//        // Fetch the data for all units
//        NativeArray<Translation> unitPositions = unitQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
//        NativeArray<Entity> unitEntities = unitQuery.ToEntityArray(Allocator.TempJob);
//        NativeArray<CollisionBounds> collisionBounds = unitQuery.ToComponentDataArray<CollisionBounds>(Allocator.TempJob);
//        NativeArray<PositionComponent> positionComponents = unitQuery.ToComponentDataArray<PositionComponent>(Allocator.TempJob);
//        NativeArray<Unit> unitData = unitQuery.ToComponentDataArray<Unit>(Allocator.TempJob);
//        NativeArray<GridID> gridData = unitQuery.ToComponentDataArray<GridID>(Allocator.TempJob);
//        NativeArray<MovementSpeedComponent> movementData = unitQuery.ToComponentDataArray<MovementSpeedComponent>(Allocator.TempJob);

//        var job = new UnitCollisionJob
//        {
//            unitPositions = unitPositions,
//            unitEntities = unitEntities,
//            collisionBounds = collisionBounds,
//            unitData = unitData,
//            gridData = gridData,
//            movementData = movementData,
//            positionComponents = positionComponents,
//            ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter(),

//            deltaTime = Time.DeltaTime
//        };

//        // Schedule the job
//        JobHandle jobHandle = job.Schedule(unitQuery.CalculateEntityCount(), 256, Dependency);
//        jobHandle.Complete();

//        // Dispose of the NativeArrays after job completion
//        unitPositions.Dispose();
//        unitEntities.Dispose();
//        collisionBounds.Dispose();
//        positionComponents.Dispose();
//        unitData.Dispose();
//        movementData.Dispose();

//        ecbSystem.AddJobHandleForProducer(jobHandle);
//    }

//    [BurstCompile]
//    struct UnitCollisionJob : IJobParallelFor
//    {
//        // Input data
//        [NativeDisableParallelForRestriction]
//        public NativeArray<Translation> unitPositions;
//        [ReadOnly] public NativeArray<Entity> unitEntities;
//        [NativeDisableParallelForRestriction]
//        public NativeArray<PositionComponent> positionComponents;
//        [ReadOnly] public NativeArray<CollisionBounds> collisionBounds;
//        [ReadOnly] public NativeArray<Unit> unitData;
//        [ReadOnly] public NativeArray<GridID> gridData;
//        [NativeDisableParallelForRestriction]
//        public NativeArray<MovementSpeedComponent> movementData;
//        public EntityCommandBuffer.ParallelWriter ecb;

//        public float deltaTime;

//        public void Execute(int index)
//        {
//            if (index >= unitPositions.Length || index >= unitEntities.Length || index >= positionComponents.Length)
//            {
//                return;
//            }

//            var currentUnitTranslation = unitPositions[index];
//            var currentEntity = unitEntities[index];
//            var positionComponent = positionComponents[index];
//            var unit = unitData[index];
//            var collisionBound = collisionBounds[index];
//            var movementSpeedComponent = movementData[index];
//            var currentGridID = gridData[index].value; // Get the grid_id for the current unit

//            // Get the grid_id of the other unit for comparison
//            for (int otherIndex = 0; otherIndex < unitPositions.Length; otherIndex++)
//            {
//                if (otherIndex == index) continue; // Skip comparing the unit against itself

//                var otherUnitTranslation = unitPositions[otherIndex];
//                var otherGridID = gridData[otherIndex].value; // Get the grid_id for the other unit

//                // Skip if the grid IDs don't match
//                if (currentGridID != otherGridID)
//                {
//                    continue; // Skip the collision check if grid IDs don't match
//                }


//                // Check X and Y distance first to quickly eliminate unnecessary distance calculations
//                if (math.abs(currentUnitTranslation.Value.x - otherUnitTranslation.Value.x) > collisionBound.radius * 2f)
//                    continue;  // Skip if units are too far apart on X axis
//                if (math.abs(currentUnitTranslation.Value.y - otherUnitTranslation.Value.y) > collisionBound.radius * 2f)
//                    continue;  // Skip if units are too far apart on Y axis

//                // Now you can proceed with the distance-based collision check
//                float distance = math.distance(currentUnitTranslation.Value, otherUnitTranslation.Value);

//                if (distance < collisionBound.radius) // If collision occurs
//                {
//                    // Handle the collision logic
//                    //HandleCollision(unit, ref movementSpeedComponent, ref positionComponent, ref currentUnitTranslation, unitPositions[otherIndex], collisionBound, ecb, index);...
//                    float3 direction = currentUnitTranslation.Value - otherUnitTranslation.Value;
//                    movementSpeedComponent.knockedBackDirection = direction;
//                    movementSpeedComponent.isBlocked = false;
//                    movementSpeedComponent.isKnockedBack = true;
//                    ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);
//                }
//                else
//                {
//                    movementSpeedComponent.isBlocked = false;
//                    movementSpeedComponent.isKnockedBack = false;
//                    ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);

//                }
//            }
//        }

//        public void HandleCollision(Unit unit, ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, Translation otherTranslation, CollisionBounds collisionBounds, EntityCommandBuffer.ParallelWriter ecb, int index)
//        {
//            // Calculate the direction vector between the two entities
//            float3 direction = translation.Value - otherTranslation.Value;

//            // Calculate the distance between the entities' centers
//            //float distance = math.length(direction);

//            // Check if the entities are colliding using the distance check (radius-based collision)
//            //if (distance < collisionBounds.radius * 2)  // 2 * radius to account for both entities
//            //{
//                //// Calculate the overlap distance (how much the entities are overlapping)
//                //float overlap = (collisionBounds.radius * 2) - distance;
//                //movementSpeedComponent.isKnockedBack = true;

//                //if (overlap > 0f)
//                //{
//                    // Set the knock-back direction
//                    movementSpeedComponent.knockedBackDirection = direction;

//                // Set the knock-back and block states
//                movementSpeedComponent.isKnockedBack = true;
//                movementSpeedComponent.isBlocked = false;

//            // Apply a simple push-back logic based on the overlap
//            //float3 pushBackDirection = math.normalize(direction); // Normalize to avoid scaling effects
//            //translation.Value += pushBackDirection * overlap;  // Push the unit back to resolve overlap

//            //// Update position after the push-back
//            //position.value = translation.Value;
//            //movementSpeedComponent.isKnockedBack = false;

//            // Optionally, handle sliding or other responses here if needed
//            // For example, you could slide the entity along the collision surface by applying a secondary force:
//            // float3 slideDirection = math.cross(direction, new float3(0f, 1f, 0f));  // Cross with Y-axis for horizontal sliding
//            // slideDirection = math.normalize(slideDirection);  // Normalize to avoid scaling effects
//            // translation.Value += slideDirection * overlap * someSlidingFactor;

//            // Update position after sliding if necessary
//            // position.value = translation.Value;
//            //}
//            //}
//            //else
//            //{
//            //    // If no collision, reset the blocked and knocked-back states
//            //    movementSpeedComponent.isBlocked = false;
//            //    movementSpeedComponent.isKnockedBack = false;
//            //}

//            // Set the updated movement speed component in the EntityCommandBuffer
//            ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);

//        }

//    }
//}

public struct PhysicsColliderComponent : IComponentData
{
    public Unity.Physics.SphereCollider Collider; // Unit's collider
}

public struct HitDetectionComponent : IComponentData
{
    public bool IsHit;
    public Entity CollidingWith; // Optional: store the entity it collided with
    public float Radius;
}

[BurstCompile]
[UpdateAfter(typeof(MovementSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[DisableAutoCreation]
public class UnitCollisionSystem : SystemBase
{
    // Temporary struct to store unit position and collider data
    struct UnitData
    {
        public Entity entity;
        public Translation translation;
        public float radius; // Assuming a SphereCollider for simplicity
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob); // EntityCommandBuffer to safely update components

        // Create a NativeList to store positions and colliders for all units
        NativeList<UnitData> unitDataList = new NativeList<UnitData>(Allocator.TempJob);

        // Gather all entities' positions and colliders (assuming they all use SphereCollider for simplicity)
        Entities
            .WithAll<PhysicsColliderComponent>() // Only units with colliders
            .ForEach((Entity entity, in Translation translation, in PhysicsColliderComponent collider) =>
            {
                // Get the radius of the sphere collider
                float radius = 0.25f; // Default radius
                if (collider.Collider is Unity.Physics.SphereCollider sphereCollider)
                {
                    radius = sphereCollider.Radius;
                }

                unitDataList.Add(new UnitData
                {
                    entity = entity,
                    translation = translation,
                    radius = radius
                });
            }).Schedule();

        // Ensure that the first job has completed before proceeding
        Dependency.Complete();

        // Now check for collisions between all units
        Entities
            .WithAll<PhysicsColliderComponent>()
            .ForEach((ref Translation translation, in HitDetectionComponent hitDetection) =>
            {
                // Skip processing if the entity is invalid
                if (hitDetection.CollidingWith == Entity.Null)
                    return;

                for (int i = 0; i < unitDataList.Length; i++)
                {
                    var unitData = unitDataList[i];

                    // Skip self-collision and collisions with entities already collided
                    if (unitData.entity == Entity.Null || unitData.entity == hitDetection.CollidingWith)
                        continue;

                    // Get the distance between the two units
                    float3 offset = unitData.translation.Value - translation.Value;
                    if (math.any(math.isnan(offset))) continue; // Avoid NaN values

                    float distance = math.length(offset);

                    // Check if the distance is less than the sum of both radii (i.e., collision)
                    float combinedRadius = unitData.radius + hitDetection.Radius;

                    if (distance < combinedRadius)
                    {
                        // Apply push-back logic (separation of units to prevent overlap)
                        float3 pushDirection = math.normalize(offset);
                        float pushStrength = 0.5f; // Adjust this value for the amount of push-back

                        // Apply the push-back effect to both units (use EntityCommandBuffer for safety)
                        translation.Value -= pushDirection * pushStrength * deltaTime;

                        // Safely update the other unit's translation using the command buffer
                        if (unitData.entity != Entity.Null) // Check if the unit data entity is valid
                        {
                            ecb.SetComponent(unitData.entity, new Translation { Value = unitData.translation.Value + pushDirection * pushStrength * deltaTime });
                        }

                        // Update the hit detection component
                        if (hitDetection.CollidingWith != Entity.Null) // Ensure CollidingWith is valid
                        {
                            ecb.SetComponent(hitDetection.CollidingWith, new HitDetectionComponent
                            {
                                IsHit = true,
                                CollidingWith = unitData.entity,
                                Radius = hitDetection.Radius
                            });
                        }
                    }
                }
            }).WithoutBurst().Run();

        // Playback and dispose of the command buffer after the update
        ecb.Playback(EntityManager);
        ecb.Dispose();

        // Dispose of the temporary list after the update
        unitDataList.Dispose();
    }
}
