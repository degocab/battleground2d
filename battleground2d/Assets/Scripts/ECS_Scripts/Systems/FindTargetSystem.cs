using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TargetReevaluationSystem))]
[UpdateAfter(typeof(TargetValidationSystem))]
public partial class FindTargetSystem : SystemBase
{
    // Constants
    private const int UpdateInterval = 2;

    // Fields
    private EntityQuery _findTargetQuery;
    private EntityQuery _targetQuery;
    private EndSimulationEntityCommandBufferSystem _endSimulationECBSystem;
    private int _updateCounter;

    // Double buffer for closest targets
    private NativeArray<EntityWithPosition> _closestTargetsBuffer1;
    private NativeArray<EntityWithPosition> _closestTargetsBuffer2;
    private bool _useBuffer1 = true;

    // Structs
    private struct EntityWithPosition
    {
        public Entity Entity;
        public float2 Position;
    }

    // System Lifecycle
    protected override void OnCreate()
    {
        InitializeQueries();
        _endSimulationECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate(_findTargetQuery);
    }

    protected override void OnDestroy()
    {
        DisposeBuffers();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        _updateCounter++;
        if (_updateCounter % UpdateInterval != 0)
            return;

        if (_targetQuery.CalculateEntityCount() == 0)
        {
            ClearCommands();
            return;
        }

        FindTargets();
    }

    // Initialization Methods
    private void InitializeQueries()
    {
        _findTargetQuery = GetEntityQuery(
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<AnimationComponent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadWrite<FindTargetTag>(),
            ComponentType.Exclude<CommanderComponent>()
            //, ComponentType.Exclude<HasTarget>()
        );

        _targetQuery = GetEntityQuery(
            ComponentType.ReadOnly<TargetComponent>(),
            ComponentType.ReadOnly<Translation>()
        );
    }

    private void DisposeBuffers()
    {
        if (_closestTargetsBuffer1.IsCreated) _closestTargetsBuffer1.Dispose();
        if (_closestTargetsBuffer2.IsCreated) _closestTargetsBuffer2.Dispose();
    }

    // Jobs
    [BurstCompile]
    private struct AddTargetComponentJob : IJobChunk
    {
        [ReadOnly] public NativeArray<EntityWithPosition> ClosestTargets;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentTypeHandle<CombatTarget> CombatTargetTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkEntities = chunk.GetNativeArray(EntityTypeHandle);
            bool chunkCombatTarget = chunk.Has<CombatTarget>(CombatTargetTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = chunkEntities[i];
                int flatIndex = firstEntityIndex + i;

                if (ClosestTargets[flatIndex].Entity != Entity.Null)
                {
                    var target = new CombatTarget
                    {
                        TargetEntity = ClosestTargets[flatIndex].Entity,
                        TargetPosition = ClosestTargets[flatIndex].Position,
                        isActive = true
                        //Type = CombatTarget.TargetType.Entity
                    };

                    if (chunkCombatTarget)
                    {
                        ECB.SetComponent(chunkIndex, entity, target);
                    }
                    else
                    {
                        ECB.AddComponent(chunkIndex, entity, target);
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct ClearCommandsJob : IJobChunk
    {
        public ComponentTypeHandle<FindTargetTag> FindTargetCommandTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var commandDataArray = chunk.GetNativeArray(FindTargetCommandTypeHandle);
            var entityArray = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entityArray[i];
                ECB.RemoveComponent<CombatTarget>(chunkIndex, entity);
                ECB.RemoveComponent<FindTargetTag>(chunkIndex, entity);
            }
        }
    }

    [BurstCompile]
    private struct FindTargetsJob : IJobChunk
    {
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> QuadrantHashMap;
        public NativeArray<EntityWithPosition> ClosestTargets;

        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<QuadrantEntity> QuadrantEntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DeadTagComponent> DeadTagTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            if (chunk.Has<DeadTagComponent>(DeadTagTypeHandle))
                return;

            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var quadrantEntities = chunk.GetNativeArray(QuadrantEntityTypeHandle);
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                float2 unitPosition = translations[i].Value.xy;
                var quadrantEntity = quadrantEntities[i];
                var animation = animations[i];

                Entity closestTarget = Entity.Null;
                float2 closestPosition = float2.zero;
                float closestDistanceSq = float.MaxValue;

                int hashKey = QuadrantSystem.GetPositionHashMapKey(unitPosition);

                CheckSurroundingQuadrants(hashKey, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);

                ClosestTargets[firstEntityIndex + i] = new EntityWithPosition
                {
                    Entity = closestTarget,
                    Position = closestPosition
                };
            }
        }

