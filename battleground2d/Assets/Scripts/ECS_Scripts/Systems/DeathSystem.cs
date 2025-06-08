using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(RenderSystem))]
public class DeathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Create an EntityCommandBuffer
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Iterate through entities that are marked as dead
        //update movement x and y
        Entities
            .WithAll<HealthComponent>()
            .ForEach((ref Entity entity, ref HealthComponent health, ref AnimationComponent animation) =>
            {
                if (animation.isFrozen)
                {
                    // Mark the entity as dead and freeze the animation

                    //// Optionally remove components, destroy entity, or do other cleanup
                    ecb.RemoveComponent<HealthComponent>(entity);  // Example: Remove HealthComponent
                    ecb.RemoveComponent<MovementSpeedComponent>(entity);  // Example: Remove MovementComponent
                    ecb.RemoveComponent<AttackComponent>(entity);  // Example: Remove CombatComponent
                    ecb.RemoveComponent<AttackCooldownComponent>(entity);  // Example: Remove CombatComponent
                    ecb.RemoveComponent<PositionComponent>(entity);  // Example: Remove CombatComponent
                    //ecb.DestroyEntity(entity);  // Optionally destroy the entity
                }
            }).WithoutBurst().Run();

        // Apply the command buffer at the end of the frame
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}