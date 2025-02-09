using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.CanvasScaler;

//[UpdateAfter(typeof(CombatSystem))]
[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateAfter(typeof(UnitCollisionSystem))]
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
            if (movementSpeedComponent.isBlocked == false)
            {
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
            }
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
                    animationComponent.animationWidthOffset = 1;
                }
                else if (velocity.value.x < 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Left;
                    animationComponent.animationWidthOffset = 2;
                }
                else if (velocity.value.y > 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Up;
                    animationComponent.animationWidthOffset = 3;
                }
                else if (velocity.value.y < 0)
                {
                    animationComponent.direction = EntitySpawner.Direction.Down;
                    animationComponent.animationWidthOffset = 4;
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
        // Actual movement system
        Entities
            .ForEach((ref Translation translation, ref PositionComponent position, ref MovementSpeedComponent movementSpeedComponent, ref Unit unit, ref CollisionBounds collisionBounds, ref AnimationComponent animationComponent) =>
            {
                // Stop moving on attack
                if (animationComponent.animationType == EntitySpawner.AnimationType.Run || animationComponent.animationType == EntitySpawner.AnimationType.Walk)
                {
                    // If the unit is blocked, stop its movement
                    if (movementSpeedComponent.isBlocked)
                    {
                        return; // Do not move this unit this frame
                    }

                    // If the unit is knocked back, handle knock-back logic
                    else if (movementSpeedComponent.isKnockedBack)
                    {
                        // Use the knocked-back direction to push the unit back
                        float3 direction = movementSpeedComponent.knockedBackDirection;
                        float distance = math.length(direction);

                        // Calculate the overlap (how much the entities are overlapping)
                        float overlap = (collisionBounds.radius * 2) - distance;

                        // Apply a small amount of knockback before allowing movement again
                        float knockbackStrength = unit.isMounted ? 1.5f : 0.05f;
                        float maxKnockback = .5f; // Limit to a max knockback distance

                        // Apply the knockback, but limit it to a small amount
                        float knockbackAmount = math.min(overlap, maxKnockback);
                        float3 pushBackDirection = math.normalize(direction); // Normalize to avoid scaling issues
                        position.value += pushBackDirection * knockbackAmount * knockbackStrength;

                        // After applying the knockback, decrease the knockback strength
                        // If the knockback has been resolved (enough distance moved), stop the knockback
                        if (knockbackAmount < overlap)
                        {
                            movementSpeedComponent.isKnockedBack = false; // Allow movement again after knockback is resolved
                        }
                    }
                    else
                    {
                        // If no collision, update position based on velocity
                        position.value += movementSpeedComponent.value * deltaTime;
                    }

                    // Apply the new position to the entity's translation (moving in the scene)
                    translation.Value = position.value;
                }
            }).WithBurst().ScheduleParallel();


        //}).WithoutBurst().Run();

    }
}
