using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(AnimationSystem))]
//public class AnimationRenderingSystem : SystemBase
//{
//    protected override void OnUpdate()
//    {
//        // Create a MaterialPropertyBlock to store material data
//        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

//        // Iterate over entities that have both AnimationComponent and Renderer
//        Entities.ForEach((ref AnimationComponent animation, in Renderer renderer) =>
//        {
//            // Update MaterialPropertyBlock based on the animation data
//            materialPropertyBlock.SetFloat("_CurrentFrame", animation.currentFrame);

//            // Optionally, you can add other properties like animation index, speed, etc.
//            materialPropertyBlock.SetFloat("_TotalFrames", animation.totalFrames);

//            // Set the updated MaterialPropertyBlock to the renderer
//            renderer.SetPropertyBlock(materialPropertyBlock);

//        }).WithoutBurst().Run(); // We need to run on the main thread
//    }
//}
