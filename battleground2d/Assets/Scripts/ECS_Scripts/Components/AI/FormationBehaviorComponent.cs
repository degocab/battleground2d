using Unity.Entities;

/// <summary>
/// Holds the most recently computed behavior for a formation.
/// FormationCaptainDecisionSystem writes this.
/// Movement, combat, and targeting systems read it.
/// </summary>
public struct FormationBehaviorComponent : IComponentData
{
    public FormationBehaviorType Type;

    public float Aggression;
    public float MoveSpeedMultiplier;

    public bool MaintainFormation;
    public bool AllowPursuit;
    public bool RequestSupport;

    public FormationCaptainState State;
}
