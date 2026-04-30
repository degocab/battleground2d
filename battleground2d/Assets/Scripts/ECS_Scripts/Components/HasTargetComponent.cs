using Unity.Entities;
using Unity.Mathematics;

public struct FormationSlotGoal : IComponentData
{
    //public enum TargetType { Entity, Position, NoTargetFindAttacker, None }

    //public TargetType Type;
    public Entity TargetEntity; // Used if Type == Entity
    public float2 TargetPosition; // Used if Type == Position

    public bool isActive;
}
