using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TargetValidationSystem))]
[UpdateAfter(typeof(PlayerControlSystem))]
public class ProcessOrderSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _formationGroupQuery;
    public FormationManagerSystem fms;
    private EntityManager entityManager;
    private EntityQuery _unitQuery;
    private EntityQuery _unitGroupQuery;

    [NativeDisableParallelForRestriction]
    private NativeMultiHashMap<Entity, Entity> _groupToUnitsMap;
    [NativeDisableParallelForRestriction]
    private NativeHashMap<Entity, float2> _groupAveragePositions;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _groupToUnitsMap.Dispose();
    }

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();

        _formationGroupQuery = GetEntityQuery(typeof(FormationGroupComponent));
        entityManager = EntityManager;
        _unitQuery = GetEntityQuery(
    ComponentType.ReadWrite<FormationComponent>(),
    ComponentType.Exclude<DeadTagComponent>()
);
        _unitGroupQuery = GetEntityQuery(typeof(FormationGroupComponent));

    }

    private void InitializeOrClearNativeMaps(int unitCount, int groupCount)
    {
        if (!_groupToUnitsMap.IsCreated)
            _groupToUnitsMap = new NativeMultiHashMap<Entity, Entity>(unitCount * 2, Allocator.Persistent);
        else
            _groupToUnitsMap.Clear();

    }
    private void PopulateGroupToUnitsMap(NativeArray<Entity> unitEntities, JobHandle inputDeps)
    {
        var groupToUnitsWriter = _groupToUnitsMap.AsParallelWriter();
        Entities
            .WithAll<FormationComponent>()
            .ForEach((Entity entity, ref FormationComponent formationComponent) =>
            {
                if (formationComponent.FormationGroupEntity.HasValue)
                {
                    groupToUnitsWriter.Add(formationComponent.FormationGroupEntity.Value, entity);
                }
            }).WithBurst().Schedule(inputDeps).Complete();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (fms == null)
        {
            fms = World.GetExistingSystem<FormationManagerSystem>();
        }
        var unitEntities = _unitQuery.ToEntityArray(Allocator.TempJob);
        var translations = GetComponentDataFromEntity<Translation>(true);
        var groupCount = _unitGroupQuery.CalculateEntityCount();

        InitializeOrClearNativeMaps(unitEntities.Length, groupCount);
        PopulateGroupToUnitsMap(unitEntities, inputDeps);



        //get commander 
        // Check if we have a commander
        EntityQuery _query = GetEntityQuery(
ComponentType.ReadOnly<Unit>(),
ComponentType.ReadWrite<OrderData>(),
ComponentType.ReadWrite<CombatState>(),
ComponentType.ReadOnly<DefenseComponent>(),
ComponentType.ReadOnly<AttackComponent>(),
ComponentType.ReadOnly<Translation>(),
ComponentType.ReadOnly<AnimationComponent>(),
ComponentType.ReadWrite<FormationOrderIntent>(),
ComponentType.Exclude<CommanderComponent>());

        var ecb = _ecbSystem.CreateCommandBuffer();

    //    Entities
    //.WithName("ProcessCommandsENEMYAI")
    //.WithAll<CommandData, FormationGroupComponent>()
    //.ForEach((int entityInQueryIndex, Entity entity,
    //         ref CommandData order,
    //         ref FormationGroupComponent formationGroup) =>
    //{
    //    //Simple enemy AI order
    //    if (formationGroup.UnitType == EntitySpawner.UnitType.Enemy && order.InitialCommand == true)
    //    {
    //        order = CommandFactory.CreateMoveDirectionalRangeCommand(CommandType.MoveDirectionalRange, 10f, EntitySpawner.Direction.Right);
    //        formationGroup.CurrentCommand = order.Command;

    //    }

    //}).WithoutBurst().Run();
        Entities
            .WithName("ProcessCommands")
            .WithAll<OrderData, FormationGroupComponent>()
            .ForEach((int entityInQueryIndex, Entity entity,
                     ref OrderData order,
                     ref FormationGroupComponent formationGroup) =>
            {

                float distance = math.distance(formationGroup.CurrentUnitAveragePosition, formationGroup.AnchorPosition);
                if (formationGroup.FormationGroupStatus == FormationStatusEnum.Engaged && order.CurrentOrder == OrderType.Idle)
                {
                    order.CurrentOrder = OrderType.Defend;
                }
                switch (order.CurrentOrder)
                {
                    case OrderType.Idle:
                        break;
                    case OrderType.FindTarget:
                        if (distance > 5f)
                        {
                            //formationGroup.AnchorPosition = formationGroup.CurrentUnitAveragePosition;
                        }
                        //formationGroup.FormationGroupStatus = FormationStatusEnum.Engaged;
                        break;
                    case OrderType.MoveTo:
                        break;
                    case OrderType.March:
                        break;
                    case OrderType.Charge:
                        break;
                    case OrderType.MoveDirectionalRange:
                        if (order.InitialOrder)//only set this once at start of order!
                        {
                            var currentFormationPos = formationGroup.AnchorPosition;
                            var directionVector = CombatUtils.GetDirectionVector(order.FormationDirectionToMove) * order.MoveRange;
                            formationGroup.AnchorPosition = currentFormationPos + (directionVector * .25f);//.25 to convert to meters! 
                            order.InitialOrder = false;
                        }
                        break;
                    case OrderType.Attack:

                        if (distance > 5f)
                        {
                            //formationGroup.AnchorPosition = formationGroup.CurrentUnitAveragePosition;
                        }
                        break;
                    case OrderType.Defend:
                        formationGroup.FormationGroupStatus = FormationStatusEnum.Hold;
                        //if (fms._groupAveragePositions.TryGetValue(formationGroup.FormationGroupEntity, out var currentAveragePos))
                        //{
                        //formationGroup.AnchorPosition = currentAveragePos;
                        // Only update if we've moved significantly from current anchor
                        if (distance > 5f)
                        {
                            //formationGroup.AnchorPosition = formationGroup.CurrentUnitAveragePosition;
                        }
                        //}
                        break;
                    default:
                        break;
                }

                if (order.CurrentOrder == OrderType.MoveTo)
                {
                    // Update formation anchor position directly!
                    formationGroup.AnchorPosition = order.TargetPosition;
                    //ecb.SetComponent( entity, formation);
                }
                formationGroup.CurrentOrder = order.CurrentOrder;
            }).WithBurst().Run();

        var job = new AssignOrderJob
        {
            //Time = UnityEngine.Time.deltaTime,
            OrderDataTypeHandle = GetComponentTypeHandle<OrderData>(false),
            FormationComponentTypeHandle = GetComponentTypeHandle<FormationComponent>(false),
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            EntityTypeHandle = GetEntityTypeHandle(),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            DefenseComponentTypeHandle = GetComponentTypeHandle<DefenseComponent>(true),
            AttackComponentTypeHandle = GetComponentTypeHandle<AttackComponent>(true),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            FormationOrderIntentTypeHandle = GetComponentTypeHandle<FormationOrderIntent>(false),
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter()
            //,entityManager = EntityManager
        };

        var handle = job.ScheduleParallel(_query, inputDeps);
        _ecbSystem.AddJobHandleForProducer(handle);
        unitEntities.Dispose();
        return handle;
    }

    [BurstCompile]
    private struct AssignOrderJob : IJobChunk
    {
        public ComponentTypeHandle<OrderData> OrderDataTypeHandle;
        public ComponentTypeHandle<FormationComponent> FormationComponentTypeHandle;
        public ComponentTypeHandle<CombatState> CombatStateTypeHandle;
        //public ComponentTypeHandle<MovementSpeedComponent> MovementSpeedTypeHandle;

        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DefenseComponent> DefenseComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AttackComponent> AttackComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;

        public ComponentTypeHandle<FormationOrderIntent> FormationOrderIntentTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {

            var orderDataArray = chunk.GetNativeArray(OrderDataTypeHandle);
            var formations = chunk.GetNativeArray(FormationComponentTypeHandle);
            var combatStateArray = chunk.GetNativeArray(CombatStateTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            //var movementSpeeds = chunk.GetNativeArray(MovementSpeedTypeHandle);
            var defenseComponents = chunk.GetNativeArray(DefenseComponentTypeHandle);
            var attackComponents = chunk.GetNativeArray(AttackComponentTypeHandle);       
            var formationOrderIntents = chunk.GetNativeArray(FormationOrderIntentTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                Translation translation = translations[i];
                AnimationComponent animationData = animations[i];
                //MovementSpeedComponent movementSpeed = movementSpeeds[i];
                float2 entityPos = translation.Value.xy;
                var order = orderDataArray[i];
                var formation = formations[i];
                var combatState = combatStateArray[i];
                var defenseComponent = defenseComponents[i];
                var attackComponent = attackComponents[i];
                var formationOrderIntent = formationOrderIntents[i];

                if (order.CurrentOrder != order.PreviousOrder)
                    order.TargetEntity = Entity.Null;

                ProcessOrder(ref order, ref combatState, /*ref movementSpeed, */attackComponent, defenseComponent, entity, entityPos,
             animationData.Direction, chunkIndex, ECB, ref formation, ref formationOrderIntent);


                order.PreviousOrder = order.CurrentOrder;
                orderDataArray[i] = order;  // You do this for order, but not for formation!
                formations[i] = formation;  // You do this for order, but not for formation!
                combatStateArray[i] = combatState;  // You do this for order, but not for formation!
                //movementSpeeds[i] = movementSpeed;  // You do this for order, but not for formation!
                formationOrderIntents[i] = formationOrderIntent;
            }
        }

        private void ProcessOrder(ref OrderData order, ref CombatState combatState,
                                     /*ref MovementSpeedComponent movementSpeed, */AttackComponent attackComponent, DefenseComponent defenseComponent, Entity entity,
                                     float2 entityPos, EntitySpawner.Direction direction,
                                     int chunkIndex, EntityCommandBuffer.ParallelWriter ecb
             , ref FormationComponent formation, ref FormationOrderIntent formationOrderIntent)
        {

            //maybe dont do anything if attacking/defending/blocking///process after?
            //if (attackComponent.isDefending || defenseComponent.IsBlocking) return;

            switch (order.CurrentOrder)
            {
                case OrderType.Idle:
                    break;

                case OrderType.March:
                case OrderType.Charge:
                    HandleMovementOrder(order.CurrentOrder, ref combatState/*, ref movementSpeed*/, entity, entityPos, direction, chunkIndex, ecb);
                    break;
                case OrderType.FindTarget:
                    formationOrderIntent.Status = FormationStatusEnum.Hold;
                    HandleFindTargetOrder(ref order, ref combatState, entity, chunkIndex, ecb);
                    break;

                case OrderType.MoveTo:
                    if (formationOrderIntent.Status == FormationStatusEnum.Engaged)
                    {
                        formationOrderIntent.Status = FormationStatusEnum.Disengaging;
                    }
                    else
                    {
                        formationOrderIntent.Status = FormationStatusEnum.Hold; // moving into a new hold position
                    }
                    HandleMoveToOrder(ref order, entity, entityPos, chunkIndex, ecb, ref formation);
                    break;
                    

                case OrderType.MoveDirectionalRange:
                    if (formationOrderIntent.Status == FormationStatusEnum.Engaged)
                    {
                        formationOrderIntent.Status = FormationStatusEnum.Disengaging;
                    }
                    else
                    {
                        formationOrderIntent.Status = FormationStatusEnum.Hold; // moving into a new hold position
                    }
                    MarchInDirectionWithRange(order.CurrentOrder, ref combatState/*, ref movementSpeed*/, entity, entityPos, direction, chunkIndex, ecb, 10f);
                    break;

                case OrderType.Attack:
                    HandleAttackOrder(ref order, ref combatState, entity, chunkIndex, ecb, ref formation, ref formationOrderIntent);
                    break;

                case OrderType.Defend:
                    // TODO: Implement defend logic
                    if (formationOrderIntent.Status == FormationStatusEnum.Engaged)
                    {
                        ecb.AddComponent<FindTargetTag>(chunkIndex, entity); //target closest and fight them while staying in formation! 
                    }
                    else
                    {
                        formationOrderIntent.Status = FormationStatusEnum.Hold;
                    }
                    break;
            }
        }
        private void MarchInDirectionWithRange(OrderType orderType, ref CombatState combatState,
                                         /*ref MovementSpeedComponent movementSpeed,*/ Entity entity,
                                         float2 entityPos, EntitySpawner.Direction direction,
                                         int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, float rangeToMarch)
        {
            float2 currentTargetLocation = entityPos;
            // => direction right: float2(1,0);
            // * range(10) = float2(1,0) * 10 = float2(10, 0);
            float2 newTargetLocation = CombatUtils.GetDirectionVector(direction) * rangeToMarch;
            float2 targetPos = currentTargetLocation + newTargetLocation;
            ecb.AddComponent(chunkIndex, entity, new FormationSlotGoal
            {
                TargetPosition = targetPos,
                //TargetEntity = Entity.Null
            });
            ecb.AddComponent(chunkIndex, entity, new CombatTarget
            {
                //Type = FormationSlotGoal.TargetType.Position,
                ////TargetPosition = targetPos,
                ////TargetEntity = Entity.Null
                isActive = true
            });
        }

        private void HandleMovementOrder(OrderType orderType, ref CombatState combatState,
                                         /*ref MovementSpeedComponent movementSpeed, */Entity entity,
                                         float2 entityPos, EntitySpawner.Direction direction,
                                         int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {
            float2 dir = CombatUtils.GetDirectionVector(direction);
            float endlessDistance = 1000f;
            float2 targetPos = entityPos + (dir * endlessDistance);
            Debug.Log("FormationSlotGoal updated by HandleMovementOrder in ProcessOrderSystem");

            ecb.AddComponent(chunkIndex, entity, new FormationSlotGoal
            {
                TargetPosition = targetPos
            });

            //movementSpeed.isRunnning = orderType == OrderType.Charge;
            combatState.CurrentState = orderType == OrderType.Charge ?
                CombatState.State.SeekingTarget : combatState.CurrentState;
        }

        private void HandleFindTargetOrder(ref OrderData order, ref CombatState combatState,
                                           Entity entity, int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {
            order.TargetEntity = Entity.Null;
            order.TargetPosition = float2.zero;
            ecb.AddComponent<FindTargetTag>(chunkIndex, entity);
            combatState.CurrentState = CombatState.State.SeekingTarget;
            order.CurrentOrder = OrderType.Idle;
        }

        private void HandleMoveToOrder(ref OrderData order, Entity entity, float2 entityPos,
                                       int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, ref FormationComponent formation)
        {
            float2 targetPos = math.lengthsq(order.TargetPosition) > 0.4f ?
                order.TargetPosition : entityPos + order.TargetPosition;

            ecb.AddComponent(chunkIndex, entity, new FormationSlotGoal
            {
                TargetPosition = targetPos,
                //TargetEntity = Entity.Null
            });

            order.CurrentOrder = OrderType.Idle;
        }

        private void HandleAttackOrder(ref OrderData order, ref CombatState combatState,
                                       Entity entity, int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, ref FormationComponent formation, ref FormationOrderIntent formationOrderIntent)
        {

            //recheck if unit is not taking dmg/blocking

            if (order.TargetEntity == Entity.Null && math.lengthsq(order.TargetPosition) > 0.4f)
            {
                // Move-then-attack
                combatState.CurrentState = CombatState.State.SeekingTarget;
                ecb.AddComponent<FindTargetTag>(chunkIndex, entity);
                ecb.AddComponent<AttackOrderTag>(chunkIndex, entity);
            }
            else if (order.TargetEntity != Entity.Null)
            {
                // Direct attack on entity
                combatState.CurrentState = CombatState.State.Attacking;
                Debug.Log("FormationSlotGoal updated by HandleAttackOrder in ProcessOrderSystem");
                formationOrderIntent.Status = FormationStatusEnum.Engaged;
                ecb.AddComponent(chunkIndex, entity, new FormationSlotGoal
                {
                    //TargetEntity = order.TargetEntity
                });
                ecb.AddComponent<AttackOrderTag>(chunkIndex, entity);
            }

            order.CurrentOrder = OrderType.Idle;
        }

    }
}

public enum OrderType : byte
{
    Idle,
    FindTarget,
    MoveTo,
    March, //march forward endlesslly
    Charge, //charge in facing direction until reaching enemies
    Attack,
    Defend
    , Follow
    , MoveDirectionalRange
}

public struct FormationOrderIntent : IComponentData
{
    public FormationStatusEnum Status;
    public float2 TargetPosition; // Optional (used for MoveTo, etc.)
    public Entity TargetEntity;   // Optional (used for Attack, etc.)
    public Entity FormationGroupEntity; // For group orders
}