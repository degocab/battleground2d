using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Velocity2D : IComponentData
{
    public float2 Value;
    public float2 PrevValue;
}
