using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using System.Linq;


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
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
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
