using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionResolutionSystem))]
[BurstCompile]
public class PhysicsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities
            .WithAll<ECS_PhysicsBody2DAuthoring>() // Optional: Only move dynamic bodies
                                                   //.ForEach((ref Translation translation, in Velocity2D velocity, in PhysicsBody2D body) =>
            .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity) =>
            {
                //if (body.IsStatic)
                //    return;




                //// Apply force to update velocity (F = ma -> v = v0 + a * t)
                //velocity.Value += force.Value * deltaTime;
                //velocity.Value.z = 0;
                //// Update position based on velocity (p = p0 + vt)
                //position.Value += velocity.Value * deltaTime;
                //position.Value.z = 0;
                //translation.Value += position.Value;
                //// Reset force after applying it to prevent it from accumulating
                //force.Value = float3.zero;

                translation.Value.xy += velocity.Value.xy * deltaTime;
                position.Value.xy = translation.Value.xy;
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

public struct ECS_CircleCollider2DAuthoring : IComponentData
{
    public float Radius;
}

public struct ECS_PhysicsBody2DAuthoring : IComponentData
{
    public float2 initialVelocity;
    public float mass;
    public bool isStatic;
}

public struct ECS_Velocity2D : IComponentData
{
    public float2 Value;
    public float2 PrevValue;
}


//var otherGridID = gridData[otherIndex].value; // Get the grid_id for the other unit
//                                              // Skip if the grid IDs don't match
//if (currentGridID != otherGridID)
//{
//    continue; // Skip the collision check if grid IDs don't match
//}