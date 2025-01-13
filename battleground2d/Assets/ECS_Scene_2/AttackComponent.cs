using Unity.Entities;

public struct AttackComponent : IComponentData
{
    public float damage;
    public float range;
}