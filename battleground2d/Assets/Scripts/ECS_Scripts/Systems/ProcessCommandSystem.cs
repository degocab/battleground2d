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
public class ProcessCommandSystem : JobComponentSystem
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
ComponentType.ReadWrite<CommandData>(),
ComponentType.ReadWrite<CombatState>(),
ComponentType.ReadOnly<DefenseComponent>(),
ComponentType.ReadOnly<AttackComponent>(),
ComponentType.ReadOnly<Translation>(),
ComponentType.ReadOnly<AnimationComponent>(),
ComponentType.ReadWrite<MovementSpeedComponent>(),
ComponentType.Exclude<CommanderComponent>());

        var ecb = _ecbSystem.CreateCommandBuffer();

    //    Entities
    //.WithName("ProcessCommandsENEMYAI")
    //.WithAll<CommandData, FormationGroupComponent>()
    //.ForEach((int entityInQueryIndex, Entity entity,
    //         ref CommandData command,
    //         ref FormationGroupComponent formationGroup) =>
    //{
    //    //Simple enemy AI command
    //    if (formationGroup.UnitType == EntitySpawner.UnitType.Enemy && command.InitialCommand == true)
    //    {
    //        command = CommandFactory.CreateMoveDirectionalRangeCommand(CommandType.MoveDirectionalRange, 10f, EntitySpawner.Direction.Right);
    //        formationGroup.CurrentCommand = command.Command;

    //    }

    //}).WithoutBurst().Run();
        Entities
            .WithName("ProcessCommands")
            .WithAll<CommandData, FormationGroupComponent>()
            .ForEach((int entityInQueryIndex, Entity entity,
                     ref CommandData command,
                     ref FormationGroupComponent formationGroup) =>
            {

                float distance = math.distance(formationGroup.CurrentUnitAveragePosition, formationGroup.AnchorPosition);

                switch (command.Command)
                {
                    case CommandType.Idle:
                        break;
                    case CommandType.FindTarget:
                        if (distance > 5f)
                        {
                            //formationGroup.AnchorPosition = formationGroup.CurrentUnitAveragePosition;
                        }
                        //formationGroup.FormationGroupStatus = FormationStatus.Engaged;
                        break;
                    case CommandType.MoveTo:
                        break;
                    case CommandType.March:
                        break;
                    case CommandType.Charge:
                        break;
                    case CommandType.MoveDirectionalRange:
                        if (command.InitialCommand)//only set this once at start of command!
                        {
                            var currentFormationPos = formationGroup.AnchorPosition;
                            var directionVector = CombatUtils.GetDirectionVector(command.FormationDirectionToMove) * command.MoveRange;
                            formationGroup.AnchorPosition = currentFormationPos + (directionVector * .25f);//.25 to convert to meters! 
                            command.InitialCommand = false;
                        }
                        break;
                    case CommandType.Attack:

                        if (distance > 5f)
                        {
                            //formationGroup.AnchorPosition = formationGroup.CurrentUnitAveragePosition;
                        }
                        break;
                    case CommandType.Defend:
                        formationGroup.FormationGroupStatus = FormationStatus.Hold;
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

                if (command.Command == CommandType.MoveTo)
                {
                    // Update formation anchor position directly!
                    formationGroup.AnchorPosition = command.TargetPosition;
                    //ecb.SetComponent( entity, formation);
                }
                formationGroup.CurrentCommand = command.Command;
            }).WithoutBurst().Run();

        
        var job = new AssignCommandJob
        {
            //Time = UnityEngine.Time.deltaTime,
            CommandDataTypeHandle = GetComponentTypeHandle<CommandData>(false),
            FormationComponentTypeHandle = GetComponentTypeHandle<FormationComponent>(false),
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            EntityTypeHandle = GetEntityTypeHandle(),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            DefenseComponentTypeHandle = GetComponentTypeHandle<DefenseComponent>(true),
            AttackComponentTypeHandle = GetComponentTypeHandle<AttackComponent>(true),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            MovementSpeedTypeHandle = GetComponentTypeHandle<MovementSpeedComponent>(false),
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter()
            //,entityManager = EntityManager
        };

        var handle = job.ScheduleParallel(_query, inputDeps);
        _ecbSystem.AddJobHandleForProducer(handle);
        unitEntities.Dispose();
        return handle;
    }

    [BurstCompile]
    private struct AssignCommandJob : IJobChunk
    {
        public ComponentTypeHandle<CommandData> CommandDataTypeHandle;
        public ComponentTypeHandle<FormationComponent> FormationComponentTypeHandle;
        public ComponentTypeHandle<CombatState> CombatStateTypeHandle;
        public ComponentTypeHandle<MovementSpeedComponent> MovementSpeedTypeHandle;

        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DefenseComponent> DefenseComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AttackComponent> AttackComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {

            var commandDataArray = chunk.GetNativeArray(CommandDataTypeHandle);
            var formations = chunk.GetNativeArray(FormationComponentTypeHandle);
            var combatStateArray = chunk.GetNativeArray(CombatStateTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            var movementSpeeds = chunk.GetNativeArray(MovementSpeedTypeHandle);
            var defenseComponents = chunk.GetNativeArray(DefenseComponentTypeHandle);
            var attackComponents = chunk.GetNativeArray(AttackComponentTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                Translation translation = translations[i];
                AnimationComponent animationData = animations[i];
                MovementSpeedComponent movementSpeed = movementSpeeds[i];
                float2 entityPos = translation.Value.xy;
                var command = commandDataArray[i];
                var formation = formations[i];
                var combatState = combatStateArray[i];
                var defenseComponent = defenseComponents[i];
                var attackComponent = attackComponents[i];

                if (command.Command != command.previousCommand)
                    command.TargetEntity = Entity.Null;

                ProcessCommand(ref command, ref combatState, ref movementSpeed, attackComponent, defenseComponent, entity, entityPos,
             animationData.Direction, chunkIndex, ECB, ref formation);


                command.previousCommand = command.Command;
                commandDataArray[i] = command;  // You do this for command, but not for formation!
                formations[i] = formation;  // You do this for command, but not for formation!
                //combatStateArray[i] = combatState;  // You do this for command, but not for formation!
                //movementSpeeds[i] = movementSpeed;  // You do this for command, but not for formation!
            }
        }

        private void ProcessCommand(ref CommandData command, ref CombatState combatState,
                                     ref MovementSpeedComponent movementSpeed, AttackComponent attackComponent, DefenseComponent defenseComponent, Entity entity,
                                     float2 entityPos, EntitySpawner.Direction direction,
                                     int chunkIndex, EntityCommandBuffer.ParallelWriter ecb
             , ref FormationComponent formation)
        {

            //maybe dont do anything if attacking/defending/blocking///process after?
            //if (attackComponent.isDefending || defenseComponent.IsBlocking) return;

            switch (command.Command)
            {
                case CommandType.Idle:
                    break;

                case CommandType.March:
                case CommandType.Charge:
                    HandleMovementCommand(command.Command, ref combatState, ref movementSpeed, entity, entityPos, direction, chunkIndex, ecb);
                    break;
                case CommandType.FindTarget:
                    formation.Status = FormationStatus.Hold;
                    HandleFindTargetCommand(ref command, ref combatState, entity, chunkIndex, ecb);
                    break;

                case CommandType.MoveTo:
                    if (formation.Status == FormationStatus.Engaged)
                    {
                        formation.Status = FormationStatus.Disengaging;
                    }
                    else
                    {
                        formation.Status = FormationStatus.Hold; // moving into a new hold position
                    }
                    HandleMoveToCommand(ref command, entity, entityPos, chunkIndex, ecb, ref formation);
                    break;
                    

                case CommandType.MoveDirectionalRange:
                    if (formation.Status == FormationStatus.Engaged)
                    {
                        formation.Status = FormationStatus.Disengaging;
                    }
                    else
                    {
                        formation.Status = FormationStatus.Hold; // moving into a new hold position
                    }
                    MarchInDirectionWithRange(command.Command, ref combatState, ref movementSpeed, entity, entityPos, direction, chunkIndex, ecb, 10f);
                    break;

                case CommandType.Attack:
                    HandleAttackCommand(ref command, ref combatState, entity, chunkIndex, ecb, ref formation);
                    break;

                case CommandType.Defend:
                    // TODO: Implement defend logic
                    formation.Status = FormationStatus.Hold;
                    ecb.AddComponent<FindTargetCommandTag>(chunkIndex, entity); //target closest and fight them while staying in formation!
                    break;
            }
        }
        private void MarchInDirectionWithRange(CommandType commandType, ref CombatState combatState,
                                         ref MovementSpeedComponent movementSpeed, Entity entity,
                                         float2 entityPos, EntitySpawner.Direction direction,
                                         int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, float rangeToMarch)
        {
            float2 currentTargetLocation = entityPos;
            // => direction right: float2(1,0);
            // * range(10) = float2(1,0) * 10 = float2(10, 0);
            float2 newTargetLocation = CombatUtils.GetDirectionVector(direction) * rangeToMarch;
            float2 targetPos = currentTargetLocation + newTargetLocation;
            ecb.AddComponent(chunkIndex, entity, new HasTarget
            {
                Type = HasTarget.TargetType.Position,
                //TargetPosition = targetPos,
                //TargetEntity = Entity.Null
            });
        }

        private void HandleMovementCommand(CommandType commandType, ref CombatState combatState,
                                         ref MovementSpeedComponent movementSpeed, Entity entity,
                                         float2 entityPos, EntitySpawner.Direction direction,
                                         int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {
            float2 dir = CombatUtils.GetDirectionVector(direction);
            float endlessDistance = 1000f;
            float2 targetPos = entityPos + (dir * endlessDistance);
            Debug.Log("HasTarget.TargetPosition updated by HandleMovementCommand in ProcessCommandSystem");

            ecb.AddComponent(chunkIndex, entity, new HasTarget
            {
                Type = HasTarget.TargetType.Position,
                TargetPosition = targetPos,
                TargetEntity = Entity.Null
            });

            movementSpeed.isRunnning = commandType == CommandType.Charge;
            combatState.CurrentState = commandType == CommandType.Charge ?
                CombatState.State.SeekingTarget : combatState.CurrentState;
        }

        private void HandleFindTargetCommand(ref CommandData command, ref CombatState combatState,
                                           Entity entity, int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {
            command.TargetEntity = Entity.Null;
            command.TargetPosition = float2.zero;
            ecb.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
            combatState.CurrentState = CombatState.State.SeekingTarget;
            command.Command = CommandType.Idle;
        }

        private void HandleMoveToCommand(ref CommandData command, Entity entity, float2 entityPos,
                                       int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, ref FormationComponent formation)
        {
            //    float2 targetPos = math.lengthsq(command.TargetPosition) > 0.4f ?
            //        command.TargetPosition : entityPos + command.TargetPosition;

            ecb.AddComponent(chunkIndex, entity, new HasTarget
            {
                Type = HasTarget.TargetType.Position,
                //TargetPosition = targetPos,
                //TargetEntity = Entity.Null
            });

            command.Command = CommandType.Idle;
        }

        private void HandleAttackCommand(ref CommandData command, ref CombatState combatState,
                                       Entity entity, int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, ref FormationComponent formation)
        {

            //recheck if unit is not taking dmg/blocking

            if (command.TargetEntity == Entity.Null && math.lengthsq(command.TargetPosition) > 0.4f)
            {
                // Move-then-attack
                combatState.CurrentState = CombatState.State.SeekingTarget;
                ecb.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
                ecb.AddComponent<AttackCommandTag>(chunkIndex, entity);
            }
            else if (command.TargetEntity != Entity.Null)
            {
                // Direct attack on entity
                combatState.CurrentState = CombatState.State.Attacking;
                Debug.Log("HasTarget.TargetPosition updated by HandleAttackCommand in ProcessCommandSystem");
                formation.Status = FormationStatus.Engaged;
                ecb.AddComponent(chunkIndex, entity, new HasTarget
                {
                    Type = HasTarget.TargetType.Entity,
                    TargetEntity = command.TargetEntity,
                    TargetPosition = float2.zero
                });
                ecb.AddComponent<AttackCommandTag>(chunkIndex, entity);
            }

            command.Command = CommandType.Idle;
        }

    }
}

public enum CommandType : byte
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
