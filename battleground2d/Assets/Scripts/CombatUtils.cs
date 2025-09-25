// Static helper class - no system overhead
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class CombatUtils
{
    public static bool IsTargetValid(Entity target, ComponentDataFromEntity<Translation> translations)
    {
        return target != Entity.Null && translations.HasComponent(target);
    }

    public static bool IsTargetInRange(float3 sourcePos, float3 targetPos, float range)
    {
        return math.distance(sourcePos, targetPos) <= range;
    }
}