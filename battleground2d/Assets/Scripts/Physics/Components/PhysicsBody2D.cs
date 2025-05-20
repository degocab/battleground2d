using Unity.Entities;
using Unity.Mathematics;

public struct PhysicsBody2D : IComponentData
{
    public float2 Velocity;
    public float Mass;
    public bool IsStatic;
}