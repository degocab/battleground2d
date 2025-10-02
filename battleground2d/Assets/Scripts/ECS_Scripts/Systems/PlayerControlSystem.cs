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
                    .WithName("ApplyMouseCommandCleanHastarget")
                    .WithAll<Unit>()
                    .WithAll<HasTarget>()
                    .WithNone<CommanderComponent>()
                    .ForEach((Entity entity, int entityInQueryIndex) =>
                    {
                        parallelEcb.RemoveComponent<HasTarget>(entityInQueryIndex, entity);
                    }).ScheduleParallel();

                Entities
                    .WithName("ApplyMouseCommand")
                    .WithAll<Unit>()
                    .WithAll<CommandData>()
                    .WithNone<CommanderComponent>()
                    .ForEach((Entity entity, int entityInQueryIndex, ref CommandData commandData) =>
                    {
                        commandData = command;
                        //parallelEcb.SetComponent(entityInQueryIndex, entity, command);
                    }).ScheduleParallel();



                Debug.Log($"Assigned command: {command.Command} to all units");
            }
        }

        float deltaTime = Time.DeltaTime;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isAttacking = Input.GetMouseButtonDown(0);
        bool isDefending = Input.GetMouseButton(1);
        Debug.Log($"Is Defending: {isDefending}");


        UpdateCameraZoom();
        float currentTime = (float)Time.ElapsedTime;
        Entities
            .WithoutBurst()
            .ForEach((
                ref PlayerInputComponent playerInput,
                ref Translation translation,
                ref CombatState combatState,
                ref AttackComponent attackComponent,
                ref AttackCooldownComponent attackCooldown,
                ref AnimationComponent animationComponent,
                ref MovementSpeedComponent movementSpeedComponent,
                ref DefenseComponent defenseComponent
            ) =>
            {

                if (attackComponent.isTakingDamage) return;


                attackComponent.isAttacking = false;
                attackComponent.isDefending = false;
                defenseComponent.IsBlocking = false;


                // Step 1: Reduce only attack rate cooldown (we don't touch animation cooldown)
                if (attackComponent.AttackRateRemaining > 0f)
                {
                    attackComponent.AttackRateRemaining -= deltaTime;
                }
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
                attackComponent.isAttacking = false;
                attackComponent.isDefending = false;
                //defenseComponent.IsBlocking = false;


                bool blocking = isStillBlocking(defenseComponent);

                // Step 4: Handle state transitions
                if (isAttacking)
                {
                    if (canAttack)
                    {
                        PerformAttack(ref combatState, ref attackComponent, ref animationComponent);
                        StartAttack(ref combatState, ref attackCooldown); // animation system will handle timeRemaining now
                        attackComponent.isAttacking = true; // SET FLAG
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
                    attackComponent.isDefending = true;
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


                ProcessMovement(ref movementSpeedComponent, GetMovementInput(), isRunning);
                UpdateCameraPosition(translation.Value);

            }).Run();

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

    private void UpdateCameraPosition(float3 playerPosition)
    {
        Vector3 cameraPosition = playerPosition;
        cameraPosition.z = -13f;
        Camera.main.transform.position = cameraPosition;
    }

    private void StartAttack(ref CombatState combatState,
                           ref AttackCooldownComponent attackCooldown)
    {
        combatState.CurrentState = CombatState.State.Attacking;
        attackCooldown.attackCoolTimeRemaining = attackCooldown.attackCoolDownDuration;
        Debug.Log("Attack Started");
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
        attackComponent.isAttacking = true;
        animationComponent.finishAnimation = true;
        animationComponent.AnimationType = EntitySpawner.AnimationType.Attack;

        Debug.Log("Player attacked!");
    }

    private void SetToIdle(ref CombatState combatState, ref AnimationComponent animationComponent)
    {
        combatState.CurrentState = CombatState.State.Idle;
        animationComponent.AnimationType = EntitySpawner.AnimationType.Idle;
        Debug.Log("Player is idle");

    }
}



