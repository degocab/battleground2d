using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[ExecuteAlways] // So it works in editor
public class PhysicsDebugDrawer : MonoBehaviour
{
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entities = entityManager.GetAllEntities(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            if (!entityManager.HasComponent<Translation>(entity) ||
                !entityManager.HasComponent<CircleCollider2D>(entity))
                continue;

            float3 pos = entityManager.GetComponentData<Translation>(entity).Value;
            float radius = entityManager.GetComponentData<CircleCollider2D>(entity).Radius;

            Gizmos.color = Color.green;
            DrawWireCircle2D(new float2(pos.x, pos.y), radius);

            // Optionally show collisions
            if (entityManager.HasComponent<CollisionEvent2D>(entity))
            {
                var buffer = entityManager.GetBuffer<CollisionEvent2D>(entity);
                foreach (var hit in buffer)
                {
                    if (!entityManager.HasComponent<Translation>(hit.OtherEntity))
                        continue;

                    float3 otherPos = entityManager.GetComponentData<Translation>(hit.OtherEntity).Value;
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(pos, otherPos);
                }
            }
        }

        entities.Dispose();
    }

    void DrawWireCircle2D(float2 center, float radius, int segments = 24)
    {
        float angleStep = math.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle0 = i * angleStep;
            float angle1 = (i + 1) * angleStep;

            float2 p0 = center + new float2(math.cos(angle0), math.sin(angle0)) * radius;
            float2 p1 = center + new float2(math.cos(angle1), math.sin(angle1)) * radius;

            Gizmos.DrawLine(new float3(p0.x, p0.y, 0), new float3(p1.x, p1.y, 0));
        }
    }
}
