
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UIElements;
using static Unity.Burst.Intrinsics.X86.Avx;

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
        public float DistanceSq;
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
        if (FormationQuadrantMultiHashMap.IsCreated)
            FormationQuadrantMultiHashMap.Dispose();

        if (_closestTargetsBuffer1.IsCreated)
            _closestTargetsBuffer1.Dispose();

        if (_closestTargetsBuffer2.IsCreated)
            _closestTargetsBuffer2.Dispose();

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
            // If the field already has a created array, dispose it before reallocating
            if (_useBuffer1)
            {
                if (_closestTargetsBuffer1.IsCreated)
                    _closestTargetsBuffer1.Dispose();

                _closestTargetsBuffer1 = new NativeArray<EntityWithPosition>(
                    entityCount,
                    Allocator.Persistent
                );
                writeBuffer = _closestTargetsBuffer1;
            }
            else
            {
                if (_closestTargetsBuffer2.IsCreated)
                    _closestTargetsBuffer2.Dispose();

                _closestTargetsBuffer2 = new NativeArray<EntityWithPosition>(
                    entityCount,
                    Allocator.Persistent
                );
                writeBuffer = _closestTargetsBuffer2;
            }
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
        Debug.Log($"writeBuffer: { (writeBuffer != null ? writeBuffer.Length : 0)}");

        var addComponentJob = new AddTargetComponentJob
        {
            ClosestTargets = writeBuffer,
            EntityTypeHandle = GetEntityTypeHandle(),
            CommandTypeHandle = GetComponentTypeHandle<CommandData>(false),
            //ECB = ecbWriter,
            EngagementRadius = 5f,
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
                     , in CommandData command
                     ) =>
            {
                var unitFormatonStatus = formation.Status;
                //if (animationComponent.UnitType == EntitySpawner.UnitType.Enemy)
                //    unitFormatonStatus = FormationStatus.Hold;
                switch (unitFormatonStatus)
                {
                    case FormationStatus.Hold:
                    default:
                        animationComponent.Direction = formation.Direction;
                        HandleHoldFormation(ref hasTarget, ref combatState, ref formation, translation);
                        break;

                    case FormationStatus.Engaged:
                        HandleEngagedFormation(ref hasTarget, ref combatState, ref formation, translation, command);
                        break;

                    case FormationStatus.Broken:
                        // Let normal combat system handle it  
                        break;

                    case FormationStatus.Disengaging:
                        HandleDisengagingFormation(ref hasTarget, in combatState, ref formation, in translation);
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
                                               ref FormationComponent formation, Translation translation, CommandData command)
    {
        if (command.Command == CommandType.Defend)
        {
            float maxEngageDistance = 0.5f;

            float2 formationPos = formation.FormationPosition;
            float distanceFromFormation = math.distance(translation.Value.xy, formationPos);
            if (distanceFromFormation > maxEngageDistance)
            {
                //Debug.Log("HasTarget.TargetPosition updated by HandleHoldFormation in FormationCombatSystem");

                // Too far - return to formation immediately
                hasTarget.Type = HasTarget.TargetType.Position;
                hasTarget.TargetPosition = formationPos;
                combatState.CurrentState = CombatState.State.Idle;
            }
        }
    }

    private static void HandleDisengagingFormation(
    ref HasTarget hasTarget,
    in CombatState combatState,
    ref FormationComponent formation,
    in Translation translation)
    {
        float2 currentPos = translation.Value.xy;
        float2 formationPos = formation.FormationPosition;

        // How close counts as "we've regrouped"
        const float reformedRadius = 1.0f;

        // 1) Force movement toward anchor / retreat point
        hasTarget.Type = HasTarget.TargetType.Position;
        hasTarget.TargetEntity = Entity.Null;
        hasTarget.TargetPosition = formationPos;

        // 2) Do NOT touch combatState here.
        //    CombatSystem will see TargetEntity == Null and, via
        //    HandleAttackingState / HandleSeekingState, naturally
        //    transition to Idle when appropriate.

        float distanceToAnchor = math.distance(currentPos, formationPos);

        // 3) Only consider the disengage "complete" when:
        //    - we're near the anchor AND
        //    - combat is no longer in an active fighting state.
        bool combatIsActive =
            combatState.CurrentState == CombatState.State.Attacking ||
            combatState.CurrentState == CombatState.State.SeekingTarget ||
            combatState.CurrentState == CombatState.State.Defending ||
            combatState.CurrentState == CombatState.State.Blocking;

        if (distanceToAnchor <= reformedRadius && !combatIsActive)
        {
            // We’re back in position and not actively fighting anymore:
            // let formation go back to Hold behavior.
            formation.Status = FormationStatus.Hold;
            // Hold logic will handle keeping them in slot, and CombatSystem will keep
            // doing its thing based on HasTarget/valid enemies.
        }
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

    [BurstCompile]
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
            var bestDistSq = float.MaxValue;
            for (int i = 0; i < chunk.Count; i++)
            {
                var quadrantEntity = quadrantEntities[i];
                var entity = entities[i];

                Entity closestTarget = Entity.Null;
                float2 closestPosition = float2.zero;
                float closestDistanceSq = float.MaxValue;
                var formationGroup = formationGroups[i];
                //if (formationGroup.CurrentCommand != CommandType.FindTarget)
                //    continue;
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
                    FormationRadius = radius,
                    DistanceSq = closestTarget == Entity.Null
                                ? float.MaxValue
                                : closestDistanceSq
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

    [BurstCompile]
    private struct AddTargetComponentJob : IJobChunk
    {
        [ReadOnly] public NativeArray<EntityWithPosition> ClosestTargets;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<CommandData> CommandTypeHandle;
        public ComponentTypeHandle<FormationGroupComponent> FormationGroupTypeHandle;

        public float EngagementRadius; // radius, not squared

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkEntities = chunk.GetNativeArray(EntityTypeHandle);
            var formationGroups = chunk.GetNativeArray(FormationGroupTypeHandle);
            var commands = chunk.GetNativeArray(CommandTypeHandle);

            float engagementRadiusSq = EngagementRadius * EngagementRadius;

            for (int i = 0; i < chunk.Count; i++)
            {
                int flatIndex = firstEntityIndex + i;

                var group = formationGroups[i];
                var cmd = commands[i];
                var closest = ClosestTargets[flatIndex];

                if (closest.Entity == Entity.Null)
                    continue;

                bool isAdvancing =
                    group.CurrentCommand == CommandType.March ||
                    group.CurrentCommand == CommandType.MoveDirectionalRange;

                // transition to engaged ONCE when we first make contact
                if (isAdvancing && closest.DistanceSq <= engagementRadiusSq)
                {
                    group.CurrentCommand = CommandType.FindTarget;
                    group.FormationGroupStatus = FormationStatus.Engaged;

                    cmd.Command = CommandType.FindTarget;

                    formationGroups[i] = group;
                    commands[i] = cmd;
                }

                // No anchor updates here. Slot follow happens elsewhere via CurrentUnitAveragePosition.
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