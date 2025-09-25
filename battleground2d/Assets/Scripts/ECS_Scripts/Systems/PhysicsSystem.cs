﻿using Unity.Entities;
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
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        float deltaTime = Time.DeltaTime;

        Entities
            .WithAll<ECS_PhysicsBody2DAuthoring>() // Optional: Only move dynamic bodies
                                                   //.ForEach((ref Translation translation, in Velocity2D velocity, in PhysicsBody2D body) =>
            .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent movementSpeedComponent) =>
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

                translation.Value.xy += movementSpeedComponent.velocity.xy * deltaTime;
                position.Value.xy = translation.Value.xy;
            }).ScheduleParallel();
    }
}





