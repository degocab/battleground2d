// Calculates all formation positions once per frame
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateBefore(typeof(FormationCombatSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
public class FormationManagerSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _formationGroupQuery;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        _formationGroupQuery = GetEntityQuery(typeof(FormationGroupComponent));
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer();

        // Early exit if no formation groups
        if (_formationGroupQuery.CalculateEntityCount() == 0)
            return;

        // Get all formation groups (shared components)
        var formationGroups = new List<FormationGroupComponent>();
        EntityManager.GetAllUniqueSharedComponentData(formationGroups);
        var formationEntities = _formationGroupQuery.ToEntityArray(Allocator.Temp);

        // Process only non-zero formation IDs
        for (int i = 0; i < formationGroups.Count; i++)
        {
            if (formationGroups[i].FormationID != 0)
            {
                UpdateFormationPositions(formationGroups[i].FormationID, formationEntities[i], ecb);
            }
        }

        formationEntities.Dispose();
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    private void UpdateFormationPositions(int formationID, Entity groupEntity, EntityCommandBuffer ecb)
    {
        // Early exit if group entity is invalid
        if (!EntityManager.Exists(groupEntity))
            return;

        var formationQuery = GetEntityQuery(
            ComponentType.ReadWrite<FormationComponent>(),
            ComponentType.ReadOnly<FormationGroupComponent>()
        );

        var formationGroup = new FormationGroupComponent { FormationID = formationID };
        formationQuery.SetSharedComponentFilter(formationGroup);

        // Single allocation for unit data
        var formationUnits = formationQuery.ToEntityArray(Allocator.Temp);
        var currentFormations = formationQuery.ToComponentDataArray<FormationComponent>(Allocator.Temp);

        if (formationUnits.Length == 0)
        {
            formationUnits.Dispose();
            currentFormations.Dispose();
            formationQuery.ResetFilter();
            return;
        }

        // Get group data once
        var groupData = EntityManager.GetComponentData<FormationComponent>(groupEntity);

        // Generate positions - using static calls
        List<float2> newPositions;
        if (groupData.FormationType == FormationType.Phalanx)
        {
            newPositions = FormationGenerator.GeneratePhalanxFormation(formationUnits.Length, groupData.AnchorPosition);
        }
        else
        {
            newPositions = FormationGenerator.GenerateHordeFormation(formationUnits.Length, 20f, 1f, groupData.UnitSpacing, 12345, groupData.AnchorPosition);
        }

        // Batch update formation components
        for (int i = 0; i < formationUnits.Length; i++)
        {
            var updatedFormation = currentFormations[i];
            updatedFormation.FormationPosition = newPositions[i];
            ecb.SetComponent(formationUnits[i], updatedFormation);
        }

        formationUnits.Dispose();
        currentFormations.Dispose();
        formationQuery.ResetFilter();
    }
}
[UpdateAfter(typeof(FormationManagerSystem))]
[UpdateBefore(typeof(CombatSystem))]
public partial class FormationCombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithName("FormationCombatLogic")
            .WithAll<FormationComponent>()
            .WithNone<DeadTagComponent>()
            .ForEach((Entity entity,
                     ref HasTarget hasTarget,
                     ref CombatState combatState,
                     in FormationComponent formation,
                     in Translation translation) =>
            {
                switch (formation.Status)
                {
                    case FormationStatus.Hold:
                    default:
                        HandleHoldFormation(ref hasTarget, ref combatState, formation, translation);
                        break;

                    case FormationStatus.Engaged:
                        HandleEngagedFormation(ref hasTarget, ref combatState, formation, translation);
                        break;

                    case FormationStatus.Broken:
                        // Let normal combat system handle it
                        break;

                    case FormationStatus.Returning:
                        //HandleReturningFormation(ref hasTarget, ref combatState, formation, translation);
                        break;
                }
            }).ScheduleParallel();
    }

    private static void HandleHoldFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                   FormationComponent formation, Translation translation)
    {
        // Override target to formation position
        hasTarget.Type = HasTarget.TargetType.Position;
        hasTarget.TargetPosition = formation.FormationPosition;
        //hasTarget.TargetPosition = new float2(formation.FormationPosition.x, formation.FormationPosition.y);

        // Can still fight from formation position
        if (combatState.CurrentState == CombatState.State.Attacking)
        {
            // Stay in formation but attack nearby enemies
            combatState.CurrentState = CombatState.State.Attacking;
        }
    }

    private static void HandleEngagedFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                      FormationComponent formation, Translation translation)
    {
        // Limited movement - can engage nearby enemies but stay roughly in position
        float maxEngageDistance = 1.5f; // How far from formation position they can move

        if (hasTarget.Type == HasTarget.TargetType.Entity)
        {
            float2 formationPos = formation.FormationPosition;
            float distanceFromFormation = math.distance(translation.Value.xy, formationPos);

            if (distanceFromFormation > maxEngageDistance)
            {
                // Too far - return to formation
                hasTarget.Type = HasTarget.TargetType.Position;
                hasTarget.TargetPosition = formationPos;
            }
        }
    }
}