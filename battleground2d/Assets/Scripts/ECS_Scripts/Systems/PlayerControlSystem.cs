using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateAfter(typeof(QuadrantSystem))]
[UpdateBefore(typeof(ProcessOrderSystem))]
public class PlayerControlSystem : SystemBase
{

    private string lastOrderText = "";
    private float lastOrderTime = 0f;
    private const float COMMAND_DISPLAY_DURATION = 1.5f;


    public Transform cameraMain;
    private Vector3 cameraVelocity = Vector3.zero;
    protected override void OnStartRunning()
    {
        if (cameraMain == null)
            cameraMain = Camera.main.transform;
    }

    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;


        // Check if we have a commander
        if (!HasSingleton<PlayerInputComponent>())
            return;

        float moveX = 0f;
        float moveY = 0f;
        bool isRunnning = false;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) isRunnning = true;

        // Get mouse position for aiming
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -Camera.main.transform.position.z;
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
        float2 worldMousePosFloat = new float2(worldMousePos.x, worldMousePos.y);

        // Get commander position FIRST using .Run()
        float2 commanderPosition = float2.zero;
        //Entities
        //    .WithAll<CommanderComponent>()
        //    .ForEach((in Translation translation) =>
        //    {
        //        commanderPosition = translation.Value.xy;
        //    }).Run();

        // We need to get the commander's position first
        //float2 commanderPosition = float2.zero;

        // Calculate movement penalty based on angle between movement and aim
        //if (foundCommander)

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {

            float2 aimDirection = worldMousePosFloat - commanderPosition;
            aimDirection = math.normalize(aimDirection);

            float2 moveDirection = new float2(moveX, moveY);
            float moveMagnitude = math.length(moveDirection);

            if (moveMagnitude > 0)
            {
                moveDirection = math.normalize(moveDirection);

                // Calculate dot product to get angle between movement and aim
                float dotProduct = math.dot(moveDirection, aimDirection);

                // Apply speed multipliers based on direction
                float speedMultiplier = 1.0f;

                if (dotProduct > 0.8f)
                {
                    speedMultiplier = 1.0f; // Moving forward (full speed)
                }
                else if (dotProduct > 0.3f)
                {
                    speedMultiplier = 0.8f; // Moving somewhat sideways
                }
                else if (dotProduct > -0.3f)
                {
                    speedMultiplier = 0.6f; // Moving mostly sideways
                }
                else
                {
                    speedMultiplier = 0.4f; // Moving backwards (slowest)
                }

                // Apply the speed penalty
                moveX *= speedMultiplier;
                moveY *= speedMultiplier;
            }

        }
        // This job sets the desired velocity based on input or AI for commander.
        //var inputJobHandle = 
        //Entities
        //    .WithName("SetCommanderVelocity")
        //    .WithAll<PlayerInputComponent>()
        //    .ForEach((ref MovementSpeedComponent movementSpeedComponent) =>
        //    {



        //        movementSpeedComponent.velocity = new float3(moveX, moveY, 0);
        //        movementSpeedComponent.isRunnning = isRunnning;
        //        movementSpeedComponent.isPlayerControlled = true;
        //        movementSpeedComponent.aimDirection = worldMousePosFloat;

        //    }).Run();


        //var ecb = _ecbSystem.CreateCommandBuffer();
        var parallelEcb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        Translation commanderTranslation = new Translation();

        Entities
            .WithName("GetPlayerCommanderPosition")
            .WithAll<PlayerInputComponent>()
            .ForEach((
                in CommanderComponent commander,
                in Translation translation) =>
            {
                if (!commander.isPlayerControlled)
                    return;

                commanderTranslation = translation;
            })
            .WithoutBurst()
            .Run();
        // Number keys 1-9
        for (int key = (int)KeyCode.Alpha1; key <= (int)KeyCode.Alpha9; key++)
        {
            var orderKey = (KeyCode)key;
            bool preview = orderKey == KeyCode.Alpha5 && Input.GetKey(orderKey);
            bool issue = orderKey == KeyCode.Alpha5 ? Input.GetKeyUp(orderKey) : Input.GetKeyDown(orderKey);

            //if (Input.GetKeyDown((KeyCode)key))
            if (preview || issue)
            {
                int orderType = key - (int)KeyCode.Alpha1;
                var newOrder = CreateOrderFromKey(orderType, commanderTranslation.Value, GetMouseWorldPosition());
                // DEBUG UI
                lastOrderText = $"Order: {newOrder.CurrentOrder.ToString()}";
                lastOrderTime = (float)Time.ElapsedTime;
                // Use a local variable to capture the command for the job
                var orderCopy = newOrder;


                //get average group center
                float2 groupCenter = float2.zero;
                int groupCount = 0;
                Entities
                     .WithName("UpdateGroupCenter")
                     .WithAll<FormationGroupComponent>()
                     .WithAll<AllyTag>()
                     .WithAll<OrderData>()
                     .WithNone<CommanderComponent, Unit>()
                     .ForEach((Entity entity, int entityInQueryIndex,
                     in FormationGroupComponent formationGroup) =>
                     {
                         groupCenter += formationGroup.CurrentUnitAveragePosition;
                         groupCount++;
                     }).WithoutBurst().Run();
                if (groupCount > 0)
                {
                    groupCenter /= groupCount; 
                }

                //update groups
                Entities
                     .WithName("UpdateOrdersForGroups")
                     .WithAll<FormationGroupComponent>()
                     .WithAll<AllyTag>()
                     .WithNone<CommanderComponent, Unit>()
                     .ForEach((Entity entity, int entityInQueryIndex,
                     ref FormationGroupComponent formationGroup, ref OrderData order) =>
                     {

                         // Then update command data
                         var plannedOrder = orderCopy;

                         if (plannedOrder.CurrentOrder == OrderType.MoveTo)
                         {
                             float2 offset = formationGroup.CurrentUnitAveragePosition - groupCenter;
                             plannedOrder.TargetPosition = orderCopy.TargetPosition + offset;

                             float2 shift = plannedOrder.TargetPosition - formationGroup.CurrentUnitAveragePosition;
                             float2 min = formationGroup.BoundsMin + shift;
                             float2 max = formationGroup.BoundsMax + shift;

                             var bottomLeft = new Vector3(min.x, min.y, 0f);
                             var bottomRight = new Vector3(max.x, min.y, 0f);
                             var topRight = new Vector3(max.x, max.y, 0f);
                             var topLeft = new Vector3(min.x, max.y, 0f);

                             Debug.DrawLine(bottomLeft, bottomRight, Color.cyan, 0f);
                             Debug.DrawLine(bottomRight, topRight, Color.cyan, 0f);
                             Debug.DrawLine(topRight, topLeft, Color.cyan, 0f);
                             Debug.DrawLine(topLeft, bottomLeft, Color.cyan, 0f);
                         }

                         if (issue)
                             order = plannedOrder;

                     }).WithoutBurst().Run();
                break; // Important: Only process one key per frame
            }
        }
        //this.Dependency.Complete();
        float deltaTime = Time.DeltaTime;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isAttacking = Input.GetMouseButtonDown(0);
        bool isDefending = Input.GetMouseButton(1);
        //Debug.Log($"Is Defending: {isDefending}");


        UpdateCameraZoom();
        float currentTime = (float)Time.ElapsedTime;

        Dependency.Complete();
        Entities
            .WithoutBurst()
            .ForEach((
                ref PlayerInputComponent playerInput,
                ref Translation translation,
                ref CombatState combatState,
                ref AttackComponent attackComponent,
                ref AttackCooldownComponent attackCooldown,
                ref AnimationComponent animationComponent,
                //ref MovementSpeedComponent movementSpeedComponent,
                ref DefenseComponent defenseComponent
            ) =>
            {

                if (combatState.CurrentState == CombatState.State.TakingDamage)
                {
                    //combatState.CurrentState = CombatState.State.TakingDamage;
                    return;
                }



                //attackComponent.isAttacking = false;
                //attackComponent.isDefending = false;
                //defenseComponent.IsBlocking = false;


                // Step 1: Reduce only attack rate cooldown (we don't touch animation cooldown)
                //if (attackComponent.AttackRateRemaining > 0f)
                //{
                //    attackComponent.AttackRateRemaining -= deltaTime;
                //}
                if (attackCooldown.attackCoolTimeRemaining > 0f)
                {
                    //attackCooldown.timeRemaining -= deltaTime;
                    isAttacking = true;
                }



                // Step 2: Determine whether we are allowed to attack
                bool animationReady = attackCooldown.attackCoolTimeRemaining <= 0f;
                bool attackReady = attackComponent.AttackRateRemaining <= 0f;
                bool canAttack = animationReady && attackReady;

                // Step 3: RESET flags at the start of each frame
                //attackComponent.isAttacking = false;
                //attackComponent.isDefending = false;
                //defenseComponent.IsBlocking = false;


                bool blocking = isStillBlocking(defenseComponent);

                // Step 4: Handle state transitions
                if (isAttacking)
                {
                    if (canAttack)
                    {
                        PerformAttack(ref combatState, ref attackComponent, ref animationComponent);
                        StartAttack(ref combatState, ref attackCooldown); // animation system will handle timeRemaining now
                        //attackComponent.isAttacking = true; // SET FLAG
                        playerInput.stillAttacking = true;
                    }
                    else
                    {
                        if (isAttacking)
                        {
                            //still attacking
                        }
                        else
                        {
                            SetToIdle(ref combatState, ref animationComponent);

                        }
                    }
                }
                else if (blocking)
                {
                    //keep blocking, shuld override defending
                    defenseComponent.IsBlocking = true;
                    combatState.CurrentState = CombatState.State.Blocking;
                }
                else if (isDefending)
                {
                    //defenseComponent.IsBlocking = true;
                    //attackComponent.isDefending = true;
                    combatState.CurrentState = CombatState.State.Defending;
                }
                else if (animationReady && !attackReady)
                {
                    // We’ve recovered from animation but are still waiting on attack rate cooldown
                    SetToIdle(ref combatState, ref animationComponent);
                }
                else
                {
                    SetToIdle(ref combatState, ref animationComponent);
                }


                if (combatState.CurrentState != CombatState.State.TakingDamage)
                {
                    //ProcessMovement(ref movementSpeedComponent, GetMovementInput(), isRunning);
                    UpdateCameraPosition(translation.Value); 
                }

            }).Run();
        // Add the command buffer system
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    private bool isStillBlocking(DefenseComponent defenseComponent)
    {
        return defenseComponent.BlockDuration > 0f;
    }
    void OnGUI()
    {
        if (Time.ElapsedTime - lastOrderTime > COMMAND_DISPLAY_DURATION)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(20, 20, 600, 40),
            lastOrderText,
            style
        );
    }

    private OrderData CreateOrderFromKey(int number, float3 commanderPosition, float2 moveToPosition)
    {
        OrderData order = new OrderData();
        switch (number)
        {
            case 0: // Move -  1
                order = OrderFactory.CreateChargeOrder();
                break;

            case 1: // Find target - 2
                order = OrderFactory.CreateMarchOrder();
                break;

            case 2: // MarchForward - 3
                order = OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange,  10f, EntitySpawner.Direction.Right);
                break;

            case 3: // Defend - 4
                order = OrderFactory.CreateOrder(OrderType.Defend);
                break;

            case 4: // Long move - 5
                order = OrderFactory.CreateMoveOrder(moveToPosition);
                break;

            case 5: // Stop - 6
                order = OrderFactory.CreateOrder(OrderType.Idle);
                break;

            case 6: // FindTarget command - 7
                order = OrderFactory.CreateFindTargetOrder();
                break;

            case 7: // Custom command 2
                order = OrderFactory.CreateMoveOrder(moveToPosition);
                break;

            case 8: // Custom command 3
                Debug.Log("create find comand");
                order = OrderFactory.CreateFindTargetOrder(); // Attack anything
                break;

            default: // Fallback
                order = OrderFactory.CreateOrder(OrderType.Idle);
                break;


        }
        Debug.Log("Order#" + order.CurrentOrder.ToString());
        OrderDebugUI.Text = $"Order: {order.CurrentOrder.ToString()}";
        OrderDebugUI.TimeRemaining = 1.5f;

        return order;
    }

    public static float2 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return new float2(worldPos.x, worldPos.y);
    }

    private Vector2 GetMovementInput()
    {
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;

        return new Vector2(moveX, moveY);
    }

    [SerializeField] float smooth = 0.2f;
    private float zoomVelocity;

    private void UpdateCameraZoom()
    {
        float targetSize = 3f;

        if (Input.GetKey(KeyCode.LeftShift))
            targetSize = 4f;
        if (Input.GetKey(KeyCode.Tab))
            targetSize = 8f;

        Camera.main.orthographicSize = Mathf.SmoothDamp(
            Camera.main.orthographicSize,
            targetSize,
            ref zoomVelocity,
            smooth);
    }

    //private void UpdateCameraPosition(float3 playerPosition)
    //{
    //    Vector3 cameraPosition = playerPosition;
    //    cameraPosition.z = -13f;
    //    Camera.main.transform.position = cameraPosition;
    //}
    private void UpdateCameraPosition(float3 playerPosition)
    {
        Vector3 targetPosition = playerPosition;
        targetPosition.z = -13f;

        //TODO: Add camera offset based on player facing direction or mouse position
        if (Input.GetKey(KeyCode.Tab))
            targetPosition.y = playerPosition.y + 5f; 

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            Camera.main.transform.position,
            targetPosition,
            ref cameraVelocity,
            0.1f); // Adjust 0.1f for smooth time

        Camera.main.transform.position = smoothedPosition;
    }

    private void StartAttack(ref CombatState combatState,
                           ref AttackCooldownComponent attackCooldown)
    {
        //combatState.CurrentState = CombatState.State.Attacking;
        attackCooldown.attackCoolTimeRemaining = attackCooldown.attackCoolDownDuration;
        //Debug.Log("Attack Started");
    }

    //private void ProcessMovement(ref MovementSpeedComponent playerInput, Vector2 movementInput, bool isRunning)
    //{
    //    playerInput.velocity.x = movementInput.x;
    //    playerInput.velocity.y = movementInput.y;
    //    playerInput.isRunnning = isRunning;
    //}


    private void PerformAttack(ref CombatState combatState, ref AttackComponent attackComponent, ref AnimationComponent animationComponent)
    {
        attackComponent.AttackRateRemaining = attackComponent.AttackRate;
        combatState.CurrentState = CombatState.State.Attacking;
        //attackComponent.isAttacking = true;
        animationComponent.finishAnimation = true;
        //animationComponent.AnimationType = EntitySpawner.AnimationType.Attack;

        //Debug.Log("Player attacked!");
    }

    private void SetToIdle(ref CombatState combatState, ref AnimationComponent animationComponent)
    {
        combatState.CurrentState = CombatState.State.Idle;
        animationComponent.AnimationType = EntitySpawner.AnimationType.Idle;
        //Debug.Log("Player is idle");

    }
}



