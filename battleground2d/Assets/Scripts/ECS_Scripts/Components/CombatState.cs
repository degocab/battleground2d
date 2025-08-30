using Unity.Entities;

public struct CombatState : IComponentData
{
    public enum State
    {
        Idle, SeekingTarget, Attacking, Defending, Fleeing,
        TakingDamage
    }
    public State CurrentState;
    public Entity TargetEntity;
    public float StateTimer;
}