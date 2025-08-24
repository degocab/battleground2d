using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateBefore(typeof(UnitMoveToTargetSystem))]
//public class HasTargetDebug : ComponentSystem
//{
//    protected override void OnUpdate()
//    {
//        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) =>
//        {
//            if (entity != Entity.Null)
//            {
//                float2 targetTranslation = hasTarget.TargetPosition;// EntityManager.GetComponentData<Translation>(hasTarget.TargetEntity);
//                Debug.DrawLine(translation.Value, new float3(targetTranslation,0), Color.red);
//                // https://youtu.be/t11uB7Gl6m8?t=823
//            }

//        });
//    }
//}
