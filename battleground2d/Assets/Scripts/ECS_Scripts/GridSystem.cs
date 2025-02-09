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
    private NativeArray<NativeList<Entity>> gridCells;

    protected override void OnCreate()
    {

    }

    protected override void OnUpdate()
    {

        // Calculate the size of each division (assuming we want a grid of 10x10 parts)
        int gridSize = 1000; // 10x10 grid = 100 parts
        float2 divisionSize = mapSize / gridSize;

        // Iterate through all entities that have Translation component (your existing entities)
        Entities.ForEach((ref Translation translation,ref GridID grid) =>
        {
            // Find the grid index based on the entity's position
            int gridX = Mathf.FloorToInt(translation.Value.x / divisionSize.x);
            int gridY = Mathf.FloorToInt(translation.Value.y / divisionSize.y);

            // Ensure the grid values are clamped to prevent index out-of-range errors
            gridX = math.clamp(gridX, 0, gridSize - 1);
            gridY = math.clamp(gridY, 0, gridSize - 1);

            // Calculate the grid_id based on the grid's position (1 to 100)
            int gridId = gridY * gridSize + gridX + 1;

            grid.value = gridId;

        }).WithBurst().ScheduleParallel();
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