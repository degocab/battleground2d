using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(FormationCombatSystem))]
[UpdateAfter(typeof(ProcessCommandSystem))]
public class FormationManagerSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _unitQuery;
    private EntityQuery _unitGroupQuery;

    /// <summary>
    /// Holds a runtime mapping of FormationGroupEntity → UnitEntities
    /// Built each frame by FormationManagerSystem
    /// Read by systems like FormationCollisionSystem, FormationIntegritySystem, etc.
    /// </summary>
    public NativeMultiHashMap<Entity, Entity> _groupToUnits;

    /// <summary>
    /// Cached average positions of all units in each formation group
    /// </summary>
    public NativeHashMap<Entity, float2> _groupAveragePositions;

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

        // Get unit entities and formation components
        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob);
        var formationComponents = _unitQuery.ToComponentDataArray<FormationComponent>(Allocator.TempJob);
        var translations = GetComponentDataFromEntity<Translation>(true);

        // STEP 1: Build mapping of group → unit indices
        var groupToUnitIndices = new NativeMultiHashMap<Entity, int>(unitEntities.Length, Allocator.TempJob);
        var processedGroups = new NativeHashSet<Entity>(256, Allocator.TempJob);

        for (int i = 0; i < formationComponents.Length; i++)
        {
            var groupEntity = formationComponents[i].FormationGroupEntity.GetValueOrDefault(Entity.Null);
            if (groupEntity != Entity.Null)
                groupToUnitIndices.Add(groupEntity, i);
        }

        // STEP 2: Prepare runtime caches
        var groupCount = _unitGroupQuery.CalculateEntityCount();

        // Update _groupToUnits capacity if needed
