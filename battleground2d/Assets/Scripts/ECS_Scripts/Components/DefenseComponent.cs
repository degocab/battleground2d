using Unity.Entities;

public struct DefenseComponent : IComponentData
{
    public bool IsBlocking;
    public float BlockAngle; // 45° arcs? 90°? 180°?
    public float ParryChance;
    public float BlockDamageReduction;
}