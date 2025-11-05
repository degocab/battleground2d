using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystem))]
[UpdateAfter(typeof(CollisionDetectionSystem))]
public class CollisionResolutionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        EntityManager entityManager = EntityManager;

        foreach (var (transform, velocity, body, collider, collisions) in 
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<ECS_Velocity2D>, RefRO<ECS_PhysicsBody2DAuthoring>, RefRO<ECS_CircleCollider2DAuthoring>, DynamicBuffer<CollisionEvent2D>>()
                .WithNone<DeadTagComponent>())
        {
            if (body.ValueRO.isStatic || collisions.Length == 0)
            {
                //velocity.Value = velocity.PrevValue;
                continue;
            }

            float2 position = transform.ValueRO.Position.xy;
            float totalPushX = 0f;
            float totalPushY = 0f;

            for (int i = 0; i < collisions.Length; i++)
            {
                var collision = collisions[i];
                //if (!entityManager.HasComponent<LocalTransform>(collision.OtherEntity) ||
                //    !entityManager.HasComponent<ECS_CircleCollider2DAuthoring>(collision.OtherEntity) ||
                //    !entityManager.HasComponent<ECS_PhysicsBody2DAuthoring>(collision.OtherEntity))
                //    continue;

                float2 otherPos = collision.OtherTransform.Position.xy;
                float otherRadius = collision.OtherCollider.Radius;
                var otherBody = collision.OtherBody;

                float2 delta = position - otherPos;
                float dist = math.length(delta);
                float minDist = collider.ValueRO.Radius + otherRadius;

                // Prevent divide by zero
                if (dist == 0f)
                {
                    delta = new float2(0.1375f, 0f); // Arbitrary push direction
                    dist = 0.001f;
                }

                float2 direction = delta / dist;
                float penetration = minDist - dist;

                if (penetration > 0f)
                {
                    // Distribute movement (half if both are dynamic)
                    float2 push = direction * penetration;

                    if (!otherBody.isStatic)
                        push *= 0.1375f;

                    totalPushX += push.x;
                    totalPushY += push.y;
                }
            }

            // Apply final push
            var pos = transform.ValueRW.Position;
            pos.xy += new float2(totalPushX, totalPushY);
            pos.z = 0f;
            transform.ValueRW.Position = pos;
            //velocity.Value.xy += new float2(totalPushX, totalPushY);

        }
    }
}
