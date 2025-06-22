using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

[UpdateBefore(typeof(UnitMoveToTargetSystem))]
public class FindTargetSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithNone<CommanderComponent>().WithNone<HasTarget>().WithAll<Unit>().ForEach((Entity entity, ref Translation unitTranslation) =>
        {
            float2 unitPosition = unitTranslation.Value.xy;
            Entity closestTargetEntity = Entity.Null;
            float2 closestTargetPosition = float2.zero;
            float closestDistance = float.MaxValue;

            Entities.WithAll<Target>().ForEach((Entity targetEntity, ref Translation targetTranslation) =>
            {
                float2 targetPos = targetTranslation.Value.xy;
                float dist = math.distancesq(unitPosition, targetPos); // faster than distance()

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTargetEntity = targetEntity;
                    closestTargetPosition = targetPos;
                }
            });

            if (closestTargetEntity != Entity.Null)
            {

                // PostUpdateCommands is the "old" way of doing structural changes
                PostUpdateCommands.AddComponent(entity, new HasTarget
                {
                    targetEntity = closestTargetEntity
                });
            }
        });
    }
}


public struct Target : IComponentData { }
public struct HasTarget : IComponentData
{
    public Entity targetEntity;
}

public class HasTargetDebug : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) =>
        {
            Translation targetTranslation = EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);
            Debug.DrawLine(translation.Value, targetTranslation.Value, Color.red);
            // https://youtu.be/t11uB7Gl6m8?t=823
        });
    }
}