if (!_groupToUnits.IsCreated)
{
    _groupToUnits = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length * 2, Allocator.Persistent);
}
else
{
    _groupToUnits.Clear();
    // Let it auto-grow if needed - NativeCollections handle this fine
}

        // Update _groupAveragePositions capacity if needed
        if (_groupAveragePositions.Capacity < groupCount)
        {
            _groupAveragePositions.Dispose();
            _groupAveragePositions = new NativeHashMap<Entity, float2>(groupCount * 2, Allocator.Persistent);
        }
        else
        {
            _groupAveragePositions.Clear();
        }

        //var groupToUnits = new NativeMultiHashMap<Entity, Entity>(unitEntities.Length, Allocator.Persistent);

        // STEP 3: Iterate over each formation group
        var entityManager = EntityManager;
        var groupKeys = groupToUnitIndices.GetKeyArray(Allocator.TempJob);

        foreach (var groupEntity in groupKeys)
        {
            if (!processedGroups.Add(groupEntity)) continue;
            if (!entityManager.Exists(groupEntity)) continue;
            if (!entityManager.HasComponent<FormationGroupComponent>(groupEntity)) continue;

            FormationGroupComponent formationGroup = entityManager.GetComponentData<FormationGroupComponent>(groupEntity);

            // Gather units in this group
            var unitIndices = new NativeList<int>(Allocator.TempJob);
            NativeMultiHashMapIterator<Entity> it;
            int idx;
            if (groupToUnitIndices.TryGetFirstValue(groupEntity, out idx, out it))
            {
                do { unitIndices.Add(idx); }
                while (groupToUnitIndices.TryGetNextValue(out idx, ref it));
            }

            int unitCount = unitIndices.Length;
            if (unitCount == 0)
            {
                unitIndices.Dispose();
                continue;
            }

            // Calculate and cache average position for this group
            float2 averagePosition = CalculateAveragePositionForGroup(groupEntity, unitIndices, unitEntities, translations);
            _groupAveragePositions.TryAdd(groupEntity, averagePosition);

            var unitEntitiesForGroup = new NativeArray<Entity>(unitCount, Allocator.TempJob);
            var newPositions = new NativeArray<float2>(unitCount, Allocator.TempJob);
            var updatedFormations = new NativeArray<FormationComponent>(unitCount, Allocator.TempJob);

            // Generate formation positions
            FormationGenerator.GeneratePhalanxFomationForJob(newPositions, formationGroup.UnitsPerRow, formationGroup.UnitSpacing, formationGroup.AnchorPosition);

            // Calculate bounds from new positions
            float2 minPos = newPositions[0];
            float2 maxPos = newPositions[0];

            for (int i = 1; i < newPositions.Length; i++)
            {
                var pos = newPositions[i];
                minPos = math.min(minPos, pos);
                maxPos = math.max(maxPos, pos);
            }

            // Expand bounds by unit radius so units fit inside AABB
            float unitRadius = .125f;
            minPos -= new float2(unitRadius, unitRadius);
            maxPos += new float2(unitRadius, unitRadius);
            formationGroup.BoundsMin = minPos;
            formationGroup.BoundsMax = maxPos;
            entityManager.SetComponentData(groupEntity, formationGroup);

            // Prepare unit data for job
            for (int i = 0; i < unitCount; i++)
            {
                int unitIndex = unitIndices[i];
                unitEntitiesForGroup[i] = unitEntities[unitIndex];
                updatedFormations[i] = formationComponents[unitIndex];
                _groupToUnits.Add(groupEntity, unitEntities[unitIndex]);
            }

            // Assign update positions using parallel job
            var applyJob = new ApplyFormationPositionJob
            {
                Entities = unitEntitiesForGroup,
                UpdatedFormations = updatedFormations,
                NewPositions = newPositions,
                formationGroupData = formationGroup,
                ECB = ecb
            };
            Dependency = applyJob.Schedule(unitCount, 64, Dependency);
            unitIndices.Dispose();
        }

        // STEP 4: Store the cache so other systems can read it
        //_groupToUnits = groupToUnits;

        // STEP 5: Dispose temp data and finalize
        unitEntities.Dispose();
        formationComponents.Dispose();
        groupToUnitIndices.Dispose();
        processedGroups.Dispose();
        groupKeys.Dispose();

        // STEP 6: Remove group colliders from units in formation
        Entities
            .WithAll<FormationComponent, CollidableTag>()
            .WithBurst()
            .ForEach((Entity entity, int entityInQueryIndex, ref FormationComponent formation) =>
            {
                if (formation.ColliderStatus == FormationColliderStatus.Group)
                {
                    ecb.RemoveComponent<CollidableTag>(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
        CompleteDependency();
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

    [BurstCompile]
    private struct ApplyFormationPositionJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Entity> Entities;
        [DeallocateOnJobCompletion] public NativeArray<FormationComponent> UpdatedFormations;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<float2> NewPositions;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] internal FormationGroupComponent formationGroupData;

        public void Execute(int index)
        {
            if (Entities[index] == Entity.Null) return;
            var formation = UpdatedFormations[index];

            // Safety check for slot index bounds
            if (formation.SlotIndex < 0 || formation.SlotIndex >= NewPositions.Length)
                return;

            formation.FormationPosition = NewPositions[formation.SlotIndex];

            // Only use group collider if not colliding with other formations
            if (!formationGroupData.isColliding)
            {
                formation.ColliderStatus = FormationColliderStatus.Group;
            }

            ECB.SetComponent(index, Entities[index], formation);
        }
    }
}





public class ProfileRecorderSystem : SystemBase
{
    private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
    private System.IO.StreamWriter csvWriter;

    protected override void OnCreate()
    {
        csvWriter = new System.IO.StreamWriter("profile.csv");
        csvWriter.WriteLine("System,TotalMS,SelfMS,GCAlloc,Calls");
    }

    protected override void OnUpdate()
    {
        sw.Restart();

        // Your system code...

        sw.Stop();
        var memory = GC.GetTotalMemory(false);

        csvWriter.WriteLine($"FormationManager,{sw.ElapsedMilliseconds},0,{memory},1");
        csvWriter.Flush();
    }
}