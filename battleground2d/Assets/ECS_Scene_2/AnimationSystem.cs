using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(MovementSpeedSystem))]  // AnimationSystem runs after MovementSystem
//[UpdateBefore(typeof(CombatSystem))]
public class AnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities.ForEach((ref AnimationComponent spriteSheetAnimationData, ref Translation translation) =>
        {
            spriteSheetAnimationData.frameTimer += deltaTime;
            while (spriteSheetAnimationData.frameTimer >= spriteSheetAnimationData.frameTimerMax)
            {
                spriteSheetAnimationData.frameTimer -= spriteSheetAnimationData.frameTimerMax;
                spriteSheetAnimationData.currentFrame = (spriteSheetAnimationData.currentFrame + 1) % spriteSheetAnimationData.frameCount;

                float uvWidth = 1f / spriteSheetAnimationData.frameCount;
                float uvHeight = 1f;
                float uvOffsetX = uvWidth * spriteSheetAnimationData.currentFrame;
                float uvOffsetY = 0f;
                spriteSheetAnimationData.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

                float3 position = translation.Value;
                position.z = position.y * .01f;
                spriteSheetAnimationData.matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            }
        }).ScheduleParallel();
    }
}

[UpdateAfter(typeof(AnimationSystem))]
public class RenderSystem : SystemBase
{

    public static EntitySpawner entitySpawner;

    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
        //Debug.Log(entitySpawner);
    }
    protected override void OnUpdate()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        Vector4[] uv = new Vector4[1];
        Camera camera = Camera.main;
        Mesh quadMesh = entitySpawner.quadMesh;
        Material[] materials = entitySpawner.defaultRunDownMaterials;
        Entities.ForEach((ref Translation translation, ref AnimationComponent spriteSheetAnimationData) => {

            uv[0] = spriteSheetAnimationData.uv;
            materialPropertyBlock.SetVectorArray("_MainTex_UV", uv);

            Graphics.DrawMesh(
                quadMesh,
                spriteSheetAnimationData.matrix,
                GetMaterials(EntitySpawner.UnitType.Enemy, EntitySpawner.Direction.Down, EntitySpawner.AnimationType.Attack)[spriteSheetAnimationData.currentFrame],
                0, // Layer
                camera,
                0, // Submesh index
                materialPropertyBlock
            );
        }).WithoutBurst().Run();
    }

    public Material[] GetMaterials(EntitySpawner.UnitType unitType, EntitySpawner.Direction direction, EntitySpawner.AnimationType animationType)
    {
        var key = (unitType, direction, animationType);  // Tuple as key
        if (entitySpawner.materialDictionary.TryGetValue(key, out Material[] materials))
        {
            return materials; // Return the materials if found
        }
        else
        {
            Debug.LogError("Materials not found for " + key);
            return null; // Return null or handle the missing case
        }
    }
}

