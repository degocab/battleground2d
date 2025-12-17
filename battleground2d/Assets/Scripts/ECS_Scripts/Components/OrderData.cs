using Unity.Entities;
using Unity.Mathematics;

public struct OrderData : IComponentData
{
    public OrderType CurrentOrder;
    public float2 TargetPosition; // Optional (used for MoveTo, etc.)
    public Entity TargetEntity;   // Optional (used for Attack, etc.)
    public OrderType PreviousOrder;
    public float MoveRange;
    public EntitySpawner.Direction FormationDirectionToMove;
    public bool InitialOrder;
}