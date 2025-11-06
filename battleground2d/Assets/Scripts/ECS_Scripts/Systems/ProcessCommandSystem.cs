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
public class ProcessCommandSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _formationGroupQuery;
    public FormationManagerSystem fms;
    private EntityManager entityManager;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();

        _formationGroupQuery = GetEntityQuery(typeof(FormationGroupComponent));
        entityManager = EntityManager;
    }

    protected override void OnUpdate()
    {
        if (fms == null)
        {
            fms = World.GetExistingSystemManaged<FormationManagerSystem>();
        }
        //get commander 
        // Check if we have a commander
        EntityQuery _query = GetEntityQuery(
ComponentType.ReadOnly<Unit>(),
ComponentType.ReadWrite<CommandData>(),
ComponentType.ReadWrite<CombatState>(),
ComponentType.ReadOnly<DefenseComponent>(),
ComponentType.ReadOnly<AttackComponent>(),
ComponentType.ReadOnly<LocalTransform>(),
ComponentType.ReadOnly<AnimationComponent>(),
ComponentType.ReadWrite<MovementSpeedComponent>(),
ComponentType.Exclude<CommanderComponent>());

        var ecb = _ecbSystem.CreateCommandBuffer();
        ////get command position and update anchor position for formations
        //List<FormationGroupComponent> formationGroups = new List<FormationGroupComponent>();
        //EntityManager.GetAllUniqueSharedComponentData(formationGroups);
        //var formationEntities = _formationGroupQuery.ToEntityArray(Allocator.TempJob);

        //var groupLookup = new NativeHashMap<int, Entity>(formationGroups.Count, Allocator.TempJob);
        //for (int i = 0; i < formationGroups.Count; i++)
        //{
        //    groupLookup.TryAdd(formationGroups[i].FormationID, formationEntities[i]);
        //}

        // this can prob move to formation manager, because we run that after Processing Commands
        foreach (var (command, formationGroup, entity) in 
            SystemAPI.Query<RefRW<CommandData>, RefRW<FormationGroupComponent>>()
                .WithAll<CommandData, FormationGroupComponent>()
                .WithEntityAccess())
        {
            //Simple enemy AI command
            if (formationGroup.ValueRO.UnitType == EntitySpawner.UnitType.Enemy)
            {
                command.ValueRW.Command = CommandType.Defend;
                continue; //AI needs to handle its own
            }

            switch (command.ValueRO.Command)
            {
                case CommandType.Idle:
                    break;
                case CommandType.FindTarget:
                    break;
                case CommandType.MoveTo:
                    break;
                case CommandType.March:
                    break;
                case CommandType.Charge:
                    break;
                case CommandType.Attack:
                    break;
                case CommandType.Defend:
                    if (fms._groupAveragePositions.TryGetValue(formationGroup.ValueRO.FormationGroupEntity, out var currentAveragePos))
                    {
                        //formationGroup.AnchorPosition = currentAveragePos;
                        float distance = math.distance(currentAveragePos, formationGroup.ValueRO.AnchorPosition);

                        // Only update if we've moved significantly from current anchor
                        if (distance > 5f)
                        {
                            formationGroup.ValueRW.AnchorPosition = currentAveragePos;
                        }
                    }
                    break;
                default:
                    break;  
            }

            if (command.ValueRO.Command == CommandType.MoveTo)
            {
                // Update formation anchor position directly!
                formationGroup.ValueRW.AnchorPosition = command.ValueRO.TargetPosition;
                //ecb.SetComponent( entity, formation);
            }
            formationGroup.ValueRW.CurrentCommand = command.ValueRO.Command;
        }




        var job = new AssignCommandJob
        {
            //Time = UnityEngine.Time.deltaTime,
            CommandDataTypeHandle = GetComponentTypeHandle<CommandData>(false),
            FormationComponentTypeHandle = GetComponentTypeHandle<FormationComponent>(false),
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            EntityTypeHandle = GetEntityTypeHandle(),
            TransformTypeHandle = GetComponentTypeHandle<LocalTransform>(true),
            DefenseComponentTypeHandle = GetComponentTypeHandle<DefenseComponent>(true),
            AttackComponentTypeHandle = GetComponentTypeHandle<AttackComponent>(true),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            MovementSpeedTypeHandle = GetComponentTypeHandle<MovementSpeedComponent>(false),
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter()
            //,entityManager = EntityManager
        };

        var handle = job.ScheduleParallel(_query, Dependency);
        _ecbSystem.AddJobHandleForProducer(handle);
        Dependency = handle;
    }

    [BurstCompile]
    private struct AssignCommandJob : IJobChunk
    {
        public ComponentTypeHandle<CommandData> CommandDataTypeHandle;
        public ComponentTypeHandle<FormationComponent> FormationComponentTypeHandle;
        public ComponentTypeHandle<CombatState> CombatStateTypeHandle;
        public ComponentTypeHandle<MovementSpeedComponent> MovementSpeedTypeHandle;

        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentTypeHandle<LocalTransform> TransformTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DefenseComponent> DefenseComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AttackComponent> AttackComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {

            var commandDataArray = chunk.GetNativeArray(ref CommandDataTypeHandle);
            var formations = chunk.GetNativeArray(ref FormationComponentTypeHandle);
            var combatStateArray = chunk.GetNativeArray(ref CombatStateTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var transforms = chunk.GetNativeArray(ref TransformTypeHandle);
            var animations = chunk.GetNativeArray(ref AnimationTypeHandle);
            var movementSpeeds = chunk.GetNativeArray(ref MovementSpeedTypeHandle);
            var defenseComponents = chunk.GetNativeArray(ref DefenseComponentTypeHandle);
            var attackComponents = chunk.GetNativeArray(ref AttackComponentTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                LocalTransform transform = transforms[i];
                AnimationComponent animationData = animations[i];
                MovementSpeedComponent movementSpeed = movementSpeeds[i];
                float2 entityPos = transform.Position.xy;
                var command = commandDataArray[i];
                var formation = formations[i];
                var combatState = combatStateArray[i];
                var defenseComponent = defenseComponents[i];
                var attackComponent = attackComponents[i];

                if (command.Command != command.previousCommand)
                    command.TargetEntity = Entity.Null;

                ProcessCommand(ref command, ref combatState, ref movementSpeed, attackComponent, defenseComponent, entity, entityPos,
             animationData.Direction, unfilteredChunkIndex, ECB, ref formation);


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
                    formation.Status = FormationStatus.Hold;
                    HandleMoveToCommand(ref command, entity, entityPos, chunkIndex, ecb, ref formation);
                    break;

                case CommandType.Attack:
                    HandleAttackCommand(ref command, ref combatState, entity, chunkIndex, ecb);
                    break;

                case CommandType.Defend:
                    // TODO: Implement defend logic
                    formation.Status = FormationStatus.Hold;
                    break;
            }
        }

        private void HandleMovementCommand(CommandType commandType, ref CombatState combatState,
                                         ref MovementSpeedComponent movementSpeed, Entity entity,
                                         float2 entityPos, EntitySpawner.Direction direction,
                                         int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {
            float2 dir = GetDirectionVector(direction);
            float endlessDistance = 1000f;
            float2 targetPos = entityPos + (dir * endlessDistance);

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
            formation.AnchorPosition = command.TargetPosition;

            command.Command = CommandType.Idle;
        }

        private void HandleAttackCommand(ref CommandData command, ref CombatState combatState,
                                       Entity entity, int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
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
        private float2 GetDirectionVector(EntitySpawner.Direction direction)
        {
            switch (direction)
            {
                case EntitySpawner.Direction.Up:
                    return new float2(0, 1);
                case EntitySpawner.Direction.Down:
                    return new float2(0, -1);
                case EntitySpawner.Direction.Left:
                    return new float2(-1, 0);
                case EntitySpawner.Direction.Right:
                default:
                    return new float2(1, 0);
            }
        }
    }
    private float2 CalculateAveragePositionForGroup(Entity groupEntity)
    {
        float2 sum = float2.zero;
        int unitCount = 0;

        // You'd need access to the formation manager system's _groupToUnits
        var fms = World.GetExistingSystemManaged<FormationManagerSystem>();
        var transforms = GetComponentLookup<LocalTransform>(true);

        if (fms._groupToUnitsMap.TryGetFirstValue(groupEntity, out var unitEntity, out var iterator))
        {
            do
            {
                if (translations.HasComponent(unitEntity))
                {
                    var pos = translations[unitEntity].Value;
                    sum += new float2(pos.x, pos.y);
                    unitCount++;
                }
            }
            while (fms._groupToUnitsMap.TryGetNextValue(out unitEntity, ref iterator));
        }

        return unitCount > 0 ? sum / unitCount : float2.zero;
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
}
