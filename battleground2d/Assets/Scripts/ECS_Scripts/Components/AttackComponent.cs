using Unity.Entities;

public struct AttackComponent : IComponentData
{
    public float damage;
    public float range;

    public bool isAttacking;
    public bool isDefending;
    public bool isTakingDamage;
}

public struct AttackCooldownComponent : IComponentData
{
    public float timeRemaining; // Time left before the next attack can happen
    public float cooldownDuration; // Duration of the cooldown (in seconds)
    public float takeDamageCooldownDuration;
}