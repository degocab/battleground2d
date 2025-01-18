using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(CombatSystem))]
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
[BurstCompile]
public class MovementSystem : SystemBase
{
    //public static EntitySpawner entitySpawner;

    //protected override void OnStartRunning()
    //{
    //    entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
    //}

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;


        Entities.ForEach((ref AnimationComponent animationComponent, ref MovementSpeedComponent movementSpeedComponent, ref AttackComponent attackComponent, in Entity entity) =>
        {
            if (!attackComponent.isAttacking)
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
                        EntitySpawner.UpdateAnimationFields(ref animationComponent);
                    }
                    else //(animationComponent.animationType == EntitySpawner.AnimationType.Run)
                    {

                        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)entity.Index);

                        EntitySpawner.UpdateAnimationFields(ref animationComponent, random);
                    }
                    animationComponent.prevAnimationType = animationComponent.animationType;
                }
            }

        }).ScheduleParallel();

        // Movement speed randomizer
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


            float3 vel = (new float3(moveX, moveY, 0) *
                                    //(entitySpawner != null ? entitySpawner.mainSpeedVar : 2f)d
                                    velocity.randomSpeed
                                    );
            vel.z = 0;
            velocity.value = vel;
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
                if (!animationComponent.finishAnimation)
                {
                    animationComponent.direction = animationComponent.prevDirection; // Or some default direction 
                }
            }

            animationComponent.prevDirection = animationComponent.direction;

        }).ScheduleParallel();

        //bool attack = false;
        //if (Input.GetKeyDown(KeyCode.Space)) // Detect spacebar press only
        //{
        //    attack = true;
        //}

        //Entities.ForEach((ref MovementSpeedComponent velocity, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref Translation translation, ref AnimationComponent animationComponent) =>
        //{
        //    if (attack || animationComponent.finishAnimation)
        //    {
        //        animationComponent.animationType = EntitySpawner.AnimationType.Attack;
        //        animationComponent.direction = animationComponent.prevDirection;
        //    }
        //    // If the attack button is pressed and cooldown is finished
        //    if (attack && attackCooldown.timeRemaining <= 0f && !animationComponent.finishAnimation)
        //    {
        //        // Start the attack animation
        //        animationComponent.animationType = EntitySpawner.AnimationType.Attack;

        //        // Initialize animation parameters only once (on first attack)
        //        if (!animationComponent.finishAnimation)
        //        {
        //            animationComponent.finishAnimation = true;
        //            animationComponent.frameCount = 6; // Example: 6 frames for the attack animation
        //            animationComponent.currentFrame = 0; // Start at the first frame
        //            animationComponent.frameTimerMax = 0.2f; // Example: 0.2 seconds per frame
        //            animationComponent.frameTimer = 0f; // Reset the frame timer
        //            attackCooldown.timeRemaining = attackCooldown.cooldownDuration; // Set the cooldown duration
        //            animationComponent.prevAnimationType = animationComponent.animationType; // Set previous animation type to attack
        //        }

        //        // Optionally: Apply damage to nearby entities within attack range
        //        float attackRange = attackComponent.range;
        //        float attackDamage = attackComponent.damage;
        //        // You can use your existing logic here to apply damage based on range
        //    }

        //    // Handle cooldown timer
        //    if (attackCooldown.timeRemaining > 0f)
        //    {
        //        attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
        //    }

        //    // Once the animation is complete, reset the animation and allow the next attack
        //    if (animationComponent.currentFrame >= animationComponent.frameCount)
        //    {
        //        animationComponent.finishAnimation = false; // Reset finish flag after animation is done
        //        animationComponent.animationType = EntitySpawner.AnimationType.Idle; // Switch to idle animation (or another animation type)
        //    }

        //    // Animation logic (frame updates, etc.)
        //    //if (animationComponent.finishAnimation)
        //    //{
        //    //    // Handle frame updates here based on your frame timing system
        //    //    // Increment `currentFrame` based on time (use `frameTimer` for frame timing)
        //    //    animationComponent.frameTimer += deltaTime;

        //    //    if (animationComponent.frameTimer >= animationComponent.frameTimerMax)
        //    //    {
        //    //        animationComponent.frameTimer = 0f;
        //    //        animationComponent.currentFrame++;
        //    //    }
        //    //}

        //}).ScheduleParallel();



        //actual movement system
        Entities.ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
        {
            //stop moving on attack
            if (animationComponent.animationType == EntitySpawner.AnimationType.Run)//walk || animationComponent.animationType != EntitySpawner.AnimationType.Run)
            {
                // Update position based on velocity (movement calculations)
                position.value += velocity.value * deltaTime;

                // Apply the new position to the entity's translation (moving in the scene)
                translation.Value = position.value;
            }

        }).ScheduleParallel();
    }
}

[UpdateAfter(typeof(MovementSystem))]
public class CombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        bool attack = false;
        if (Input.GetKeyDown(KeyCode.Space)) // Detect spacebar press only
            attack = true;

        Entities.ForEach((ref Entity entity, ref Translation translation, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref AnimationComponent animationComponent) =>
        {
            if (attack)
            {
                if (!attackComponent.isAttacking) //dont reset until we are done
                {
                    attackComponent.isAttacking = true;
                    animationComponent.animationType = EntitySpawner.AnimationType.Attack;
                    EntitySpawner.UpdateAnimationFields(ref animationComponent);
                    //animationComponent.finishAnimation = true;
                    //animationComponent.frameCount = 6; // Example: 6 frames for the attack animation
                    //animationComponent.currentFrame = 0; // Start at the first frame
                    //animationComponent.frameTimerMax = 0.12f; // Example: 0.2 seconds per frame
                    //animationComponent.frameTimer = 0f; // Reset the frame timer
                    attackCooldown.timeRemaining = attackCooldown.cooldownDuration; // Set the cooldown duration 
                }
            }
        }).ScheduleParallel();

        Entities.ForEach((ref Entity entity, ref MovementSpeedComponent velocity, ref AttackComponent attackComponent, ref AttackCooldownComponent attackCooldown, ref Translation translation, ref AnimationComponent animationComponent) =>
        {
            // If the attack button is pressed and cooldown is finished
            if (attackComponent.isAttacking)
            {
                if (attackCooldown.timeRemaining > 0f)
                {
                    attackCooldown.timeRemaining -= deltaTime; // Reduce cooldown
                }
                else
                {
                    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
                    attackComponent.isAttacking = false; // Reset finish flag after animation is done

                    animationComponent.animationType = EntitySpawner.AnimationType.Idle;
                    EntitySpawner.UpdateAnimationFields(ref animationComponent);

                } 
            }



            //// Once the animation is complete, reset the animation and allow the next attack
            //if (attackCooldown.timeRemaining <= 0f)
            //{
            //    animationComponent.finishAnimation = false; // Reset finish flag after animation is done
            //    attackComponent.isAttacking = false; // Reset finish flag after animation is done


            //}

        }).ScheduleParallel();
    }
}