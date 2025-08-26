using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
//[UpdateBefore(typeof(GridSystem))]
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
            Camera.main.orthographicSize = 10f;
        else
            Camera.main.orthographicSize = 4f;
        var time = Time.DeltaTime;


        Vector3 newCamPos = new Vector3();
        Entities.ForEach((ref PlayerInputComponent playerInputComponent, ref Translation translation) =>
        {
            newCamPos = translation.Value;
            newCamPos.z = -13f;
            cameraMain.position = newCamPos;
        }).WithoutBurst().Run();

    }

    private CommandData CreateCommandFromNumber(int number, float3 commanderPosition, float2 moveToPosition)
    {
        CommandData comm = new CommandData();
        switch (number)
        {
            case 0: // Move
                comm =  CommandFactory.CreateChargeCommand();
                break;

            case 1: // Find target
                comm =  CommandFactory.CreateMarchCommand();
                break;

            case 2: // Attack position
                comm =  CommandFactory.CreateAttackCommand(moveToPosition);
                break;

            case 3: // Defend
                comm =  CommandFactory.CreateCommand(CommandType.Defend);
                break;

            case 4: // Long move
                comm =  CommandFactory.CreateMoveCommand( moveToPosition);
                break;

            case 5: // Stop
                comm =  CommandFactory.CreateCommand(CommandType.Idle);
                break;

            case 6: // Custom command 1
                comm = CommandFactory.CreateFindTargetCommand();
                break;

            case 7: // Custom command 2
                comm =  CommandFactory.CreateMoveCommand(moveToPosition);
                break;

            case 8: // Custom command 3
                Debug.Log("create find comand");
                comm =  CommandFactory.CreateFindTargetCommand(); // Attack anything
                break;

            default: // Fallback
                comm =  CommandFactory.CreateCommand(CommandType.Idle);
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
}
