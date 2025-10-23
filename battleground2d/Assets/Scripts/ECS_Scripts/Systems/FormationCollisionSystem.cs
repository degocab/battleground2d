using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct InGroupTag : IComponentData { }
public struct OutOfGroupTag : IComponentData { }

public class FormationCollisionSystem : SystemBase
{
    protected override void OnUpdate()
    {

        //CollisionQuadrantData
        //clean up group unit collisions
        // we want to only have group collisions unless we re add them!
        // Gather all formation groups into a native array
        var formationGroups = GetComponentDataFromEntity<FormationGroupComponent>(true);
        var groupEntities = GetEntityQuery(typeof(FormationGroupComponent)).ToEntityArray(Allocator.TempJob);
        var groups = GetComponentDataFromEntity<FormationGroupComponent>(false);

        // Simple double loop to check pairs
        for (int i = 0; i < groupEntities.Length; i++)
        {
            var groupA = groups[groupEntities[i]];

            for (int j = i + 1; j < groupEntities.Length; j++)
            {
                var groupB = groups[groupEntities[j]];

                if (AABBOverlap(groupA.BoundsMin, groupA.BoundsMax, groupB.BoundsMin, groupB.BoundsMax))
                {
                    // Do something on overlap: e.g. log, set a flag, etc.
                    UnityEngine.Debug.Log($"Formation groups {groupEntities[i]} and {groupEntities[j]} overlap!");
                }
            }
        }
        for (int i = 0; i < groupEntities.Length; i++)
        {
            var group = groups[groupEntities[i]];
            DrawAABB(group.BoundsMin, group.BoundsMax, Color.green);
        }

        groupEntities.Dispose();
    }
    void DrawAABB(float2 min, float2 max, Color color)
    {
        Vector3 bottomLeft = new Vector3(min.x, min.y, 0);
        Vector3 bottomRight = new Vector3(max.x, min.y, 0);
        Vector3 topLeft = new Vector3(min.x, max.y, 0);
        Vector3 topRight = new Vector3(max.x, max.y, 0);

        Debug.DrawLine(bottomLeft, bottomRight, color);
        Debug.DrawLine(bottomRight, topRight, color);
        Debug.DrawLine(topRight, topLeft, color);
        Debug.DrawLine(topLeft, bottomLeft, color);
    }

    public static bool AABBOverlap(float2 minA, float2 maxA, float2 minB, float2 maxB)
    {
        return !(maxA.x < minB.x || minA.x > maxB.x || maxA.y < minB.y || minA.y > maxB.y);
    }
    public static AABB CalculateGroupBounds(NativeArray<float2> positions, float unitRadius)
    {
        if (positions.Length == 0)
            return new AABB();

        float minX = positions[0].x;
        float maxX = positions[0].x;
        float minY = positions[0].y;
        float maxY = positions[0].y;

        for (int i = 1; i < positions.Length; i++)
        {
            var pos = positions[i];
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        return new AABB
        {
            Min = new float2(minX - unitRadius, minY - unitRadius),
            Max = new float2(maxX + unitRadius, maxY + unitRadius)
        };
    }
}



public struct AABB
{
    public float2 Min;
    public float2 Max;

    public bool Overlaps(AABB other)
    {
        return !(Max.x < other.Min.x || Min.x > other.Max.x ||
                 Max.y < other.Min.y || Min.y > other.Max.y);
    }
}