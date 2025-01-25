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

[UpdateAfter(typeof(CombatSystem))]
[BurstCompile]
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
        bool isRunnning = false;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) isRunnning = true;

        //update movement x and y
        Entities
        .WithAll<CommanderComponent>()// remove to let all units update
        .ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
        {
            movementSpeedComponent.moveX = moveX;
            movementSpeedComponent.moveY = moveY;
            movementSpeedComponent.isRunnning = isRunnning;
        }).ScheduleParallel();


        //move all units?
        Entities
        .ForEach((ref MovementSpeedComponent movementSpeedComponent, ref PositionComponent position, ref Translation translation, ref TargetPositionComponent targetLocationComponent) =>
        {
            float3 targetPosition = targetLocationComponent.targetPosition;
            float3 direction = targetPosition - translation.Value;
            direction.z = 0;
            if (math.length(direction) < 0.1f)
            {
                float newX = translation.Value.x;
                newX -= 50f;
                targetPosition = new float3(newX, translation.Value.y, 0f);
                targetLocationComponent.targetPosition = targetPosition;
                direction = targetPosition - translation.Value;
                //direction = float3.zero;
            }
            else
            {
                direction = math.normalize(direction);
                movementSpeedComponent.direction = direction;
            }


            //movementSpeedComponent.normalizedDirection = math.normalize(direction);
            movementSpeedComponent.moveX = direction.x;
            movementSpeedComponent.moveY = direction.y;
            //movementSpeedComponent.isRunnning = isRunnning;
        }).WithBurst().ScheduleParallel();


        // Movement speed randomizer
        float minRange = 1f;
        float maxRange = 1.25f;
        Entities
            .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity, in Entity entity) =>
        {
            //if (velocity.randomSpeed == 0f)
            //{
            // Create a random generator, seeded by the entity's index
            if (velocity.isRunnning)
            {
                // Generate a random float between 1f and 1.25f
                Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);
                velocity.randomSpeed = random.NextFloat(minRange, maxRange);
            }
            else
            {
                Unity.Mathematics.Random random2 = new Unity.Mathematics.Random((uint)entity.Index);
                velocity.randomSpeed = random2.NextFloat(.5f, .6f);
                //velocity.randomSpeed = entitySpawner.movementSpeedDebug;
            }

            //}

            float3 vel = (new float3(velocity.moveX, velocity.moveY, 0) * velocity.randomSpeed);
            vel.z = 0;
            velocity.value = vel;
        }).WithBurst().ScheduleParallel();

        //get direction
        Entities
            .ForEach((ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
        {
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
                if (!animationComponent.finishAnimation)
                {
                    animationComponent.direction = animationComponent.prevDirection; // Or some default direction 
                }
            }
            animationComponent.prevDirection = animationComponent.direction;

        }).WithBurst().ScheduleParallel();

        //actual movement system
        Entities
        .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
        {
            //stop moving on attack
            if (animationComponent.animationType == EntitySpawner.AnimationType.Run || animationComponent.animationType == EntitySpawner.AnimationType.Walk)
            {
                // Update position based on velocity (movement calculations)
                position.value += velocity.value * deltaTime;

                // Apply the new position to the entity's translation (moving in the scene)
                translation.Value = position.value;
            }
        }).WithBurst().ScheduleParallel();
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