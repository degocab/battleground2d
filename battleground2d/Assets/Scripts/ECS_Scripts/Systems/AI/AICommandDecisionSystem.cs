using System;
using System.Security.AccessControl;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static EntitySpawner;
using static UnityEngine.EventSystems.EventTrigger;

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

    protected override void OnStartRunning()
    {
    }

    protected override void OnCreate()
    {
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
            .WithAll<
                OwnedFormationGroup,
                CommandKnownFormation>()
            .WithNone<PlayerInputComponent>()
            .ForEach((
                Entity commanderEntity,
                DynamicBuffer<AICommandDecisionState> decisions,
                ref CommandComponent command,
                in CommandAwareness awareness,
                in DynamicBuffer<OwnedFormationGroup> ownedFormations,
                in DynamicBuffer<CommandKnownFormation> knownFormations) =>
            {

                for (int i = decisions.Length - 1; i >= 0; i--)
                {
                    AICommandDecisionState decisionState = decisions[i];
                    if (decisionState.HasActiveDecision)
                    {
                        UpdateActiveDecision(ref decisionState, now, formationOrders, knownFormations);
                    }
                    else if (decisionState.HasPendingDecision)
                    {
                        UpdatePendingDecision(ref decisionState, now, formationOrders);
                    }

                    if (!decisionState.HasActiveDecision &&
                        !decisionState.HasPendingDecision)
                    {
                        decisions.RemoveAt(i);
                    }
                    else
                    {
                        decisions[i] = decisionState;
                    }
                }


                if (now < command.NextEvaluationTime)
                    return;
                // Decision timing will go here next.
                command.NextEvaluationTime = now + EvaluationInterval;
                for (int i = 0; i < knownFormations.Length; i++)
                {
                    AICommandDecisionState decisionState = default;

                    var knownFormation = knownFormations[i];

                    bool alreadyAssigned = HasDecision(ref decisions, knownFormation);

                    if (alreadyAssigned)
                        continue;

                    bool createdDecision = TryCreateDecision(command, knownFormation, ownedFormations, knownFormations, ref decisions, out AICommandDecision decision);

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
                        decisions.Add(decisionState);

                    }

                }
            })
            .WithoutBurst()
            .Run();
    }

    private static bool HasDecision(ref DynamicBuffer<AICommandDecisionState> decisions, CommandKnownFormation knownFormation)
    {
        bool alreadyAssigned = false;
        for (int j = 0; j < decisions.Length; j++)
        {
            var state = decisions[j];

            if ((state.HasPendingDecision &&
                 state.PendingDecision.OrderedFormation == knownFormation.Formation) ||
                (state.HasActiveDecision &&
                 state.ActiveDecision.OrderedFormation == knownFormation.Formation))
            {
                alreadyAssigned = true;
                break;
            }
        }

        return alreadyAssigned;
    }

    private static void UpdatePendingDecision(ref AICommandDecisionState decisionState, double now, ComponentDataFromEntity<OrderData> formationOrders)
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
    }

    private static void UpdateActiveDecision(
    ref AICommandDecisionState decisionState,
    double now,
    ComponentDataFromEntity<OrderData> formationOrders,
    DynamicBuffer<CommandKnownFormation> knownFormations)
    {
        // Currently executing a decision, so skip evaluation.
        //for reinforce only
        // check if arrived to reinforce location
        if (decisionState.ActiveDecision.Type == AICommandDecisionType.Reinforce)
        {
            if (formationOrders.HasComponent(decisionState.ActiveDecision.OrderedFormation))
            {
                OrderData order = formationOrders[decisionState.ActiveDecision.OrderedFormation];
                if (order.CurrentOrder != OrderType.MoveTo)
                {
                    ClearActiveDecision(ref decisionState, now);
                    return;
                }
            }
            else
            {
                ClearActiveDecision(ref decisionState, now);
                return;
            }

            if (now >= decisionState.DecisionTimeOut)
            {
                Debug.Log($"AI formation {decisionState.ActiveDecision.OrderedFormation} acive decision resets!");
                if (TryGetKnownFormation(decisionState.ActiveDecision.OrderedFormation, knownFormations, out CommandKnownFormation activeFormation2))
                {
                    OrderData defendOrder = OrderFactory.CreateDefendOrder(new float3(GetKnownPosition(activeFormation2), 0));
                    formationOrders[decisionState.ActiveDecision.OrderedFormation] = defendOrder;
                }
                ClearActiveDecision(ref decisionState, now);
                return;
            }

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
                ClearActiveDecision(ref decisionState, now);
            }
        }
    }

    private static void ClearActiveDecision(ref AICommandDecisionState decisionState, double now)
    {
        decisionState.HasActiveDecision = false;
        decisionState.ActiveDecision = default;
        decisionState.DecisionTimeOut = default;
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
    private static bool TryCreateDecision(CommandComponent command, CommandKnownFormation knownFormation, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, ref DynamicBuffer<AICommandDecisionState> decisions, out AICommandDecision decision)
    {
        //loop through ownde formations and check if any of them are struggling, if so, try to find a suitable helper formation to reinforce it

        if (IsStruggling(knownFormation, command.FactionType) && TryCreateReinforceDecision(command, knownFormation, ownedFormations, knownFormations, ref decisions, out decision))
            return true;
        if (IsAbleToCharge(knownFormation, command.FactionType) && TryCreateChargeDecision(command, knownFormation, ownedFormations, knownFormations, out decision))
            return true;
        if (IsHolding(knownFormation, command.FactionType) && TryCreateHoldDecision(command, knownFormation, ownedFormations, knownFormations, out decision))
            return true;
        if (IsInNeedOfRetreat(knownFormation, command.FactionType) && TryCreateRetreatDecision(command, knownFormation, ownedFormations, knownFormations, out decision))
            return true;
        decision = default;
        return false;

    }

    private static bool IsInNeedOfRetreat(CommandKnownFormation formation, UnitType commandFaction)
    {
        if (formation.Faction != commandFaction)
            return false;
        //TODO: pass in the formation size and check if the formation is too small to continue fighting, if so, return true
        //if (formation.AliveUnitCount > 25)
        //    return false;

        //if (formation.Confidence < MinimumConfidence)
        //    return false;

        return
            formation.CaptainState == FormationCaptainState.Broken;
    }

    private static bool IsHolding(CommandKnownFormation formation, UnitType commandFaction)
    {
        return false;
    }

    private static bool IsAbleToCharge(CommandKnownFormation formation, UnitType commandFaction)
    {
        return false;
    }

    private static bool TryCreateReinforceDecision(CommandComponent command, CommandKnownFormation strugglingFormation, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, ref DynamicBuffer<AICommandDecisionState> decisions, out AICommandDecision decision)
    {
        decision = default;

        for (int i = 0; i < decisions.Length; i++)
        {
            var state = decisions[i];

            if ((state.HasPendingDecision &&
                 state.PendingDecision.Type == AICommandDecisionType.Reinforce &&
                 state.PendingDecision.RelatedFormation == strugglingFormation.Formation) ||
                (state.HasActiveDecision &&
                 state.ActiveDecision.Type == AICommandDecisionType.Reinforce &&
                 state.ActiveDecision.RelatedFormation == strugglingFormation.Formation))
            {
                return false; // This formation already has help assigned.
            }
        }


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

                bool hasDecision = HasDecision(ref decisions, knownHelper);

                if (!IsSuitableHelper(
                    knownHelper,
                    strugglingFormation,
                    command.FactionType) || hasDecision)
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

    private static bool TryCreateHoldDecision(CommandComponent command, CommandKnownFormation holdingFormation, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, out AICommandDecision decision)
    {

        decision = default;
        return false;
    }

    private static bool TryCreateChargeDecision(CommandComponent command, CommandKnownFormation chargingFormation, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, out AICommandDecision decision)
    {

        decision = default;
        return false;
    }

    private static bool TryCreateRetreatDecision(CommandComponent command, CommandKnownFormation retreatingFormation, DynamicBuffer<OwnedFormationGroup> ownedFormations, DynamicBuffer<CommandKnownFormation> knownFormations, out AICommandDecision decision)
    {

        decision = default;
        return false;
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
    FullRetreat,
    Charge
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

public struct AICommandDecisionState : IBufferElementData
{
    public AICommandDecision PendingDecision;

    public double ExecuteAfterTime;

    public bool HasPendingDecision;

    public bool HasActiveDecision;
    public AICommandDecision ActiveDecision;

    public double DecisionTimeOut;
}
