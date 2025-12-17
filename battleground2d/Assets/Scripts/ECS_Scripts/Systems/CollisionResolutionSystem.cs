using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystem))]
[UpdateAfter(typeof(CollisionDetectionSystem))]
public class CollisionResolutionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        var formationData = GetComponentDataFromEntity<FormationComponent>(true);

        Entities
          .WithName("CollisionResolutionSystem")
          .WithNone<DeadTagComponent>()
          .WithAll<CollidableTag>()
          .WithReadOnly(formationData)
          .WithBurst()
          .ForEach((Entity entity,
                    ref Translation translation,
                    ref ECS_Velocity2D velocity,
                    in ECS_PhysicsBody2DAuthoring body,
                    in ECS_CircleCollider2DAuthoring collider,
                    in DynamicBuffer<CollisionEvent2D> collisions,
                    in OrderData order) =>
          {
              if (body.isStatic || collisions.Length == 0)
                  return;

              // --- Identify weight + anchored state (phalanx defending) ---
              float myWeight = 1f;
              bool isAnchored = false;

              if (formationData.HasComponent(entity))
              {
                  var myFormation = formationData[entity];
                  myWeight = myFormation.FormationWeight;

                  isAnchored =
                      myFormation.FormationType == FormationType.Phalanx &&
                      order.CurrentOrder == OrderType.Defend;
              }

              float2 startPos = translation.Value.xy;
              float2 pos = startPos;

              // --- Tunables ---
              const int iterations = 2;
              float stiffness = 0.35f;
              float slop = 0.005f;
              float maxPenPerPair = 0.06f;

              // These are the key "wall" knobs:
              float maxStepPerIter = isAnchored ? 0.03f : 0.10f;      // anchored moves far less each iteration
              float frictionStrength = isAnchored ? 0.35f : 0.15f;    // anchored resists sideways sliding

              // Per-frame clamp for anchored units (prevents slow creep)
              float maxAnchoredStep = 0.01f;

              for (int it = 0; it < iterations; it++)
              {
                  float2 totalPush = float2.zero;
                  float2 frictionDeltaV = float2.zero;
                  int frictionCount = 0;

                  for (int i = 0; i < collisions.Length; i++)
                  {
                      var c = collisions[i];

                      float2 otherPos = c.OtherTranslation.Value.xy;
                      float2 delta = pos - otherPos;
                      float dist = math.length(delta);
                      if (dist <= 1e-5f) continue;

                      float minDist = collider.Radius + c.OtherCollider.Radius;
                      float penetration = minDist - dist;
                      if (penetration <= slop) continue;

                      penetration -= slop;

                      float2 n = delta / dist;

                      // --- Other weight ---
                      float otherWeight = 1f;
                      if (formationData.HasComponent(c.OtherEntity))
                          otherWeight = formationData[c.OtherEntity].FormationWeight;

                      if (c.OtherBody.isStatic)
                          otherWeight = 999999f;

                      // --- Nonlinear weighting (stronger effect) ---
                      float effMy = myWeight * myWeight;
                      float effOther = otherWeight * otherWeight;

                      float denom = effMy + effOther;
                      if (denom <= 1e-6f) continue;

                      float myMoveFraction = effOther / denom;

                      // clamp per-contact push so one deep overlap doesn't explode
                      float pen = math.min(penetration, maxPenPerPair);
                      totalPush += n * (pen * myMoveFraction);

                      // Tangential friction to reduce orbiting/sliding
                      float2 t = new float2(-n.y, n.x);
                      float vT = math.dot(velocity.Value, t);
                      frictionDeltaV += (-t * vT) * frictionStrength;
                      frictionCount++;
                  }

                  // clamp total correction this iteration
                  float len = math.length(totalPush);
                  if (len > maxStepPerIter)
                      totalPush = (totalPush / len) * maxStepPerIter;

                  pos += totalPush * stiffness;

                  if (frictionCount > 0)
                      velocity.Value += frictionDeltaV / frictionCount;
              }

              // Final per-frame anchor clamp (prevents gradual shove)
              if (isAnchored)
              {
                  float2 d = pos - startPos;
                  float dist = math.length(d);
                  if (dist > maxAnchoredStep)
                      pos = startPos + (d / dist) * maxAnchoredStep;
              }

              translation.Value.xy = pos;
              translation.Value.z = 0f;

              // mild damping only
              velocity.Value *= 0.985f;
          }).ScheduleParallel();


    }
}
