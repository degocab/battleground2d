using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateBefore(typeof(CombatSystem))]
[BurstCompile]
public class GridSystem : SystemBase
{
    private static readonly float2 mapSize = new float2(1000f, 1000f); // Example size of the map
    private static readonly int gridSize = 100;  // 100x100 grid
    private static readonly float2 divisionSize = mapSize / gridSize;

    protected override void OnUpdate()
    {

        // Iterate through all entities with Translation and GridID components
        Entities.ForEach((ref Translation translation, ref GridID grid) =>
        {
            // Find the grid cell index based on the entity's position
            int gridX = Mathf.FloorToInt(translation.Value.x / divisionSize.x);
            int gridY = Mathf.FloorToInt(translation.Value.y / divisionSize.y);

            // Ensure the grid values are clamped to prevent index out-of-range errors
            gridX = math.clamp(gridX, 0, gridSize - 1);
            gridY = math.clamp(gridY, 0, gridSize - 1);

            // Calculate the grid ID (1-based index for better readability, optional)
            int gridId = gridY * gridSize + gridX;

            // Update the GridID component with the calculated grid ID
            grid.value = gridId;

        }).WithBurst().ScheduleParallel();
    }
}

// Create a simple GridID component
public struct GridID : IComponentData
{
    public int value;
}
