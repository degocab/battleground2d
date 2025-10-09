using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TargetValidationSystem))]
[UpdateAfter(typeof(PlayerControlSystem))]
public class ProcessCommandSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    //private EntityQuery _query;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    [BurstCompile]
    private struct AssignCommandJob : IJobChunk
    {
        //public float Time;

        public ComponentTypeHandle<CommandData> CommandDataTypeHandle;
        public ComponentTypeHandle<CombatState> CombatStateTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DefenseComponent> DefenseComponentTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AttackComponent> AttackComponentTypeHandle;

        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;

        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;
        public ComponentTypeHandle<MovementSpeedComponent> MovementSpeedTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {

            var commandDataArray = chunk.GetNativeArray(CommandDataTypeHandle);
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
                var combatState = combatStateArray[i];
                var defenseComponent = defenseComponents[i];
                var attackComponent = attackComponents[i];

                if (command.Command != command.previousCommand)
                    command.TargetEntity = Entity.Null;

                ProcessCommand(ref command, ref combatState, ref movementSpeed, attackComponent, defenseComponent, entity, entityPos,
             animationData.Direction, chunkIndex, ECB);


                command.previousCommand = command.Command;
            }
        }

        private void ProcessCommand(ref CommandData command, ref CombatState combatState,
                                     ref MovementSpeedComponent movementSpeed, AttackComponent attackComponent, DefenseComponent defenseComponent, Entity entity,
                                     float2 entityPos, EntitySpawner.Direction direction,
                                     int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {

            //maybe dont do anything if attacking/defending/blocking///process after?
            //if (attackComponent.isDefending || defenseComponent.IsBlocking) return;

            switch (command.Command)
            {
                case CommandType.Idle:
                    break;

                case CommandType.March:
                case CommandType.Charge:
                    HandleMovementCommand(command.Command, ref combatState, ref movementSpeed,
                                        entity, entityPos, direction, chunkIndex, ecb);
                    break;

                case CommandType.FindTarget:
                    HandleFindTargetCommand(ref command, ref combatState, entity, chunkIndex, ecb);
                    break;

                case CommandType.MoveTo:
                    HandleMoveToCommand(ref command, entity, entityPos, chunkIndex, ecb);
                    break;

                case CommandType.Attack:
                    HandleAttackCommand(ref command, ref combatState, entity, chunkIndex, ecb);
                    break;

                case CommandType.Defend:
                    // TODO: Implement defend logic
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
                                       int chunkIndex, EntityCommandBuffer.ParallelWriter ecb)
        {
            float2 targetPos = math.lengthsq(command.TargetPosition) > 0.4f ?
                command.TargetPosition : entityPos + command.TargetPosition;

            ecb.AddComponent(chunkIndex, entity, new HasTarget
            {
                Type = HasTarget.TargetType.Position,
                TargetPosition = targetPos,
                TargetEntity = Entity.Null
            });

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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

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

        var job = new AssignCommandJob
        {
            //Time = UnityEngine.Time.deltaTime,
            CommandDataTypeHandle = GetComponentTypeHandle<CommandData>(false),
            CombatStateTypeHandle = GetComponentTypeHandle<CombatState>(false),
            EntityTypeHandle = GetEntityTypeHandle(),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            DefenseComponentTypeHandle = GetComponentTypeHandle<DefenseComponent>(true),
            AttackComponentTypeHandle = GetComponentTypeHandle<AttackComponent>(true),
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            MovementSpeedTypeHandle = GetComponentTypeHandle<MovementSpeedComponent>(false),
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter()
        };

        var handle = job.ScheduleParallel(_query, inputDeps);
        _ecbSystem.AddJobHandleForProducer(handle);
        return handle;
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
