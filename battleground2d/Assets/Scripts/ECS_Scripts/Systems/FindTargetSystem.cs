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



//https://youtu.be/hP4Vu6JbzSo?t=198
//working on setting up this to match codemoney
// use IJobChunk instead of Ijobforeach....
// finished separating find targets and add component into two jobs.
// need to implement quadrant system
// https://docs.unity3d.com/Packages/com.unity.entities@0.16/manual/chunk_iteration_job.html


[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateBefore(typeof(UnitMoveToTargetSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
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
        ComponentType.ReadWrite<FindTargetCommandTag>(),
        ComponentType.Exclude<CommanderComponent>(), // <== exclude commanders
        ComponentType.Exclude<HasTarget>());


        endSimlationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();

    }

    [BurstCompile]
    private struct FindTargetBurstJob : IJobChunk
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<EntityWithPosition> targetArray;
        public NativeArray<Entity> closestTargetEntityArray;

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

                closestTargetEntityArray[firstEntityIndex + i] = closestTargetEntity;
            }

        }
    }

    private struct AddComponentJob : IJobChunk
    {

        public EntityCommandBuffer.ParallelWriter entityCommandBuffer;
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<Entity> closestTargetEntityArray;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkEntities = chunk.GetNativeArray(entityTypeHandle);
            for (var index = 0; index < chunk.Count; index++)
            {
                Entity entity = chunkEntities[index];
                int flatIndex = firstEntityIndex + index;
                if (closestTargetEntityArray[flatIndex] != Entity.Null)
                {
                    entityCommandBuffer.AddComponent(index, entity, new HasTarget() { targetEntity = closestTargetEntityArray[flatIndex] });
                }
            }
        }
    }

    [BurstCompile]
    private struct ClearCommandJob : IJobChunk
    {
        public ComponentTypeHandle<CommandData> CommandDataTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var commandDataArray = chunk.GetNativeArray(CommandDataTypeHandle);
            var entityArray = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entityArray[i];
                var command = commandDataArray[i];

                if (command.Command == CommandType.FindTarget ||
                    command.Command == CommandType.Attack ||
                    command.Command == CommandType.MoveTo)
                {
                    command.Command = CommandType.Idle;
                    command.TargetEntity = Entity.Null;
                    command.TargetPosition = float3.zero;

                    commandDataArray[i] = command;

                    // Remove HasTarget if present
                    ECB.RemoveComponent<HasTarget>(chunkIndex, entity);
                    ECB.RemoveComponent<FindTargetCommandTag>(chunkIndex, entity);
                }
            }
        }
    }

    [BurstCompile]
    private struct FindTargetQuadrantSystemob : IJobChunk
    {
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
        public NativeArray<Entity> closestTargetEntityArray;

        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<QuadrantEntity> QuadrantEntityTypeHandle;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkTranslations = chunk.GetNativeArray(TranslationTypeHandle);
            NativeArray<QuadrantEntity> chunkQuadrantEntities = chunk.GetNativeArray(QuadrantEntityTypeHandle);
            var chunkEntities = chunk.GetNativeArray(entityTypeHandle);
            for (var i = 0; i < chunk.Count; i++)
            {
                //Entity entity = chunkEntities[i];
                var unitTranslation = chunkTranslations[i];
                //var chunkUNit = chunkUnits[i];
                float2 unitPosition = unitTranslation.Value.xy;
                Entity closestTargetEntity = Entity.Null;
                QuadrantEntity quadrantEntity = chunkQuadrantEntities[i];
                float2 closestTargetPosition = float2.zero;
                float closestTargetDistance = float.MaxValue;
                int hashMapKey = QuadrantSystem.GetPositionHashMapKey(unitPosition);

                FindTarget(hashMapKey, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey + 1, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey + -1, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey + QuadrantSystem.quadrantYMultiplier, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey - QuadrantSystem.quadrantYMultiplier, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);

                //corners
                FindTarget(hashMapKey + 1 + QuadrantSystem.quadrantYMultiplier, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey - 1 + QuadrantSystem.quadrantYMultiplier, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey + 1 - QuadrantSystem.quadrantYMultiplier, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);
                FindTarget(hashMapKey - 1 - QuadrantSystem.quadrantYMultiplier, unitPosition, quadrantEntity, ref closestTargetEntity, ref closestTargetDistance, ref quadrantMultiHashMap);


                closestTargetEntityArray[firstEntityIndex + i] = closestTargetEntity;
            }

        }
    }

    private static void FindTarget(int hashMapKey
        , float2 unitPosition
        , QuadrantEntity quadrantEntity
        , ref Entity closestTargetEntity
        , ref float closestTargetDistance
        , ref NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap)
    {
        QuadrantData quadrantData;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;

        if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
        {
            do
            {
                if (quadrantEntity.typeEnum != quadrantData.quadrantEntity.typeEnum)
                {
                    if (closestTargetEntity == Entity.Null)
                    {
                        // no target
                        closestTargetEntity = quadrantData.entity;
                        closestTargetDistance = math.distancesq(unitPosition, quadrantData.position);
                        //closestTargetPosition = quadrantData.position;
                    }
                    else
                    {
                        if (math.distancesq(unitPosition, quadrantData.position) < closestTargetDistance)
                        {
                            //target is closer
                            closestTargetEntity = quadrantData.entity;
                            closestTargetDistance = math.distancesq(unitPosition, quadrantData.position);
                            //closestTargetPosition = quadrantData.position;
                        }
                    }
                }

            } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery targetQuery = GetEntityQuery(typeof(TargetComponent), ComponentType.ReadOnly<Translation>());
        NativeArray<Entity> targetEntityArray = targetQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> targetTranslationArray = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<EntityWithPosition> targetArray = new NativeArray<EntityWithPosition>(targetEntityArray.Length, Allocator.TempJob);

        if (targetArray.Length == 0)
        {
            targetEntityArray.Dispose();
            targetTranslationArray.Dispose();
            targetArray.Dispose();

            var commandDataTypeHandle = GetComponentTypeHandle<CommandData>(false);
            var entityTypeHandle = GetEntityTypeHandle();
            var ecb = endSimlationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            var clearJob = new ClearCommandJob
            {
                CommandDataTypeHandle = commandDataTypeHandle,
                EntityTypeHandle = entityTypeHandle,
                ECB = ecb,
            };

            var handle = clearJob.ScheduleParallel(m_Query, inputDeps);
            endSimlationEntityCommandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }

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
        EntityQuery unitQuery = GetEntityQuery(typeof(Unit), ComponentType.Exclude<HasTarget>(), ComponentType.Exclude<CommanderComponent>());
        NativeArray<Entity> closestTargetEntityArray = new NativeArray<Entity>(unitQuery.CalculateEntityCount(), Allocator.TempJob);

        FindTargetQuadrantSystemob findTargetQuadrantSystemob = new FindTargetQuadrantSystemob
        {
            quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
            closestTargetEntityArray = closestTargetEntityArray,
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            QuadrantEntityTypeHandle = GetComponentTypeHandle<QuadrantEntity>(true),
            entityTypeHandle = GetEntityTypeHandle(),
        };
        var jobHandle = findTargetQuadrantSystemob.ScheduleParallel(m_Query, inputDeps);

        /*
        var job = new FindTargetBurstJob()
        {
            targetArray = targetArray,
            closestTargetEntityArray = closestTargetEntityArray,
            UnitTypeHandle = GetComponentTypeHandle<Unit>(true),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            entityTypeHandle = GetEntityTypeHandle(),
        };

        var jobHandle = job.ScheduleParallel(m_Query, inputDeps);
        */

        AddComponentJob addComponentJob = new AddComponentJob()
        {
            entityTypeHandle = GetEntityTypeHandle(),
            closestTargetEntityArray = closestTargetEntityArray,
            entityCommandBuffer = endSimlationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()
        };
        var addComponentJobHandler = addComponentJob.ScheduleParallel(m_Query, jobHandle);

        endSimlationEntityCommandBufferSystem.AddJobHandleForProducer(addComponentJobHandler);
        targetArray.Dispose();

        return addComponentJobHandler;
    }


}


