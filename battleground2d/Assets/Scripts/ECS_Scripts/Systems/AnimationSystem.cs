using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(MovementSystem))]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(CollisionResolutionSystem))]
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
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var deltaTime = Time.DeltaTime;

        Entities.ForEach((ref AnimationComponent spriteSheetAnimationData, ref Translation translation) =>
        {

            if (spriteSheetAnimationData.isFrozen)
            {
                // Do nothing or handle frozen state (keep the last frame as it is)
                return;
            }
            spriteSheetAnimationData.FrameTimer += deltaTime;

            if (spriteSheetAnimationData.FrameCount > 0)
            {
                //float frameTimerMax = entitySpawner.frameTimerMaxDebug;
                float frameTimerMax = spriteSheetAnimationData.FrameTimerMax;
                while (spriteSheetAnimationData.FrameTimer >= frameTimerMax)
                {
                    spriteSheetAnimationData.FrameTimer -= frameTimerMax;
                    spriteSheetAnimationData.CurrentFrame = (spriteSheetAnimationData.CurrentFrame + 1) % spriteSheetAnimationData.FrameCount;

                    //float uvWidth = 1f / spriteSheetAnimationData.frameCount;
                    //float uvHeight = 1f;
                    var cellHeight = 1f / 24f;// => 24 is grid count of pixel art frames
                    float uvWidth = cellHeight;// divide by num of sprites horizontally
                    float uvHeight = cellHeight;// divide by num of sprites vertically
                    float uvOffsetX = uvWidth * (spriteSheetAnimationData.CurrentFrame  +  (((spriteSheetAnimationData.animationWidthOffset -1 ))* spriteSheetAnimationData.FrameCount));
                    float uvOffsetY = uvHeight * (spriteSheetAnimationData.animationHeightOffset + (spriteSheetAnimationData.UnitType == EntitySpawner.UnitType.Enemy ?  16 : 0)) ;
                    spriteSheetAnimationData.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

                    float3 position = translation.Value;
                    position.z = position.y * .01f;
                    //spriteSheetAnimationData.matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
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
