using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(FindTargetSystem))]
[UpdateAfter(typeof(GridSystem))]
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

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                Translation translation = translations[i];
                AnimationComponent animationData = animations[i];
                MovementSpeedComponent movementSpeed = movementSpeeds[i];
                float2 entityPos = translation.Value.xy;
                var command = commandDataArray[i];
                var combatState = combatStateArray[i];

                
                switch (command.Command)
                {
                    case CommandType.Idle:
                        break;
                    case CommandType.March:

                        float2 direction = float2.zero;
                        switch (animationData.Direction)
                        {
                            case EntitySpawner.Direction.Up:
                                direction = new float2(0, 1);
                                break;
                            case EntitySpawner.Direction.Down:
                                direction = new float2(0, -1);
                                break;
                            case EntitySpawner.Direction.Left:
                                direction = new float2(-1, 0); // Left direction
                                break;
                            case EntitySpawner.Direction.Right:
                            default:
                                direction = new float2(1, 0);
                                break;
                        }

                        // Set a very far target position for endless movement
                        float endlessDistance = 1000f; // Large distance for "endless" movement
                        float2 targetPos = entityPos + (direction * endlessDistance);

                        ECB.AddComponent(chunkIndex, entity, new HasTarget
                        {
                            Type = HasTarget.TargetType.Position,
                            TargetPosition = targetPos, // Convert float2 to float3
                            TargetEntity = Entity.Null
                        });
                        movementSpeed.isRunnning = false;
                        break;
                    case CommandType.Charge:
                        float2 direction2 = float2.zero;
                        switch (animationData.Direction)
                        {
                            case EntitySpawner.Direction.Up:
                                direction = new float2(0, 1);
                                break;
                            case EntitySpawner.Direction.Down:
                                direction = new float2(0, -1);
                                break;
                            case EntitySpawner.Direction.Left:
                                direction = new float2(-1, 0); // Left direction
                                break;
                            case EntitySpawner.Direction.Right:
                            default:
                                direction = new float2(1, 0);
                                break;
                        }

                        // Set a very far target position for endless movement
                        float endlessDistance2 = 1000f; // Large distance for "endless" movement
                        float2 targetPos2 = entityPos + (direction * endlessDistance2);

                        ECB.AddComponent(chunkIndex, entity, new HasTarget
                        {
                            Type = HasTarget.TargetType.Position,
                            TargetPosition = targetPos2, // Convert float2 to float3
                            TargetEntity = Entity.Null
                        });
                        movementSpeed.isRunnning = true;
                        break;
                    case CommandType.FindTarget:
                        command.TargetEntity = Entity.Null;
                        command.TargetPosition = float2.zero;
                        ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
                        command.Command = CommandType.Idle;
                        commandDataArray[i] = command;
                        break;
                    case CommandType.MoveTo:
                        // Check if we are moving to an entity's location or a specific point in the world
                        //if (command.TargetEntity != Entity.Null)
                        //{
                        //    // We are targeting another entity (chase/follow)
                        //    ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
                        //}
                        //else 
                        if (math.lengthsq(command.TargetPosition) > .25) // Check if a valid position is set
                        {
                            // We are moving to a specific location. Add the HasTarget component directly.
                            ECB.AddComponent(chunkIndex, entity, new HasTarget
                            {
                                Type = HasTarget.TargetType.Position,
                                TargetPosition = new float2(command.TargetPosition), // Convert float2 to float3
                                TargetEntity = Entity.Null
                            });
                        }
                        else
                        {
                            // We are moving to a specific location. Add the HasTarget component directly.
                            ECB.AddComponent(chunkIndex, entity, new HasTarget
                            {
                                Type = HasTarget.TargetType.Position,
                                TargetPosition = new float2(entityPos + command.TargetPosition), // Convert float2 to float3
                                TargetEntity = Entity.Null
                            });
                        }
                        //Clear the command so it doesn't keep re-triggering
                        command.Command = CommandType.Idle;
                        commandDataArray[i] = command;
                        break;
                    case CommandType.Attack:
                        if (command.TargetEntity == Entity.Null && math.lengthsq(command.TargetPosition) > 0)
                        {
                            // This is a move-then-attack command
                            // First, move to the position

                            ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);

                            // Then add a tag to find targets after arriving
                            ECB.AddComponent<AttackCommandTag>(chunkIndex, entity);
                            command.Command = CommandType.Idle;
                            commandDataArray[i] = command;
                        }
                        else if (command.TargetEntity != Entity.Null)
                        {
                            ECB.AddComponent(chunkIndex, entity, new HasTarget
                            {
                                Type = HasTarget.TargetType.Entity,
                                TargetEntity = command.TargetEntity, // ← Specific entity
                                TargetPosition = float2.zero
                            });
                            // This is a direct attack command on a specific entity
                            ECB.AddComponent<AttackCommandTag>(chunkIndex, entity);
                            command.Command = CommandType.Idle;
                            commandDataArray[i] = command;
                        }


                        break;
                    case CommandType.Defend:
                        break;
                    default:
                        break;
                }
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
    March, //march forward endlesslly, take 
    Charge, //charge in facing direction until 
    Attack,
    Defend
}

public struct FindTargetCommandTag : IComponentData { }
public struct CommandData : IComponentData
{
    public CommandType Command;
    public float2 TargetPosition; // Optional (used for MoveTo, etc.)
    public Entity TargetEntity;   // Optional (used for Attack, etc.)
}

// Add to existing components
public struct AttackCommandTag : IComponentData { }

public struct IsAttacking : IComponentData { }

//public struct HealthComponent : IComponentData
//{
//    public float CurrentHealth;
//    public float MaxHealth;
//}

public struct DamageComponent : IComponentData
{
    public float Value;
    public Entity SourceEntity;
}

public struct DeathTag : IComponentData { }
