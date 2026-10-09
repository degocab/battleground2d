using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using static EntitySpawner;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CommandAwarenessSystem))]
public partial class AICommandDecisionSystem : SystemBase
{
    private string lastOrderText = "";
    private float lastOrderTime = 0f;
    private const float COMMAND_DISPLAY_DURATION = 1.5f;

    private const float EvaluationInterval = 4f;
    private const float MinimumConfidence = 0.35f;
    private const float ReactionDelay = 0.5f;
    private const float DecisionCooldown = 4f;

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

        ComponentDataFromEntity<OrderData> formationOrders =
            GetComponentDataFromEntity<OrderData>(false);
        ComponentDataFromEntity<Translation> formationPositions =
            GetComponentDataFromEntity<Translation>(false);

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

                if (decisionState.HasActiveDecision)
                {
                    // Currently executing a decision, so skip evaluation.
                    //for reinforce only
                    // check if arrived to reinforce location
                    if (decisionState.ActiveDecision.Type == AICommandDecisionType.Reinforce)
                    {

                        if (now >= decisionState.DecisionTimeOut)
                        {
                            Debug.Log($"AI formation {decisionState.ActiveDecision.OrderedFormation} acive decision resets!");
                            if (formationOrders.HasComponent(decisionState.ActiveDecision.OrderedFormation) && TryGetKnownFormation(decisionState.ActiveDecision.OrderedFormation, knownFormations, out CommandKnownFormation activeFormation))
                            {
                                OrderData findTargetOrder = OrderFactory.CreateDefendOrder(new float3(GetKnownPosition(activeFormation), 0));
                                formationOrders[decisionState.ActiveDecision.OrderedFormation] = findTargetOrder;
                            }


                            decisionState.HasActiveDecision = false;
                            decisionState.ActiveDecision = default;
                            decisionState.NextEvaluationTime = now + DecisionCooldown;
                            decisionState.DecisionTimeOut = default;

                            return;
                        }


                        if (formationOrders.HasComponent(decisionState.ActiveDecision.OrderedFormation))
                        {
                            OrderData order = formationOrders[decisionState.ActiveDecision.OrderedFormation];

                            // TODO: if this returns false what do we do the the active decision? cancel it? wait longer?
                            if (!TryGetKnownFormation(decisionState.ActiveDecision.OrderedFormation, knownFormations, out CommandKnownFormation activeFormation))
                                return;

                            float2 currentPosition = GetKnownPosition(activeFormation);
                            float distanceSq = math.distancesq(currentPosition, decisionState.ActiveDecision.TargetPosition);
                            if (distanceSq < 1f)
                            {
                                Debug.Log($"AI formation {decisionState.ActiveDecision.OrderedFormation} has arrived to reinforce {decisionState.ActiveDecision.RelatedFormation}.");


                                OrderData findTargetOrder = OrderFactory.CreateFindTargetOrder();
                                formationOrders[decisionState.ActiveDecision.OrderedFormation] = findTargetOrder;
                                Debug.Log($"AI formation {decisionState.ActiveDecision.OrderedFormation} issued find target order!");

                                decisionState.HasActiveDecision = false;
                                decisionState.ActiveDecision = default;
                                decisionState.NextEvaluationTime = now + DecisionCooldown;
                                decisionState.DecisionTimeOut = default;


                            }
                        }
                    }
                    return;
                }

                if (decisionState.HasPendingDecision)
                {

                    if (now < decisionState.ExecuteAfterTime)
                        return;

                    bool executed = ExecuteDecision(decisionState.PendingDecision, formationOrders);

                    if (executed)
                    {
                        Debug.Log(
                            $"AI issued {decisionState.PendingDecision.Type} " +
                            $"to formation " +
                            $"{decisionState.PendingDecision.OrderedFormation}.");
                        decisionState.HasActiveDecision = true;
                        decisionState.ActiveDecision = decisionState.PendingDecision;
                        decisionState.DecisionTimeOut = now + 15f;
                    }
                    else
                    {
                        Debug.Log(
    $"AI cancelled {decisionState.PendingDecision.Type}.");
                    }

                    decisionState.PendingDecision = default;
                    decisionState.HasPendingDecision = false;
                    decisionState.ExecuteAfterTime = 0;
                    decisionState.NextEvaluationTime =
                        now + DecisionCooldown;

                    return;
                }

