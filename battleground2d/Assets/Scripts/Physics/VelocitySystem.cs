using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(CollisionResolutionSystem))]
public class VelocitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities
            .WithAll<PhysicsBody2D>() // Optional: Only move dynamic bodies
            .ForEach((ref Translation translation, in Velocity2D velocity, in PhysicsBody2D body) =>
            {
                if (body.IsStatic)
                    return;

                translation.Value.xy += velocity.Value * deltaTime;
            }).ScheduleParallel();

        // Clear previous collision buffers
        Entities
            .WithAll<CollisionEvent2D>()
            .ForEach((DynamicBuffer<CollisionEvent2D> buffer) =>
            {
                buffer.Clear();
            }).ScheduleParallel();
    }
}
