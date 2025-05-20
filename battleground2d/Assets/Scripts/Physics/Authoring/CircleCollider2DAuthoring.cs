using Unity.Entities;
using UnityEngine;

public class CircleCollider2DAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float radius = 0.125f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CircleCollider2D
        {
            Radius = radius
        });
    }
}
