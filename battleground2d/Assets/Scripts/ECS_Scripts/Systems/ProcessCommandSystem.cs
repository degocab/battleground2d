using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(FindTargetSystem))]
[UpdateAfter(typeof(GridSystem))]
public class ProcessCommandSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        //_query = GetEntityQuery(new EntityQueryDesc
        //{
        //    All = new ComponentType[] {
        //          ComponentType.ReadOnly<Unit>(),
        //                                ComponentType.ReadOnly<CommandData>(),
        //    },

        //    None = new ComponentType[] { typeof(CommanderComponent) }
        //});
        _query = GetEntityQuery(
        ComponentType.ReadOnly<Unit>(),
        ComponentType.ReadOnly<CommandData>(),
        ComponentType.Exclude<CommanderComponent>());



        base.OnCreate();
    }

    [BurstCompile]
    private struct AssignFindTargetCommandJob : IJobChunk
    {
        //public float Time;

        public ComponentTypeHandle<CommandData> CommandDataTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var commandDataArray = chunk.GetNativeArray(CommandDataTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            //bool shouldAssign = (Time % 5f < 0.1f); // simulate assignment every 5 seconds

            //if (!shouldAssign)
            //    return;         

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = entities[i];
                var command = commandDataArray[i];

                //todo: clean commandtags.....
                //if (command.Command != CommandType.FindTarget)
                //{
                //    command.Command = CommandType.FindTarget;
                //    command.TargetEntity = Entity.Null;
                //    command.TargetPosition = float3.zero;

                //    commandDataArray[i] = command;

                //    ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
                //}
                switch (command.Command)
                {
                    case CommandType.Idle:
                        break;
                    case CommandType.FindTarget:
                        command.TargetEntity = Entity.Null;
                        command.TargetPosition = float2.zero;
                        ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
                        break;
                    case CommandType.MoveTo:
                        // Check if we are moving to an entity's location or a specific point in the world
                        if (command.TargetEntity != Entity.Null)
                        {
                            // We are targeting another entity (chase/follow)
                            ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
                        }
                        else if (math.lengthsq(command.TargetPosition) > 0) // Check if a valid position is set
                        {
                            // We are moving to a specific location. Add the HasTarget component directly.
                            ECB.AddComponent(chunkIndex, entity, new HasTarget
                            {
                                Type = HasTarget.TargetType.Position,
                                TargetPosition = new float2(command.TargetPosition), // Convert float2 to float3
                                TargetEntity = Entity.Null
                            });
                        }
                        //Clear the command so it doesn't keep re-triggering
                        command.Command = CommandType.Idle;
                        commandDataArray[i] = command;
                        break;
                    case CommandType.Attack:
                        command.TargetEntity = Entity.Null;
                        command.TargetPosition = float2.zero;
                        ECB.AddComponent<FindTargetCommandTag>(chunkIndex, entity);
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
        var job = new AssignFindTargetCommandJob
        {
            //Time = UnityEngine.Time.deltaTime,
            CommandDataTypeHandle = GetComponentTypeHandle<CommandData>(false),
            EntityTypeHandle = GetEntityTypeHandle(),
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
