using Unity.Entities;
using Unity.Mathematics;

public struct CommandData : IComponentData
{
    public CommandType Command;
    public float2 TargetPosition; // Optional (used for MoveTo, etc.)
    public Entity TargetEntity;   // Optional (used for Attack, etc.)
    public CommandType previousCommand;
}