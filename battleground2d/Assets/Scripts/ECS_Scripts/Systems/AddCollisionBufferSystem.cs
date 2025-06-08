using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;


[UpdateInGroup(typeof(InitializationSystemGroup))]
public class AddCollisionBufferSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Use a single ForEach to add the buffer only if it's missing
        Entities
            .WithNone<CollisionEvent2D>() // Only entities that don't have the buffer
            .WithAll<CollidableTag>()     // Only collidable entities
            .ForEach((Entity entity) =>
            {
                // Add the CollisionEvent2D buffer component to the entity
                EntityManager.AddBuffer<CollisionEvent2D>(entity);
            })
            .WithStructuralChanges() // Ensure structural changes like adding components
            .Run(); // Run immediately as it's initialization
    }
}
