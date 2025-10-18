// Static helper class - no system overhead
using System.Security.Policy;
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

    public static void SetAnimationDirection(ref AnimationComponent animationComponent, float2 viewDirection)
    {
        if (math.abs(viewDirection.x) > math.abs(viewDirection.y))
        {
            if (viewDirection.x > 0)
            {
                animationComponent.Direction = EntitySpawner.Direction.Right;
                animationComponent.animationWidthOffset = 1;
            }
            else
            {
                animationComponent.Direction = EntitySpawner.Direction.Left;
                animationComponent.animationWidthOffset = 2;
            }
        }
        else
        {
            if (viewDirection.y > 0)
            {
                animationComponent.Direction = EntitySpawner.Direction.Up;
                animationComponent.animationWidthOffset = 3;
            }
            else
            {
                animationComponent.Direction = EntitySpawner.Direction.Down;
                animationComponent.animationWidthOffset = 4;
            }
        }
    }
}