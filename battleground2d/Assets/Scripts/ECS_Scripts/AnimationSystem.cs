using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(CombatSystem))]
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

[UpdateBefore(typeof(AnimationSystem))]
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

        //Entities
        ////.WithAll<CommanderComponent>()// remove to let all units update
        //.ForEach((ref AnimationComponent animationComponent, ref MovementSpeedComponent movementSpeedComponent, ref AttackComponent attackComponent, in Entity entity) =>
        //{
        //    if (!attackComponent.isAttacking)
        //    {
        //        if (movementSpeedComponent.moveX == 0f && movementSpeedComponent.moveY == 0f) //not moving
        //        {
        //            animationComponent.animationType = EntitySpawner.AnimationType.Idle;
        //            EntitySpawner.UpdateAnimationFields(ref animationComponent);
        //            movementSpeedComponent.randomSpeed = 0f;
        //        }
        //        else
        //        {
        //            if (movementSpeedComponent.isRunnning)
        //            {
        //                animationComponent.animationType = EntitySpawner.AnimationType.Run;
        //            }
        //            else
        //            {
        //                animationComponent.animationType = EntitySpawner.AnimationType.Walk;
        //            }
        //        }
        //        if (animationComponent.prevAnimationType != animationComponent.animationType)
        //        {
        //            if (animationComponent.animationType == EntitySpawner.AnimationType.Idle)
        //            {
        //                EntitySpawner.UpdateAnimationFields(ref animationComponent);
        //            }
        //            else //(animationComponent.animationType == EntitySpawner.AnimationType.Run)
        //            {
        //                Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);
        //                EntitySpawner.UpdateAnimationFields(ref animationComponent, random);
        //            }
        //            animationComponent.prevAnimationType = animationComponent.animationType;
        //        }
        //    }

        //}).WithBurst().ScheduleParallel();

        // Movement speed randomizer
        float minRange = 1f;
        float maxRange = 1.25f;
        Entities
            .WithNone<CommanderComponent>()// remove to let all units update
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
            //.WithAll<CommanderComponent>()//remove to allow all units to move from wasd
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

[UpdateAfter(typeof(MovementSystem))]
public class CombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        bool attack = false;
        //if (Input.GetKeyDown(KeyCode.Space)) // Detect spacebar press only
        if (Input.GetMouseButtonDown(0)) // Detect spacebar press only
            attack = true;

        Entities.ForEach((ref Entity entity, ref Translation translation, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref AnimationComponent animationComponent) =>
        {
            if (attack)
            {
                if (!attackComponent.isAttacking) //dont reset until we are done
                {
                    attackComponent.isAttacking = true;
                    //animationComponent.animationType = EntitySpawner.AnimationType.Attack;
                    //EntitySpawner.UpdateAnimationFields(ref animationComponent);
                    attackCooldown.timeRemaining = attackCooldown.cooldownDuration; // Set the cooldown duration 
                }
            }
        }).ScheduleParallel();




        Entities
        .ForEach((ref AnimationComponent animationComponent, ref MovementSpeedComponent movementSpeedComponent, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, in Entity entity) =>
        {
            if (attackComponent.isAttacking)
            {
                if (attackCooldown.timeRemaining == attackCooldown.cooldownDuration) //on attack trigger?
                {
                    animationComponent.animationType = EntitySpawner.AnimationType.Attack;
                }
                if (attackCooldown.timeRemaining > 0f)
                {
                    attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
                }
                else
                {
                    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
                    attackComponent.isAttacking = false; // Reset finish flag after animation is done

                    //animationComponent.animationType = EntitySpawner.AnimationType.Idle;
                    //EntitySpawner.UpdateAnimationFields(ref animationComponent);
                    //Debug.Log(animationComponent.prevAnimationType);
                }
            }
            else
            {
                if (movementSpeedComponent.moveX == 0f && movementSpeedComponent.moveY == 0f) //not moving
                {
                    animationComponent.animationType = EntitySpawner.AnimationType.Idle;
                    //EntitySpawner.UpdateAnimationFields(ref animationComponent);
                    movementSpeedComponent.randomSpeed = 0f;
                }
                else
                {
                    if (movementSpeedComponent.isRunnning)
                    {
                        animationComponent.animationType = EntitySpawner.AnimationType.Run;
                    }
                    else
                    {
                        animationComponent.animationType = EntitySpawner.AnimationType.Walk;
                    }
                }

            }

            if (animationComponent.prevAnimationType != animationComponent.animationType)
            {
                //if (animationComponent.animationType == EntitySpawner.AnimationType.Idle)
                //{
                //    EntitySpawner.UpdateAnimationFields(ref animationComponent);
                //}
                //else //(animationComponent.animationType == EntitySpawner.AnimationType.Run)
                //{

                Unity.Mathematics.Random walkRandom = new Unity.Mathematics.Random((uint)entity.Index);
                Unity.Mathematics.Random runRandom = new Unity.Mathematics.Random((uint)entity.Index * 1000);
                EntitySpawner.UpdateAnimationFields(ref animationComponent, walkRandom, runRandom);
                //}
                animationComponent.prevAnimationType = animationComponent.animationType;
            }

        }).WithBurst().ScheduleParallel();
    }
}