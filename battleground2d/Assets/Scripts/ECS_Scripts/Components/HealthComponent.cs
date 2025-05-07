using Unity.Entities;

public struct HealthComponent : IComponentData
{
    public float health;
    public float maxHealth;
    public bool isDying;
    public float timeRemaining;
    public float deathAnimationDuration;
}