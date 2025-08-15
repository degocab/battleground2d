using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//https://youtu.be/hP4Vu6JbzSo?t=739
// following code monkey quadrant system
// working to grab all units
// next is convert it to jobs
public class QuadrantSystem : SystemBase
{
    private  const int quadrantYMultiplier = 1000;
    private  const int quadrantCellSize = 5;

    //convert position to quadrant
    private static int GetPositionHashMapKey(float2 position)
    {
        return (int)(math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }

    private static void DebugDrawQuadrant(float2 position)
    {
        Vector2 lowerLeft = new Vector2(math.floor(position.x / quadrantCellSize)  * quadrantCellSize, math.floor(position.y / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector2(+1, +0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector2(+0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector2(+1, +0) * quadrantCellSize, lowerLeft + new Vector2(+1, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector2(+0, +1) * quadrantCellSize, lowerLeft + new Vector2(+1, +1) * quadrantCellSize);
        //Debug.Log(GetPositionHashMapKey(position) + " " + position);
    }

    private static int GetEntityCountInHashMap(NativeMultiHashMap<int, Entity> quadrantMultiHashMap, int hashMapKey)
    {
        Entity entity;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;
        if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out entity, out nativeMultiHashMapIterator))
        {
            do {
                count++;
            } while (quadrantMultiHashMap.TryGetNextValue(out entity, ref nativeMultiHashMapIterator));
        }
        return count;
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(Unit), typeof(Translation));
        NativeMultiHashMap<int, Entity> quadrantMultiHasMap = new NativeMultiHashMap<int, Entity>(entityQuery.CalculateEntityCount(), Allocator.TempJob);

        Entities.WithAll<Unit>().WithNone<CommanderComponent>().ForEach((Entity entity, ref Translation translation) =>
        {
            int hashMapKey = GetPositionHashMapKey(translation.Value.xy);
            quadrantMultiHasMap.Add(hashMapKey, entity);

        }).Run();

        // Get mouse position in screen space
        float3 mousePosition = Input.mousePosition;

        // Optionally convert to world space (for 2D or 3D use)
        float3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // If you're using 2D, drop the Z axis
        float2 worldMouse2D = worldPosition.xy;
        DebugDrawQuadrant(worldMouse2D);
        Debug.Log(GetEntityCountInHashMap(quadrantMultiHasMap, GetPositionHashMapKey(worldMouse2D)));

        quadrantMultiHasMap.Dispose();

    }
}
