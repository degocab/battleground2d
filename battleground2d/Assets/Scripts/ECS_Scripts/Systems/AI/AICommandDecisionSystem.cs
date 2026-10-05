using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Build.Pipeline.WriteTypes;
using UnityEngine;
using UnityEngine.Profiling;
using static EntitySpawner;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CommandAwarenessSystem))]
public partial class AICommandDecisionSystem : SystemBase
{
    private string lastOrderText = "";
    private float lastOrderTime = 0f;
    private const float COMMAND_DISPLAY_DURATION = 1.5f;

    private const float EvaluationInterval = 1f;
    private const float MinimumConfidence = 0.35f;

    protected override void OnStartRunning()
    {
    }

    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        if (!HasSingleton<GameStateComponent>())
            return;

        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;


        double now = Time.ElapsedTime;



        Entities
            .WithName("ProcessAICommanders")
            .WithAll<CommanderComponent>()
            .WithAll<CommandComponent,
                OwnedFormationGroup,
                CommandKnownFormation>()
            .WithNone<PlayerInputComponent>()
            .ForEach((
                Entity commanderEntity,
                ref AICommandDecisionState decisionState,
                in CommandComponent command,
                in CommandAwareness awareness,
                in DynamicBuffer<OwnedFormationGroup> ownedFormations,
                in DynamicBuffer<CommandKnownFormation> knownFormations) =>
            {

                if (decisionState.HasPendingDecision)
                    return;

                if (now < decisionState.NextEvaluationTime)
                    return;

                // Decision timing will go here next.
                decisionState.NextEvaluationTime = now + EvaluationInterval;

                bool createdDecision = TryCreateDecision(command, awareness, ownedFormations, knownFormations, out AICommandDecision decision);

                Debug.Log(
                    $"AI Command{commanderEntity} " + 
                    $"owned={ownedFormations.Length} " + 
                    $"known={knownFormations.Length} " + 
                    $"pressure={awareness.Pressure} " + 
                    $"decision={createdDecision}");


            })
            .WithoutBurst()
            .Run();
    }

    /// <summary>
    /// Try to create a decision for the AI commander based on its awareness and known formations.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="awareness"></param>
    /// <param name="ownedFormations"></param>
    /// <param name="knownFormations"></param>
    /// <param name="decision"></param>
    /// <returns></returns>
    private static bool TryCreateDecision(CommandComponent command, CommandAwareness awareness, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, out AICommandDecision decision)
    {
        decision = default;


        CommandKnownFormation strugglingFormation = default;
        bool foundStrugglingFormation = false;

        for (int i = 0; i < knownFormations.Length; i++)
        {
            CommandKnownFormation knownFormation = knownFormations[i];

            if (!IsStruggling(knownFormation, command.FactionType))
                continue;

            strugglingFormation = knownFormation;

            foundStrugglingFormation = true;
            break;
        }

        if (!foundStrugglingFormation)  
            return false;

        Debug.Log(
      $"AI knows formation " +
      $"{strugglingFormation.Formation} is struggling. " +
      $"State={strugglingFormation.CaptainState}, " +
      $"confidence={strugglingFormation.Confidence}");

       
        float2 strugglingPosition = GetKnownPosition(strugglingFormation);
        
        CommandKnownFormation selectedHelper = default;
        bool foundSuitableHelper = false;
        float closestDistanceSq = float.MaxValue;

        int suitableHelperCount = 0;
        for (int  ownedIndex = 0;  ownedIndex < ownedFormations.Length;  ownedIndex++)
        {
            Entity ownedFormationEntity = ownedFormations[ownedIndex].Value;

            for (int knownIndex = 0;
     knownIndex < knownFormations.Length;
     knownIndex++)
            {
                CommandKnownFormation knownHelper =
                    knownFormations[knownIndex];

                if (knownHelper.Formation != ownedFormationEntity)
                    continue;

                if (!IsSuitableHelper(
                    knownHelper,
                    strugglingFormation,
                    command.FactionType))
                {
                    break;
                }

                float2 helperPosition = GetKnownPosition(knownHelper);

                float distanceSq = math.distancesq(helperPosition, strugglingPosition);

                if (!foundSuitableHelper || distanceSq < closestDistanceSq)
                {
                    selectedHelper = knownHelper;
                    
                    foundSuitableHelper = true;
                }

                // there should be only one known formation for this entity.
                break;
            }


        }


        if (!foundSuitableHelper)
        {
            Debug.Log(
                $"Formation {strugglingFormation.Formation} is struggling, " +
                $"but no suitable known owned helper was found.");

            return false;
        }

        Debug.Log(
    $"Closest helper for {strugglingFormation.Formation} is " +
    $"{selectedHelper.Formation}; remembered distance=" +
    $"{math.sqrt(closestDistanceSq)}.");

        // We found the correct entities, but have not returned
        // a complete decision yet.

        return false;
    }

    private static float2 GetKnownPosition(CommandKnownFormation formation)
    {
        return (formation.BoundsMin + formation.BoundsMax) * 0.5f;  
    }

    /// <summary>
    /// Check if matching faction is struggling based on captain state
    /// </summary>
    /// <param name="formation"></param>
    /// <param name="commandFaction"></param>
    /// <returns></returns>
    private static bool IsStruggling(CommandKnownFormation formation, UnitType commandFaction)
    {
        if (formation.Faction == commandFaction)
            return false;

        if(formation.AliveUnitCount <= 0)
            return false;

        if (formation.Confidence < MinimumConfidence)
            return true;    

        return formation.CaptainState == FormationCaptainState.Pressured
            || formation.CaptainState == FormationCaptainState.Collapsing
            || formation.CaptainState == FormationCaptainState.Broken;
    }

    private static bool IsSuitableHelper(CommandKnownFormation helper, CommandKnownFormation struggling,
       UnitType commandFaction)
    {
        if ( helper.Formation == Entity.Null)
            return false;
        if ( helper.Formation == struggling.Formation)
            return false;
        if (helper.Faction != commandFaction)
            return false;
        if (helper.AliveUnitCount <= 0)
            return false;

        if (helper.Confidence < MinimumConfidence)
            return false;

        if (helper.Status == FormationStatusEnum.Broken)
            return false;

        if (helper.CaptainState == FormationCaptainState.Broken ||
            helper.CaptainState == FormationCaptainState.Collapsing ||
            helper.CaptainState == FormationCaptainState.Pressured)
            return false;

        return true;
    }

    private OrderData CreateOrderFromDecision(CommandAwareness commandAwareness)
    {
        OrderData order = new OrderData();
        switch (commandAwareness.Pressure)
        {
            case CommandPressureState.Stable:
                // Continue current orders.
                //order = AICommandDecisionType.Hold;
                order = OrderFactory.CreateOrder(OrderType.Defend);

                break;

            case CommandPressureState.Pressured:
                // Find a struggling friendly formation
                // and send an available formation to help.
                //order = AICommandDecisionType.Reinforce;
                order = OrderFactory.CreateOrder(OrderType.MoveTo);
                break;

            case CommandPressureState.Collapsing:
                // Pull endangered formations away from the fight.
                //order = AICommandDecisionType.Retreat;
                order = OrderFactory.CreateOrder(OrderType.MoveTo);
                break;

            case CommandPressureState.Broken:
                // Order a wider withdrawal.
                //order = AICommandDecisionType.FullRetreat;
                order = OrderFactory.CreateOrder(OrderType.MoveTo);
                break;
        }
        Debug.Log("Order#" + order.CurrentOrder.ToString());
        OrderDebugUI.Text = $"Order: {order.CurrentOrder.ToString()}";
        OrderDebugUI.TimeRemaining = 1.5f;

        return order;
    }




}

public enum AICommandDecisionType : byte
{
    None,
    Hold,
    Reinforce,
    Retreat,
    FullRetreat
}

public struct AICommandDecision
{
    public AICommandDecisionType Type;

    //formation that is receiving the order
    public Entity OrderedFormation;

    //formation that needs help
    public Entity RelatedFormation;

    /// <summary>
    /// Last knowm location of strugglinf formation
    /// </summary>
    public float2 TargetPosition;
}

public struct  AICommandDecisionState : IComponentData
{
    public AICommandDecision PendingDecision;

    public double NextEvaluationTime;
    public double ExecuteAfterTime;

    public bool HasPendingDecision;

}

