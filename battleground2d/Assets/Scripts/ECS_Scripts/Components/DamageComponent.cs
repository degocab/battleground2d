using Unity.Entities;

public struct DamageComponent : IComponentData
{
    public float Value;
    public Entity SourceEntity;
}