                if (now < decisionState.NextEvaluationTime)
                    return;

                // Decision timing will go here next.
                decisionState.NextEvaluationTime = now + EvaluationInterval;

                bool createdDecision = TryCreateDecision(command, ownedFormations, knownFormations, out AICommandDecision decision);

                Debug.Log(
                    $"AI Command{commanderEntity} " +
                    $"owned={ownedFormations.Length} " +
                    $"known={knownFormations.Length} " +
                    $"pressure={awareness.Pressure} " +
                    $"decision={createdDecision}");

                decisionState.HasPendingDecision = createdDecision;
                if (createdDecision)
                {
                    decisionState.PendingDecision = decision;

                    decisionState.ExecuteAfterTime = now + ReactionDelay;
                }
            })
            .WithoutBurst()
            .Run();
    }

    private static bool ExecuteDecision(AICommandDecision pendingDecision, ComponentDataFromEntity<OrderData> formationOrders)
    {
        if (pendingDecision.Type != AICommandDecisionType.Reinforce)
            return false;
        if (pendingDecision.OrderedFormation == Entity.Null)
            return false;
        if (!formationOrders.HasComponent(pendingDecision.OrderedFormation))
            return false;


        OrderData order = OrderFactory.CreateMoveOrder(pendingDecision.TargetPosition);
        formationOrders[pendingDecision.OrderedFormation] = order;
        Debug.Log(
            $"AI ordered formation {pendingDecision.OrderedFormation} " +
            $"to move to {pendingDecision.TargetPosition} " +
            $"to reinforce {pendingDecision.RelatedFormation}.");

        return true;
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
    private static bool TryCreateDecision(CommandComponent command, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, out AICommandDecision decision)
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
        float2 selectedTargetPosition = default;

        CommandKnownFormation selectedHelper = default;
        bool foundSuitableHelper = false;
        float closestDistanceSq = float.MaxValue;

        for (int ownedIndex = 0; ownedIndex < ownedFormations.Length; ownedIndex++)
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

                float2 candidateTargetPosition =
                    GetReinforcementPosition(strugglingFormation, knownHelper);

                if (!IsReinforcementRouteClear(
                    knownHelper,
                    strugglingFormation,
                    candidateTargetPosition,
                    knownFormations))
                {
                    break;
                }

                float distanceSq =
                    math.distancesq(helperPosition, candidateTargetPosition);

                if (!foundSuitableHelper || distanceSq < closestDistanceSq)
                {
                    selectedHelper = knownHelper;
                    selectedTargetPosition = candidateTargetPosition;
                    closestDistanceSq = distanceSq;
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


        //find struggling formatoin bounds closest to helper formation position
        //float2 strugglingTargetPosition = GetReinforcementPosition(strugglingFormation, selectedHelper);



        decision = new AICommandDecision
        {
            Type = AICommandDecisionType.Reinforce,
            OrderedFormation = selectedHelper.Formation,
            RelatedFormation = strugglingFormation.Formation,
            TargetPosition = selectedTargetPosition
        };
        return true;
    }

    private static float2 GetReinforcementPosition(
        CommandKnownFormation struggling,
        CommandKnownFormation helper)
    {
        float2 helperCenter = GetKnownPosition(helper);

        // Closest point inside/on the struggling formation's bounds.
        float2 closestPoint = math.clamp(
            helperCenter,
            struggling.BoundsMin,
            struggling.BoundsMax);

        // Keep the helper outside instead of overlapping the formation.
        float2 strugglingCenter = GetKnownPosition(struggling);

        float2 centerDirection = math.normalizesafe(
            helperCenter - strugglingCenter,
            new float2(0f, 1f));

        float2 outwardDirection = math.normalizesafe(
            helperCenter - closestPoint,
            centerDirection);

        float2 helperHalfSize =
            (helper.BoundsMax - helper.BoundsMin) * 0.5f;

        float fullClearance =
            math.dot(math.abs(outwardDirection), helperHalfSize);

        float clearance = math.min(fullClearance, 2f);

        return closestPoint + outwardDirection * clearance;
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
        if (formation.Faction != commandFaction)
            return false;

        if (formation.AliveUnitCount <= 0)
            return false;

        if (formation.Confidence < MinimumConfidence)
            return false;

        return formation.CaptainState == FormationCaptainState.Pressured || 
            formation.CaptainState == FormationCaptainState.Collapsing;
    }
    private static bool IsReinforcementRouteClear(
    CommandKnownFormation helper,
    CommandKnownFormation struggling,
    float2 targetPosition,
    DynamicBuffer<CommandKnownFormation> knownFormations)
    {
        float2 start = GetKnownPosition(helper);
        float2 padding = new float2(0.0f);

        for (int i = 0; i < knownFormations.Length; i++)
        {
            CommandKnownFormation obstacle = knownFormations[i];

            if (obstacle.Formation == helper.Formation ||
                obstacle.Formation == struggling.Formation)
                continue;

            if (obstacle.Faction != helper.Faction ||
                obstacle.AliveUnitCount <= 0 ||
                obstacle.Confidence < MinimumConfidence)
                continue;

            if (SegmentIntersectsBounds(
                start,
                targetPosition,
                obstacle.BoundsMin - padding,
                obstacle.BoundsMax + padding))
                return false;
        }

        return true;
    }

    private static bool SegmentIntersectsBounds(
        float2 start,
        float2 end,
        float2 boundsMin,
        float2 boundsMax)
    {
        float2 direction = end - start;

        bool parallelX = math.abs(direction.x) < 0.0001f;
        bool parallelY = math.abs(direction.y) < 0.0001f;

        if (parallelX &&
            (start.x < boundsMin.x || start.x > boundsMax.x))
            return false;

        if (parallelY &&
            (start.y < boundsMin.y || start.y > boundsMax.y))
            return false;

        float2 safeDirection = new float2(
            parallelX ? 0.0001f : direction.x,
            parallelY ? 0.0001f : direction.y);

        float2 first = (boundsMin - start) / safeDirection;
        float2 second = (boundsMax - start) / safeDirection;

        float enterTime = math.cmax(math.min(first, second));
        float exitTime = math.cmin(math.max(first, second));

        float routeEntry = math.max(enterTime, 0f);
        float routeExit = math.min(exitTime, 1f);

        if (routeEntry > routeExit)
            return false;

        float overlapFraction = routeExit - routeEntry;

        float overlapDistanceSq =
            overlapFraction *
            overlapFraction *
            math.lengthsq(direction);

        const float allowedOverlapDistance = 1f;

        return overlapDistanceSq >
               allowedOverlapDistance *
               allowedOverlapDistance;
    }
    private static bool IsSuitableHelper(CommandKnownFormation helper, CommandKnownFormation struggling,
       UnitType commandFaction)
    {
        if (helper.Formation == Entity.Null)
            return false;
        if (helper.Formation == struggling.Formation)
            return false;
        if (helper.Faction != commandFaction)
            return false;
        if (helper.AliveUnitCount <= 0)
            return false;

        if (helper.Confidence < MinimumConfidence)
            return false;
        //temp fix for now
        if (helper.CurrentOrder != OrderType.Defend)
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

    private static bool TryGetKnownFormation(Entity formationEntity, DynamicBuffer<CommandKnownFormation> knownFormations, out CommandKnownFormation knownFormation)
    {
        for (int i = 0; i < knownFormations.Length; i++)
        {
            if (knownFormations[i].Formation != formationEntity)
                continue;

            knownFormation = knownFormations[i];
            return true;
        }

        knownFormation = default;
        return false;
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

public struct AICommandDecisionState : IComponentData
{
    public AICommandDecision PendingDecision;

    public double NextEvaluationTime;
    public double ExecuteAfterTime;

    public bool HasPendingDecision;

    public bool HasActiveDecision;
    public AICommandDecision ActiveDecision;

    public double DecisionTimeOut;
}
