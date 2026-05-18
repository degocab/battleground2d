using Unity.Entities;
using Unity.Mathematics;

public struct FormationSlotGoal : IComponentData
{
    public float2 TargetPosition; // Used if Type == Position

    public bool isActive;
}
