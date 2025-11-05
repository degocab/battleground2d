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
[UpdateAfter(typeof(MovementSystem))]
public partial class CollisionQuadrantSystem : SystemBase
{
    public const int quadrantYMultiplier = 1000;
    public const int quadrantCellSize = 1;

    public static NativeMultiHashMap<int, CollisionQuadrantData> collisionQuadrantMap;

    private EntityQuery _collisionQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        _collisionQuery = GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<ECS_CircleCollider2DAuthoring>(),
            ComponentType.ReadOnly<CollidableTag>()
            //, ComponentType.ReadOnly<OutOfGroupTag>()
        //, ComponentType.Exclude<DeadTagComponent>()
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
        [ReadOnly] public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<ECS_CircleCollider2DAuthoring> ecsCircleCollider2DAuthoringType;
        [ReadOnly] public EntityTypeHandle EntityType;
        public NativeMultiHashMap<int, CollisionQuadrantData>.ParallelWriter QuadrantMap;
        [ReadOnly] public ComponentTypeHandle<DeadTagComponent> DeadTagTypeHandle; //ignore dead units


        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationComponentType;
        [ReadOnly] public ComponentTypeHandle<ECS_PhysicsBody2DAuthoring> ecsPhysicsBody2DAuthoringAuthoringType;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // Check if chunk has DeadTagComponent - if so, skip this chunk entirely
            if (chunk.Has(ref DeadTagTypeHandle))
                return;
            var transforms = chunk.GetNativeArray(ref TransformType);
            var animationComponents = chunk.GetNativeArray(ref AnimationComponentType);
            var entities = chunk.GetNativeArray(EntityType);
            var ECS_CircleCollider2DAuthorings = chunk.GetNativeArray(ref ecsCircleCollider2DAuthoringType);
            var ecsPhysicsBody2DAuthorings = chunk.GetNativeArray(ref ecsPhysicsBody2DAuthoringAuthoringType);

            for (int i = 0; i < chunk.Count; i++)
            {
                //float2 pos = translations[i].Value.xy;
                float2 pos = new float2(transforms[i].Position.x, transforms[i].Position.y - .25f);
                int key = GetPositionHashMapKey(pos);
                QuadrantMap.Add(key, new CollisionQuadrantData
                {
                    entity = entities[i],
                    position = pos,
                    radius = ECS_CircleCollider2DAuthorings[i].Radius,
                    unitType = animationComponents[i].UnitType
                    , CollisionSourceTransform = transforms[i]
                    , CollisionSourceCollider = ECS_CircleCollider2DAuthorings[i]
                    , CollisionSourceBody = ecsPhysicsBody2DAuthorings[i]

                });
            }
        }
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        collisionQuadrantMap.Clear();
        int count = _collisionQuery.CalculateEntityCount();

        if (collisionQuadrantMap.Capacity < count)
            collisionQuadrantMap.Capacity = count;

        var job = new SetCollisionQuadrantMapJob
        {
            TransformType = GetComponentTypeHandle<LocalTransform>(true),
            AnimationComponentType = GetComponentTypeHandle<AnimationComponent>(true),
            ecsCircleCollider2DAuthoringType = GetComponentTypeHandle<ECS_CircleCollider2DAuthoring>(true),
            ecsPhysicsBody2DAuthoringAuthoringType = GetComponentTypeHandle<ECS_PhysicsBody2DAuthoring>(true),
            EntityType = GetEntityTypeHandle(),
            QuadrantMap = collisionQuadrantMap.AsParallelWriter(),
            DeadTagTypeHandle = GetComponentTypeHandle<DeadTagComponent>(true)
        };

        Dependency = job.ScheduleParallel(_collisionQuery, Dependency);
        Dependency.Complete(); // Optional depending on if you're accessing it immediately
    }
}
