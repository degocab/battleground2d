using Unity.Entities;
using Unity.Mathematics;

public struct PlayerInputComponent : IComponentData
{
    public float3 moveDirection;  // For movement input
    public bool attackCommand;     // Attack input
}