using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

//public class MovementSystem2 : SystemBase
//{
//    protected override void OnUpdate()
//    {
//        float deltaTime = Time.DeltaTime;

//        Entities
//            .WithName("MovementSystem")
//            .ForEach((ref Translation translation, ref PhysicsBody2D body) =>
//            {
//                if (body.IsStatic)
//                    return;

//                float2 pos = translation.Value.xy;
//                pos += body.Velocity * deltaTime;
//                translation.Value = new float3(pos.x, pos.y, 0f); // lock Z to 0
//            })
//            .ScheduleParallel(); // use parallel jobs for performance
//    }
//}


[UpdateInGroup(typeof(InitializationSystemGroup))]
public class AddCollisionBufferSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Use a single ForEach to add the buffer only if it's missing
        Entities
            .WithNone<CollisionEvent2D>() // Only entities that don't have the buffer
            .WithAll<CollidableTag>()     // Only collidable entities
            .ForEach((Entity entity) =>
            {
                // Add the CollisionEvent2D buffer component to the entity
                EntityManager.AddBuffer<CollisionEvent2D>(entity);
            })
            .WithStructuralChanges() // Ensure structural changes like adding components
            .Run(); // Run immediately as it's initialization
    }
}
