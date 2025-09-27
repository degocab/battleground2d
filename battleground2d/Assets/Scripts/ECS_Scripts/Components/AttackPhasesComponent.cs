using Unity.Entities;

public struct AttackPhasesComponent : IComponentData
{
    public float WindUpTime;    // Attack preparation
    public float StrikeTime;    // Moment of impact
    public float RecoveryTime;  // Attack follow-through
    public float CurrentPhaseTimer;
}