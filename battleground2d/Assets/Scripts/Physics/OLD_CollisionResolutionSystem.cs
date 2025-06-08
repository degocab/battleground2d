using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(OLD_CollisionDetectionSystem))]
public class OLD_CollisionResolutionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityManager entityManager = EntityManager;

        Entities
            .WithName("CollisionResolutionSystem")
            .WithBurst() // Optional: add after testing
            .ForEach((ref Translation translation, ref Velocity2D velocity, ref PhysicsBody2D body, ref CircleCollider2D collider, ref DynamicBuffer<CollisionEvent2D> collisions) =>
            {
                if (body.IsStatic || collisions.Length == 0)
                {
                    //velocity.Value = velocity.PrevValue;
                    return;
                }
                    

                float2 position = translation.Value.xy;
                float totalPushX = 0f;
                float totalPushY = 0f;

                for (int i = 0; i < collisions.Length; i++)
                {
                    var collision = collisions[i];
                    if (!entityManager.HasComponent<Translation>(collision.OtherEntity) ||
                        !entityManager.HasComponent<CircleCollider2D>(collision.OtherEntity) ||
                        !entityManager.HasComponent<PhysicsBody2D>(collision.OtherEntity))
                        continue;

                    float2 otherPos = entityManager.GetComponentData<Translation>(collision.OtherEntity).Value.xy;
                    float otherRadius = entityManager.GetComponentData<CircleCollider2D>(collision.OtherEntity).Radius;
                    var otherBody = entityManager.GetComponentData<PhysicsBody2D>(collision.OtherEntity);

                    float2 delta = position - otherPos;
                    float dist = math.length(delta);
                    float minDist = collider.Radius + otherRadius;

                    // Prevent divide by zero
                    if (dist == 0f)
                    {
                        delta = new float2(.125f, 0f); // Arbitrary push direction
                        dist = 0.001f;
                    }

                    float2 direction = delta / dist;
                    float penetration = minDist - dist;

                    if (penetration > 0f)
                    {
                        // Distribute movement (half if both are dynamic)
                        float2 push = direction * penetration;

                        if (!otherBody.IsStatic)
                            push *= 0.125f;

                        totalPushX += push.x;
                        totalPushY += push.y;
                    }
                }

                // Apply final push
                translation.Value.xy += new float2(totalPushX, totalPushY);

                // Keep Z = 0 (2D only)
                translation.Value.z = 0f;

                //velocity.Value.xy += new float2(totalPushX, totalPushY);

            })
            .Run(); // Run on main thread for now to access EntityManager



    }
}
