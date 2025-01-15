using Unity.Entities;
using Unity.Mathematics;

public struct MovementSpeedComponent : IComponentData
{
    public float3 value;
    public float randomSpeed;

}