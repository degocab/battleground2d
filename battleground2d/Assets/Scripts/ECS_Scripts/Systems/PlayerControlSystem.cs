using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateBefore(typeof(CombatSystem))]
public class PlayerControlSystem : SystemBase
{
    public Transform cameraMain;
    public static EntitySpawner entitySpawner;
    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
        if (cameraMain == null)
            cameraMain = Camera.main.transform;
    }
    protected override void OnUpdate()
    {
        float moveX = 0f;
        float moveY = 0f;
        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;
        bool isRunnning = false;
        if (Input.GetKey(KeyCode.LeftShift)) isRunnning = true;

        //zoom camera
        //will be needed for riding horse
        // should give you more zoomed out vision!
        if (Input.GetKey(KeyCode.Tab))
            Camera.main.orthographicSize = 7f;
        else
            Camera.main.orthographicSize = 4f;
        var time = Time.DeltaTime;


        ////update camera location 
        //float horizontal = Input.GetAxis("Horizontal");  // Left/Right input
        //float vertical = Input.GetAxis("Vertical");      // Up/Down inputw
        //Vector3 targetPosition = new Vector3();

        //Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;
        //targetPosition += moveDirection * 3f * time;
        //targetPosition.z = -13f;


        //Entities.WithAll<CommanderComponent>().ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
        //{
        //    movementSpeedComponent.moveX = moveX;
        //    movementSpeedComponent.moveY = moveY;
        //    movementSpeedComponent.isRunnning = isRunnning;
        //}).ScheduleParallel();

        //Entities.WithAll<CommanderComponent>().ForEach((ref PositionComponent position, ref MovementSpeedComponent velocity, in PlayerInputComponent input) => 
        //{

        //    if (velocity.isRunnning)
        //    {
        //        velocity.randomSpeed = 2f;// 1.25f;
        //    }
        //    else
        //    {
        //        velocity.randomSpeed = .525f;
        //    }
        //    float3 vel = (new float3(velocity.moveX, velocity.moveY, 0) * velocity.randomSpeed);

        //    vel.z = 0;
        //    velocity.value = vel;
        //}).ScheduleParallel();

        Vector3 newCamPos = new Vector3();
        Entities.ForEach((ref PlayerInputComponent playerInputComponent, ref Translation translation) =>
        {
            newCamPos = translation.Value;
            newCamPos.z = -13f;
            cameraMain.position = newCamPos;
        }).WithoutBurst().Run();




        //get direction
        //Entities
        //    .WithAll<CommanderComponent>()//remove to allow all units to move from wasd
        //    .ForEach((ref PlayerInputComponent playerInputComponent,ref MovementSpeedComponent velocity, ref AnimationComponent animationComponent) =>
        //    {
        //        if (velocity.value.x > 0)
        //        {
        //            animationComponent.direction = EntitySpawner.Direction.Right;
        //        }
        //        else if (velocity.value.x < 0)
        //        {
        //            animationComponent.direction = EntitySpawner.Direction.Left;
        //        }
        //        else if (velocity.value.y > 0)
        //        {
        //            animationComponent.direction = EntitySpawner.Direction.Up;
        //        }
        //        else if (velocity.value.y < 0)
        //        {
        //            animationComponent.direction = EntitySpawner.Direction.Down;
        //        }
        //        else
        //        {
        //            // In case of the entity being at the origin (0, 0)
        //            if (!animationComponent.finishAnimation)
        //            {
        //                animationComponent.direction = animationComponent.prevDirection; // Or some default direction 
        //            }
        //        }
        //        animationComponent.prevDirection = animationComponent.direction;

        //    }).ScheduleParallel();

    }

}
