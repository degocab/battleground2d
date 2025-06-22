using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]

[UpdateBefore(typeof(MovementSystem))]
public class UnitMoveToTargetSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<HasTarget>().ForEach((Entity unitEntity, ref HasTarget hasTarget, ref Translation translation, ref MovementSpeedComponent movementSpeedComponent) =>
        {
            if (EntityManager.Exists(hasTarget.targetEntity))
            {
                Translation targetTranslation = EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);
                float3 targetDir = math.normalize(targetTranslation.Value - translation.Value);
                targetDir.z = 0f;
                movementSpeedComponent.velocity = targetDir;

                if (math.distance(targetTranslation.Value, translation.Value) < .125f)
                {
                    //close to target, destroy it
                    PostUpdateCommands.DestroyEntity(hasTarget.targetEntity);
                    PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
                }
            }
            else
            {
                // target entity already destroyed
                PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));

            }

        });

    }
}
