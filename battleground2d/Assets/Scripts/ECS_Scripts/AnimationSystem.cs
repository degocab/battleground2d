using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(MovementSystem))]
[BurstCompile]
public class AnimationSystem : SystemBase
{
    //public static EntitySpawner entitySpawner;

    //protected override void OnStartRunning()
    //{
    //    entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
    //}
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities.ForEach((ref AnimationComponent spriteSheetAnimationData, ref Translation translation) =>
        {

            if (spriteSheetAnimationData.isFrozen)
            {
                // Do nothing or handle frozen state (keep the last frame as it is)
                return;
            }
            spriteSheetAnimationData.frameTimer += deltaTime;

            if (spriteSheetAnimationData.frameCount > 0)
            {
                //float frameTimerMax = entitySpawner.frameTimerMaxDebug;
                float frameTimerMax = spriteSheetAnimationData.frameTimerMax;
                while (spriteSheetAnimationData.frameTimer >= frameTimerMax)
                {
                    spriteSheetAnimationData.frameTimer -= frameTimerMax;
                    spriteSheetAnimationData.currentFrame = (spriteSheetAnimationData.currentFrame + 1) % spriteSheetAnimationData.frameCount;

                    //float uvWidth = 1f / spriteSheetAnimationData.frameCount;
                    //float uvHeight = 1f;
                    float uvWidth = 1f / 12f;// divide by num of sprites horizontally
                    float uvHeight = 1f / 12f;// divide by num of sprites vertically
                    float uvOffsetX = uvWidth * spriteSheetAnimationData.currentFrame;
                    float uvOffsetY = uvHeight * 6;
                    spriteSheetAnimationData.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

                    float3 position = translation.Value;
                    position.z = position.y * .01f;
                    spriteSheetAnimationData.matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
                }
            }
            else
            {
                // Handle invalid frame count, maybe log a warning
                Debug.LogWarning("Invalid frame count detected for animation.");
            }


        }).ScheduleParallel();
    }
}
