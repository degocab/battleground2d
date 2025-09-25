using Unity.Entities;
using Unity.Mathematics;

public struct ECS_Velocity2D : IComponentData
{
    public float2 Value;
    public float2 PrevValue;
}
