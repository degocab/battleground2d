using Unity.Entities;

public struct HealthComponent : IComponentData
{
    public float health;
    public float maxHealth;
}