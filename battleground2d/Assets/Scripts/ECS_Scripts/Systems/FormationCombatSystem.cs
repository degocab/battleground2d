
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(FormationCollisionSystem))]
[UpdateBefore(typeof(CombatSystem))]
public partial class FormationCombatSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    public FormationManagerSystem fms;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        Entities
            .WithName("FormationCombatLogic")
            .WithAll<FormationComponent>()
            .WithNone<DeadTagComponent>()
            .ForEach((Entity entity,
                     ref HasTarget hasTarget,
                     ref CombatState combatState,
                     ref FormationComponent formation
                     ,ref AnimationComponent animationComponent
                     , in Translation translation
                     ) =>
            {
                var unitFormatonStatus = formation.Status;
                if (animationComponent.UnitType == EntitySpawner.UnitType.Enemy)
                    unitFormatonStatus = FormationStatus.Broken;
                switch (unitFormatonStatus)
                {
                    case FormationStatus.Hold:
                    default:
                        animationComponent.Direction = formation.Direction;
                        HandleHoldFormation(ref hasTarget, ref combatState, ref formation, translation);
                        break;

                    case FormationStatus.Engaged:
                        HandleEngagedFormation(ref hasTarget, ref combatState, ref formation, translation);
                        break;

                    case FormationStatus.Broken:
                        // Let normal combat system handle it  
                        break;

                    case FormationStatus.Returning:
                        break;
                }
            }).ScheduleParallel();

        CompleteDependency();
    }

    private static void HandleHoldFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                   ref FormationComponent formation, Translation translation)
    {
        // Tight formation - very little movement allowed
        float maxEngageDistance = 0.5f;

        float2 formationPos = formation.FormationPosition;
        float distanceFromFormation = math.distance(translation.Value.xy, formationPos);
        if (hasTarget.Type == HasTarget.TargetType.Entity && hasTarget.TargetEntity != Entity.Null)
        {
            //check target position from current before moving
            float distanceFromCurrentTranslation = math.distance(translation.Value.xy, hasTarget.TargetPosition);
            if (distanceFromCurrentTranslation > maxEngageDistance)
            {
                // Too far - return to formation
                //hasTarget.Type = HasTarget.TargetType.Position;
                //hasTarget.TargetPosition = formationPos;
                //combatState.CurrentState = CombatState.State.Idle;
                return;
            }
        }
        if (distanceFromFormation > maxEngageDistance)
        {
            Debug.Log("HasTarget.TargetPosition updated by HandleHoldFormation in FormationCombatSystem");

            // Too far - return to formation immediately
            hasTarget.Type = HasTarget.TargetType.Position;
            hasTarget.TargetPosition = formationPos;
            combatState.CurrentState = CombatState.State.Idle;
        }
        // If they have an enemy target AND are close enough, let them attack!
        else
        {
            //formation.FormationPosition = hasTarget.TargetPosition;
            //hasTarget.Type = HasTarget.TargetType.Position;
            //combatState.CurrentState = CombatState.State.Attacking;
        }
    }

    private static void HandleEngagedFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                      ref FormationComponent formation, Translation translation)
    {
        // Loose formation - more freedom to engage
        float maxEngageDistance = 10f;

        float2 formationPos = formation.FormationPosition;
        //if (hasTarget.Type == HasTarget.TargetType.Entity && hasTarget.TargetEntity != Entity.Null)
        //{
        //    //check target position from current before moving
        //    float distanceFromCurrentTranslation = math.distance(translation.Value.xy, hasTarget.TargetPosition);
        //    if (distanceFromCurrentTranslation > maxEngageDistance)
        //    {
        //        //// Too far - return to formation
        //        //hasTarget.Type = HasTarget.TargetType.Position;
        //        //hasTarget.TargetPosition = formationPos;
        //        //combatState.CurrentState = CombatState.State.Idle;
        //        return;
        //    }
        //}

        float distanceFromFormation = math.distance(translation.Value.xy, formationPos);

        if (distanceFromFormation > maxEngageDistance)
        {
            Debug.Log("HasTarget.TargetPosition updated by HandleEngagedFormation in FormationCombatSystem");

            // Too far - return to formation
            hasTarget.Type = HasTarget.TargetType.Position;
            hasTarget.TargetPosition = formationPos;
            combatState.CurrentState = CombatState.State.Idle;
        }
        else
        {
            //formation.FormationPosition = hasTarget.TargetPosition;
            //hasTarget.Type = HasTarget.TargetType.Position;
            //combatState.CurrentState = CombatState.State.Attacking;
        }
        // Otherwise, let them keep their current target (enemy) and attack freely!
    }
}