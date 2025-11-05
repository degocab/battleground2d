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
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (spriteSheetAnimationData, transform) in SystemAPI.Query<RefRW<AnimationComponent>, RefRO<LocalTransform>>())
        {

            if (spriteSheetAnimationData.ValueRO.isFrozen)
            {
                // Do nothing or handle frozen state (keep the last frame as it is)
                continue;
            }
            spriteSheetAnimationData.ValueRW.FrameTimer += deltaTime;

            if (spriteSheetAnimationData.ValueRO.FrameCount > 0)
            {
                //float frameTimerMax = entitySpawner.frameTimerMaxDebug;
                float frameTimerMax = spriteSheetAnimationData.ValueRO.FrameTimerMax;
                while (spriteSheetAnimationData.ValueRO.FrameTimer >= frameTimerMax)
                {
                    spriteSheetAnimationData.ValueRW.FrameTimer -= frameTimerMax;
                    spriteSheetAnimationData.ValueRW.CurrentFrame = (spriteSheetAnimationData.ValueRO.CurrentFrame + 1) % spriteSheetAnimationData.ValueRO.FrameCount;

                    //float uvWidth = 1f / spriteSheetAnimationData.frameCount;
                    //float uvHeight = 1f;
                    var cellHeight = 1f / 24f;// => 24 is grid count of pixel art frames
                    float uvWidth = cellHeight;// divide by num of sprites horizontally
                    float uvHeight = cellHeight;// divide by num of sprites vertically
                    float uvOffsetX = uvWidth * (spriteSheetAnimationData.ValueRO.CurrentFrame  +  (((spriteSheetAnimationData.ValueRO.animationWidthOffset -1 ))* spriteSheetAnimationData.ValueRO.FrameCount));
                    float uvOffsetY = uvHeight * (spriteSheetAnimationData.ValueRO.animationHeightOffset + (spriteSheetAnimationData.ValueRO.UnitType == EntitySpawner.UnitType.Enemy ?  16 : 0)) ;
                    spriteSheetAnimationData.ValueRW.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

                    //float3 position = translation.Value;
                    //position.z = position.y * .01f;
                    //spriteSheetAnimationData.matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
                }
            }
            else
            {
                // Handle invalid frame count, maybe log a warning
                Debug.LogWarning("Invalid frame count detected for animation.");
            }


        }
    }
}
