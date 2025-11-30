
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UIElements;

[UpdateAfter(typeof(FormationCollisionSystem))]
[UpdateBefore(typeof(CombatSystem))]
public partial class FormationCombatSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    public FormationManagerSystem fms;
    public const int QuadrantYMultiplier = 1000;
    public const int quadrantCellSize = 10;
    public static NativeMultiHashMap<int, QuadrantData> FormationQuadrantMultiHashMap;
    private EntityQuery _query;





    // Structs
    private struct EntityWithPosition
    {
        public Entity Entity;
        public float2 Position;
        public float FormationRadius;
    }

    // Fields
    private EntityQuery _findTargetQuery;
    private EntityQuery _targetQuery;
    private int _updateCounter;

    // Double buffer for closest targets
    private NativeArray<EntityWithPosition> _closestTargetsBuffer1;
    private NativeArray<EntityWithPosition> _closestTargetsBuffer2;
    private bool _useBuffer1 = true;






    protected override void OnCreate()
    {
        FormationQuadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        _query = GetEntityQuery(
                            ComponentType.ReadOnly<FormationGroupComponent>()
                            );
        _findTargetQuery = GetEntityQuery(
    ComponentType.ReadWrite<FormationGroupComponent>()
);
    }
    protected override void OnDestroy()
    {
        FormationQuadrantMultiHashMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        //formation group mapping
        // to apply commands?

        FormationQuadrantMultiHashMap.Clear();
        var entityCount = _query.CalculateEntityCount();
        if (entityCount > FormationQuadrantMultiHashMap.Capacity)
        {
            FormationQuadrantMultiHashMap.Capacity = entityCount;
        }
        var job = new SetQuadrantDataHashMapJob
        {
            FormationGroupTypeHandle = GetComponentTypeHandle<FormationGroupComponent>(true),
            quadrantEntityTypeHandle = GetComponentTypeHandle<QuadrantEntity>(true),
            entityTypeHandle = GetEntityTypeHandle(),
            quadrantMultiHashMap = FormationQuadrantMultiHashMap.AsParallelWriter(),
        };
        Dependency = job.ScheduleParallel(_query, Dependency);
        Dependency.Complete();
        // ADDED: Debug drawing for quadrants
        DebugDrawQuadrants();

        //formation movment

        //formation find target
        entityCount = _findTargetQuery.CalculateEntityCount();
        NativeArray<EntityWithPosition> writeBuffer = _useBuffer1 ? _closestTargetsBuffer1 : _closestTargetsBuffer2;

        if (!writeBuffer.IsCreated || writeBuffer.Length != entityCount)
        {
            if (writeBuffer.IsCreated) writeBuffer.Dispose();
            writeBuffer = new NativeArray<EntityWithPosition>(entityCount, Allocator.Persistent);

            if (_useBuffer1)
                _closestTargetsBuffer1 = writeBuffer;
            else
                _closestTargetsBuffer2 = writeBuffer;
        }
        var findJob = new FindTargetsJob
        {
            FormationQuadrantMultiHashMap = FormationQuadrantMultiHashMap,
            ClosestTargets = writeBuffer,
            QuadrantEntityTypeHandle = GetComponentTypeHandle<QuadrantEntity>(true),
            FormationGroupTypeHandle = GetComponentTypeHandle<FormationGroupComponent>(true),
            EntityTypeHandle = GetEntityTypeHandle(),
        };
        var findHandle = findJob.ScheduleParallel(_findTargetQuery, Dependency);
        Dependency.Complete();
        var ecbWriter = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var addComponentJob = new AddTargetComponentJob
        {
            ClosestTargets = writeBuffer,
            EntityTypeHandle = GetEntityTypeHandle(),
            ECB = ecbWriter,
            FormationGroupTypeHandle = GetComponentTypeHandle<FormationGroupComponent>(false)
        };

        var addHandle = addComponentJob.ScheduleParallel(_findTargetQuery, findHandle);
        _ecbSystem.AddJobHandleForProducer(addHandle);

        _useBuffer1 = !_useBuffer1;
        Dependency = addHandle;

        //formation combat
        Entities
            .WithName("FormationCombatLogic")
            .WithAll<FormationComponent>()
            .WithNone<DeadTagComponent>()
            .ForEach((Entity entity,
                     ref HasTarget hasTarget,
                     ref CombatState combatState,
                     ref FormationComponent formation
                     , ref AnimationComponent animationComponent
                     , in Translation translation
                     ) =>
            {
                var unitFormatonStatus = formation.Status;
                if (animationComponent.UnitType == EntitySpawner.UnitType.Enemy)
                    unitFormatonStatus = FormationStatus.Broken;
                switch (unitFormatonStatus)
                {
                    case FormationStatus.Hold:
                    default:
                        animationComponent.Direction = formation.Direction;
                        HandleHoldFormation(ref hasTarget, ref combatState, ref formation, translation);
                        break;

                    case FormationStatus.Engaged:
                        HandleEngagedFormation(ref hasTarget, ref combatState, ref formation, translation);
                        break;

                    case FormationStatus.Broken:
                        // Let normal combat system handle it  
                        break;

                    case FormationStatus.Returning:
                        break;
                }
            }).ScheduleParallel();

        CompleteDependency();
    }

    public static int GetPositionHashMapKey(float2 position)
    {
        return (int)(math.floor(position.x / quadrantCellSize) + (QuadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }


    private static void HandleHoldFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                   ref FormationComponent formation, Translation translation)
    {
        // Tight formation - very little movement allowed
        float maxEngageDistance = 0.5f;

        float2 formationPos = formation.FormationPosition;
        float distanceFromFormation = math.distance(translation.Value.xy, formationPos);
        if (hasTarget.Type == HasTarget.TargetType.Entity && hasTarget.TargetEntity != Entity.Null)
        {
            //check target position from current before moving
            float distanceFromCurrentTranslation = math.distance(translation.Value.xy, hasTarget.TargetPosition);
            if (distanceFromCurrentTranslation > maxEngageDistance)
            {
                // Too far - return to formation
                //hasTarget.Type = HasTarget.TargetType.Position;
                //hasTarget.TargetPosition = formationPos;
                //combatState.CurrentState = CombatState.State.Idle;
                return;
            }
        }
        if (distanceFromFormation > maxEngageDistance)
        {
            //Debug.Log("HasTarget.TargetPosition updated by HandleHoldFormation in FormationCombatSystem");

            // Too far - return to formation immediately
            hasTarget.Type = HasTarget.TargetType.Position;
            hasTarget.TargetPosition = formationPos;
            combatState.CurrentState = CombatState.State.Idle;
        }
        // If they have an enemy target AND are close enough, let them attack!
        else
        {
            //formation.FormationPosition = hasTarget.TargetPosition;
            //hasTarget.Type = HasTarget.TargetType.Position;
            //combatState.CurrentState = CombatState.State.Attacking;
        }
    }

    private static void HandleEngagedFormation(ref HasTarget hasTarget, ref CombatState combatState,
                                      ref FormationComponent formation, Translation translation)
    {
        // Loose formation - more freedom to engage
        float maxEngageDistance = 10f;

        float2 formationPos = formation.FormationPosition;
        //if (hasTarget.Type == HasTarget.TargetType.Entity && hasTarget.TargetEntity != Entity.Null)
        //{
        //    //check target position from current before moving
        //    float distanceFromCurrentTranslation = math.distance(translation.Value.xy, hasTarget.TargetPosition);
        //    if (distanceFromCurrentTranslation > maxEngageDistance)
        //    {
        //        //// Too far - return to formation
        //        //hasTarget.Type = HasTarget.TargetType.Position;
        //        //hasTarget.TargetPosition = formationPos;
        //        //combatState.CurrentState = CombatState.State.Idle;
        //        return;
        //    }
        //}

        float distanceFromFormation = math.distance(translation.Value.xy, formationPos);

        if (distanceFromFormation > maxEngageDistance)
        {
            Debug.Log("HasTarget.TargetPosition updated by HandleEngagedFormation in FormationCombatSystem");

            // Too far - return to formation
            hasTarget.Type = HasTarget.TargetType.Position;
            hasTarget.TargetPosition = formationPos;
            combatState.CurrentState = CombatState.State.Idle;
        }
        else
        {
            //formation.FormationPosition = hasTarget.TargetPosition;
            //hasTarget.Type = HasTarget.TargetType.Position;
            //combatState.CurrentState = CombatState.State.Attacking;
        }
        // Otherwise, let them keep their current target (enemy) and attack freely!
    }


    [BurstCompile]
    private struct SetQuadrantDataHashMapJob : IJobChunk
    {

        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMultiHashMap;

        [ReadOnly] public ComponentTypeHandle<FormationGroupComponent> FormationGroupTypeHandle;
        [ReadOnly] public ComponentTypeHandle<QuadrantEntity> quadrantEntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {

            var entities = chunk.GetNativeArray(entityTypeHandle);
            var formationGroups = chunk.GetNativeArray(FormationGroupTypeHandle);
            var quadrantEntities = chunk.GetNativeArray(quadrantEntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var formationGroup = formationGroups[i];
                //float2 translation2d = translations[i].Value.xy;
                float2 translation2d = formationGroup.AnchorPosition;

                int hashMapKey = GetPositionHashMapKey(translation2d);
                quadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                {
                    Entity = entities[i],
                    Position = translation2d,
                    UnitType = formationGroup.UnitType,
                    QuadrantEntity = quadrantEntities[i]
                });
            }
        }
    }

    //[BurstCompile]
    private struct FindTargetsJob : IJobChunk
    {
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> FormationQuadrantMultiHashMap;
        public NativeArray<EntityWithPosition> ClosestTargets;

        [ReadOnly] public ComponentTypeHandle<FormationGroupComponent> FormationGroupTypeHandle;

        [ReadOnly] public ComponentTypeHandle<QuadrantEntity> QuadrantEntityTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {

            var formationGroups = chunk.GetNativeArray(FormationGroupTypeHandle);
            var quadrantEntities = chunk.GetNativeArray(QuadrantEntityTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var quadrantEntity = quadrantEntities[i];
                var entity = entities[i];

                Entity closestTarget = Entity.Null;
                float2 closestPosition = float2.zero;
                float closestDistanceSq = float.MaxValue;
                var formationGroup = formationGroups[i];
                if (formationGroup.CurrentCommand != CommandType.FindTarget)
                    continue;
                float2 unitPosition = formationGroup.AnchorPosition;

                int hashKey = GetPositionHashMapKey(unitPosition);

                CheckSurroundingQuadrants(hashKey, unitPosition, entity, 
                    ref closestTarget, ref closestDistanceSq, ref closestPosition, formationGroup);

                float2 halfSize = (formationGroup.BoundsMax - formationGroup.BoundsMin) * 0.5f;
                var radius = math.min(halfSize.x, halfSize.y);

                ClosestTargets[firstEntityIndex + i] = new EntityWithPosition
                {
                    Entity = closestTarget,
                    Position = closestPosition,
                    FormationRadius = radius
                };
            }
        }

        private void CheckSurroundingQuadrants(int hashKey, float2 unitPosition, Entity currentEntity,
             ref Entity closestTarget, ref float closestDistanceSq, ref float2 closestPosition, FormationGroupComponent formationGroup)
        {
            //if (closestDistanceSq < 10.0f) return;

            CheckQuadrant(hashKey, unitPosition, currentEntity, ref closestTarget, ref closestDistanceSq, ref closestPosition, formationGroup);
            CheckQuadrant(hashKey + 1, unitPosition, currentEntity, ref closestTarget, ref closestDistanceSq, ref closestPosition, formationGroup);
            CheckQuadrant(hashKey - 1, unitPosition, currentEntity, ref closestTarget, ref closestDistanceSq, ref closestPosition, formationGroup);
            CheckQuadrant(hashKey + QuadrantYMultiplier, unitPosition, currentEntity, ref closestTarget, ref closestDistanceSq, ref closestPosition, formationGroup);
            CheckQuadrant(hashKey - QuadrantYMultiplier, unitPosition, currentEntity, ref closestTarget, ref closestDistanceSq, ref closestPosition, formationGroup);
        }

        private void CheckQuadrant(int hashKey, float2 unitPosition, Entity currentEntity,
             ref Entity closestTarget, ref float closestDistanceSq, ref float2 closestPosition, FormationGroupComponent formationGroup)
        {
            if (FormationQuadrantMultiHashMap.TryGetFirstValue(hashKey, out QuadrantData data, out var iterator))
            {
                do
                {
                    if (currentEntity == data.Entity) continue;
                    //if (formationGroup.UnitType == data.UnitType) continue;

                    float distanceSq = math.distancesq(unitPosition, data.Position);
                    if (distanceSq >= closestDistanceSq) continue;

                    closestTarget = data.Entity;
                    closestDistanceSq = distanceSq;
                    closestPosition = data.Position;

                } while (FormationQuadrantMultiHashMap.TryGetNextValue(out data, ref iterator));
            }
        }
    }

    //[BurstCompile]
    private struct AddTargetComponentJob : IJobChunk
    {
        [ReadOnly] public NativeArray<EntityWithPosition> ClosestTargets;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ECB;

        public ComponentTypeHandle<FormationGroupComponent> FormationGroupTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkEntities = chunk.GetNativeArray(EntityTypeHandle);
            var formationGroupComponents = chunk.GetNativeArray(FormationGroupTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = chunkEntities[i];
                int flatIndex = firstEntityIndex + i;
                var formationGroup = formationGroupComponents[i];
                if (ClosestTargets[flatIndex].Entity != Entity.Null)
                {
                    var currentPos = formationGroup.AnchorPosition;
                    float2 halfSize = (formationGroup.BoundsMax - formationGroup.BoundsMin) * 0.5f;
                    var radius = math.min(halfSize.x, halfSize.y);

                    // Calculate direction from current position to target
                    float2 toTarget = ClosestTargets[flatIndex].Position - currentPos;

                    // Normalize the direction (check for zero first)
                    if (math.lengthsq(toTarget) > 0.001f)
                    {
                        float2 direction = math.normalize(toTarget);

                        // Set anchor position so our formation edge meets their formation edge
                        // Our radius + their radius gives the total distance between centers
                        float totalSeparation = radius + ClosestTargets[flatIndex].FormationRadius;

                        // Position our formation so edges touch
                        formationGroup.AnchorPosition = ClosestTargets[flatIndex].Position - direction * totalSeparation;
                    }
                    else
                    {
                        // If we're already at the target position, just offset slightly
                        formationGroup.AnchorPosition = ClosestTargets[flatIndex].Position - new float2(radius + ClosestTargets[flatIndex].FormationRadius, 0);
                    }

                    formationGroupComponents[i] = formationGroup;
                }
            }
        }
    }
    // ADDED: Method to debug draw quadrant boundaries
    private void DebugDrawQuadrants()
    {
        // Get all unique quadrant keys from the hashmap
        var quadrantKeys = new NativeHashSet<int>(1000, Allocator.Temp);
        var enumerator = FormationQuadrantMultiHashMap.GetKeyValueArrays(Allocator.Temp);

        for (int i = 0; i < enumerator.Keys.Length; i++)
        {
            quadrantKeys.Add(enumerator.Keys[i]);
        }

        // Draw each quadrant
        foreach (int key in quadrantKeys)
        {
            DrawQuadrantBoundary(key);
        }

        quadrantKeys.Dispose();
        enumerator.Dispose();
    }

    // ADDED: Method to draw individual quadrant boundaries
    private void DrawQuadrantBoundary(int hashKey)
    {
        // Convert hash key back to grid coordinates
        int gridY = hashKey / QuadrantYMultiplier;
        int gridX = hashKey % QuadrantYMultiplier;

        // Calculate world position of quadrant
        float worldX = gridX * quadrantCellSize;
        float worldY = gridY * quadrantCellSize;

        // Define quadrant corners (using only x,y - z=0)
        Vector3 bottomLeft = new Vector3(worldX, worldY, 0);
        Vector3 bottomRight = new Vector3(worldX + quadrantCellSize, worldY, 0);
        Vector3 topLeft = new Vector3(worldX, worldY + quadrantCellSize, 0);
        Vector3 topRight = new Vector3(worldX + quadrantCellSize, worldY + quadrantCellSize, 0);

        // Draw quadrant boundary in green
        Color quadrantColor = Color.green;
        float duration = 0.1f; // Last for one frame

        Debug.DrawLine(bottomLeft, bottomRight, quadrantColor, duration);
        Debug.DrawLine(bottomRight, topRight, quadrantColor, duration);
        Debug.DrawLine(topRight, topLeft, quadrantColor, duration);
        Debug.DrawLine(topLeft, bottomLeft, quadrantColor, duration);

        // Optional: Draw a diagonal to see which quadrants are active
        Debug.DrawLine(bottomLeft, topRight, quadrantColor, duration);
    }

}