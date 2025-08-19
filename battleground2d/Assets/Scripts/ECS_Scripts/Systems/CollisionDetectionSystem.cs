using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using System.Linq;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public partial class CollisionDetectionSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    // Persistent buffers for quadrant offsets and collision events
    private NativeArray<int2> quadrantOffsets;
    private NativeMultiHashMap<Entity, Entity> collisionEvents;

    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();

        // 9 offsets for current + neighbors (including diagonals)
        quadrantOffsets = new NativeArray<int2>(9, Allocator.Persistent);
        quadrantOffsets[0] = new int2(0, 0);
        quadrantOffsets[1] = new int2(1, 0);
        quadrantOffsets[2] = new int2(-1, 0);
        quadrantOffsets[3] = new int2(0, 1);
        quadrantOffsets[4] = new int2(0, -1);
        quadrantOffsets[5] = new int2(1, 1);
        quadrantOffsets[6] = new int2(1, -1);
        quadrantOffsets[7] = new int2(-1, 1);
        quadrantOffsets[8] = new int2(-1, -1);

        // Initial capacity, will grow automatically if needed (tweak as needed)
        collisionEvents = new NativeMultiHashMap<Entity, Entity>(1024, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        if (quadrantOffsets.IsCreated) quadrantOffsets.Dispose();
        if (collisionEvents.IsCreated) collisionEvents.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(ECS_CircleCollider2DAuthoring), typeof(CollidableTag), ComponentType.ReadWrite<CollisionEvent2D>());

        int totalEntities = entityQuery.CalculateEntityCount();

        const int maxCollisionsPerEntity = 16; // realistic max collisions per entity
        int estimatedCapacity = math.max(1024, totalEntities * maxCollisionsPerEntity);

        if (collisionEvents.Capacity < estimatedCapacity)
        {
            // Dispose old and allocate new only if really needed, with a max cap to avoid overflow
            int newCapacity = math.min(estimatedCapacity, 10_000_000); // limit max allocation
            collisionEvents.Dispose();
            collisionEvents = new NativeMultiHashMap<Entity, Entity>(newCapacity, Allocator.Persistent);
        }
        else
        {
            collisionEvents.Clear();
        }

        //var quadrantOffsetsCopy = quadrantOffsets;
        //var collisionEventsParallelWriter = collisionEvents.AsParallelWriter();

        //// Get handles for job safety and performance
        //var translationType = GetComponentTypeHandle<Translation>(true);
        //var colliderType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true);
        //var entityType = GetEntityTypeHandle();

        //var collisionQuadrantMap = CollisionQuadrantSystem.collisionQuadrantMap;

        // Schedule collision detection job
        //var collisionJob = new CollisionDetectionJob
        //{
        //    TranslationType = translationType,
        //    ColliderType = colliderType,
        //    EntityType = entityType,
        //    QuadrantOffsets = quadrantOffsetsCopy,
        //    collisionQuadrantMap = collisionQuadrantMap,
        //    CollisionEvents = collisionEventsParallelWriter
        //};

        //Dependency = collisionJob.ScheduleParallel(entityQuery, 64, Dependency);
        //Dependency.Complete();

        var collisionJob = new CollisionDetectionJob
        {
            TranslationType = GetComponentTypeHandle<Translation>(true),
            ColliderType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true),
            EntityType = GetEntityTypeHandle(),
            QuadrantOffsets = quadrantOffsets,
            collisionQuadrantMap = CollisionQuadrantSystem.collisionQuadrantMap,
            CollisionEvents = collisionEvents.AsParallelWriter()
        };

        Dependency = collisionJob.ScheduleParallel(entityQuery, Dependency);
        Dependency.Complete();


        //var bufferEntities = GetEntityQuery(typeof(CollisionEvent2D));
        //var bufferEntitySet = bufferEntities.ToEntityArray(Allocator.TempJob);
        //var keys = collisionEvents.GetKeyArray(Allocator.TempJob);

        //var filteredKeys = new NativeList<Entity>(Allocator.TempJob);
        //for (int i = 0; i < keys.Length; i++)
        //{
        //    if (bufferEntitySet.Contains(keys[i]))
        //        filteredKeys.Add(keys[i]);
        //}
        //BufferFromEntity<CollisionEvent2D> entityBuffers = GetBufferFromEntity<CollisionEvent2D>();
        //if (filteredKeys.Length > 0)
        //{
        //    var writeJob = new WriteCollisionBuffersJob
        //    {
        //        Keys = filteredKeys.AsDeferredJobArray(),
        //        CollisionEvents = collisionEvents,
        //        CollisionEventBuffers = entityBuffers
        //    };

        //    Dependency = writeJob.Schedule(filteredKeys.Length, 64, Dependency);
        //    Dependency.Complete();
        //}

        //bufferEntitySet.Dispose();
        //filteredKeys.Dispose();
        //keys.Dispose();
        var job = new WriteCollisionBuffersChunkJob
        {
            CollisionEvents = collisionEvents,
            EntityHandle = GetEntityTypeHandle(),
            CollisionBufferHandle = GetBufferTypeHandle<CollisionEvent2D>(false),
        };

        Dependency = job.ScheduleParallel(entityQuery, Dependency);



        //// Collect all unique entities from collisionEvents keys
        //var keys = collisionEvents.GetKeyArray(Allocator.TempJob);

        //// Schedule job to write collision buffers
        //var writeJob = new WriteCollisionBuffersJob
        //{
        //    Keys = keys,
        //    CollisionEvents = collisionEvents,
        //    CollisionEventBuffers = GetBufferFromEntity<CollisionEvent2D>()
        //};
        //Dependency = writeJob.Schedule(keys.Length, 64, Dependency);
        //Dependency.Complete();
        //keys.Dispose();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }

    public struct WriteCollisionBuffersChunkJob : IJobChunk
    {
        [ReadOnly] public NativeMultiHashMap<Entity, Entity> CollisionEvents;
        [ReadOnly] public EntityTypeHandle EntityHandle;
        public BufferTypeHandle<CollisionEvent2D> CollisionBufferHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(EntityHandle);
            var buffers = chunk.GetBufferAccessor(CollisionBufferHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                var buffer = buffers[i];
                buffer.Clear();

                if (CollisionEvents.TryGetFirstValue(entity, out var other, out var it))
                {
                    const int MaxCollisions = 16;
                    int count = 0;
                    do
                    {
                        if (count++ < MaxCollisions)
                            buffer.Add(new CollisionEvent2D { OtherEntity = other });
                    }
                    while (CollisionEvents.TryGetNextValue(out other, ref it));
                }
            }
        }
    }


    [BurstCompile]
    struct CollisionDetectionJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationType;
        [ReadOnly] public ComponentTypeHandle<ECS_CircleCollider2DAuthoring> ColliderType;
        [ReadOnly] public EntityTypeHandle EntityType;

        [ReadOnly] public NativeArray<int2> QuadrantOffsets;
        [ReadOnly] public NativeMultiHashMap<int, CollisionQuadrantData> collisionQuadrantMap;
        public NativeMultiHashMap<Entity, Entity>.ParallelWriter CollisionEvents;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var translations = chunk.GetNativeArray(TranslationType);
            var colliders = chunk.GetNativeArray(ColliderType);
            var entities = chunk.GetNativeArray(EntityType);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entityA = entities[i];
                float2 posA = translations[i].Value.xy;
                float radiusA = colliders[i].Radius;

                int baseX = (int)math.floor(posA.x / CollisionQuadrantSystem.quadrantCellSize);
                int baseY = (int)math.floor(posA.y / CollisionQuadrantSystem.quadrantCellSize);

                for (int j = 0; j < QuadrantOffsets.Length; j++)
                {
                    int2 offset = QuadrantOffsets[j];
                    int2 cell = new int2(baseX + offset.x, baseY + offset.y);
                    int hash = cell.x + cell.y * CollisionQuadrantSystem.quadrantYMultiplier;

                    if (!collisionQuadrantMap.TryGetFirstValue(hash, out var otherData, out var it))
                        continue;

                    do
                    {
                        Entity entityB = otherData.entity;
                        if (entityA == entityB)
                            continue;

                        float2 posB = otherData.position;
                        float radiusB = otherData.radius;

                        float distSq = math.distancesq(posA, posB);
                        float combinedRadius = radiusA + radiusB;

                        if (distSq <= combinedRadius * combinedRadius)
                        {
                            CollisionEvents.Add(entityA, entityB);
                            CollisionEvents.Add(entityB, entityA);
                        }
                    }
                    while (collisionQuadrantMap.TryGetNextValue(out otherData, ref it));
                }
            }
        }
    }



    //[BurstCompile]
    //struct CollisionDetectionJob : IJobEntityBatch
    //{
    //    [ReadOnly] public ComponentTypeHandle<Translation> TranslationType;
    //    [ReadOnly] public ComponentTypeHandle<ECS_CircleCollider2DAuthoring> ColliderType;
    //    [ReadOnly] public EntityTypeHandle EntityType;

    //    [ReadOnly] public NativeArray<int2> QuadrantOffsets;
    //    [ReadOnly] public NativeMultiHashMap<int, CollisionQuadrantData> collisionQuadrantMap;
    //    public NativeMultiHashMap<Entity, Entity>.ParallelWriter CollisionEvents;

    //    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    //    {
    //        var entities = batchInChunk.GetNativeArray(EntityType);
    //        var positions = batchInChunk.GetNativeArray(TranslationType);
    //        var colliders = batchInChunk.GetNativeArray(ColliderType);

    //        for (int i = 0; i < batchInChunk.Count; i++)
    //        {
    //            Entity entityA = entities[i];
    //            float2 posA = positions[i].Value.xy;
    //            float radiusA = colliders[i].Radius;

    //            int baseX = (int)math.floor(posA.x / CollisionQuadrantSystem.quadrantCellSize);
    //            int baseY = (int)math.floor(posA.y / CollisionQuadrantSystem.quadrantCellSize);

    //            for (int offsetIndex = 0; offsetIndex < QuadrantOffsets.Length; offsetIndex++)
    //            {
    //                int2 offset = QuadrantOffsets[offsetIndex];
    //                int2 cell = new int2(baseX + offset.x, baseY + offset.y);

    //                int neighborKey = cell.x + cell.y * CollisionQuadrantSystem.quadrantYMultiplier;

    //                CollisionQuadrantData otherData;
    //                NativeMultiHashMapIterator<int> it;

    //                if (collisionQuadrantMap.TryGetFirstValue(neighborKey, out otherData, out it))
    //                {
    //                    do
    //                    {
    //                        if (otherData.entity == entityA)
    //                            continue;

    //                        float2 posB = otherData.position;
    //                        float radiusB = otherData.radius;

    //                        float distSq = math.distancesq(posA, posB);
    //                        float radiusSum = radiusA + radiusB;

    //                        if (distSq <= radiusSum * radiusSum)
    //                        {
    //                            CollisionEvents.Add(entityA, otherData.entity);
    //                            CollisionEvents.Add(otherData.entity, entityA);
    //                        }
    //                    } while (collisionQuadrantMap.TryGetNextValue(out otherData, ref it));
    //                }
    //            }
    //        }
    //    }
    //}


    //[BurstCompile]
    //struct WriteCollisionBuffersJob : IJobParallelFor
    //{
    //    [ReadOnly] public NativeArray<Entity> Keys;
    //    [ReadOnly] public NativeMultiHashMap<Entity, Entity> CollisionEvents;
    //    [NativeDisableParallelForRestriction] public BufferFromEntity<CollisionEvent2D> CollisionEventBuffers;

    //    public void Execute(int index)
    //    {
    //        Entity entity = Keys[index];

    //        if (!CollisionEventBuffers.HasComponent(entity))
    //            return;

    //        var buffer = CollisionEventBuffers[entity];
    //        buffer.Clear();

    //        if (!CollisionEvents.TryGetFirstValue(entity, out var other, out var it))
    //            return;

    //        const int MaxCollisions = 16;

    //        buffer.Add(new CollisionEvent2D { OtherEntity = other });

    //        while (CollisionEvents.TryGetNextValue(out other, ref it) && buffer.Length < MaxCollisions)
    //        {
    //            buffer.Add(new CollisionEvent2D { OtherEntity = other });
    //        }
    //    }
    //}


    //    [BurstCompile]
    //    struct WriteCollisionBuffersJob : IJobParallelFor
    //    {
    //        [ReadOnly] public NativeArray<Entity> Keys;
    //        [ReadOnly] public NativeMultiHashMap<Entity, Entity> CollisionEvents;
    //        [NativeDisableParallelForRestriction] public BufferFromEntity<CollisionEvent2D> CollisionEventBuffers;

    //        public void Execute(int index)
    //        {
    //            Entity entity = Keys[index];

    //            if (!CollisionEventBuffers.HasComponent(entity))
    //                return;

    //            var buffer = CollisionEventBuffers[entity];
    //            // Extra safety: clear only if buffer is not null (should always be true)
    //            if (buffer.IsCreated)
    //            {
    //                buffer.Clear();
    //            }
    //            else
    //            {
    //                // Early exit if buffer is invalid somehow
    //                return;
    //            }

    //            NativeMultiHashMapIterator<Entity> it;
    //            Entity other;
    //            if (CollisionEvents.TryGetFirstValue(entity, out other, out it))
    //            {
    //                do
    //                {
    //                    buffer.Add(new CollisionEvent2D { OtherEntity = other });
    //                } while (CollisionEvents.TryGetNextValue(out other, ref it));
    //            }
    //        }
    //    }
}


