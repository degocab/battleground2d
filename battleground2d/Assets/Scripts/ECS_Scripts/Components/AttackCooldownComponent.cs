using Unity.Entities;

public struct AttackCooldownComponent : IComponentData
{
    public float timeRemaining; // Time left before the next attack can happen
    public float takingDmgTimeRemaining; // Time left before the next attack can happen
    public float cooldownDuration; // Duration of the cooldown (in seconds)
    public float takeDamageCooldownDuration;
}