        private void CheckSurroundingQuadrants(int hashKey, float2 unitPosition, QuadrantEntity quadrantEntity,
            AnimationComponent animation, ref Entity closestTarget, ref float closestDistanceSq, ref float2 closestPosition)
        {
            if (closestDistanceSq < 4.0f) return;

            CheckQuadrant(hashKey, unitPosition, quadrantEntity, animation, ref closestTarget, ref closestDistanceSq, ref closestPosition);
            CheckQuadrant(hashKey + 1, unitPosition, quadrantEntity, animation, ref closestTarget, ref closestDistanceSq, ref closestPosition);
            CheckQuadrant(hashKey - 1, unitPosition, quadrantEntity, animation, ref closestTarget, ref closestDistanceSq, ref closestPosition);
            CheckQuadrant(hashKey + QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation, ref closestTarget, ref closestDistanceSq, ref closestPosition);
            CheckQuadrant(hashKey - QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation, ref closestTarget, ref closestDistanceSq, ref closestPosition);
        }

        private void CheckQuadrant(int hashKey, float2 unitPosition, QuadrantEntity quadrantEntity,
            AnimationComponent animation, ref Entity closestTarget, ref float closestDistanceSq, ref float2 closestPosition)
        {
            if (QuadrantHashMap.TryGetFirstValue(hashKey, out QuadrantData data, out var iterator))
            {
                do
                {
                    if (animation.UnitType == data.AnimationComponent.UnitType) continue;

                    float distanceSq = math.distancesq(unitPosition, data.Position);
                    if (distanceSq >= closestDistanceSq) continue;

                    closestTarget = data.Entity;
                    closestDistanceSq = distanceSq;
                    closestPosition = data.Position;

                } while (QuadrantHashMap.TryGetNextValue(out data, ref iterator));
            }
        }
    }

    // Helper Methods
    private void ClearCommands()
    {
        var clearJob = new ClearCommandsJob
        {
            FindTargetCommandTypeHandle = GetComponentTypeHandle<FindTargetTag>(false),
            EntityTypeHandle = GetEntityTypeHandle(),
            ECB = _endSimulationECBSystem.CreateCommandBuffer().AsParallelWriter()
        };

        var handle = clearJob.ScheduleParallel(_findTargetQuery, Dependency);
        _endSimulationECBSystem.AddJobHandleForProducer(handle);
        Dependency = handle;
    }

    private void FindTargets()
    {
        int entityCount = _findTargetQuery.CalculateEntityCount();
        NativeArray<EntityWithPosition> writeBuffer = _useBuffer1 ? _closestTargetsBuffer1 : _closestTargetsBuffer2;

        if (!writeBuffer.IsCreated || writeBuffer.Length != entityCount)
        {
            if (writeBuffer.IsCreated) writeBuffer.Dispose();
            writeBuffer = new NativeArray<EntityWithPosition>(entityCount, Allocator.Persistent);

            if (_useBuffer1)
                _closestTargetsBuffer1 = writeBuffer;
            else
                _closestTargetsBuffer2 = writeBuffer;
        }

        var findJob = new FindTargetsJob
        {
            QuadrantHashMap = QuadrantSystem.QuadrantMultiHashMap,
            ClosestTargets = writeBuffer,
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            QuadrantEntityTypeHandle = GetComponentTypeHandle<QuadrantEntity>(true),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            EntityTypeHandle = GetEntityTypeHandle(),
            DeadTagTypeHandle = GetComponentTypeHandle<DeadTagComponent>(true),
        };

        var findHandle = findJob.ScheduleParallel(_findTargetQuery, Dependency);

        var addComponentJob = new AddTargetComponentJob
        {
            ClosestTargets = writeBuffer,
            EntityTypeHandle = GetEntityTypeHandle(),
            ECB = _endSimulationECBSystem.CreateCommandBuffer().AsParallelWriter(),
            CombatTargetTypeHandle = GetComponentTypeHandle<CombatTarget>(true)
        };

        var addHandle = addComponentJob.ScheduleParallel(_findTargetQuery, findHandle);
        _endSimulationECBSystem.AddJobHandleForProducer(addHandle);

        _useBuffer1 = !_useBuffer1;
        Dependency = addHandle;
    }
}

public struct CombatTarget : IComponentData
{
    public Entity TargetEntity;
    public float2 TargetPosition;

    public bool isActive;
}