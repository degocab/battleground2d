using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using System.Linq;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(CollisionResolutionSystem))]
[UpdateAfter(typeof(CollisionQuadrantSystem))]
public partial class CollisionDetectionSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    // Persistent buffers for quadrant offsets and collision events
    private NativeArray<int2> quadrantOffsets;
    private NativeMultiHashMap<Entity, Entity> collisionEvents;
    private EntityQuery _entityQuery; // Add this field
    protected override void OnCreate()
    {

        _entityQuery = GetEntityQuery(typeof(Translation), typeof(ECS_CircleCollider2DAuthoring), typeof(CollidableTag), ComponentType.ReadWrite<CollisionEvent2D>());
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
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

        //EntityQuery _entityQuery = GetEntityQuery(typeof(Translation), typeof(ECS_CircleCollider2DAuthoring), typeof(CollidableTag), ComponentType.ReadWrite<CollisionEvent2D>());

        int totalEntities = _entityQuery.CalculateEntityCount();

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


        var collisionJob = new CollisionDetectionJob
        {
            TranslationType = GetComponentTypeHandle<Translation>(true),
            ColliderType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true),
            EntityType = GetEntityTypeHandle(),
            QuadrantOffsets = quadrantOffsets,
            collisionQuadrantMap = CollisionQuadrantSystem.collisionQuadrantMap,
            CollisionEvents = collisionEvents.AsParallelWriter()
        };

        JobHandle collisionJobHandle = collisionJob.ScheduleParallel(_entityQuery, Dependency);


        var job = new WriteCollisionBuffersChunkJob
        {
            CollisionEvents = collisionEvents,
            EntityHandle = GetEntityTypeHandle(),
            CollisionBufferHandle = GetBufferTypeHandle<CollisionEvent2D>(false),
        };

        JobHandle bufferJobHandle = job.ScheduleParallel(_entityQuery, collisionJobHandle);


        ecbSystem.AddJobHandleForProducer(Dependency);
        // Set the system's Dependency to the final job handle, so the next system waits for us.
        Dependency = bufferJobHandle;
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

}