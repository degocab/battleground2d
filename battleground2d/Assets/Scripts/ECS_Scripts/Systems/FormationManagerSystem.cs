// Calculates all formation positions once per frame
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(FormationCombatSystem))]
public class FormationManagerSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private FormationGenerator _formationGenerator;
    private EntityQuery _formationQuery;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _formationQuery = GetEntityQuery(typeof(FormationComponent), typeof(FormationGroupComponent));
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer();

        // Get all unique formation groups
        var uniqueGroups = new List<FormationGroupComponent>();
        EntityManager.GetAllUniqueSharedComponentData<FormationGroupComponent>(uniqueGroups);

        foreach (var group in uniqueGroups)
        {
            if (group.FormationID == 0) continue;

            // Filter query to this specific formation group
            _formationQuery.SetSharedComponentFilter(group);

            // Get all entities in this formation
            var entities = _formationQuery.ToEntityArray(Allocator.TempJob);

            if (entities.Length == 0)
            {
                entities.Dispose();
                continue;
            }

            // Generate formation positions once for the entire group
            List<float2> positions;
            switch (group.FormationType)
            {
                case FormationType.Phalanx:
                    positions = FormationGenerator.GeneratePhalanxFormation(
                        entities.Length, group.AnchorPosition);
                    break;
                case FormationType.Horde:
                default:
                    positions = FormationGenerator.GenerateHordeFormation(
                        entities.Length, 20f, 1f, group.UnitSpacing, 12345, group.AnchorPosition);
                    break;
            }

            // Update each unit's FormationComponent
            for (int i = 0; i < entities.Length; i++)
            {
                var formationComp = EntityManager.GetComponentData<FormationComponent>(entities[i]);
                formationComp.FormationPosition = positions[i];
                ecb.SetComponent( entities[i], formationComp);
            }

            entities.Dispose();
            _formationQuery.ResetFilter();
        }

        //uniqueGroups.Dispose();
        _ecbSystem.AddJobHandleForProducer(Dependency);
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
        //hasTarget.TargetPosition = formation.FormationPosition;
        hasTarget.TargetPosition = new float2(formation.FormationPosition.x - 1.5f, formation.FormationPosition.y);

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