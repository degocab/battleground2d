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
                var command = CreateCommandFromNumber(commandType, commanderTranslation.Value);


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

    private CommandData CreateCommandFromNumber(int number, float3 commanderPosition)
    {
        Debug.Log("Commander Number" + number);

        switch (number)
        {
            case 0: // Move
                return CommandFactory.CreateMoveCommand(commanderPosition, 5f, 0.5f);

            case 1: // Find target
                return CommandFactory.CreateFindTargetCommand();

            case 2: // Attack position
                return CommandFactory.CreateAttackCommand(GetMouseWorldPosition());

            case 3: // Defend
                return CommandFactory.CreateCommand(CommandType.Defend);

            case 4: // Long move
                return CommandFactory.CreateMoveCommand(commanderPosition, 10f, 1f);

            case 5: // Stop
                return CommandFactory.CreateCommand(CommandType.Idle);

            case 6: // Custom command 1
                return CommandFactory.CreateCommand(CommandType.FindTarget);

            case 7: // Custom command 2
                return CommandFactory.CreateMoveCommand(commanderPosition, 3f, 0.2f);

            case 8: // Custom command 3
                return CommandFactory.CreateAttackCommand(Entity.Null); // Attack anything

            default: // Fallback
                return CommandFactory.CreateCommand(CommandType.Idle);
        }
    }

    private float2 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return new float2(worldPos.x, worldPos.y);
    }
}
