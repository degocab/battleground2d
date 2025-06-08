using Unity.Entities;
using Unity.Mathematics;

public struct MovementSpeedComponent : IComponentData
{
    public float3 Value;
    public float randomSpeed;


    public float moveX;
    public float moveY;
    public float3 direction;
    public float3 normalizedDirection;
    public bool isRunnning;
    public bool isBlocked;
    public bool isKnockedBack;
    public float3 knockedBackDirection;
    public float3 nextFloat3ToCheck;
    public float sqrDistance;
    public int frameCounter;
}