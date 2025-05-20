using Unity.Entities;
using UnityEngine;

public class SetInitialVelocity : MonoBehaviour, IConvertGameObjectToEntity
{
    public Vector2 initialVelocity = new Vector2(1f, 0f);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Velocity2D { Value = initialVelocity, PrevValue = initialVelocity });
    }
}
