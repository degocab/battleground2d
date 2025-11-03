
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
                     in FormationComponent formation,
                     in Translation translation,
                     in AnimationComponent animationComponent) =>
            {

                var unitFormatonStatus = formation.Status;
                if (animationComponent.UnitType == EntitySpawner.UnitType.Enemy)
                    unitFormatonStatus = FormationStatus.Broken;
                switch (unitFormatonStatus)
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
                        break;
                }
            }).ScheduleParallel();

        CompleteDependency();
    }

    private static void HandleHoldFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                   FormationComponent formation, Translation translation)
    {
        // Tight formation - very little movement allowed
        float maxEngageDistance = 0.5f;

        float2 formationPos = formation.FormationPosition;
        float distanceFromFormation = math.distance(translation.Value.xy, formationPos);

        if (distanceFromFormation > maxEngageDistance)
        {
            // Too far - return to formation immediately
            hasTarget.Type = HasTarget.TargetType.Position;
            hasTarget.TargetPosition = formationPos;
            combatState.CurrentState = CombatState.State.Idle;
        }
        // If they have an enemy target AND are close enough, let them attack!
    }

    private static void HandleEngagedFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                      FormationComponent formation, Translation translation)
    {
        // Loose formation - more freedom to engage
        float maxEngageDistance = 10f;

        float2 formationPos = formation.FormationPosition;
        float distanceFromFormation = math.distance(translation.Value.xy, formationPos);

        if (distanceFromFormation > maxEngageDistance)
        {
            // Too far - return to formation
            hasTarget.Type = HasTarget.TargetType.Position;
            hasTarget.TargetPosition = formationPos;
            combatState.CurrentState = CombatState.State.Idle;
        }
        // Otherwise, let them keep their current target (enemy) and attack freely!
    }
}