using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class HasTargetDebug : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) =>
        {
            Translation targetTranslation = EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);
            Debug.DrawLine(translation.Value, targetTranslation.Value, Color.red);
            // https://youtu.be/t11uB7Gl6m8?t=823
        });
    }
}
