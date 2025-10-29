using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[InternalBufferCapacity(32)]
public struct CollisionEvent2D : IBufferElementData
{
    public Entity OtherEntity;
    public float2 OtherPosition;
    public float OtherRadius;
    public bool OtherIsStatic;

    // other date
    public Translation OtherTranslation;
    public ECS_CircleCollider2DAuthoring OtherCollider;
    public ECS_PhysicsBody2DAuthoring OtherBody;
}


public struct CollidableTag : IComponentData { }