using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystem))]
[UpdateAfter(typeof(CollisionDetectionSystem))]
public class CollisionResolutionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        var formationData = GetComponentDataFromEntity<FormationComponent>(true);

        Entities
            .WithName("CollisionResolutionSystem")
            .WithNone<DeadTagComponent>()
            .WithAll<CollidableTag>()
            .WithReadOnly(formationData)
            .WithBurst()
            .ForEach((ref Translation translation,
                      ref ECS_Velocity2D velocity,
                      ref ECS_PhysicsBody2DAuthoring body,
                      ref ECS_CircleCollider2DAuthoring collider,
                      ref DynamicBuffer<CollisionEvent2D> collisions) =>
            {
                if (body.isStatic || collisions.Length == 0)
                    return;

                float2 position = translation.Value.xy;
                float2 totalPush = float2.zero;

                for (int i = 0; i < collisions.Length; i++)
                {
                    var collision = collisions[i];
                    var otherBody = collision.OtherBody;
                    var otherCollider = collision.OtherCollider;
                    float2 otherPos = collision.OtherTranslation.Value.xy;

                    float2 delta = position - otherPos;
                    float dist = math.length(delta);
                    if (dist == 0f) continue;

                    float minDist = collider.Radius + otherCollider.Radius;
                    float penetration = minDist - dist;
                    if (penetration <= 0f)
                        continue;

                    float2 dir = delta / dist;

                    // Get weights
                    float myWeight = 1f;
                    float otherWeight = 1f;
                    if (formationData.HasComponent(collision.OtherEntity))
                        otherWeight = formationData[collision.OtherEntity].FormationWeight;

                    // heavier units move less
                    float myMoveFraction = otherWeight / (myWeight + otherWeight);
                    float otherMoveFraction = myWeight / (myWeight + otherWeight);

                    // final push
                    float2 push = dir * (penetration * myMoveFraction);
                    totalPush += push;
                }

                // Apply smooth correction
                float stiffness = 0.2f; // tweak for stability
                translation.Value.xy += totalPush * stiffness;
                translation.Value.z = 0f;

                // Damp velocity to avoid jitter
                velocity.Value *= 0.95f;
            }).ScheduleParallel();
    }
}
