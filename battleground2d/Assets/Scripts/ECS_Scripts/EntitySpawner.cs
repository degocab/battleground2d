using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    [Header("Debug")]
    public bool EnableDebugDrawing = true;
    private Entity _debugSettingsEntity;

    private EntityManager _entityManager;
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

    public static EntitySpawner Instance { get; set; }
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
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    private void Start()
    {
        //_entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //unitFactory = new UnitFactory(_entityManager);

        //unitFactory.SpawnCommander();
        //unitFactory.SpawnUnits(spawnConfig.UnitCountToSpawn);
        World world = World.DefaultGameObjectInjectionWorld;
        _entityManager = world.EntityManager;

        _debugSettingsEntity = _entityManager.CreateEntity(typeof(DebugSettings));

        UpdateDebugSettings();
    }

    private void UpdateDebugSettings()
    {
        if (!_entityManager.Exists(_debugSettingsEntity))
            return;

        _entityManager.SetComponentData(
            _debugSettingsEntity,
            new DebugSettings
            {
                EnableDebug = EnableDebugDrawing
            });
    }

    private void Update()
    {
        if (hasSpawnedUnits) return; 

        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (_entityManager.TryGetSingleton<GameStateComponent>(out var gameState))
        {
            if (gameState.CurrentState == GameState.Playing)
            {
                unitFactory = new UnitFactory(_entityManager);
                int entitiesToSpawn = math.clamp(spawnConfig.UnitCountToSpawn, 1, 256);

                // --- ALLIES ---
                // Add more rows here whenever you want (e.g., new[] {0f, -8f, -16f})
                float[] allyRowsY = { -2f/*, -5f */};
                Entity playerCommand = SpawnAllyPhalanxRows(unitFactory, entitiesToSpawn, allyRowsY);

                // --- ENEMIES ---
                float enemyMoveRange = 50f;
                // Add more rows by just appending values
                float[] enemyRowsY = { 10f/*, 15f, 20f, 25f*/ };
                SpawnEnemyHordeRows(unitFactory, entitiesToSpawn, enemyMoveRange, enemyRowsY);

                Entity playerCommander = unitFactory.SpawnCommander();
                var command = _entityManager.GetComponentData<CommandComponent>(playerCommand);
                command.AwarenessOrigin = playerCommander;
                _entityManager.SetComponentData(playerCommand, command);

                hasSpawnedUnits = true;
            }
        }

    }

    private Entity SpawnAllyPhalanxRows(UnitFactory factory, int unitsPerFormation, float[] rowsY)
    {
        // X positions for ally phalanx columns
        float[] allyXs = { -12f  -6f, 0f, 6f, 12f, 18f, 24f, 30f, 36f, 42f, 48f, 54f, 60f, 66f};
        Entity cmd = _entityManager.CreateEntity();
        _entityManager.AddComponentData(cmd, new CommandComponent
        {
            FactionType = UnitType.Ally,
            CommandID = 0 // Left
        });
        _entityManager.AddComponentData(cmd, new CommandPerception
        {
            IntelVersion = 0,
            Control = 0f,
            Intensity01 = 0f,
            Momentum = 0f,
            PrevControl = 0f,
            Pressure = CommandPressureState.Stable
        });

        _entityManager.AddBuffer<OwnedFormationGroup>(cmd);
        AddCommandAwarenessComponents(cmd);
        // Collect first (safe across structural changes)
        var spawnedGroups = new List<Entity>(rowsY.Length * allyXs.Length);

        foreach (var rowY in rowsY)
        {
            foreach (var x in allyXs)
            {
                var position2D = new float2(x, rowY);
                var defendOrder = OrderFactory.CreateDefendOrder(new float3(x, rowY, 0));

                var formationGroupEntity = factory.SpawnUnits(
                    unitsPerFormation,
                    UnitType.Ally,
                    Direction.Left,
                    defendOrder,
                    position2D,
                    FormationType.Phalanx);

                spawnedGroups.Add(formationGroupEntity);
            }
        }

        // Now get the buffer AFTER spawns (no structural changes after this point)
        var buffer = _entityManager.GetBuffer<OwnedFormationGroup>(cmd);
        for (int i = 0; i < spawnedGroups.Count; i++)
            buffer.Add(new OwnedFormationGroup { Value = spawnedGroups[i] });
        return cmd;
    }

    private void SpawnEnemyHordeRows(UnitFactory factory, int unitsPerFormation, float enemyMoveRange, float[] rowsY)
    {
        float[] colsX = { -12f -6f, 0f, 6f, 12f, 18f, 24f, 30f, 36f, 42f, 48f, 54f, 60f, 66f };

        Entity cmd = _entityManager.CreateEntity();

        _entityManager.AddComponentData(cmd, new CommandComponent
        {
            FactionType = UnitType.Enemy,
            CommandID = 0
        });        _entityManager.AddComponentData(cmd, new Translation
        {
            Value = new float3(0, 0, 0)
        });

        _entityManager.AddComponentData(cmd, new CommandPerception
        {
            IntelVersion = 0,
            Control = 0f,
            Intensity01 = 0f,
            Momentum = 0f,
            PrevControl = 0f,
            Pressure = CommandPressureState.Stable
        });

        _entityManager.AddBuffer<OwnedFormationGroup>(cmd);
        AddCommandAwarenessComponents(cmd);

        var spawnedGroups = new List<Entity>(rowsY.Length * colsX.Length);

        foreach (var rowY in rowsY)
        {
            foreach (var x in colsX)
            {
                var position2D = new float2(x, rowY);
                var moveOrder = OrderFactory.CreateMoveDirectionalRangeOrder(
                    OrderType.MoveDirectionalRange,
                    enemyMoveRange,
                    Direction.Down);

                var formationGroupEntity = factory.SpawnUnits(
                    unitsPerFormation,
                    UnitType.Enemy,
                    Direction.Right,
                    moveOrder,
                    position2D,
                    FormationType.Horde);

                spawnedGroups.Add(formationGroupEntity);
            }
        }

        var buffer = _entityManager.GetBuffer<OwnedFormationGroup>(cmd);
        for (int i = 0; i < spawnedGroups.Count; i++)
            buffer.Add(new OwnedFormationGroup { Value = spawnedGroups[i] });
    }

    private void AddCommandAwarenessComponents(Entity command)
    {
        _entityManager.AddComponentData(command, new CommandAwarenessConfig
        {
            ObservationRadius = 14f,
            MemoryDuration = 10f
        });
        _entityManager.AddComponentData(command, new CommandAwareness());
        _entityManager.AddBuffer<CommandKnownSlice>(command);
        _entityManager.AddBuffer<CommandKnownFormation>(command);
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


