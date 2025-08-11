using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using static UnityEngine.EventSystems.EventTrigger;
using Unity.Burst;

//[UpdateBefore(typeof(UnitMoveToTargetSystem))]
//public class FindTargetSystem : ComponentSystem
//{

//    protected override void OnUpdate()
//    {
//        Entities.WithNone<CommanderComponent>().WithNone<HasTarget>().WithAll<Unit>().ForEach((Entity entity, ref Translation unitTranslation) =>
//        {
//            float2 unitPosition = unitTranslation.Value.xy;
//            Entity closestTargetEntity = Entity.Null;
//            float2 closestTargetPosition = float2.zero;
//            float closestDistance = float.MaxValue;

//            Entities.WithAll<TargetComponent>().ForEach((Entity targetEntity, ref Translation targetTranslation) =>
//            {
//                // Cycling through all entities with "Target" tagg
//                float2 targetPos = targetTranslation.Value.xy;
//                if (closestTargetEntity == Entity.Null)
//                {
//                    // no target
//                    closestTargetEntity = targetEntity;
//                    closestTargetPosition = targetPos;
//                }
//                else
//                {
//                    if (math.distance(unitPosition, targetTranslation.Value.xy) < math.distance(unitPosition, closestTargetPosition))
//                    {
//                        closestTargetEntity = targetEntity;
//                        closestTargetPosition = targetPos;
//                    }
//                }
//            });

//            //https://youtu.be/nuxTq0AQAyY?t=367
//            // working on setting up this to match codemoney
//            // use IJobChunk instead of Ijobforeach....
//            // https://docs.unity3d.com/Packages/com.unity.entities@0.16/manual/chunk_iteration_job.html

//            // closest target
//            if (closestTargetEntity != Entity.Null)
//            {

//                // PostUpdateCommands is the "old" way of doing structural changes
//                PostUpdateCommands.AddComponent(entity, new HasTarget
//                {
//                    targetEntity = closestTargetEntity
//                });
//            }
//        });
//    }
//}


//https://youtu.be/nuxTq0AQAyY?t=888
// working on setting up this to match codemoney
// use IJobChunk instead of Ijobforeach....
// finished converting, need to find out if this will work with 10,000 units
// https://docs.unity3d.com/Packages/com.unity.entities@0.16/manual/chunk_iteration_job.html

[UpdateBefore(typeof(UnitMoveToTargetSystem))]
public class FindTargetSystem : JobComponentSystem
{
    private struct EntityWithPosition
    {
        public Entity entity;
        public float2 position;
    }

    private EntityQuery m_Query;
    private EndSimulationEntityCommandBufferSystem endSimlationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(
            ComponentType.ReadOnly<Unit>(),
    ComponentType.ReadOnly<Translation>(),
    ComponentType.Exclude<CommanderComponent>(), // <== exclude commanders
        ComponentType.Exclude<HasTarget>());
        endSimlationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery targeQuery = GetEntityQuery(typeof(TargetComponent), ComponentType.ReadOnly<Translation>());
        NativeArray<Entity> targetEntityArray = targeQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> targetTranslationArray = targeQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<EntityWithPosition> targetArray = new NativeArray<EntityWithPosition>(targetEntityArray.Length, Allocator.TempJob);

        for (int i = 0; i < targetEntityArray.Length; i++)
        {
            targetArray[i] = new EntityWithPosition
            {
                entity = targetEntityArray[i],
                position = targetTranslationArray[i].Value.xy
            };
        }
        targetEntityArray.Dispose();
        targetTranslationArray.Dispose();
        var job = new FindTargetJob()
        {
            targetArray = targetArray,
            UnitTypeHandle = GetComponentTypeHandle<Unit>(true),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            entityTypeHandle = GetEntityTypeHandle(),
            entityCommandBuffer = endSimlationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()
        };


        var jobHandle = job.ScheduleParallel(m_Query);
        endSimlationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }

    [BurstCompile]
    private struct FindTargetJob : IJobChunk
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<EntityWithPosition> targetArray;
        public EntityCommandBuffer.ParallelWriter entityCommandBuffer;

        [ReadOnly] public ComponentTypeHandle<Unit> UnitTypeHandle;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkUnits = chunk.GetNativeArray(UnitTypeHandle);
            var chunkTranslations = chunk.GetNativeArray(TranslationTypeHandle);
            var chunkEntities = chunk.GetNativeArray(entityTypeHandle);
            for (var i = 0; i < chunk.Count; i++)
            {
                Entity entity = chunkEntities[i];
                var unitTranslation = chunkTranslations[i];
                var chunkUNit = chunkUnits[i];
                float2 unitPosition = unitTranslation.Value.xy;
                Entity closestTargetEntity = Entity.Null;
                float2 closestTargetPosition = float2.zero;
                float closestDistance = float.MaxValue;

                for (var j = 0; j < targetArray.Length; j++)
                {
                    // Cycling through all entities with "Target" tagg

                    EntityWithPosition targetEntityWithPosition = targetArray[j];

                    float2 targetPos = targetEntityWithPosition.position.xy;
                    if (closestTargetEntity == Entity.Null)
                    {
                        // no target
                        closestTargetEntity = targetEntityWithPosition.entity;
                        closestTargetPosition = targetPos;
                    }
                    else
                    {
                        if (math.distance(unitPosition, targetEntityWithPosition.position.xy) < math.distance(unitPosition, closestTargetPosition))
                        {
                            closestTargetEntity = targetEntityWithPosition.entity;
                            closestTargetPosition = targetPos;
                        }
                    }
                }




                // closest target
                if (closestTargetEntity != Entity.Null)
                {

                    // PostUpdateCommands is the "old" way of doing structural changes
                    entityCommandBuffer.AddComponent(chunkIndex, entity, new HasTarget
                    {
                        targetEntity = closestTargetEntity
                    });
                }
            }

        }
    }
}


