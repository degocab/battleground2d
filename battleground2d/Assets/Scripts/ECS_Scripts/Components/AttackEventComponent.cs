using Unity.Entities;

public struct AttackEventComponent : IComponentData
{
    public Entity TargetEntity;
    public float Damage;
    public Entity SourceEntity;
    public float AttackTime;
    public float AttackDuration;


    public float WindUpTime;    // Attack preparation
    public float StrikeTime;    // Moment of impact
    public float RecoveryTime;  // Attack follow-through
    public float CurrentPhaseTimer;

    public EntitySpawner.Direction AttackerDirection; // Add this
    public EntitySpawner.Direction DefenderDirection; // Add this
}