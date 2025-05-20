using Unity.Entities;

public struct CollisionEvent2D : IBufferElementData
{
    public Entity OtherEntity;
}


public struct CollidableTag : IComponentData { }