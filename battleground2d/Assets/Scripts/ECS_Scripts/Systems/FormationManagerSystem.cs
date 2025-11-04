using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateBefore(typeof(FormationCombatSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
public partial class FormationManagerSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _unitQuery;
    private EntityQuery _unitGroupQuery;

    private ComponentTypeHandle<FormationComponent> _formationType;
    private ComponentTypeHandle<FormationGroupComponent> _groupType;

    /// <summary>
    /// Holds a runtime mapping of FormationGroupEntity → UnitEntities
    /// Built each frame by FormationManagerSystem
    /// Read by systems like FormationCollisionSystem, FormationIntegritySystem, etc.
    /// </summary>
    [NativeDisableParallelForRestriction]
    public NativeMultiHashMap<Entity, Entity> _groupToUnitsMap;

    /// <summary>
    /// Cached average positions of all units in each formation group
    /// </summary>
    [NativeDisableParallelForRestriction]
    public NativeHashMap<Entity, float2> _groupAveragePositions;

    [NativeDisableParallelForRestriction]
    public NativeHashMap<Entity, FormationGroupComponent> _formationGroupMap;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        _unitQuery = GetEntityQuery(
            ComponentType.ReadWrite<FormationComponent>(),
            ComponentType.Exclude<DeadTagComponent>()
        );
        _unitGroupQuery = GetEntityQuery(typeof(FormationGroupComponent));

        // Initialize the average positions cache
        _groupAveragePositions = new NativeHashMap<Entity, float2>(64, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        // Clean up the cache
        if (_groupAveragePositions.IsCreated)
            _groupAveragePositions.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob);
        var formationComponents = _unitQuery.ToComponentDataArray<FormationComponent>(Allocator.TempJob);
        var translations = GetComponentDataFromEntity<Translation>(true);

        var groupCount = _unitGroupQuery.CalculateEntityCount();

        // Build group → unit map
        if (!_groupToUnitsMap.IsCreated)
            _groupToUnitsMap = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length * 2, Allocator.Persistent);
        else
            _groupToUnitsMap.Clear();


        if (!_formationGroupMap.IsCreated)
            _formationGroupMap = new NativeHashMap<Entity, FormationGroupComponent>(groupCount * 2, Allocator.Persistent);
        else
            _formationGroupMap.Clear();
        //NativeHashMap<Entity, FormationGroupComponent> formationGroupMap = new NativeHashMap<Entity, FormationGroupComponent>(groupCount * 2, Allocator.TempJob);
        var formationGroupWriter = _formationGroupMap.AsParallelWriter();
        var addGroupToNativeHashMapJobHandle = Entities
            .WithAll<FormationGroupComponent>()
            .ForEach((Entity entity, ref FormationGroupComponent formationGroupComponent) =>
            {
                formationGroupWriter.TryAdd(entity, formationGroupComponent);
            }).WithBurst().ScheduleParallel(Dependency);
        addGroupToNativeHashMapJobHandle.Complete();


        var groupToUnitIndicesMap = new NativeMultiHashMap<Entity, int>(unitEntities.Length * 2, Allocator.TempJob);
        var groupToUnitIndicesWriter = groupToUnitIndicesMap.AsParallelWriter();

        var groupToUnitsWriter = _groupToUnitsMap.AsParallelWriter(); // local copy (struct)
        var addJobHandle = Entities
            .WithAll<FormationComponent>()
            .ForEach((Entity entity, ref FormationComponent formationComponent) =>
            {
                if (formationComponent.FormationGroupEntity.HasValue)
                {
                    groupToUnitsWriter.Add(formationComponent.FormationGroupEntity.Value, entity);
                    groupToUnitIndicesWriter.Add(formationComponent.FormationGroupEntity.Value, formationComponent.SlotIndex);
                }
            }).WithBurst().ScheduleParallel(Dependency);

        addJobHandle.Complete(); // Wait so we can read _groupToUnits



        // Now safe to read counts
        var groupToUnitCountMap = GetCountsPerKey(_groupToUnitsMap, Allocator.TempJob);
        var formationGroupMapTemp = _formationGroupMap;
        // calculate new individual formation position using
        // tempGrouptoTUnits for hashed group list to group entity.
        var updateFormationCompsJobHandle = Entities
            .WithAll<FormationComponent>()
            .WithReadOnly(groupToUnitCountMap)
            .WithReadOnly(formationGroupMapTemp)
            .ForEach((Entity entity, ref FormationComponent formationComponent) =>
            {
                if (groupToUnitCountMap.TryGetValue(formationComponent.FormationGroupEntity.Value, out var groupValueCount)
                    &&
                    formationGroupMapTemp.TryGetValue(formationComponent.FormationGroupEntity.Value, out var formationGroup) //should be fast enough to do here
                )
                {
                    var pos = CalculatePhalanxPosition(
                        unitIndex: formationComponent.SlotIndex,
                        totalUnits: groupValueCount,
                        unitsPerRow: 16,
                        spacing: formationGroup.UnitSpacing,
                        anchor: formationGroup.AnchorPosition);
                    formationComponent.FormationPosition = pos;

                    //if (!formationGroup.isColliding)
                    //{
                    //    formationComponent.ColliderStatus = FormationColliderStatus.Group;
                    //}
                    //else
                    //{
                    //    formationComponent.ColliderStatus = FormationColliderStatus.Individual;
                    //}
                }
            })
            .WithBurst().ScheduleParallel(Dependency);
        updateFormationCompsJobHandle.Complete();
        //.WithoutBurst()   
        //.Run();



        // (Optional) compute group average positions later


        //after calculating group's formation positions, we can now get the average positions to update anchor collision radius
        //and we can calc the bounds of the current formation

        //var afpmj = new AssignFormationPositionsMathJob

        //{
        //    /*
        //        [ReadOnly] public NativeMultiHashMap<Entity, int> GroupToUnitIndices;
        //        [ReadOnly] public NativeArray<Entity> UnitEntities;
        //        public NativeArray<FormationComponent> FormationComponents;
        //        [ReadOnly] public ComponentDataFromEntity<FormationGroupComponent> GroupComponents;
        //        [ReadOnly] public ComponentDataFromEntity<Translation> Translations;

        //        public NativeMultiHashMap<Entity, Entity>.ParallelWriter GroupToUnits;
        //        public EntityCommandBuffer.ParallelWriter ECB;
        //    */

        //    //GroupToUnitIndices = //do I need this?
        //    //UnitEntities = //get by using UnitEntities hashmap
        //    //FormationComponents = //get by using FormationComponents hashmap
        //    //GroupComponents // also get by using GroupComponents hashmap
        //    FormationComponentsTypeHandle = GetComponentTypeHandle<FormationComponent>(false),
        //    GroupToUnitIndices = groupToUnitIndicesMap,
        //    GroupComponentsMap = formationGroupMap,


        //}; //////_unitQuery uses this query


        //var formationGroupBoundJobHandle = 
            Entities
            .WithAll<FormationGroupComponent>()
            .WithReadOnly(groupToUnitCountMap)
            .WithReadOnly(_groupToUnitsMap)
            .ForEach((Entity groupEntity, ref FormationGroupComponent formationGroupComponent) =>
            {

                if (groupToUnitCountMap.TryGetValue(groupEntity, out var groupValueCount))
                {
                    var unitEntitiesList = new NativeList<Entity>(groupValueCount + 5, Allocator.Temp);

                    unitEntitiesList = GetValuesForKey(_groupToUnitsMap, groupEntity, Allocator.TempJob);
                    // Calculate and cache average position for this group
                    //if (formationGroupComponent.ShouldUpdateAnchorToCurrentPosition)
                    //{
                        float2 averagePosition = CalculateAveragePositionForGroup(groupEntity, unitEntitiesList, translations);
                        formationGroupComponent.AnchorPosition = averagePosition; 
                    //}
                    //tempGroupAveragePositions.TryAdd(groupEntity, averagePosition); 

                    // Calculate formation bounds mathematically (no array generation)
                    var bounds = CalculateFormationBounds(groupValueCount, 16,
                        formationGroupComponent.UnitSpacing, formationGroupComponent.AnchorPosition);

                    // Update group bounds
                    //Debug.Log($"should drawwwwwwwwwwwwww min:{bounds.Min}, max:{bounds.Max}");

                    FormationCollisionSystem.DrawAABB(bounds.Min, bounds.Max, Color.green);

                    formationGroupComponent.BoundsMin = bounds.Min;
                    formationGroupComponent.BoundsMax = bounds.Max;
                }


            })
        //    .WithBurst().ScheduleParallel(Dependency);
        //formationGroupBoundJobHandle.Complete();
        .WithoutBurst()
        .Run();


        //Dependency = assignJob;
        //_groupAveragePositions = tempGroupAveragePositions;
        // STEP 5: Remove group colliders from units in formation
        var removeCollidersJob = Entities
            .WithAll<FormationComponent, CollidableTag>()
            .WithBurst()
            .ForEach((Entity entity, int entityInQueryIndex, ref FormationComponent formation) =>
            {
                if (formation.ColliderStatus == FormationColliderStatus.Group)
                {
                    ecb.RemoveComponent<CollidableTag>(entityInQueryIndex, entity);
                }
            }).ScheduleParallel(Dependency);

        Dependency = removeCollidersJob;

        // STEP 6: Add dependencies and cleanup - FIXED: Combine dependencies properly
        _ecbSystem.AddJobHandleForProducer(Dependency);

        // Dispose temporary arrays after job completion - chain them properly
        Dependency = unitEntities.Dispose(Dependency);
        Dependency = formationComponents.Dispose(Dependency);
        Dependency.Complete();

    }

    private static float2 CalculateAveragePositionForGroup(
        Entity groupEntity,
        //NativeMultiHashMap<Entity, Entity> groupToUnits, 
        NativeList<Entity> unitEntities,
        ComponentDataFromEntity<Translation> translations)
    {
        float2 sum = float2.zero;
        int validUnitCount = 0;

        //var unitEntities = GetValuesForKey(groupToUnits, groupEntity, Allocator.TempJob);

        foreach (Entity unitEntity in unitEntities)
        {
            //Entity unitEntity = unitEntities[unitIndex];
            if (translations.HasComponent(unitEntity))
            {
                var translation = translations[unitEntity];
                sum += new float2(translation.Value.x, translation.Value.y);
                validUnitCount++;
            }
        }

        return validUnitCount > 0 ? sum / validUnitCount : float2.zero;
    }

    // Helper method to calculate average position
    private float2 CalculateAveragePositionForGroup(Entity groupEntity, NativeList<int> unitIndices,
        NativeArray<Entity> unitEntities, ComponentDataFromEntity<Translation> translations)
    {
        float2 sum = float2.zero;
        int validUnitCount = 0;

        foreach (int unitIndex in unitIndices)
        {
            Entity unitEntity = unitEntities[unitIndex];
            if (translations.HasComponent(unitEntity))
            {
                var translation = translations[unitEntity];
                sum += new float2(translation.Value.x, translation.Value.y);
                validUnitCount++;
            }
        }

        return validUnitCount > 0 ? sum / validUnitCount : float2.zero;
    }

    // Calculate formation bounds using pure math (no array generation)
    private FormationBounds CalculateFormationBounds(int unitCount, int unitsPerRow, float spacing, float2 anchor)
    {
        if (unitCount == 0 || unitsPerRow <= 0)
            return new FormationBounds { Min = anchor, Max = anchor };

        int totalRows = (unitCount + unitsPerRow - 1) / unitsPerRow;

        // Calculate formation dimensions (same as position calculation)
        float formationWidth = (math.min(unitsPerRow, unitCount) - 1) * spacing;
        float formationHeight = (totalRows - 1) * spacing;

        // Calculate bounds WITH VERTICAL CENTERING (this was missing!)
        float2 min = new float2(
            anchor.x - formationWidth * 0.5f,
            anchor.y - formationHeight * 0.5f  // Now centered vertically
        );

        float2 max = new float2(
            anchor.x + formationWidth * 0.5f,
            anchor.y + formationHeight * 0.5f  // Now centered vertically  
        );

        // Expand bounds by unit radius
        float unitRadius = 0.125f;
        min -= new float2(unitRadius, unitRadius);
        max += new float2(unitRadius, unitRadius);

        return new FormationBounds { Min = min, Max = max };
    }
    // Pure math function to calculate single unit position in phalanx
    //[BurstCompile]
    //public static float2 CalculatePhalanxPosition(int unitIndex, int totalUnits, int unitsPerRow, float spacing, float2 anchor)
    //{
    //    if (totalUnits <= 0 || unitsPerRow <= 0) return anchor;



    //    // Calculate grid position
    //    int row = unitIndex / unitsPerRow;
    //    int col = unitIndex % unitsPerRow;

    //    // Center the formation horizontally
    //    float formationWidth = (math.min(unitsPerRow, totalUnits) - 1) * spacing;
    //    float offsetX = col * spacing - formationWidth * 0.5f;
    //    float offsetY = row * spacing;

    //    return anchor + new float2(offsetX, offsetY);
    //}
    [BurstCompile]
    public static float2 CalculatePhalanxPosition(
        int unitIndex,
        int totalUnits,
        int unitsPerRow,
        float spacing,
        float2 anchor)
    {
        //// --- quick guards ---
        //if (totalUnits <= 0 | unitsPerRow <= 0)
        //    return anchor;

        // --- compute row / column ---
        // Burst-friendly integer ops
        int row = unitIndex / unitsPerRow;
        int col = unitIndex - row * unitsPerRow; // slightly cheaper than % on some targets

        // --- compute how many rows are actually used ---
        int rowCount = (totalUnits + unitsPerRow - 1) / unitsPerRow; // ceil(totalUnits / unitsPerRow)
        //x = (100 + 10 - 1) / 10
        //x = (110 - 1)/ 10
        //x = 109/10
        // --- figure out number of columns in this particular row ---
        // (last row might be partially filled)
        int columnsThisRow = math.min(unitsPerRow, totalUnits - row * unitsPerRow);

        // --- horizontal centering per row ---
        float formationWidth = (columnsThisRow - 1) * spacing;
        float offsetX = col * spacing - formationWidth * 0.5f;

        // --- vertical centering of the whole formation ---
        float formationHeight = (rowCount - 1) * spacing;
        float offsetY = row * -spacing + formationHeight * 0.5f; // negative so front row is "forward"

        // --- combine with anchor ---
        return anchor + new float2(offsetX, offsetY);
    }


    //[BurstCompile]
    //private struct AssignFormationPositionsMathJob : IJobChunk
    //{
    //    [ReadOnly] public NativeMultiHashMap<Entity, int> GroupToUnitIndices;

    //    public NativeMultiHashMap<Entity, Entity>.ParallelWriter GroupToUnits;
    //    public EntityCommandBuffer.ParallelWriter ECB;
    //    [ReadOnly] internal ComponentTypeHandle<FormationComponent> FormationComponentsTypeHandle;
    //    [ReadOnly] internal NativeHashMap<Entity, FormationGroupComponent> GroupComponentsMap;

    //    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    //    {
    //        var translations = chunk.GetNativeArray(TranslationTypeHandle);
    //        var quadrantEntities = chunk.GetNativeArray(QuadrantEntityTypeHandle);
    //        var animations = chunk.GetNativeArray(AnimationTypeHandle);
    //        var entities = chunk.GetNativeArray(EntityTypeHandle);

    //        for (int i = 0; i < chunk.Count; i++)
    //        {
    //            float2 unitPosition = translations[i].Value.xy;
    //            var quadrantEntity = quadrantEntities[i];
    //            var animation = animations[i];
    //            var groupKeys = GroupToUnitIndices.GetKeyArray(Allocator.Temp);

    //            var formationGroup = GroupComponentsMap.TryGetValue(formation;
    //            int unitCount = 0;

    //            // First pass: count units in this group
    //            NativeMultiHashMapIterator<Entity> it;
    //            int unitIndex;
    //            if (GroupToUnitIndices.TryGetFirstValue(groupEntity, out unitIndex, out it))
    //            {
    //                do { unitCount++; }
    //                while (GroupToUnitIndices.TryGetNextValue(out unitIndex, ref it));
    //            }

    //            if (unitCount == 0) continue;

    //            // Second pass: assign positions mathematically
    //            int slotIndex = 0;
    //            if (GroupToUnitIndices.TryGetFirstValue(groupEntity, out unitIndex, out it))
    //            {
    //                do
    //                {
    //                    if (unitIndex < 0 || unitIndex >= FormationComponents.Length) continue;

    //                    var formation = FormationComponents[unitIndex];
    //                    var unitEntity = UnitEntities[unitIndex];

    //                    // Calculate position using pure math
    //                    formation.FormationPosition = CalculatePhalanxPosition(
    //                        slotIndex, unitCount, formationGroup.UnitsPerRow,
    //                        formationGroup.UnitSpacing, formationGroup.AnchorPosition);

    //                    // Only use group collider if not colliding with other formations
    //                    if (!formationGroup.isColliding)
    //                    {
    //                        formation.ColliderStatus = FormationColliderStatus.Group;
    //                    }

    //                    FormationComponents[unitIndex] = formation;
    //                    GroupToUnits.Add(groupEntity, unitEntity);

    //                    ECB.SetComponent(unitIndex, unitEntity, formation);
    //                    slotIndex++;
    //                }
    //                while (GroupToUnitIndices.TryGetNextValue(out unitIndex, ref it));
    //            }
    //        }
    //    }

    //}

    private struct FormationBounds
    {
        public float2 Min;
        public float2 Max;
    }




    //[BurstCompile]
    //public struct AssignFormationPositionsJob : IJobChunk
    //{
    //    [ReadOnly] public ComponentTypeHandle<FormationGroupComponent> GroupTypeHandle;
    //    public ComponentTypeHandle<FormationComponent> FormationTypeHandle;
    //    [ReadOnly] public EntityTypeHandle EntityTypeHandle;

    //    // External data (already built from earlier jobs)
    //    [ReadOnly] public NativeHashMap<Entity, FormationGroupComponent> GroupMap;

    //    public EntityCommandBuffer.ParallelWriter ECB;

    //    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    //    {
    //        var entities = chunk.GetNativeArray(EntityTypeHandle);
    //        var formations = chunk.GetNativeArray(FormationTypeHandle);

    //        for (int i = 0; i < chunk.Count; i++)
    //        {
    //            var formation = formations[i];
    //            var entity = entities[i];
    //            var groupEntity = formation.FormationGroupEntity.Value;

    //            // Try to look up this entity's group info
    //            if (!GroupMap.TryGetValue(groupEntity, out var group))
    //                continue;
    //            // Try to look up this entity's group info
    //            if (!GroupMap.TryGetValue(groupEntity, out var group))
    //                continue;

    //            // Compute position based on slot index and group properties
    //            formation.FormationPosition = CalculatePhalanxPosition(
    //                formation.SlotIndex,
    //                group.UnitCount,
    //                group.UnitsPerRow,
    //                group.UnitSpacing,
    //                group.AnchorPosition);

    //            if (!group.isColliding)
    //                formation.ColliderStatus = FormationColliderStatus.Group;

    //            formations[i] = formation;

    //            ECB.SetComponent(chunkIndex, entity, formation);
    //        }
    //    }
    //}







    public static NativeHashMap<Entity, int> GetCountsPerKey(
        NativeMultiHashMap<Entity, Entity> groupToUnits,
        Allocator allocator)
    {
        var counts = new NativeHashMap<Entity, int>(groupToUnits.Count(), allocator);

        var enumerator = groupToUnits.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var kv = enumerator.Current;

            int current;
            if (counts.TryGetValue(kv.Key, out current))
                counts[kv.Key] = current + 1;
            else
                counts[kv.Key] = 1;
        }
        enumerator.Dispose();

        return counts;
    }
    public static NativeList<Entity> GetValuesForKey(
         NativeMultiHashMap<Entity, Entity> map,
         Entity key,
         Allocator allocator)
    {
        var values = new NativeList<Entity>(allocator);

        NativeMultiHashMapIterator<Entity> it;
        Entity value;

        if (map.TryGetFirstValue(key, out value, out it))
        {
            do
            {
                values.Add(value);
            }
            while (map.TryGetNextValue(out value, ref it));
        }

        return values;
    }
}

