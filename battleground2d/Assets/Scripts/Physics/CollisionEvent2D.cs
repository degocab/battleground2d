using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(32)]
public struct CollisionEvent2D : IBufferElementData
{
    public Entity OtherEntity;
    public float2 OtherPosition;
    public float OtherRadius;
    public bool OtherIsStatic;
}


public struct CollidableTag : IComponentData { }