using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(MovementSystem))]
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



[UpdateBefore(typeof(AnimationSystem))]
//[BurstCompile]
public class MovementSystem : SystemBase
{
    public static EntitySpawner entitySpawner;

    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;


        Entities.ForEach((ref AnimationComponent animationComponent, ref MovementSpeedComponent movementSpeedComponent, in Entity entity) =>
        {
            if (moveX == 0f && moveY == 0f) //not moving
            {
                animationComponent.animationType = EntitySpawner.AnimationType.Idle;
                movementSpeedComponent.randomSpeed = 0f;
            }
            else
            {
                animationComponent.animationType = EntitySpawner.AnimationType.Run;
            }

            if (animationComponent.prevAnimationType != animationComponent.animationType)
            {
                if (animationComponent.animationType == EntitySpawner.AnimationType.Idle)
                {
                    animationComponent.frameCount = 2;
                    animationComponent.currentFrame = 0;
                    animationComponent.frameTimerMax = .1f;
                }
                else //(animationComponent.animationType == EntitySpawner.AnimationType.Run)
                {

                    Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);

                    // Generate a random float between 1f and 1.25f
                    animationComponent.currentFrame = random.NextInt(0,6);
                    //animationComponent.currentFrame = 0;
                    animationComponent.frameCount = 6;
                    animationComponent.frameTimerMax = entitySpawner != null ? entitySpawner.speedVar : .95f;
                }
                animationComponent.prevAnimationType = animationComponent.animationType;
            }

        }).ScheduleParallel();

        // Movement speed
        float minRange = 2f;
        float maxRange = 2.5f;
        Entities.ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity, in Entity entity) =>
        {
            if (velocity.randomSpeed == 0f)
            {
                // Create a random generator, seeded by the entity's index
                Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);

                // Generate a random float between 1f and 1.25f
                velocity.randomSpeed = random.NextFloat(minRange, maxRange);

            }

            //velocity.randomSpeed

            float3 vel = (new float3(moveX, moveY, 0) *
                                    (entitySpawner != null ? entitySpawner.mainSpeedVar : 2f)
                                    );
            vel.z = 0;
            velocity.value = vel;
        }).ScheduleParallel();



        //set animation
        Entities.ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity) =>
        {
            // Update position based on velocity (movement calculations)
            position.value += velocity.value * deltaTime;

            // Apply the new position to the entity's translation (moving in the scene)
            translation.Value = position.value;

        }).ScheduleParallel();

        //get direction
        Entities.ForEach((ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
        {
            //Debug.Log("x: " + translation.Value.x);
            //Debug.Log("y: " + translation.Value.y);
            if (velocity.value.x > 0)
            {
                animationComponent.direction = EntitySpawner.Direction.Right;
            }
            else if (velocity.value.x < 0)
            {
                animationComponent.direction = EntitySpawner.Direction.Left;
            }
            else if (velocity.value.y > 0)
            {
                animationComponent.direction = EntitySpawner.Direction.Up;
            }
            else if (velocity.value.y < 0)
            {
                animationComponent.direction = EntitySpawner.Direction.Down;
            }
            else
            {
                // In case of the entity being at the origin (0, 0)
                animationComponent.direction = animationComponent.prevDirection; // Or some default direction
            }

            animationComponent.prevDirection = animationComponent.direction;

        }).ScheduleParallel();


    }
}