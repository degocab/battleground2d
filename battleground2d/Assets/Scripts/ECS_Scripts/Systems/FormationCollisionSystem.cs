using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct InGroupTag : IComponentData { }
public struct OutOfGroupTag : IComponentData { }

[UpdateAfter(typeof(FormationManagerSystem))]
public class FormationCollisionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    public FormationManagerSystem fms;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var formationGroups = GetComponentDataFromEntity<FormationGroupComponent>(false);
        var formationComponents = GetComponentDataFromEntity<FormationComponent>(false);
        var translations = GetComponentDataFromEntity<Translation>(true);
        var groupEntities = GetEntityQuery(typeof(FormationGroupComponent)).ToEntityArray(Allocator.TempJob);
        var groups = GetComponentDataFromEntity<FormationGroupComponent>(false);

        float unitRadius = 0.5f; // Your unit radius

        if (fms == null)
        {
            fms = World.GetExistingSystem<FormationManagerSystem>(); 
        }
        // Calculate current bounds for each group and check collisions
        for (int i = 0; i < groupEntities.Length; i++)
        {
            var groupEntityA = groupEntities[i];
            var formationGroupDataA = formationGroups[groupEntityA];

            // Update bounds with current unit positions
            var boundsA = CalculateCurrentBoundsFromHashMap(groupEntityA, translations, unitRadius);
            formationGroupDataA.BoundsMin = boundsA.Min;
            formationGroupDataA.BoundsMax = boundsA.Max;
            //formationGroups[groupEntityA] = groupA;
            formationGroupDataA.isColliding = false;

            DrawAABB(boundsA.Min, boundsA.Max, Color.green);

            // Check collisions with other groups
            for (int j = i + 1; j < groupEntities.Length; j++)
            {
                var groupEntityB = groupEntities[j];
                var formationGroupDataB = formationGroups[groupEntityB];
                formationGroupDataB.isColliding = false;
                var boundsB = CalculateCurrentBoundsFromHashMap(groupEntityB, translations, unitRadius);
                formationGroupDataB.BoundsMin = boundsB.Min;
                formationGroupDataB.BoundsMax = boundsB.Max;
                //formationGroups[groupEntityB] = groupB;

                if (AABBOverlap(boundsA.Min, boundsA.Max, boundsB.Min, boundsB.Max))
                {
                    UnityEngine.Debug.Log($"Formation groups {groupEntityA} and {groupEntityB} overlap!");

                    // Handle collision - add OutOfGroupTag to units, etc.
                    HandleGroupCollision(formationComponents, groupEntityA, groupEntityB);
                    formationGroupDataA.ShouldUpdateAnchorToCurrentPosition = true;
                    formationGroupDataB.ShouldUpdateAnchorToCurrentPosition = true;

                    formationGroupDataB.isColliding = true;
                    formationGroupDataA.isColliding = true;
                }

                if (formationGroupDataB.isColliding)
                {
                    // ✅ Iterate all members of this group
                    if (fms._groupToUnits.TryGetFirstValue(groupEntityB, out var unitEntity, out var it))
                    {
                        do
                        {
                            var formation = formationComponents[unitEntity];
                            formation.ColliderStatus = FormationColliderStatus.Individual;
                            formationComponents[unitEntity] = formation;
                        } while (fms._groupToUnits.TryGetNextValue(out unitEntity, ref it));
                    }
                }
                formationGroups[groupEntityB] = formationGroupDataB;

            }
            if (formationGroupDataA.isColliding)
            {
                // ✅ Iterate all members of this group
                if (fms._groupToUnits.TryGetFirstValue(groupEntityA, out var unitEntity, out var it))
                {
                    do
                    {
                        var formation = formationComponents[unitEntity];
                        formation.ColliderStatus = FormationColliderStatus.Individual;
                        formationComponents[unitEntity] = formation;
                    } while (fms._groupToUnits.TryGetNextValue(out unitEntity, ref it));
                } 
            }
            formationGroups[groupEntityA] = formationGroupDataA;

        }

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
    }

    private AABB CalculateCurrentBoundsFromHashMap(Entity groupEntity, ComponentDataFromEntity<Translation> translations, float unitRadius)
    {
        float2 min = new float2(float.MaxValue, float.MaxValue);
        float2 max = new float2(float.MinValue, float.MinValue);
        int unitCount = 0;

        // Use the pre-built hashmap to efficiently find all units in this group
        if (fms._groupToUnits.TryGetFirstValue(groupEntity, out var unitEntity, out var iterator))
        {
            do
            {
                if (translations.HasComponent(unitEntity))
                {
                    var pos = translations[unitEntity].Value;
                    var pos2D = new float2(pos.x, pos.y);

                    min = math.min(min, pos2D);
                    max = math.max(max, pos2D);
                    unitCount++;
                }
            }
            while (fms._groupToUnits.TryGetNextValue(out unitEntity, ref iterator));
        }

        if (unitCount == 0)
            return new AABB { Min = float2.zero, Max = float2.zero };

        // Expand bounds by unit radius
        return new AABB
        {
            Min = min - new float2(unitRadius, unitRadius),
            Max = max + new float2(unitRadius, unitRadius)
        };
    }

    private void HandleGroupCollision(ComponentDataFromEntity<FormationComponent> formations, Entity groupA, Entity groupB)
    {
        // Example: Convert to individual unit collision by adding OutOfGroupTag
        // You might want to do this selectively based on your game rules

        // var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Remove units from formation control temporarily
        // if (_groupToUnits.TryGetFirstValue(groupA, out var unit, out var it))
        // {
        //     do { ecb.AddComponent<OutOfGroupTag>(unit); } 
        //     while (_groupToUnits.TryGetNextValue(out unit, ref it));
        // }
        // 
        // ecb.Playback(EntityManager);
        // ecb.Dispose();

        //if (formations.HasComponent(groupA))
        //{
        //    var formation = formations[groupA];
        //    formation.ColliderStatus = FormationColliderStatus.Individual;

        //    formations[groupA] = formation;
        //}
        //if (formations.HasComponent(groupB))
        //{
        //    var formation = formations[groupB];
        //    formation.ColliderStatus = FormationColliderStatus.Individual;

        //    formations[groupB] = formation;
        //}

    }
    protected  void OnUpdateOld()
    {

        //CollisionQuadrantData
        //clean up group unit collisions
        // we want to only have group collisions unless we re add them!
        // Gather all formation groups into a native array
        var formationGroups = GetComponentDataFromEntity<FormationGroupComponent>(true);
        var groupEntities = GetEntityQuery(typeof(FormationGroupComponent)).ToEntityArray(Allocator.TempJob);
        var groups = GetComponentDataFromEntity<FormationGroupComponent>(false);

        // Simple double loop to check pairs
        for (int i = 0; i < groupEntities.Length; i++)
        {
            var groupA = groups[groupEntities[i]];

            for (int j = i + 1; j < groupEntities.Length; j++)
            {
                var groupB = groups[groupEntities[j]];

                if (AABBOverlap(groupA.BoundsMin, groupA.BoundsMax, groupB.BoundsMin, groupB.BoundsMax))
                {
                    // Do something on overlap: e.g. log, set a flag, etc.
                    UnityEngine.Debug.Log($"Formation groups {groupEntities[i]} and {groupEntities[j]} overlap!");

                    //convert to individual unit collision
                    //by adding tag OutOfGroupTag component
                    //or add in formation state processing in FormatoinCombatSystem
                }
            }
        }
        for (int i = 0; i < groupEntities.Length; i++)
        {
            var group = groups[groupEntities[i]];
            DrawAABB(group.BoundsMin, group.BoundsMax, Color.green);
        }

        groupEntities.Dispose();
    }
    void DrawAABB(float2 min, float2 max, Color color)
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
    public static AABB CalculateGroupBounds(NativeArray<float2> positions, float unitRadius)
    {
        if (positions.Length == 0)
            return new AABB();

        float minX = positions[0].x;
        float maxX = positions[0].x;
        float minY = positions[0].y;
        float maxY = positions[0].y;

        for (int i = 1; i < positions.Length; i++)
        {
            var pos = positions[i];
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        return new AABB
        {
            Min = new float2(minX - unitRadius, minY - unitRadius),
            Max = new float2(maxX + unitRadius, maxY + unitRadius)
        };
    }
    [BurstCompile]
    public struct UpdateFormationJob : IJobParallelFor
    {
        public NativeArray<Entity> Entities;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<FormationComponent> FormationFromEntity;
        public float DeltaTime;

        public void Execute(int index)
        {
            Entity entity = Entities[index];



            if (FormationFromEntity.HasComponent(entity))
            {
                var formation = FormationFromEntity[entity];
                formation.ColliderStatus = FormationColliderStatus.Individual;
                FormationFromEntity[entity] = formation;
            }
        }
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