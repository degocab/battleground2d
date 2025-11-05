using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionResolutionSystem))]
[UpdateBefore(typeof(SetAnimationTypeSystem))] // Before transforms are synced for rendering
[BurstCompile]
public class PhysicsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, position, movementSpeed) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PositionComponent>, RefRW<MovementSpeedComponent>>()
            .WithNone<DeadTagComponent>()
            .WithAll<ECS_PhysicsBody2DAuthoring>())
        {
            //// Apply force to update velocity (F = ma -> v = v0 + a * t)
            //velocity.Value += force.Value * deltaTime;
            //velocity.Value.z = 0;
            //// Update position based on velocity (p = p0 + vt)
            //position.Value += velocity.Value * deltaTime;
            //position.Value.z = 0;
            //translation.Value += position.Value;
            //// Reset force after applying it to prevent it from accumulating
            //force.Value = float3.zero;

            transform.ValueRW.Position.xy += movementSpeed.ValueRO.velocity.xy * deltaTime;
            position.ValueRW.Value.xy = transform.ValueRO.Position.xy;
        }
    }
}





