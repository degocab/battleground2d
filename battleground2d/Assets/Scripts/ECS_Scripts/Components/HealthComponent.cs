using Unity.Entities;

public struct HealthComponent : IComponentData
{
    public float Health;
    public float maxHealth;
    public bool isDying;
    public float timeRemaining;
    public float deathAnimationDuration;

    public float CurrentHealth;
    public float MaxHealth;
}