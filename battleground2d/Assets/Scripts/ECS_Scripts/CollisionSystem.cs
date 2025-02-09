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

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[BurstCompile]
//[DisableAutoCreation]
public partial class UnitCollisionSystem : SystemBase
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
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<GridID>(),
            ComponentType.ReadOnly<CollisionBounds>(),
            ComponentType.ReadWrite<PositionComponent>(),
            ComponentType.ReadWrite<MovementSpeedComponent>()
        );

        ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // Fetch the data for all units
        NativeArray<Translation> unitPositions = unitQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Entity> unitEntities = unitQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<CollisionBounds> collisionBounds = unitQuery.ToComponentDataArray<CollisionBounds>(Allocator.TempJob);
        NativeArray<PositionComponent> positionComponents = unitQuery.ToComponentDataArray<PositionComponent>(Allocator.TempJob);
        NativeArray<Unit> unitData = unitQuery.ToComponentDataArray<Unit>(Allocator.TempJob);
        NativeArray<GridID> gridData = unitQuery.ToComponentDataArray<GridID>(Allocator.TempJob);
        NativeArray<MovementSpeedComponent> movementData = unitQuery.ToComponentDataArray<MovementSpeedComponent>(Allocator.TempJob);

        var job = new UnitCollisionJob
        {
            unitPositions = unitPositions,
            unitEntities = unitEntities,
            collisionBounds = collisionBounds,
            unitData = unitData,
            gridData = gridData,
            movementData = movementData,
            positionComponents = positionComponents,
            ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter(),

            deltaTime = Time.DeltaTime
        };

        // Schedule the job
        JobHandle jobHandle = job.Schedule(unitQuery.CalculateEntityCount(), 512, Dependency);
        jobHandle.Complete();

        // Dispose of the NativeArrays after job completion
        unitPositions.Dispose();
        unitEntities.Dispose();
        collisionBounds.Dispose();
        positionComponents.Dispose();
        unitData.Dispose();
        movementData.Dispose();

        ecbSystem.AddJobHandleForProducer(jobHandle);
    }

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
        [ReadOnly] public NativeArray<GridID> gridData;
        [NativeDisableParallelForRestriction]
        public NativeArray<MovementSpeedComponent> movementData;
        public EntityCommandBuffer.ParallelWriter ecb;

        public float deltaTime;

        public void Execute(int index)
        {
            if (index >= unitPositions.Length || index >= unitEntities.Length || index >= positionComponents.Length)
            {
                return;
            }

            var currentUnitTranslation = unitPositions[index];
            var currentEntity = unitEntities[index];
            var positionComponent = positionComponents[index];
            var unit = unitData[index];
            var collisionBound = collisionBounds[index];
            var movementSpeedComponent = movementData[index];
            var currentGridID = gridData[index].value; // Get the grid_id for the current unit

            // Get the grid_id of the other unit for comparison
            for (int otherIndex = 0; otherIndex < unitPositions.Length; otherIndex++)
            {
                if (otherIndex == index) continue; // Skip comparing the unit against itself

                var otherUnitTranslation = unitPositions[otherIndex];
                var otherGridID = gridData[otherIndex].value; // Get the grid_id for the other unit

                // Skip if the grid IDs don't match
                if (currentGridID != otherGridID)
                {
                    continue; // Skip the collision check if grid IDs don't match
                }

                // Now you can proceed with the distance-based collision check
                float distance = math.distance(currentUnitTranslation.Value, otherUnitTranslation.Value);

                if (distance < collisionBound.radius) // If collision occurs
                {
                    // Handle the collision logic
                    //HandleCollision(unit, ref movementSpeedComponent, ref positionComponent, ref currentUnitTranslation, unitPositions[otherIndex], collisionBound, ecb, index);...
                    float3 direction = currentUnitTranslation.Value - otherUnitTranslation.Value;
                    movementSpeedComponent.knockedBackDirection = direction;
                    movementSpeedComponent.isBlocked = false;
                    movementSpeedComponent.isKnockedBack = true;
                    ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);
                }
                else
                {
                    movementSpeedComponent.isBlocked = false;
                    movementSpeedComponent.isKnockedBack = false;
                    ecb.SetComponent(index, unitEntities[index], movementSpeedComponent);

                }
            }
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
}
