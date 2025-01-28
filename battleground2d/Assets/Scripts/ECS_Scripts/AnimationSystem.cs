using Unity.Burst;
using Unity.Collections;
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

                    float uvWidth = 1f / spriteSheetAnimationData.frameCount;
                    float uvHeight = 1f;
                    float uvOffsetX = uvWidth * spriteSheetAnimationData.currentFrame;
                    float uvOffsetY = 0f;
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

[UpdateAfter(typeof(AnimationSystem))]
public class RenderSystem : SystemBase
{

    public static EntitySpawner entitySpawner;

    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
    }
    protected override void OnUpdate()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        Vector4[] uv = new Vector4[1];
        Camera camera = Camera.main;
        Mesh quadMesh = entitySpawner.quadMesh;
        Entities.ForEach((ref Translation translation, ref AnimationComponent spriteSheetAnimationData) =>
        {

            uv[0] = spriteSheetAnimationData.uv;
            materialPropertyBlock.SetVectorArray("_MainTex_UV", uv);

            Graphics.DrawMesh(
                quadMesh,
                spriteSheetAnimationData.matrix,
                GetMaterials(spriteSheetAnimationData.unitType, spriteSheetAnimationData.direction, spriteSheetAnimationData.animationType)[spriteSheetAnimationData.currentFrame],
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




[UpdateAfter(typeof(RenderSystem))]
public class DeathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Create an EntityCommandBuffer
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Iterate through entities that are marked as dead
        //update movement x and y
        Entities
            .WithAll<HealthComponent>()
            .ForEach((ref Entity entity, ref HealthComponent health, ref AnimationComponent animation) =>
            {
                if (animation.isFrozen)
                {
                    // Mark the entity as dead and freeze the animation

                    //// Optionally remove components, destroy entity, or do other cleanup
                    ecb.RemoveComponent<HealthComponent>(entity);  // Example: Remove HealthComponent
                    ecb.RemoveComponent<MovementSpeedComponent>(entity);  // Example: Remove MovementComponent
                    ecb.RemoveComponent<AttackComponent>(entity);  // Example: Remove CombatComponent
                    ecb.RemoveComponent<AttackCooldownComponent>(entity);  // Example: Remove CombatComponent
                    ecb.RemoveComponent<PositionComponent>(entity);  // Example: Remove CombatComponent
                    //ecb.DestroyEntity(entity);  // Optionally destroy the entity
                }
            }).WithoutBurst().Run();

        // Apply the command buffer at the end of the frame
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}