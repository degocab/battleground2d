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
[UpdateBefore(typeof(ProcessCommandSystem))]
public partial class PlayerControlSystem : SystemBase
{
    public Transform cameraMain;
    public static EntitySpawner entitySpawner;
    private Vector3 cameraVelocity = Vector3.zero;
    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
        if (cameraMain == null)
            cameraMain = Camera.main.transform;
    }

    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        if (SystemAPI.GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;


        // Check if we have a commander
        if (!HasSingleton<CommanderComponent>())
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
        foreach (var movementSpeed in SystemAPI.Query<RefRW<MovementSpeedComponent>>().WithAll<PlayerInputComponent>())
        {
            movementSpeed.ValueRW.velocity = new float3(moveX, moveY, 0);
            movementSpeed.ValueRW.isRunnning = isRunnning;
            movementSpeed.ValueRW.isPlayerControlled = true;
            movementSpeed.ValueRW.aimDirection = worldMousePosFloat;
        }


        //var ecb = _ecbSystem.CreateCommandBuffer();
        var parallelEcb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var commanderEntity = GetSingletonEntity<CommanderComponent>();
        var commanderTransform = GetComponent<LocalTransform>(commanderEntity);
        // Number keys 1-9
        for (int key = (int)KeyCode.Alpha1; key <= (int)KeyCode.Alpha9; key++)
        {
            if (Input.GetKeyDown((KeyCode)key))
            {
                int commandType = key - (int)KeyCode.Alpha1;
                var newCommand = CreateCommandFromNumber(commandType, commanderTransform.Position, GetMouseWorldPosition());

                // Use a local variable to capture the command for the job
                var commandCopy = newCommand;

                // SINGLE job that does both operations safely
                var commandJobHandle = Entities
                    .WithName("ProcessCommandInput")
                    .WithAll<Unit>()
                    .WithNone<CommanderComponent>()
                    .ForEach((Entity entity, int entityInQueryIndex, ref CommandData commandData, in AnimationComponent animationComponent) =>
                    {
                        if (animationComponent.UnitType == EntitySpawner.UnitType.Enemy)
                        {
                            return;
                        }
                        // First remove HasTarget component
                        parallelEcb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);

                        // Then update command data
                        commandData = commandCopy;

                    }).ScheduleParallel(Dependency);


                Debug.Log($"Assigned command: {newCommand.Command} to all units");
                Dependency = commandJobHandle;



                //update groups
                foreach (var (formationGroup, commandData, entity) in SystemAPI.Query<RefRW<FormationGroupComponent>, RefRW<CommandData>>()
                    .WithAll<FormationGroupComponent>()
                    .WithNone<CommanderComponent, Unit>()
                    .WithEntityAccess())
                {
                    // Then update command data
                    commandData.ValueRW = commandCopy;
                    //if (commandCopy.Command == CommandType.FindTarget)
                    //{
                    //    formationGroup.FormationGroupStatus = FormationStatus.Engaged;
                    //}
                }
                break; // Important: Only process one key per frame
            }
        }
        //this.Dependency.Complete();
        float deltaTime = SystemAPI.Time.DeltaTime;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isAttacking = Input.GetMouseButtonDown(0);
        bool isDefending = Input.GetMouseButton(1);
        //Debug.Log($"Is Defending: {isDefending}");


        UpdateCameraZoom();
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        Dependency.Complete();
        // Split query into two parts due to 7-parameter limit on SystemAPI.Query
        foreach (var (playerInput, transform, combatState, attackComponent, attackCooldown, animationComponent, movementSpeed) 
            in SystemAPI.Query<RefRW<PlayerInputComponent>, RefRW<LocalTransform>, RefRW<CombatState>, RefRW<AttackComponent>, RefRW<AttackCooldownComponent>, RefRW<AnimationComponent>, RefRW<MovementSpeedComponent>>()
            .WithAll<DefenseComponent>())
        {
            // Get the DefenseComponent separately since we exceeded the 7-param limit
            var entity = SystemAPI.GetSingletonEntity<PlayerInputComponent>();
            var defenseComponent = SystemAPI.GetComponentRW<DefenseComponent>(entity);

            if (combatState.ValueRO.CurrentState == CombatState.State.TakingDamage)
            {
                //combatState.CurrentState = CombatState.State.TakingDamage;
                continue;
            }



                //attackComponent.isAttacking = false;
                //attackComponent.isDefending = false;
                //defenseComponent.IsBlocking = false;


                // Step 1: Reduce only attack rate cooldown (we don't touch animation cooldown)
                //if (attackComponent.AttackRateRemaining > 0f)
                //{
                //    attackComponent.AttackRateRemaining -= deltaTime;
                //}
            if (attackCooldown.ValueRO.attackCoolTimeRemaining > 0f)
                {
                    //attackCooldown.timeRemaining -= deltaTime;
                    isAttacking = true;
                }



            // Step 2: Determine whether we are allowed to attack
            bool animationReady = attackCooldown.ValueRO.attackCoolTimeRemaining <= 0f;
            bool attackReady = attackComponent.ValueRO.AttackRateRemaining <= 0f;
                bool canAttack = animationReady && attackReady;

                // Step 3: RESET flags at the start of each frame
                //attackComponent.isAttacking = false;
                //attackComponent.isDefending = false;
                //defenseComponent.IsBlocking = false;


            bool blocking = isStillBlocking(defenseComponent.ValueRO);

                // Step 4: Handle state transitions
                if (isAttacking)
                {
                    if (canAttack)
                    {
                        PerformAttack(ref combatState.ValueRW, ref attackComponent.ValueRW, ref animationComponent.ValueRW);
                        StartAttack(ref combatState.ValueRW, ref attackCooldown.ValueRW); // animation system will handle timeRemaining now
                        //attackComponent.isAttacking = true; // SET FLAG
                        playerInput.ValueRW.stillAttacking = true;
                    }
                    else
                    {
                        if (isAttacking)
                        {
                            //still attacking
                        }
                        else
                        {
                            SetToIdle(ref combatState.ValueRW, ref animationComponent.ValueRW);

                        }
                    }
                }
                else if (blocking)
                {
                    //keep blocking, shuld override defending
                    defenseComponent.ValueRW.IsBlocking = true;
                    combatState.ValueRW.CurrentState = CombatState.State.Blocking;
                }
                else if (isDefending)
                {
                    //defenseComponent.IsBlocking = true;
                    //attackComponent.isDefending = true;
                    combatState.ValueRW.CurrentState = CombatState.State.Defending;
                }
                else if (animationReady && !attackReady)
                {
                    // We have recovered from animation but are still waiting on attack rate cooldown
                    SetToIdle(ref combatState.ValueRW, ref animationComponent.ValueRW);
                }
                else
                {
                    SetToIdle(ref combatState.ValueRW, ref animationComponent.ValueRW);
                }


                ProcessMovement(ref movementSpeed.ValueRW, GetMovementInput(), isRunning);
                UpdateCameraPosition(transform.ValueRO.Position);

        }
        // Add the command buffer system
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    private bool isStillBlocking(DefenseComponent defenseComponent)
    {
        return defenseComponent.BlockDuration > 0f;
    }

    private CommandData CreateCommandFromNumber(int number, float3 commanderPosition, float2 moveToPosition)
    {
        CommandData comm = new CommandData();
        switch (number)
        {
            case 0: // Move
                comm = CommandFactory.CreateChargeCommand();
                break;

            case 1: // Find target
                comm = CommandFactory.CreateMarchCommand();
                break;

            case 2: // Attack position
                comm = CommandFactory.CreateAttackCommand(moveToPosition);
                break;

            case 3: // Defend
                comm = CommandFactory.CreateCommand(CommandType.Defend);
                break;

            case 4: // Long move
                comm = CommandFactory.CreateMoveCommand(moveToPosition);
                break;

            case 5: // Stop
                comm = CommandFactory.CreateCommand(CommandType.Idle);
                break;

            case 6: // Custom command 1
                comm = CommandFactory.CreateFindTargetCommand();
                break;

            case 7: // Custom command 2
                comm = CommandFactory.CreateMoveCommand(moveToPosition);
                break;

            case 8: // Custom command 3
                Debug.Log("create find comand");
                comm = CommandFactory.CreateFindTargetCommand(); // Attack anything
                break;

            default: // Fallback
                comm = CommandFactory.CreateCommand(CommandType.Idle);
                break;


        }
        Debug.Log("Command#" + comm);

        return comm;
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

    private void UpdateCameraZoom()
    {
        float targetSize = Input.GetKey(KeyCode.Tab) ? 10f : 4f;
        Camera.main.orthographicSize = targetSize;
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

    private void ProcessMovement(ref MovementSpeedComponent playerInput, Vector2 movementInput, bool isRunning)
    {
        playerInput.velocity.x = movementInput.x;
        playerInput.velocity.y = movementInput.y;
        playerInput.isRunnning = isRunning;
    }


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



