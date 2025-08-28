using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateAfter(typeof(QuadrantSystem))]
[UpdateBefore(typeof(ProcessCommandSystem))]
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
        if (!HasSingleton<CommanderComponent>())
            return;

        var ecb = _ecbSystem.CreateCommandBuffer();
        var commanderEntity = GetSingletonEntity<CommanderComponent>();
        var commanderTranslation = GetComponent<Translation>(commanderEntity);
        bool attack = false;
        bool defend = false;
        //if (Input.GetKeyDown(KeyCode.Space)) // Detect spacebar press only
        if (Input.GetMouseButtonDown(0)) // Detect spacebar press only
            attack = true;

        if (Input.GetMouseButton(1)) // Detect spacebar press only
            defend = true;
        else
            defend = false;

        // Number keys 1-9
        for (int key = (int)KeyCode.Alpha1; key <= (int)KeyCode.Alpha9; key++)
        {
            if (Input.GetKeyDown((KeyCode)key))
            {
                int commandType = key - (int)KeyCode.Alpha1; // 0-8

                // Create command based on number pressed
                var command = CreateCommandFromNumber(commandType, commanderTranslation.Value, GetMouseWorldPosition());


                // Apply to all selected units (for now, just commander - extend later)
                var parallelEcb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
                Entities
                    .WithName("ApplyMouseCommand")
                    .WithAll<Unit>()
                    .WithNone<CommanderComponent>()
                    .ForEach((Entity entity, int entityInQueryIndex) =>
                    {
                        parallelEcb.SetComponent(entityInQueryIndex, entity, command);
                    }).ScheduleParallel();

                Entities
    .WithName("ApplyMouseCommandCleanHastarget")
    .WithAll<Unit>()
    .WithAll<HasTarget>()
    .WithNone<CommanderComponent>()
    .ForEach((Entity entity, int entityInQueryIndex) =>
    {
        parallelEcb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
    }).ScheduleParallel();

                Debug.Log($"Assigned command: {command.Command} to all units");
            }
        }

        float deltaTime = Time.DeltaTime;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isAttacking = Input.GetMouseButtonDown(0);
        bool isDefending = Input.GetMouseButtonDown(1);

        UpdateCameraZoom();

        Entities
            .WithoutBurst()
            .ForEach((
                ref PlayerInputComponent playerInput,
                ref Translation translation,
                ref CombatState combatState,
                ref AttackComponent attackComponent,
                ref AttackCooldownComponent attackCooldown,
                ref MovementSpeedComponent movementSpeedComponent
            ) =>
            {
                // NO MOVEMENT DURING ATTACKS - PERIOD
                if (combatState.CurrentState == CombatState.State.Attacking)
                {
                    movementSpeedComponent.velocity = float3.zero;
                    movementSpeedComponent.isRunnning = false;
                    UpdateCameraPosition(translation.Value);
                    return;
                }
                else
                {
                    // Process combat and movement normally when not attacking
                    if (!ProcessCombatActions(ref combatState, ref attackComponent, ref attackCooldown,
                                   isAttacking, isDefending, deltaTime))
                    {
                        movementSpeedComponent.velocity = float3.zero;
                        movementSpeedComponent.isRunnning = false;
                        UpdateCameraPosition(translation.Value);
                        return; ; 
                    }
                    ProcessMovement(ref movementSpeedComponent, GetMovementInput(), isRunning);
                    UpdateCameraPosition(translation.Value);
                }


            }).Run();

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

    private void UpdateCameraPosition(float3 playerPosition)
    {
        Vector3 cameraPosition = playerPosition;
        cameraPosition.z = -13f;
        Camera.main.transform.position = cameraPosition;
    }

    private bool ProcessCombatActions(ref CombatState combatState, ref AttackComponent attackComponent,
                                    ref AttackCooldownComponent attackCooldown, bool isAttacking,
                                    bool isDefending, float deltaTime)
    {
        // Handle attack input
        if (isAttacking && combatState.CurrentState != CombatState.State.Attacking)
        {
            combatState.CurrentState = CombatState.State.Attacking;
            return false;
        }

        // Handle defend input
        if (isDefending)
        {
            combatState.CurrentState = CombatState.State.Defending;
        }
        else if (combatState.CurrentState == CombatState.State.Defending)
        {
            combatState.CurrentState = CombatState.State.Idle;
        }
        return true;
    }

    private bool CanAttack(CombatState combatState, AttackCooldownComponent attackCooldown)
    {
        return combatState.CurrentState != CombatState.State.Attacking &&
               attackCooldown.timeRemaining <= 0f;
    }

    private void StartAttack(ref CombatState combatState,
                           ref AttackCooldownComponent attackCooldown)
    {
        combatState.CurrentState = CombatState.State.Attacking;
        attackCooldown.timeRemaining = attackCooldown.cooldownDuration;
    }

    private void ProcessMovement(ref MovementSpeedComponent playerInput, Vector2 movementInput, bool isRunning)
    {
        playerInput.velocity.x = movementInput.x;
        playerInput.velocity.y = movementInput.y;
        playerInput.isRunnning = isRunning;
    }
}
