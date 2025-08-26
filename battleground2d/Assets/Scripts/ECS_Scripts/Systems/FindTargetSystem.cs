using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Text;
using UnityEngine;
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(UnitMoveToTargetSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
public partial class FindTargetSystem : SystemBase
{
    private EntityQuery _findTargetQuery;
    private EntityQuery _targetQuery;
    private EndSimulationEntityCommandBufferSystem _endSimulationECBSystem;
    private int _updateCounter;
    private const int UpdateInterval = 2;

    // Double buffer for closest targets
    private NativeArray<EntityWithPosition> _closestTargetsBuffer1;
    private NativeArray<EntityWithPosition> _closestTargetsBuffer2;
    private bool _useBuffer1 = true;

    private struct EntityWithPosition
    {
        public Entity Entity;
        public float2 Position;
    }

    protected override void OnCreate()
    {
        _findTargetQuery = GetEntityQuery(
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<AnimationComponent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadWrite<FindTargetCommandTag>(),
            ComponentType.Exclude<CommanderComponent>(),
            ComponentType.Exclude<HasTarget>()
        );

        _targetQuery = GetEntityQuery(
            ComponentType.ReadOnly<TargetComponent>(),
            ComponentType.ReadOnly<Translation>()
        );

        _endSimulationECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate(_findTargetQuery);
    }

    protected override void OnDestroy()
    {
        // Clean up both buffers
        if (_closestTargetsBuffer1.IsCreated) _closestTargetsBuffer1.Dispose();
        if (_closestTargetsBuffer2.IsCreated) _closestTargetsBuffer2.Dispose();
        base.OnDestroy();
    }

    [BurstCompile]
    private struct AddTargetComponentJob : IJobChunk
    {
        [ReadOnly] public NativeArray<EntityWithPosition> ClosestTargets;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkEntities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = chunkEntities[i];
                int flatIndex = firstEntityIndex + i;

                if (ClosestTargets[flatIndex].Entity != Entity.Null)
                {
                    ECB.AddComponent(chunkIndex, entity, new HasTarget
                    {
                        TargetEntity = ClosestTargets[flatIndex].Entity,
                        TargetPosition = ClosestTargets[flatIndex].Position,
                        Type = HasTarget.TargetType.Entity
                    });
                }
            }
        }
    }


    [BurstCompile]
    private struct ClearCommandsJob : IJobChunk
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
                    command.TargetPosition = float2.zero;
                    commandDataArray[i] = command;

                    ECB.RemoveComponent<HasTarget>(chunkIndex, entity);
                    ECB.RemoveComponent<FindTargetCommandTag>(chunkIndex, entity);
                }
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

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
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

                // Check surrounding quadrants
                CheckQuadrant(hashKey, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey + 1, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey - 1, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey + QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey - QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);

                // Check corners
                CheckQuadrant(hashKey + 1 + QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey - 1 + QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey + 1 - QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);
                CheckQuadrant(hashKey - 1 - QuadrantSystem.QuadrantYMultiplier, unitPosition, quadrantEntity, animation,
                    ref closestTarget, ref closestDistanceSq, ref closestPosition);

                ClosestTargets[firstEntityIndex + i] = new EntityWithPosition
                {
                    Entity = closestTarget,
                    Position = closestPosition
                };
            }
        }

        private void CheckQuadrant(int hashKey, float2 unitPosition, QuadrantEntity quadrantEntity,
            AnimationComponent animation, ref Entity closestTarget, ref float closestDistanceSq, ref float2 closestPosition)
        {
            if (closestDistanceSq < 4.0f) return; // Early exit if already very close

            if (QuadrantHashMap.TryGetFirstValue(hashKey, out QuadrantData data, out var iterator))
            {
                do
                {
                    float distanceSq = math.distancesq(unitPosition, data.Position);
                    if (distanceSq >= closestDistanceSq) continue;

                    bool isEnemy = animation.UnitType != data.AnimationComponent.UnitType;
                    if (!isEnemy) continue;

                    closestTarget = data.Entity;
                    closestDistanceSq = distanceSq;
                    closestPosition = data.Position;

                } while (QuadrantHashMap.TryGetNextValue(out data, ref iterator));
            }
        }
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        _updateCounter++;
        if (_updateCounter % UpdateInterval != 0)
            return;

        // Check if there are any targets
        if (_targetQuery.CalculateEntityCount() == 0)
        {
            ClearCommands();
            return;
        }

        FindTargets();
    }

    private void ClearCommands()
    {
        var clearJob = new ClearCommandsJob
        {
            CommandDataTypeHandle = GetComponentTypeHandle<CommandData>(false),
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

        // Get the current buffer to write to
        NativeArray<EntityWithPosition> writeBuffer = _useBuffer1 ? _closestTargetsBuffer1 : _closestTargetsBuffer2;

        // Resize or create buffer if needed
        if (!writeBuffer.IsCreated || writeBuffer.Length != entityCount)
        {
            if (writeBuffer.IsCreated) writeBuffer.Dispose();
            writeBuffer = new NativeArray<EntityWithPosition>(entityCount, Allocator.Persistent);

            // Update the appropriate buffer reference
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
            EntityTypeHandle = GetEntityTypeHandle()
        };

        var findHandle = findJob.ScheduleParallel(_findTargetQuery, Dependency);

        var addComponentJob = new AddTargetComponentJob
        {
            ClosestTargets = writeBuffer,
            EntityTypeHandle = GetEntityTypeHandle(),
            ECB = _endSimulationECBSystem.CreateCommandBuffer().AsParallelWriter()
        };

        var addHandle = addComponentJob.ScheduleParallel(_findTargetQuery, findHandle);
        _endSimulationECBSystem.AddJobHandleForProducer(addHandle);

        // Switch buffers for next frame
        _useBuffer1 = !_useBuffer1;

        Dependency = addHandle;
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FindTargetSystem))]
public partial class TargetReevaluationSystem : SystemBase
{
    private float _nextReevaluationTime;
    private const float ReevaluationInterval = 5f;
    private EntityQuery _reevaluationQuery;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _reevaluationQuery = GetEntityQuery(
            ComponentType.ReadOnly<HasTarget>(),
            ComponentType.Exclude<FindTargetCommandTag>()
        );

        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate(_reevaluationQuery);
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        float currentTime = (float)Time.ElapsedTime;
        if (currentTime < _nextReevaluationTime)
            return;

