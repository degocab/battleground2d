using Unity.Entities;

public struct AttackComponent : IComponentData
{
    public float damage;
    public float range;

}

public struct AttackCooldownComponent : IComponentData
{
    public float timeRemaining; // Time left before the next attack can happen
    public float cooldownDuration; // Duration of the cooldown (in seconds)
}