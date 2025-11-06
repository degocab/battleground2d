using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(FormationManagerSystem))]
public class FormationCollisionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _formationGroupsQuery;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        _formationGroupsQuery = GetEntityQuery(typeof(FormationGroupComponent));
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer();

        // 1. COLLECT GROUPS
        var groupEntities = _formationGroupsQuery.ToEntityArray(Allocator.TempJob);
        var groupComponents = _formationGroupsQuery.ToComponentDataArray<FormationGroupComponent>(Allocator.TempJob);

        // Track which groups are colliding
        var groupCollisions = new NativeArray<bool>(groupEntities.Length, Allocator.TempJob);

        // 2. BROAD-PHASE: Group vs Group AABB checks
        var collisionJob = new GroupCollisionJob
        {
            GroupEntities = groupEntities,
            GroupComponents = groupComponents,
            GroupCollisions = groupCollisions
        };
        var collisionHandle = collisionJob.Schedule(groupEntities.Length, 64, Dependency);
        collisionHandle.Complete();

        // Create lookup: Group Entity -> isColliding
        var groupCollisionMap = new NativeHashMap<Entity, bool>(groupEntities.Length, Allocator.Temp);
        for (int i = 0; i < groupEntities.Length; i++)
        {
            groupCollisionMap.TryAdd(groupEntities[i], groupCollisions[i]);

            // Debug visualization
            if (groupCollisions[i])
            {
                DrawAABB(groupComponents[i].BoundsMin, groupComponents[i].BoundsMax, Color.red);
            }
            else
            {
                DrawAABB(groupComponents[i].BoundsMin, groupComponents[i].BoundsMax, Color.green);
            }
        }

        Entities
            .WithReadOnly(groupCollisionMap)
            .ForEach((Entity entity, ref FormationGroupComponent formationGroup) => {
                if (!groupCollisionMap.TryGetValue(entity, out var formationGroupIsColliding)) return;
                bool isColliding = formationGroupIsColliding;
                formationGroup.isColliding = isColliding;
            }).Run();

        // 3. PROPAGATE TO UNITS
        Entities
            .WithReadOnly(groupCollisionMap)
            .ForEach((Entity entity, ref FormationComponent formation) =>
            {
                if (!formation.FormationGroupEntity.HasValue) return;

                if (!groupCollisionMap.TryGetValue(formation.FormationGroupEntity.Value, out var formationGroupIsColliding)) return;

                bool isColliding = false;
                if (groupCollisionMap.TryGetValue(formation.FormationGroupEntity.Value, out bool collisionStatus))
                {
                    isColliding = collisionStatus;
                }

                //// Update group collision status
                //formationGroup.isColliding = isColliding;
                //formationGroup.ShouldUpdateAnchorToCurrentPosition = isColliding;

                // Update unit collision mode
                var newColliderStatus = isColliding ?
                    FormationColliderStatus.Individual :
                    FormationColliderStatus.Group;

                // Add physics collider if just switched to Individual
                if (newColliderStatus == FormationColliderStatus.Individual 
                //&& formation.PreviousColliderStatus != FormationColliderStatus.Individual
                )
                {
                    //Debug.Log("Adding collidable tag");
                    ecb.AddComponent<CollidableTag>(entity);
                }
                else
                {
                    ecb.RemoveComponent<CollidableTag>(entity);
                }

                formation.PreviousColliderStatus = formation.ColliderStatus;
                formation.ColliderStatus = newColliderStatus;

            }).Run();

        // Cleanup
        groupEntities.Dispose();
        groupComponents.Dispose();
        groupCollisions.Dispose();
        groupCollisionMap.Dispose();
    }

    [BurstCompile]
    private struct GroupCollisionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> GroupEntities;
        [ReadOnly] public NativeArray<FormationGroupComponent> GroupComponents;
        public NativeArray<bool> GroupCollisions;

        public void Execute(int index)
        {
            var groupA = GroupComponents[index];
            bool isColliding = false;

            // Check against all other groups
            for (int j = index + 1; j < GroupEntities.Length; j++)
            {
                var groupB = GroupComponents[j];
                Debug.Log($"Collision check[j:{isColliding}]");

                if (AABBOverlap(groupA.BoundsMin, groupA.BoundsMax, groupB.BoundsMin, groupB.BoundsMax))
                {
                    // Both groups are colliding with each other
                    isColliding = true;
                    GroupCollisions[j] = true; // Mark the other group as colliding too
                                               // Don't break - continue to find all collisions
                }
            }
            // If we found any collisions, mark this group as colliding
            // (or keep existing true value if already set by another thread)
            if (isColliding)
            {
                GroupCollisions[index] = true;
            }
        }
    }

    public static void DrawAABB(float2 min, float2 max, Color color)
    {
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

