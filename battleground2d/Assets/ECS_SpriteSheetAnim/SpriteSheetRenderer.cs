///* 
//    ------------------- Code Monkey -------------------

//    Thank you for downloading this package
//    I hope you find it useful in your projects
//    If you have any questions let me know
//    Cheers!

//               unitycodemonkey.com
//    --------------------------------------------------
// */

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Transforms;

//[UpdateAfter(typeof(SpriteSheetAnimation_Animate))]
//public class SpriteSheetRenderer : ComponentSystem {

//    protected override void OnUpdate() {
//        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
//        Vector4[] uv = new Vector4[1];
//        Camera camera = Camera.main;
//        Mesh quadMesh = GameHandler.GetInstance().quadMesh;
//        Material material = GameHandler.GetInstance().walkingSpriteSheetMaterial;
//        Entities.ForEach((ref Translation translation, ref SpriteSheetAnimation_Data spriteSheetAnimationData) => {

//            uv[0] = spriteSheetAnimationData.uv;
//            materialPropertyBlock.SetVectorArray("_MainTex_UV", uv);

//            Graphics.DrawMesh(
//                quadMesh, 
//                spriteSheetAnimationData.matrix,
//                material, 
//                0, // Layer
//                camera,
//                0, // Submesh index
//                materialPropertyBlock
//            );
//        });
//    }

//}
