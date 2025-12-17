using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{

    private EntityManager entityManager;
    private EntityArchetype unitArchetype;

    private Entity commanderEntity;
    private EntityArchetype commanderArchetype;
    /// <summary>
    /// Update movement speed randomizer system
    /// Set to run with .WithoutBurst() and with .Run()
    /// </summary>
    [Range(0.05f, 0.2f)]
    public float frameTimerMaxDebug;
    /// <summary>
    /// Update movement speed randomizer system
    /// Set to run with .WithoutBurst() and with .Run()
    /// </summary>
    [Range(0.1f, .75f)]
    public float movementSpeedDebug = .1f;
    [Range(1, 10000)]
    public int UnitCountToSpawn = 256;

    public EntitySpawner instance;
    public Mesh quadMesh;      // Assign your quad mesh here
    public UnityEngine.Material walkingSpriteSheetMaterial;

    public bool DrawDebugLines = false;

    public GameObject unitPrefab;  // Drag your prefab with MeshRenderer in Unity editor
    public enum Direction { Up, Down, Left, Right }
    public enum AnimationType { Idle, Run, Die, Attack, Walk, Defend, Block, TakeDamage }
    public enum UnitType { Ally, Enemy }

    public Dictionary<(UnitType, Direction, AnimationType), UnityEngine.Material[]> materialDictionary;
    [SerializeField] private SpawnConfig spawnConfig;

    private UnitFactory unitFactory;


    // Update is called once per frame
    // Now you can use this anywhere!
    private bool hasSpawnedUnits = false;

    private void Start()
    {
        //entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //unitFactory = new UnitFactory(entityManager);

        //unitFactory.SpawnCommander();
        //unitFactory.SpawnUnits(spawnConfig.UnitCountToSpawn);
    }

    private void Update()
    {
        if (hasSpawnedUnits) return; // ← CRITICAL: Don't spawn again

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (entityManager.TryGetSingleton<GameStateComponent>(out var gameState))
        {
            if (gameState.CurrentState == GameState.Playing)
            {
                unitFactory = new UnitFactory(entityManager);
                int entitiesToSpawn = spawnConfig.UnitCountToSpawn;
                unitFactory.SpawnUnits(256, UnitType.Ally, Direction.Left, OrderFactory.CreateDefendOrder(new float3(-20, 0, 0)), new float2(-20, 0), FormationType.Phalanx);
                unitFactory.SpawnUnits(256, UnitType.Ally, Direction.Left, OrderFactory.CreateDefendOrder(new float3(-10f, 0, 0)), new float2(-10, 0), FormationType.Phalanx);
                unitFactory.SpawnUnits(256, UnitType.Ally, Direction.Left, OrderFactory.CreateDefendOrder(new float3(0, 0, 0)), new float2(0, 0), FormationType.Phalanx);
                unitFactory.SpawnUnits(256, UnitType.Ally, Direction.Left, OrderFactory.CreateDefendOrder(new float3(10f, 0, 0)), new float2(10, 0), FormationType.Phalanx);
                unitFactory.SpawnUnits(256, UnitType.Ally, Direction.Left, OrderFactory.CreateDefendOrder(new float3(20f, 0, 0)), new float2(20, 0), FormationType.Phalanx);

                //enemy
                int enemyiesToSpawn = entitiesToSpawn / 4;
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(-20, 10), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(-10f, 10), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(0, 10), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(10, 10), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(20f, 10), FormationType.Horde);

                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(-20, 15), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(-10f, 15), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(0, 15), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(10, 15), FormationType.Horde);
                unitFactory.SpawnUnits(256, UnitType.Enemy, Direction.Right, OrderFactory.CreateMoveDirectionalRangeOrder(OrderType.MoveDirectionalRange, 50f, EntitySpawner.Direction.Down), new float2(20f, 15), FormationType.Horde);
                unitFactory.SpawnCommander();

                hasSpawnedUnits = true; // ← MARK AS SPAWNED
            }
        }
    }
    public static void UpdateAnimationFields(ref AnimationComponent animationComponent, Unity.Mathematics.Random? walkRandom = null, Unity.Mathematics.Random? runRandom = default)
    {



        // Depending on the animationType, set the specific frame-related values
        switch (animationComponent.AnimationType)
        {
            case EntitySpawner.AnimationType.Attack:
                animationComponent.finishAnimation = true;
                animationComponent.FrameCount = 6; // Example: 6 frames for the attack animation
                animationComponent.CurrentFrame = 0; // Start at the first frame
                animationComponent.FrameTimerMax = 0.12f; // Example: 0.2 seconds per frame
                animationComponent.FrameTimer = 0f; // Reset the frame timer
                animationComponent.animationHeightOffset = 7;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.Run:
                animationComponent.FrameCount = 6;
                animationComponent.CurrentFrame = runRandom.Value.NextInt(0, 5);
                animationComponent.FrameTimerMax = .1f;
                animationComponent.FrameTimer = 0f; // Reset the frame timer
                animationComponent.animationHeightOffset = 5;
                //animationComponent.animationWidthOffset =  horizontalMultiplier;
                break;
            default:
            case EntitySpawner.AnimationType.Idle:
                animationComponent.FrameCount = 2;
                animationComponent.CurrentFrame = 0;
                animationComponent.FrameTimerMax = .0875f;
                animationComponent.FrameTimer = 0f; // Reset the frame timer
                animationComponent.animationHeightOffset = 0;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.Walk:
                animationComponent.FrameCount = 4;
                animationComponent.CurrentFrame = walkRandom.Value.NextInt(0, 3);
                animationComponent.FrameTimerMax = 0.15f;
                animationComponent.FrameTimer = 0f;
                animationComponent.animationHeightOffset = 1;
                //animationComponent.animationWidthOffset = horizontalMultiplier;

                break;
            case EntitySpawner.AnimationType.Defend:
                animationComponent.FrameCount = 3;
                animationComponent.CurrentFrame = 0;
                animationComponent.FrameTimerMax = .1f;
                animationComponent.FrameTimer = 0f;
                animationComponent.animationHeightOffset = 2;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.Block:
                animationComponent.FrameCount = 3;
                animationComponent.CurrentFrame = 0;
                animationComponent.FrameTimerMax = .0875f;
                animationComponent.FrameTimer = 0f;
                animationComponent.animationHeightOffset = 3;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.TakeDamage:
                animationComponent.FrameCount = 3;
                animationComponent.CurrentFrame = 0;
                animationComponent.FrameTimerMax = .0875f;
                animationComponent.FrameTimer = 0f;
                animationComponent.animationHeightOffset = 6;
                //animationComponent.animationWidthOffset = horizontalMultiplier;

                break;
            case EntitySpawner.AnimationType.Die:
                animationComponent.FrameCount = 6;
                animationComponent.CurrentFrame = 0;
                animationComponent.FrameTimerMax = 0.12f;
                animationComponent.FrameTimer = 0f;
                animationComponent.animationHeightOffset = 4;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
                // Add other cases as necessary
        }
    }
}

public struct UnitPhysicsData : IComponentData
{
    public float mass;
    public float3 velocity;
    public float radius;
}


