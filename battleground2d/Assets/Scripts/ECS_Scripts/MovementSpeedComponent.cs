using Unity.Entities;
using Unity.Mathematics;

public struct MovementSpeedComponent : IComponentData
{
    public float3 value;
    public float randomSpeed;


    public float moveX;
    public float moveY;
    public float3 direction;
    public float3 normalizedDirection;
    public bool isRunnning;
}