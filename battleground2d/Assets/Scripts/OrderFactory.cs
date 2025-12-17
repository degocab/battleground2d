using Unity.Entities;
using Unity.Mathematics;

public static class OrderFactory
{
    public static OrderData CreateIdleCommand()
    {
        return new OrderData
        {
            CurrentOrder = OrderType.Idle,
            TargetPosition = float2.zero,
            TargetEntity = Entity.Null
        };
    }
    public static OrderData CreateMoveOrder( float2? targetPosition = null)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.MoveTo,
            TargetPosition = new float2(
                targetPosition.Value.x,
                targetPosition.Value.y
            ),

        };
    }

    public static OrderData CreateAttackCommand(Entity targetEntity)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.Attack,
            TargetEntity = targetEntity,
            TargetPosition = float2.zero
        };
    }

    public static OrderData CreateAttackCommand(float2 targetPosition)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.Attack,
            TargetEntity = Entity.Null,
            TargetPosition = targetPosition
        };
    }

    public static OrderData CreateFindTargetOrder()
    {
        return new OrderData
        {
            CurrentOrder = OrderType.FindTarget,
            TargetEntity = Entity.Null,
            TargetPosition = float2.zero
        };
    }

    public static OrderData CreateDefendOrder(float3 defendPosition, float defendRadius = 2.0f)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.Defend,
            TargetPosition = new float2(defendPosition.x, defendPosition.y),
            // You could add defend radius to your CommandData if needed
        };
    }

    public static OrderData CreateMarchOrder(/*float3 defendPosition*/)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.March,
            // You could add defend radius to your CommandData if needed
        };
    }
    

    public static OrderData CreateChargeOrder(/*float3 defendPosition*/)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.Charge,
            // You could add defend radius to your CommandData if needed
        };
    }

    // Generic method for any command type
    public static OrderData CreateOrder(OrderType orderType, Entity targetEntity = default, float2 targetPosition = default)
    {
        return new OrderData
        {
            CurrentOrder = orderType,
            TargetEntity = targetEntity,
            TargetPosition = targetPosition
        };
    }
    // Generic method for any command type
    public static OrderData CreateMoveDirectionalRangeOrder(OrderType orderType, float range, EntitySpawner.Direction direction)
    {
        return new OrderData
        {
            CurrentOrder = orderType,
            MoveRange = range,
            FormationDirectionToMove = direction,
            InitialOrder = true
        };
    }
}