        _nextReevaluationTime = currentTime + ReevaluationInterval;

        // Use EntityCommandBuffer for structural changes instead of EntityManager
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        //var random = new Unity.Mathematics.Random((uint)(currentTime * 1000));

        // Option 1: Using Entities.ForEach with proper Burst compatibility
        //Entities
        //    .WithName("ReevaluateTargets")
        //    .WithAll<HasTarget>()
        //    .WithNone<FindTargetCommandTag>()
        //    .ForEach((Entity entity, int entityInQueryIndex) =>
        //    {
        //        if (random.NextFloat() < 0.2f)
        //        {
        //            ecb.AddComponent<FindTargetCommandTag>(entityInQueryIndex, entity);
        //        }
        //    }).ScheduleParallel();

        //_ecbSystem.AddJobHandleForProducer(Dependency);

        // Option 2: Alternative approach using IJobChunk (more performant)


        if (!(_reevaluationQuery.CalculateEntityCount() > 0))
            return;
        var reevaluateJob = new ReevaluateTargetsJob
        {
            ECB = ecb,
            RandomSeed = (uint)(currentTime * 1000),
            EntityTypeHandle = GetEntityTypeHandle()
        };

        Dependency = reevaluateJob.ScheduleParallel(_reevaluationQuery, Dependency);
        _ecbSystem.AddJobHandleForProducer(Dependency);

    }

    // Option 2: Burst-compiled job version (recommended for performance)
    [BurstCompile]
    private struct ReevaluateTargetsJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint RandomSeed;

        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var random = new Unity.Mathematics.Random(RandomSeed + (uint)chunkIndex);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                if (random.NextFloat() < 0.2f)
                {
                    ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entities[i]);
                }
            }
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FindTargetSystem))]
    public partial class TargetValidationSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
                return;

            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var translationFromEntity = GetComponentDataFromEntity<Translation>(true);

            Entities
                .WithName("ValidateTargets")
                .WithReadOnly(translationFromEntity)
                .WithAll<HasTarget>()
                .ForEach((Entity entity, int entityInQueryIndex, ref HasTarget hasTarget) =>
                {
                    if (hasTarget.Type == HasTarget.TargetType.Entity &&
                        hasTarget.TargetEntity != Entity.Null &&
                        !translationFromEntity.HasComponent(hasTarget.TargetEntity))
                    {
                        ecb.AddComponent<FindTargetCommandTag>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                    }
                }).ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }

    // Removed SystemOrderLogger as it's likely debug-only code
