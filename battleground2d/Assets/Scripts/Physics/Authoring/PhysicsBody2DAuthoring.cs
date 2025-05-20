using Unity.Entities;
using UnityEngine;

public class PhysicsBody2DAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Vector2 initialVelocity;
    public float mass = 1f;
    public bool isStatic = false;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PhysicsBody2D
        {
            Velocity = initialVelocity,
            Mass = mass,
            IsStatic = isStatic
        });
    }
}
