using Unity.Entities;

[InternalBufferCapacity(32)]
public struct CollisionEvent2D : IBufferElementData
{
    public Entity OtherEntity;
}


public struct CollidableTag : IComponentData { }