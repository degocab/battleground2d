using Unity.Entities;
using Unity.Mathematics;

public static class CommandFactory
{
    public static CommandData CreateIdleCommand()
    {
        return new CommandData
        {
            Command = CommandType.Idle,
            TargetPosition = float2.zero,
            TargetEntity = Entity.Null
        };
    }
    public static CommandData CreateMoveCommand(float3 startPosition, float moveDistance = 1.0f, float randomVariation = 0.1f)
    {
        return new CommandData
        {
            Command = CommandType.MoveTo,
            TargetPosition = new float2(
                startPosition.x - moveDistance,
                startPosition.y + UnityEngine.Random.Range(-randomVariation, randomVariation)
            )
        };
    }

    public static CommandData CreateAttackCommand(Entity targetEntity)
    {
        return new CommandData
        {
            Command = CommandType.Attack,
            TargetEntity = targetEntity,
            TargetPosition = float2.zero
        };
    }

    public static CommandData CreateAttackCommand(float2 targetPosition)
    {
        return new CommandData
        {
            Command = CommandType.Attack,
            TargetEntity = Entity.Null,
            TargetPosition = targetPosition
        };
    }

    public static CommandData CreateFindTargetCommand()
    {
        return new CommandData
        {
            Command = CommandType.FindTarget,
            TargetEntity = Entity.Null,
            TargetPosition = float2.zero
        };
    }

    public static CommandData CreateDefendCommand(float3 defendPosition, float defendRadius = 2.0f)
    {
        return new CommandData
        {
            Command = CommandType.Defend,
            TargetPosition = new float2(defendPosition.x, defendPosition.y),
            // You could add defend radius to your CommandData if needed
        };
    }

    // Generic method for any command type
    public static CommandData CreateCommand(CommandType commandType, Entity targetEntity = default, float2 targetPosition = default)
    {
        return new CommandData
        {
            Command = commandType,
            TargetEntity = targetEntity,
            TargetPosition = targetPosition
        };
    }
}