using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using System.Linq;
using UnityEngine.Analytics;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public class CollisionDetectionSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        // Collect all positions and radii
        EntityQuery query = GetEntityQuery(typeof(Translation), typeof(ECS_CircleCollider2DAuthoring), typeof(CollidableTag) ,typeof(GridID));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeArray<GridID> gridIds = query.ToComponentDataArray<GridID>(Allocator.TempJob);
        NativeArray<Translation> positions = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<ECS_CircleCollider2DAuthoring> colliders = query.ToComponentDataArray<ECS_CircleCollider2DAuthoring>(Allocator.TempJob);

        NativeMultiHashMap<Entity, Entity> collisionEvents = new NativeMultiHashMap<Entity, Entity>(entities.Length * 4, Allocator.TempJob);


        // Clear previous collision buffers
        Entities
            .WithAll<CollisionEvent2D>()
            .ForEach((DynamicBuffer<CollisionEvent2D> buffer) =>
            {
                buffer.Clear();
            }).ScheduleParallel();

        EntityManager entityManager = EntityManager;
        CompleteDependency();

        var job = new CollisionDetectionJob
        {
            Entities = entities,
            Positions = positions,
            Colliders = colliders,
            GridIds = gridIds,
            CollisionEvents = collisionEvents.AsParallelWriter()
        };
        Dependency = job.Schedule(entities.Length, 64, Dependency);

        CompleteDependency();

        var bufferFromEntity = GetBufferFromEntity<CollisionEvent2D>();

        var keys = collisionEvents.GetKeyArray(Allocator.Temp);
        foreach (var entity in keys)
        {
            if (bufferFromEntity.HasComponent(entity))
            {
                DynamicBuffer<CollisionEvent2D> buffer = bufferFromEntity[entity];

                NativeMultiHashMapIterator<Entity> it;
                Entity other;
                if (collisionEvents.TryGetFirstValue(entity, out other, out it))
                {
                    do
                    {
                        buffer.Add(new CollisionEvent2D { OtherEntity = other });
                    } while (collisionEvents.TryGetNextValue(out other, ref it));
                }
            }
        }



        #region OriginalPhysicsSystem
        //for (int i = 0; i < entities.Length; i++)
        //{
        //    float2 posA = positions[i].Value.xy;
        //    float radiusA = colliders[i].Radius;
        //    var currentId = gridIds[i];
        //    for (int j = i + 1; j < entities.Length; j++)
        //    {
        //        var otherId = gridIds[j];
        //        if (otherId.value != currentId.value)
        //        {
        //            continue;
        //        }
        //        float2 posB = positions[j].Value.xy;
        //        float radiusB = colliders[j].Radius;

        //        float distSq = math.distancesq(posA, posB);
        //        float radiusSum = radiusA + radiusB;

        //        if (distSq <= radiusSum * radiusSum)
        //        {
        //            // Collision detected
        //            if (entityManager.HasComponent<CollisionEvent2D>(entities[i]) &&
        //                entityManager.HasComponent<CollisionEvent2D>(entities[j]))
        //            {
        //                entityManager.GetBuffer<CollisionEvent2D>(entities[i]).Add(new CollisionEvent2D { OtherEntity = entities[j] });
        //                entityManager.GetBuffer<CollisionEvent2D>(entities[j]).Add(new CollisionEvent2D { OtherEntity = entities[i] });
        //            }
        //        }
        //    }
        //} 
        #endregion

        #region CollisionDetectionTest
        //var job = new CollisionDetectionTestJob
        //{
        //    positions = positions,
        //    entities = entities,
        //    radii = colliders,
        //    gridData = gridIds,
        //    ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter()

        //};
        //// Schedule the job
        //JobHandle jobHandle = job.Schedule(query.CalculateEntityCount(), 256, Dependency);
        //jobHandle.Complete();

        //for (int i = 0; i < entities.Length-1; i++)
        //{
        //    var otherEntity = gridIds[i].otherEntity;
        //    entityManager.GetBuffer<CollisionEvent2D>(entities[i]).Add(new CollisionEvent2D { OtherEntity = otherEntity });
        //}
        //ecbSystem.AddJobHandleForProducer(jobHandle); 
        #endregion

        collisionEvents.Dispose();
        entities.Dispose();
        positions.Dispose();
        colliders.Dispose();
        gridIds.Dispose();
    }


    [BurstCompile]
    struct CollisionDetectionTestJob : IJobParallelFor
    {
        // Input data
        [NativeDisableParallelForRestriction]
        public NativeArray<Translation> positions;
        [ReadOnly] public NativeArray<Entity> entities;
        [ReadOnly] public NativeArray<ECS_CircleCollider2DAuthoring> radii;
         public NativeArray<GridID> gridData;
        //[NativeDisableParallelForRestriction]
        //public NativeArray<Translation> translations;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public BufferFromEntity<CollisionEvent2D> myComponentData;
        public float deltaTime;

        public void Execute(int index)
        {
            // Process each entity for collisions with every other entity

            if (index >= positions.Length || index >= entities.Length || index >= positions.Length)
            {
                return;
            }
            Entity entity = entities[index];
            Translation positionA = positions[index];
            ECS_CircleCollider2DAuthoring radius = radii[index];
            GridID currentGrid = gridData[index];
            int currentGridID = currentGrid.Value;

            float2 posA = positions[index].Value.xy;
            float radiusA = radius.Radius;


            for (int otherIndex = 0; otherIndex < entities.Length - 1; otherIndex++)
            {
                if (otherIndex >= entities.Length || otherIndex >= entities.Length || otherIndex >= entities.Length)
                {
                    return;
                }
                if (otherIndex == index) continue; // Skip comparing the unit against itself
                GridID otherGrid = gridData[index]; // Get the grid_id for the current unit
                var otherGridID = otherGrid.Value; // Get the grid_id for the other unit
                                                              // Skip if the grid IDs don't match
                if (currentGridID != otherGridID)
                {
                    continue; // Skip the collision check if grid IDs don't match
                }
                Entity otherEntity = entities[otherIndex];

                float2 posB = positions[otherIndex].Value.xy;
                float radiusB = radii[otherIndex].Radius;

                float distSq = math.distancesq(posA, posB);
                float radiusSum = radiusA + radiusB;

                if (distSq <= radiusSum * radiusSum)
                {

                    if (myComponentData.HasComponent(entity) && myComponentData.HasComponent(otherEntity)) // Check if entity has MyComponent
                    {
                        currentGrid.otherEntity = otherEntity; 
                        otherGrid.otherEntity = entity; 
                    }
                }
            }

        }
    }

    [BurstCompile]
    struct CollisionDetectionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<Translation> Positions;
        [ReadOnly] public NativeArray<ECS_CircleCollider2DAuthoring> Colliders;

        public NativeMultiHashMap<Entity, Entity>.ParallelWriter CollisionEvents;

        [ReadOnly] public NativeArray<GridID> GridIds;

        public void Execute(int i)
        {
            float2 posA = Positions[i].Value.xy;
            float radiusA = Colliders[i].Radius;
            GridID gridId = GridIds[i];



            for (int j = i + 1; j < Entities.Length; j++)
            {
                GridID gridIdB = GridIds[j];
                if (gridId.Value != gridIdB.Value) { continue; }
                float2 posB = Positions[j].Value.xy;
                float radiusB = Colliders[j].Radius;

                float distSq = math.distancesq(posA, posB);
                float radiusSum = radiusA + radiusB;

                if (distSq <= radiusSum * radiusSum)
                {
                    CollisionEvents.Add(Entities[i], Entities[j]);
                    CollisionEvents.Add(Entities[j], Entities[i]);
                }
            }
        }
    }



}
