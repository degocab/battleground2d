using Unity.Entities;

public struct AttackComponent : IComponentData
{
    public float Damage;
    public bool isAttacking;
    public bool isDefending;
    public bool isTakingDamage;

    public float Range;
    public float AttackRate; // Attacks per second
    public float AttackRateRemaining; // Attacks per second
    public float LastAttackTime;

    public float DefendDuration;
    public float DefendCooldownRemaining;
}

