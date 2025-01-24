using Unity.Entities;
using Unity.Mathematics;

public struct TargetPositionComponent : IComponentData
{
    public float3 nextTargetPosition;    // Starting patrol position
    public float3 targetPosition;      // Ending patrol position
}