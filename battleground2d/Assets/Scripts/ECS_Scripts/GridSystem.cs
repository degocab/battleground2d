using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateBefore(typeof(CombatSystem))]
[BurstCompile]
public class GridSystem : SystemBase
{
    private float2 mapSize = new float2(1000f, 1000f); // Example size of the map

    // Adjust grid size, for instance, 100x100 cells for a 1000x1000 map
    private int gridSize = 100;  // 100x100 grid
    private float2 divisionSize;

    protected override void OnCreate()
    {
        // Calculate the size of each grid cell based on the total map size and grid size
        divisionSize = mapSize / gridSize;
    }

    protected override void OnUpdate()
    {
        int gridSize = 100;
        float2 mapSize = new float2(1000f, 1000f);
        var t = mapSize / gridSize; ;
        // Allocate NativeArray to store grid cells as NativeLists
        //NativeArray<NativeList<Entity>> gridCells = new NativeArray<NativeList<Entity>>(gridSize * gridSize, Allocator.Temp);

        // Iterate through all entities with Translation and GridID components
        Entities.ForEach((ref Translation translation, ref GridID grid) =>
        {
            // Find the grid cell index based on the entity's position
            int gridX = Mathf.FloorToInt(translation.Value.x / t.x);
            int gridY = Mathf.FloorToInt(translation.Value.y / t.y);

            // Ensure the grid values are clamped to prevent index out-of-range errors
            gridX = math.clamp(gridX, 0, gridSize - 1);
            gridY = math.clamp(gridY, 0, gridSize - 1);

            // Calculate the grid ID (1-based index for better readability, optional)
            int gridId = gridY * gridSize + gridX;

            // Update the GridID component with the calculated grid ID
            grid.value = gridId;

            //// Ensure the corresponding grid cell list exists for the current gridId
            //if (gridCells[gridId].IsEmpty)
            //{
            //    gridCells[gridId] = new NativeList<Entity>(Allocator.Temp);
            //}

            // Add the entity to the corresponding grid cell
            //gridCells[gridId].Add(EntityManager.GetEntity(translation));

        }).WithBurst().ScheduleParallel();

        // After the grid cells have been updated, you can pass the grid information to the Collision Check Job or other systems
        // Depending on the needs of your system, you could schedule a job to handle entities in gridCells.

        // Dispose the NativeArray after usage
        //gridCells.Dispose();
    }


    protected override void OnDestroy()
    {
        // Dispose of grid cell lists
    }
}


// Create a simple GridID component
public struct GridID : IComponentData
{
    public int value;
}