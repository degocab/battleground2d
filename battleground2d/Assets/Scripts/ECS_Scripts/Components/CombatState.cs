using Unity.Entities;

public struct CombatState : IComponentData
{
    public enum State
    {
        Idle, SeekingTarget, Attacking, Defending, Fleeing,
        TakingDamage, Blocking,
        Dying
    }
    public State CurrentState;
    public Entity TargetEntity;
    public float StateTimer;
    public float SupportTimer;
    public float SupportTimer2;

    public State PreviousState;
}