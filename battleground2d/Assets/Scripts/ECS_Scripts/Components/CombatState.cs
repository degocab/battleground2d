using Unity.Entities;

public struct CombatState : IComponentData
{
    public enum State
    {
        Idle, SeekingTarget, Attacking, Defending, Fleeing,
        TakingDamage, Blocking
    }
    public State CurrentState;
    public Entity TargetEntity;
    public float StateTimer;

    public State PreviousState { get; internal set; }
}