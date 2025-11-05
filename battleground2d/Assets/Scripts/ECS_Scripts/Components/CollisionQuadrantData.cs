using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct CollisionQuadrantData
{
    public Entity entity;
    public float2 position;
    public float radius;

    public EntitySpawner.UnitType unitType;

    // other date
    public LocalTransform CollisionSourceTransform;
    public ECS_CircleCollider2DAuthoring CollisionSourceCollider;
    public ECS_PhysicsBody2DAuthoring CollisionSourceBody;
}




public struct CollisionPair
{
    public Entity A;
    public Entity B;
    public float2 PosA;
    public float2 PosB;
    public float RadiusA;
    public float RadiusB;
    public ECS_PhysicsBody2DAuthoring BodyA;
    public ECS_PhysicsBody2DAuthoring BodyB;
    public ECS_CircleCollider2DAuthoring ColliderA;
    public ECS_CircleCollider2DAuthoring ColliderB;
}
