using Unity.Entities;
using Unity.Mathematics;

public struct ECS_PhysicsBody2DAuthoring : IComponentData
{
    public float2 initialVelocity;
    public float mass;
    public bool isStatic;
}