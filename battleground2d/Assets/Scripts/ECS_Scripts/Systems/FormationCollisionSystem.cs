using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
public struct FormationCollisionTag : IBufferElementData
{
    //public Entity entity;
    //public float2 position;
    //public float radius;

    //public EntitySpawner.UnitType unitType;

    //// other date
    //public Translation CollisionSourceTranslation;
    //public ECS_CircleCollider2DAuthoring CollisionSourceCollider;
    //public ECS_PhysicsBody2DAuthoring CollisionSourceBody;
}


[UpdateAfter(typeof(FormationManagerSystem))]
public class FormationCollisionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    public FormationManagerSystem fms;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        collisionEvents = new NativeMultiHashMap<Entity, FormationCollisionTag>(1024, Allocator.Persistent);
    }
    private NativeMultiHashMap<Entity, FormationCollisionTag> collisionEvents;

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        if (fms == null)
        {
            fms = World.GetExistingSystem<FormationManagerSystem>();
        }
        var formationGroups = GetComponentDataFromEntity<FormationGroupComponent>(false);
        var formationComponents = GetComponentDataFromEntity<FormationComponent>(false);
        var translations = GetComponentDataFromEntity<Translation>(true);
        var groupEntities = fms._groupToUnitsMap.GetKeyArray(Allocator.TempJob);//GetEntityQuery(typeof(FormationGroupComponent)).ToEntityArray(Allocator.TempJob);
        var groups = fms._formationGroupMap;

        float unitRadius = 0.5f; // Your unit radius
        int estimatedCapacity = (groupEntities.Length * 2) * 16;// math.max(1024, totalEntities * maxCollisionsPerEntity);

        if (collisionEvents.Capacity < estimatedCapacity)
        {
            // Dispose old and allocate new only if really needed, with a max cap to avoid overflow
            int newCapacity = math.min(estimatedCapacity, 10_000_000); // limit max allocation
            collisionEvents.Dispose();
            collisionEvents = new NativeMultiHashMap<Entity, FormationCollisionTag>(newCapacity, Allocator.Persistent);
        }
        else
        {
            collisionEvents.Clear();
        }

        var FormationCollisionEvents = collisionEvents.AsParallelWriter();
        var jbHandler = Entities
             .WithReadOnly(groupEntities)
             .WithReadOnly(groups)
             .ForEach((Entity groupEntityA, int entityInQueryIndex, ref FormationGroupComponent formationGroupDataA) =>
             {
                 formationGroupDataA.isColliding = false;

                 DrawAABB(formationGroupDataA.BoundsMin, formationGroupDataA.BoundsMax, Color.green);

                 // Check collisions with other groups
                 for (int j = entityInQueryIndex + 1; j < groupEntities.Length; j++)
                 {
                     var groupEntityB = groupEntities[j];
                     if (groups.TryGetValue(groupEntityB, out var formationGroupDataB))
                     {
                         formationGroupDataB.isColliding = false;

                         if (AABBOverlap(formationGroupDataA.BoundsMin, formationGroupDataA.BoundsMax, formationGroupDataB.BoundsMin, formationGroupDataB.BoundsMax))
                         {
                             // Handle collision - add OutOfGroupTag to units, etc.
                             //formationGroupDataA.ShouldUpdateAnchorToCurrentPosition = true;
                             //formationGroupDataB.ShouldUpdateAnchorToCurrentPosition = true;

                             //formationGroupDataB.isColliding = true;
                             //formationGroupDataA.isColliding = true;

                             FormationCollisionEvents.Add(groupEntityA, new FormationCollisionTag());
                             FormationCollisionEvents.Add(groupEntityB, new FormationCollisionTag());
                         }

                     }
                     //formationGroups[groupEntityB] = formationGroupDataB;
                 }
                 //formationGroups[groupEntityA] = formationGroupDataA;
             })
             .WithBurst().ScheduleParallel(Dependency);
        //.WithoutBurst().Run();
        jbHandler.Complete();



        var tempCollisionEvents = collisionEvents;
        //try to add all events 
        Entities
    .WithName("FormationCollisionAddBuffer")
    .WithNone<DeadTagComponent>()
    .WithBurst() // Optional: add after testing
    .WithReadOnly(tempCollisionEvents) // Optional: add after testing
    .ForEach((Entity entity, ref DynamicBuffer<FormationCollisionTag> buffer, ref FormationGroupComponent formationGroupComponent) =>
    {
        if (tempCollisionEvents.TryGetFirstValue(entity, out var other, out var it))
        {
            const int MaxCollisions = 16;
            int count = 0;
            do
            {
                if (count++ < MaxCollisions)
                    buffer.Add(new FormationCollisionTag());
            }
            while (tempCollisionEvents.TryGetNextValue(out other, ref it));
        }

    }).Run(); // Run on main thread for now to access EntityManager


        //try to add all events 
        Entities
    .WithName("FormationCollisionResolutionSystem")
    .WithNone<DeadTagComponent>()
    .WithBurst() // Optional: add after testing
    .ForEach((ref DynamicBuffer<FormationCollisionTag> collisions, ref FormationGroupComponent formationGroupComponent) =>
    {
        formationGroupComponent.ShouldUpdateAnchorToCurrentPosition = false;
        formationGroupComponent.isColliding = false;

        if (collisions.Length == 0)
        {
            //velocity.Value = velocity.PrevValue;
            Debug.Log("No formation collisions detected");
            return;
        }
        formationGroupComponent.ShouldUpdateAnchorToCurrentPosition = true;
        formationGroupComponent.isColliding = true;

    }).Run(); // Run on main thread for now to access EntityManager






        Dependency.Complete();



        //set all units in each gruop to individual
        if (!fms._formationGroupMap.IsCreated)
            fms._formationGroupMap = new NativeHashMap<Entity, FormationGroupComponent>(groupEntities.Length * 2, Allocator.Persistent);
        else
            fms._formationGroupMap.Clear();
        var formationGroupWriter = fms._formationGroupMap.AsParallelWriter();
        var addGroupToNativeHashMapJobHandle = Entities
            .WithAll<FormationGroupComponent>()
            .ForEach((Entity entity, ref FormationGroupComponent formationGroupComponent) =>
            {
                formationGroupWriter.TryAdd(entity, formationGroupComponent);
            }).WithBurst().ScheduleParallel(Dependency);
        addGroupToNativeHashMapJobHandle.Complete();

        var formationGroupMapTemp = fms._formationGroupMap;
        var formationColliderStatusUpdateJobHandle = Entities
            .WithReadOnly(formationGroupMapTemp)
            .ForEach((Entity entity, int entityInQueryIndex, ref FormationComponent formationComponent) =>
            {
                //get group formation component
                if (formationGroupMapTemp.TryGetValue(formationComponent.FormationGroupEntity.Value, out var forGrpComp))
                {
                    if (forGrpComp.isColliding)
                    {
                        formationComponent.ColliderStatus = FormationColliderStatus.Individual;

                    }
                    else
                    {
                        formationComponent.ColliderStatus = FormationColliderStatus.Group;

                    }
                }

            }).WithBurst().ScheduleParallel(Dependency);
        formationColliderStatusUpdateJobHandle.Complete();

        groupEntities.Dispose();

        Entities
    .WithAll<FormationComponent>()
    .WithBurst()
    .ForEach((Entity entity, int entityInQueryIndex, ref FormationComponent formation) =>
    {
        // Only add CollidableTag if the collider status **just changed**
        if (formation.ColliderStatus == FormationColliderStatus.Individual &&
            formation.PreviousColliderStatus != FormationColliderStatus.Individual)
        {
            ecb.AddComponent<CollidableTag>(entityInQueryIndex, entity, new CollidableTag());
        }

        // Update previous status for next frame
        formation.PreviousColliderStatus = formation.ColliderStatus;

    }).ScheduleParallel();
        Dependency.Complete();

    }


    public static void DrawAABB(float2 min, float2 max, Color color)
    {
        //Debug.Log($"drawing min: {min}, max: {max}"); 
        Vector3 bottomLeft = new Vector3(min.x, min.y, 0);
        Vector3 bottomRight = new Vector3(max.x, min.y, 0);
        Vector3 topLeft = new Vector3(min.x, max.y, 0);
        Vector3 topRight = new Vector3(max.x, max.y, 0);

        Debug.DrawLine(bottomLeft, bottomRight, color);
        Debug.DrawLine(bottomRight, topRight, color);
        Debug.DrawLine(topRight, topLeft, color);
        Debug.DrawLine(topLeft, bottomLeft, color);
    }

    public static bool AABBOverlap(float2 minA, float2 maxA, float2 minB, float2 maxB)
    {
        return !(maxA.x < minB.x || minA.x > maxB.x || maxA.y < minB.y || minA.y > maxB.y);
    }

}



public struct AABB
{
    public float2 Min;
    public float2 Max;

    public bool Overlaps(AABB other)
    {
        return !(Max.x < other.Min.x || Min.x > other.Max.x ||
                 Max.y < other.Min.y || Min.y > other.Max.y);
    }
}