using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
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
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
        var newAnchorPositions = new NativeArray<float2>(groupEntities.Length, Allocator.TempJob);

        // 2. BROAD-PHASE: Group vs Group AABB checks
        var collisionJob = new GroupCollisionJob
        {
            GroupEntities = groupEntities,
            GroupComponents = groupComponents,
            GroupCollisions = groupCollisions,
            NewAnchorPositions = newAnchorPositions
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

            DrawAnchorPoint(groupComponents[i].AnchorPosition, Color.green, .25f);
        }
        var groupCommandMap = new NativeHashMap<Entity, CommandData>(groupEntities.Length, Allocator.Temp);

        Entities
            .WithReadOnly(groupCollisionMap)
            .ForEach((Entity entity, ref FormationGroupComponent formationGroup, ref CommandData command) => {
                if (!groupCollisionMap.TryGetValue(entity, out var formationGroupIsColliding)) return;
                bool isColliding = formationGroupIsColliding;
                formationGroup.isColliding = isColliding;
                groupCommandMap.TryAdd(entity, command);

            }).Run();

        // 3. PROPAGATE TO UNITS
        Entities
            .WithReadOnly(groupCollisionMap)
            .WithReadOnly(groupCommandMap)
            .ForEach((Entity entity, ref FormationComponent formation, ref CommandData command) =>
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

                bool wantsIndividual = isColliding;
                bool isIndividual = formation.ColliderStatus == FormationColliderStatus.Individual;

                if (wantsIndividual && !isIndividual)
                {
                    ecb.AddComponent<CollidableTag>(entity);
                    formation.Status = FormationStatus.Engaged;
                    formation.ColliderStatus = FormationColliderStatus.Individual;
                }
                else if (!wantsIndividual && isIndividual)
                {
                    ecb.RemoveComponent<CollidableTag>(entity);
                    formation.Status = FormationStatus.Hold;
                    formation.ColliderStatus = FormationColliderStatus.Group;
                }

                // Add physics collider if just switched to Individual
                //if (newColliderStatus == FormationColliderStatus.Individual 
                ////&& formation.PreviousColliderStatus != FormationColliderStatus.Individual
                //)
                //{
                //    //Debug.Log("Adding collidable tag");
                //    ecb.AddComponent<CollidableTag>(entity);
                //    //ecb.AddComponent<CommandData>(entity);
                //    formation.Status = FormationStatus.Engaged;
                //    var grpCommadn = groupCommandMap.TryGetValue(formation.FormationGroupEntity.Value, out var grpCommand);
                //    command = grpCommand;
                //}
                //else
                //{
                //    ecb.RemoveComponent<CollidableTag>(entity);
                //    //ecb.RemoveComponent<CommandData>(entity);
                //    formation.Status = FormationStatus.Hold;

                //}

                formation.PreviousColliderStatus = formation.ColliderStatus;
                formation.ColliderStatus = newColliderStatus;

            }).Run();


        // 4. After collision, update anchor positions for colliding groups
        //Entities
        //    .WithReadOnly(groupCollisionMap)
        //    .ForEach((Entity entity, ref FormationGroupComponent formationGroup) => {
        //        int idx = -1;
        //        for (int i = 0; i < groupEntities.Length; i++)
        //        {
        //            if (groupEntities[i] == entity)
        //            {
        //                idx = i;
        //                break;
        //            }
        //        }
        //        if (idx >= 0 && groupCollisions[idx])
        //        {
        //            if (!formationGroup.AnchorLocked)
        //            {
        //                formationGroup.AnchorPosition = newAnchorPositions[idx];
        //                formationGroup.AnchorLocked = true;
        //            }
        //        }
        //        else
        //        {
        //            // If not colliding, unlock anchor so it can be updated again on next collision
        //            formationGroup.AnchorLocked = false;
        //        }
        //    }).Run();
        Entities
  .WithReadOnly(groupCollisionMap)
  .ForEach((Entity entity, ref FormationGroupComponent formationGroup) =>
  {
      if (groupCollisionMap.TryGetValue(entity, out var isColliding))
          formationGroup.isColliding = isColliding;
  }).Run();

        // Cleanup
        newAnchorPositions.Dispose();

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
        public NativeArray<float2> NewAnchorPositions;

        public void Execute(int index)
        {
            var groupA = GroupComponents[index];
            bool isColliding = false;

            // Check against all other groups
            for (int j = index + 1; j < GroupEntities.Length; j++)
            {
                var groupB = GroupComponents[j];
                //Debug.Log($"Collision check[j:{isColliding}]");

                if (AABBOverlap(groupA.BoundsMin, groupA.BoundsMax, groupB.BoundsMin, groupB.BoundsMax))
                {
                    // Both groups are colliding with each other
                    isColliding = true;
                    GroupCollisions[j] = true; // Mark the other group as colliding too
                                               // Don't break - continue to find all collisions

                    // Calculate radii for both formations
                    //float2 halfSizeA = (groupA.BoundsMax - groupA.BoundsMin) * 0.5f;
                    //float radiusA = math.min(halfSizeA.x, halfSizeA.y);

                    //float2 halfSizeB = (groupB.BoundsMax - groupB.BoundsMin) * 0.5f;
                    //float radiusB = math.min(halfSizeB.x, halfSizeB.y);
                    float2 halfSizeA = (groupA.BoundsMax - groupA.BoundsMin) * 0.5f;
                    float radiusA = math.max(halfSizeA.x, halfSizeA.y);

                    float2 halfSizeB = (groupB.BoundsMax - groupB.BoundsMin) * 0.5f;
                    float radiusB = math.max(halfSizeB.x, halfSizeB.y);


                    // Direction from A to B
                    float2 toB = groupB.AnchorPosition - groupA.AnchorPosition;
                    float lenSq = math.lengthsq(toB);

                    float2 engagementPointA;
                    float2 engagementPointB;

                    if (lenSq > 0.001f)
                    {
                        float2 direction = math.normalize(toB);
                        float totalSeparation = radiusA + radiusB;

                        // Set anchor so edges touch
                        engagementPointA = groupB.AnchorPosition - direction * radiusB;
                        engagementPointB = groupA.AnchorPosition + direction * radiusA;
                    }
                    else
                    {
                        // If anchors are at the same position, just offset along X
                        engagementPointA = groupA.AnchorPosition - new float2(radiusA, 0);
                        engagementPointB = groupB.AnchorPosition + new float2(radiusB, 0);
                    }

                    NewAnchorPositions[index] = engagementPointA;
                    NewAnchorPositions[j] = engagementPointB;
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
    public static void DrawAnchorPoint(float2 position, Color color, float size = 0.5f)
    {
        Vector3 center = new Vector3(position.x, position.y, 0);
        Vector3 left = center + new Vector3(-size, 0, 0);
        Vector3 right = center + new Vector3(size, 0, 0);
        Vector3 top = center + new Vector3(0, size, 0);
        Vector3 bottom = center + new Vector3(0, -size, 0);

        Debug.DrawLine(left, right, color);
        Debug.DrawLine(top, bottom, color);
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

