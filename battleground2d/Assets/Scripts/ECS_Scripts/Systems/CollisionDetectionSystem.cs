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
    public static NativeList<CollisionPair> GlobalCollisionPairs;

    // Persistent buffers for quadrant offsets and collision events
    private NativeArray<int2> quadrantOffsets;
    private NativeMultiHashMap<Entity, CollisionQuadrantData> collisionEvents;
    private EntityQuery _entityQuery; // Add this field
    protected override void OnCreate()
    {

        _entityQuery = GetEntityQuery(typeof(Translation), typeof(ECS_CircleCollider2DAuthoring), typeof(CollidableTag), ComponentType.ReadWrite<CollisionEvent2D>() 
            //,ComponentType.Exclude<DeadTagComponent>()
            );
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
        collisionEvents = new NativeMultiHashMap<Entity, CollisionQuadrantData>(1024, Allocator.Persistent);
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

        ////int maxCollisionsPerEntity = 8;
        ////int totalEntities = _entityQuery.CalculateEntityCount();
        ////int estimatedCapacity = totalEntities * maxCollisionsPerEntity;
        ////if (!GlobalCollisionPairs.IsCreated)
        ////{
        ////    GlobalCollisionPairs = new NativeList<CollisionPair>(estimatedCapacity, Allocator.Persistent);
        ////}
        ////if (GlobalCollisionPairs.Capacity < estimatedCapacity)
        ////{
        ////    GlobalCollisionPairs.Dispose();
        ////    GlobalCollisionPairs = new NativeList<CollisionPair>(estimatedCapacity, Allocator.Persistent);
        ////}
        ////GlobalCollisionPairs.Clear();


        ////var job = new CollisionDetectionJobGlobalList
        ////{
        ////      TranslationType = GetComponentTypeHandle<Translation>(true)
        ////    , ColliderType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true)
        ////    , BodyType = GetComponentTypeHandle<ECS_PhysicsBody2DAuthoring>(true)
        ////    , EntityType = GetEntityTypeHandle()
        ////    , DeadTagTypeHandle = GetComponentTypeHandle<DeadTagComponent>(true)
        ////    , QuadrantOffsets = quadrantOffsets
        ////    , CollisionQuadrantMap = CollisionQuadrantSystem.collisionQuadrantMap
        ////    ,  GlobalCollisions = GlobalCollisionPairs.AsParallelWriter()
        ////    //CollisionEvents = collisionEvents,
        ////    //        EntityHandle = GetEntityTypeHandle(),
        ////    //        CollisionBufferHandle = GetBufferTypeHandle<CollisionEvent2D>(false)
        ////    //        ,
        ////    //        TranslationTypeHandle = GetComponentTypeHandle<Translation>(true)
        ////    //        ,
        ////    //        BodyTypeHandle = GetComponentTypeHandle<ECS_PhysicsBody2DAuthoring>(true)
        ////    //        ,
        ////    //        ColliderTypeHandle = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true)
        ////};

        //////JobHandle collisionJobHandle = job.ScheduleParallel(_entityQuery);
        //////Dependency = collisionJobHandle;

        ////// Schedule and complete the dependency chain
        ////JobHandle handle = job.ScheduleParallel(_entityQuery, Dependency);

        ////// Force completion so CollisionResolutionSystem can read the results
        ////handle.Complete();

        ////Dependency = handle;

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
            collisionEvents = new NativeMultiHashMap<Entity, CollisionQuadrantData>(newCapacity, Allocator.Persistent);
        }
        else
        {
            collisionEvents.Clear();
        }


        var collisionJob = new CollisionDetectionJob
        {
            TranslationType = GetComponentTypeHandle<Translation>(true),
            ColliderType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true),
            BodyType = GetComponentTypeHandle<ECS_PhysicsBody2DAuthoring>(true),
            EntityType = GetEntityTypeHandle(),
            QuadrantOffsets = quadrantOffsets,
            collisionQuadrantMap = CollisionQuadrantSystem.collisionQuadrantMap,
            CollisionEvents = collisionEvents.AsParallelWriter(),
            DeadTagTypeHandle = GetComponentTypeHandle<DeadTagComponent>(true) // Add this
        };

        JobHandle collisionJobHandle = collisionJob.ScheduleParallel(_entityQuery, Dependency);


        var job = new WriteCollisionBuffersChunkJob
        {
            CollisionEvents = collisionEvents,
            EntityHandle = GetEntityTypeHandle(),
            CollisionBufferHandle = GetBufferTypeHandle<CollisionEvent2D>(false)
            ,
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true)
            ,
            BodyTypeHandle = GetComponentTypeHandle<ECS_PhysicsBody2DAuthoring>(true)
            ,
            ColliderTypeHandle = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true)
        };

        JobHandle bufferJobHandle = job.ScheduleParallel(_entityQuery, collisionJobHandle);


        ecbSystem.AddJobHandleForProducer(Dependency);
        // Set the system's Dependency to the final job handle, so the next system waits for us.
        Dependency = bufferJobHandle;
    }


    [BurstCompile]
    public struct CollisionDetectionJobGlobalList : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationType;
        [ReadOnly] public ComponentTypeHandle<ECS_CircleCollider2DAuthoring> ColliderType;
        [ReadOnly] public ComponentTypeHandle<ECS_PhysicsBody2DAuthoring> BodyType;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<DeadTagComponent> DeadTagTypeHandle;

        [ReadOnly] public NativeArray<int2> QuadrantOffsets;
        [ReadOnly] public NativeMultiHashMap<int, CollisionQuadrantData> CollisionQuadrantMap;

        public NativeList<CollisionPair>.ParallelWriter GlobalCollisions;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            if (chunk.Has<DeadTagComponent>(DeadTagTypeHandle))
                return;

            var translations = chunk.GetNativeArray(TranslationType);
            var colliders = chunk.GetNativeArray(ColliderType);
            var bodies = chunk.GetNativeArray(BodyType);
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

                    if (!CollisionQuadrantMap.TryGetFirstValue(hash, out CollisionQuadrantData otherData, out NativeMultiHashMapIterator<int> it))
                        continue;

                    do
                    {
                        Entity entityB = otherData.entity;
                        if (entityA.Index >= entityB.Index) // avoid duplicates
                            continue;

                        float2 posB = otherData.position;
                        float radiusB = otherData.radius;

                        if (math.distancesq(posA, posB) <= (radiusA + radiusB) * (radiusA + radiusB))
                        {
                            GlobalCollisions.AddNoResize(new CollisionPair
                            {
                                A = entityA,
                                B = entityB,
                                PosA = posA,
                                PosB = posB,
                                RadiusA = radiusA,
                                RadiusB = radiusB,
                                BodyA = bodies[i],
                                BodyB = otherData.CollisionSourceBody,
                                ColliderA = colliders[i],
                                ColliderB = otherData.CollisionSourceCollider
                            });
                        }
                    }
                    while (CollisionQuadrantMap.TryGetNextValue(out otherData, ref it));
                }
            }
        }
    }


    public struct WriteCollisionBuffersChunkJob : IJobChunk
    {
        [ReadOnly] public NativeMultiHashMap<Entity, CollisionQuadrantData> CollisionEvents;
        [ReadOnly] public EntityTypeHandle EntityHandle;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<ECS_PhysicsBody2DAuthoring> BodyTypeHandle;
        [ReadOnly] public ComponentTypeHandle<ECS_CircleCollider2DAuthoring> ColliderTypeHandle;
        public BufferTypeHandle<CollisionEvent2D> CollisionBufferHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(EntityHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var bodies = chunk.GetNativeArray(BodyTypeHandle);
            var colliders = chunk.GetNativeArray(ColliderTypeHandle);
            var buffers = chunk.GetBufferAccessor(CollisionBufferHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                Translation translation = translations[i];
                ECS_PhysicsBody2DAuthoring body = bodies[i];
                ECS_CircleCollider2DAuthoring collider = colliders[i];
                var buffer = buffers[i];
                buffer.Clear();

                if (CollisionEvents.TryGetFirstValue(entity, out var other, out var it))
                {
                    const int MaxCollisions = 16;
                    int count = 0;
                    do
                    {
                        if (count++ < MaxCollisions)
                            buffer.Add(new CollisionEvent2D { OtherEntity = other.entity, OtherBody = other.CollisionSourceBody, OtherCollider = other.CollisionSourceCollider, OtherTranslation = other.CollisionSourceTranslation });
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
        [ReadOnly] public ComponentTypeHandle<ECS_PhysicsBody2DAuthoring> BodyType;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<DeadTagComponent> DeadTagTypeHandle; //ignore dead units

        [ReadOnly] public NativeArray<int2> QuadrantOffsets;
        [ReadOnly] public NativeMultiHashMap<int, CollisionQuadrantData> collisionQuadrantMap;
        public NativeMultiHashMap<Entity, CollisionQuadrantData>.ParallelWriter CollisionEvents;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            // Check if chunk has DeadTagComponent - if so, skip this chunk entirely
            if (chunk.Has<DeadTagComponent>(DeadTagTypeHandle))
                return;

            var translations = chunk.GetNativeArray(TranslationType);
            var colliders = chunk.GetNativeArray(ColliderType);
            var entities = chunk.GetNativeArray(EntityType);
            var bodies = chunk.GetNativeArray(BodyType);

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
                    //int hash = (cell.x * 73856093) ^ (cell.y * 19349663);

                    if (!collisionQuadrantMap
                            .TryGetFirstValue(hash, out CollisionQuadrantData otherData, out NativeMultiHashMapIterator<int> it))
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
                            CollisionEvents.Add(entityA, /*entityB*/otherData);
                            CollisionEvents.Add(entityB, /*entityA*/
                                new CollisionQuadrantData { CollisionSourceTranslation = translations[i],
                                                            CollisionSourceCollider = colliders[i],
                                                            CollisionSourceBody = bodies[i]
                                });
                        }
                    }
                    while (collisionQuadrantMap.TryGetNextValue(out otherData, ref it));
                }
            }
        }
    }

}