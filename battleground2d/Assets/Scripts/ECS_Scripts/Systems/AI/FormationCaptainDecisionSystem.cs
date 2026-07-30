using Unity.Entities;
using UnityEngine;

/// <summary>
/// Reads the current order and formation data, determines the captain
/// state, runs the tactical decision factory, and writes the result
/// into FormationBehaviorComponent for other systems to consume.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FormationManagerSystem))]
public partial class FormationCaptainDecisionSystem : SystemBase
{
    // Flip on while testing a specific order/state combination
    // (see Step 10 of the plan). Turn off before shipping.
    public static bool EnableDebugLogging = false;

    protected override void OnCreate()
    {
        Debug.Log("FormationCaptainDecisionSystem created");
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        bool debugLogging = EnableDebugLogging;

        Entities
            .ForEach((
                ref FormationBehaviorComponent behaviorComponent,
                in FormationCaptainComponent captain,
                in FormationGroupComponent formation,
                in OrderData currentOrder) =>
            {
                FormationCaptainState state =
                    CaptainStateFactory.DetermineState(
                        captain.Control,
                        captain.Intensity,
                        captain.Morale,
                        formation.AliveUnitCount);

                FormationBehavior behavior =
                    TacticalDecisionFactory.Process(
                        currentOrder.CurrentOrder,
                        state);
                behaviorComponent.State = state;
                behaviorComponent.Type = behavior.Type;
                behaviorComponent.Aggression = behavior.Aggression;
                behaviorComponent.MoveSpeedMultiplier =
                    behavior.MoveSpeedMultiplier;

                behaviorComponent.MaintainFormation =
                    behavior.MaintainFormation;

                behaviorComponent.AllowPursuit =
                    behavior.AllowPursuit;

                behaviorComponent.RequestSupport =
                    behavior.RequestSupport;

                if (debugLogging)
                {
                    Debug.Log(
                        $"FormationID: {formation.FormationID}" +
                        $"Order: {currentOrder.CurrentOrder}, " +
                        $"State: {state}, " +
                        $"Behavior: {behavior.Type}"
                        );
                }
            })
            .WithoutBurst()
            .Run();
    }
}
