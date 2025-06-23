using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;

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

            Entities.WithAll<TargetComponent>().ForEach((Entity targetEntity, ref Translation targetTranslation) =>
            {
                                // Cycling through all entities with "Target" tagg
                float2 targetPos = targetTranslation.Value.xy;
                if (closestTargetEntity == Entity.Null)
                {
                    // no target
                    closestTargetEntity = targetEntity;
                    closestTargetPosition = targetPos;
                }
                else
                {
                    if(math.distance(unitPosition, targetTranslation.Value.xy) < math.distance(unitPosition, closestTargetPosition))
                    {
                        closestTargetEntity = targetEntity;
                        closestTargetPosition = targetPos;
                    }
                }
            });

            //https://youtu.be/nuxTq0AQAyY?t=367
            // working on setting up this to match codemoney
            // use IJobChunk instead of Ijobforeach....
            // https://docs.unity3d.com/Packages/com.unity.entities@0.16/manual/chunk_iteration_job.html

            // closest target
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


public class FindTargetJobSystem : JobComponentSystem
{
    private struct EntityWithPosition
    {
        public Entity entity;
        public float2 position;
    }

    private EntityQuery m_Query;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_Query = GetEntityQuery(ComponentType.ReadOnly<Unit>(), ComponentType.Exclude<HasTarget>(),
    ComponentType.ReadOnly<Translation>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        throw new NotImplementedException();
    }

    private struct FindTargetJob : IJobChunk
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<EntityWithPosition> targetArray;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            throw new NotImplementedException();
        }
    }
}