public struct CollisionQuadrantData
{
    public Entity entity;
    public float2 position;
    public float radius;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(CollisionDetectionSystem))]
public partial class CollisionQuadrantSystem : SystemBase
{
    public const int quadrantYMultiplier = 1000;
    public const int quadrantCellSize = 10;

    public static NativeMultiHashMap<int, CollisionQuadrantData> collisionQuadrantMap;

    private EntityQuery _collisionQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        _collisionQuery = GetEntityQuery(
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<ECS_CircleCollider2DAuthoring>(),
            ComponentType.ReadOnly<CollidableTag>()
        );

        collisionQuadrantMap = new NativeMultiHashMap<int, CollisionQuadrantData>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        if (collisionQuadrantMap.IsCreated)
            collisionQuadrantMap.Dispose();

        base.OnDestroy();
    }

    public static int GetPositionHashMapKey(float2 position)
    {
        return (int)(math.floor(position.x / quadrantCellSize) +
                     quadrantYMultiplier * math.floor(position.y / quadrantCellSize));
    }

    [BurstCompile]
    private struct SetCollisionQuadrantMapJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationType;
        [ReadOnly] public ComponentTypeHandle<ECS_CircleCollider2DAuthoring> ecsCircleCollider2DAuthoringType;
        [ReadOnly] public EntityTypeHandle EntityType;
        public NativeMultiHashMap<int, CollisionQuadrantData>.ParallelWriter QuadrantMap;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var translations = chunk.GetNativeArray(TranslationType);
            var entities = chunk.GetNativeArray(EntityType);
            var ECS_CircleCollider2DAuthorings = chunk.GetNativeArray(ecsCircleCollider2DAuthoringType);

            for (int i = 0; i < chunk.Count; i++)
            {
                float2 pos = translations[i].Value.xy;
                int key = GetPositionHashMapKey(pos);
                QuadrantMap.Add(key, new CollisionQuadrantData
                {
                    entity = entities[i],
                    position = pos,
                    radius = ECS_CircleCollider2DAuthorings[i].Radius
                });
            }
        }
    }

    protected override void OnUpdate()
    {
        collisionQuadrantMap.Clear();
        int count = _collisionQuery.CalculateEntityCount();

        if (collisionQuadrantMap.Capacity < count)
            collisionQuadrantMap.Capacity = count;

        var job = new SetCollisionQuadrantMapJob
        {
            TranslationType = GetComponentTypeHandle<Translation>(true),
            ecsCircleCollider2DAuthoringType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true),
            EntityType = GetEntityTypeHandle(),
            QuadrantMap = collisionQuadrantMap.AsParallelWriter()
        };

        Dependency = job.ScheduleParallel(_collisionQuery, Dependency);
        Dependency.Complete(); // Optional depending on if you're accessing it immediately
    }
}
