using Unity.Entities;

public struct AttackEventComponent : IComponentData
{
    public Entity TargetEntity;
    public float Damage;
    public Entity SourceEntity;
    public float AttackTime;
    public float AttackDuration;
}