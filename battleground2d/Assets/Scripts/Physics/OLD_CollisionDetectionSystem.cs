using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class OLD_CollisionDetectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Collect all positions and radii
        EntityQuery query = GetEntityQuery(typeof(Translation), typeof(CircleCollider2D), typeof(CollidableTag));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> positions = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<CircleCollider2D> colliders = query.ToComponentDataArray<CircleCollider2D>(Allocator.TempJob);

        // Clear previous collision buffers
        Entities
            .WithAll<CollisionEvent2D>()
            .ForEach((DynamicBuffer<CollisionEvent2D> buffer) =>
            {
                buffer.Clear();
            }).ScheduleParallel();

        EntityManager entityManager = EntityManager;
        CompleteDependency();
        for (int i = 0; i < entities.Length; i++)
        {
            float2 posA = positions[i].Value.xy;
            float radiusA = colliders[i].Radius;

            for (int j = i + 1; j < entities.Length; j++)
            {
                float2 posB = positions[j].Value.xy;
                float radiusB = colliders[j].Radius;

                float distSq = math.distancesq(posA, posB);
                float radiusSum = radiusA + radiusB;

                if (distSq <= radiusSum * radiusSum)
                {
                    // Collision detected
                    if (entityManager.HasComponent<CollisionEvent2D>(entities[i]) &&
                        entityManager.HasComponent<CollisionEvent2D>(entities[j]))
                    {
                        entityManager.GetBuffer<CollisionEvent2D>(entities[i]).Add(new CollisionEvent2D { OtherEntity = entities[j] });
                        entityManager.GetBuffer<CollisionEvent2D>(entities[j]).Add(new CollisionEvent2D { OtherEntity = entities[i] });
                    }
                }
            }
        }

        entities.Dispose();
        positions.Dispose();
        colliders.Dispose();
    }
}
