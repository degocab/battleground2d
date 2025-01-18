using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{

    private EntityManager entityManager;
    private EntityArchetype unitArchetype;

    private Entity commanderEntity;
    private EntityArchetype commanderArchetype;

    public float speedVar;
    public float mainSpeedVar;

    public EntitySpawner instance;
    public Mesh quadMesh;      // Assign your quad mesh here

    #region Materials
    public Material[] defaultIdleDownMaterials;  // Assign the material here
    public Material[] defaultIdleLeftMaterials;  // Assign the material here
    public Material[] defaultIdleRightMaterials;  // Assign the material here
    public Material[] defaultIdleUpMaterials;  // Assign the material here

    public Material[] defaultDieDownMaterials;  // Assign the material here
    public Material[] defaultDieLeftMaterials;  // Assign the material here
    public Material[] defaultDieRightMaterials;  // Assign the material here
    public Material[] defaultDieUpMaterials;  // Assign the material here

    public Material[] defaultRunDownMaterials;  // Assign the material here
    public Material[] defaultRunLeftMaterials;  // Assign the material here
    public Material[] defaultRunRightMaterials;  // Assign the material here
    public Material[] defaultRunUpMaterials;  // Assign the material here


    public Material[] defaultAttackDownMaterials;  // Assign the material here
    public Material[] defaultAttackLeftMaterials;  // Assign the material here
    public Material[] defaultAttackRightMaterials;  // Assign the material here
    public Material[] defaultAttackUpMaterials;  // Assign the material here


    public Material[] enemyIdleDownMaterials;  // Assign the material here
    public Material[] enemyIdleLeftMaterials;  // Assign the material here
    public Material[] enemyIdleRightMaterials;  // Assign the material here
    public Material[] enemyIdleUpMaterials;  // Assign the material here

    public Material[] enemyDieDownMaterials;  // Assign the material here
    public Material[] enemyDieLeftMaterials;  // Assign the material here
    public Material[] enemyDieRightMaterials;  // Assign the material here
    public Material[] enemyDieUpMaterials;  // Assign the material here

    public Material[] enemyRunDownMaterials;  // Assign the material here
    public Material[] enemyRunLeftMaterials;  // Assign the material here
    public Material[] enemyRunRightMaterials;  // Assign the material here
    public Material[] enemyRunUpMaterials;  // Assign the material here


    public Material[] enemyAttackDownMaterials;  // Assign the material here
    public Material[] enemyAttackLeftMaterials;  // Assign the material here
    public Material[] enemyAttackRightMaterials;  // Assign the material here
    public Material[] enemyAttackUpMaterials;  // Assign the material here 
    #endregion

    public GameObject unitPrefab;  // Drag your prefab with MeshRenderer in Unity editor
    public enum Direction { Up, Down, Left, Right }
    public enum AnimationType { Idle, Run, Die, Attack }
    public enum UnitType { Default, Enemy }

    public Dictionary<(UnitType, Direction, AnimationType), Material[]> materialDictionary;

    /// <summary>
    /// Load all animation materials into the material Dictionary
    /// </summary>
    public void LoadMaterials()
    {
        // Initialize the dictionary for Default and Enemy materials
        materialDictionary = new Dictionary<(UnitType, Direction, AnimationType), Material[]>();

        //// Default unit type materials
        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Idle)] = defaultIdleDownMaterials;
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Idle)] = defaultIdleUpMaterials;
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Idle)] = defaultIdleLeftMaterials;
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Idle)] = defaultIdleRightMaterials;

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Run)] = defaultRunDownMaterials;
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Run)] = defaultRunUpMaterials;
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Run)] = defaultRunLeftMaterials;
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Run)] = defaultRunRightMaterials;

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Die)] = defaultDieDownMaterials;
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Die)] = defaultDieUpMaterials;
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Die)] = defaultDieLeftMaterials;
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Die)] = defaultDieRightMaterials;

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Attack)] = defaultAttackDownMaterials;
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Attack)] = defaultAttackUpMaterials;
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Attack)] = defaultAttackLeftMaterials;
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Attack)] = defaultAttackRightMaterials;

        //// Enemy unit type materials
        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Idle)] = enemyIdleDownMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Idle)] = enemyIdleUpMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Idle)] = enemyIdleLeftMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Idle)] = enemyIdleRightMaterials;

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Run)] = enemyRunDownMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Run)] = enemyRunUpMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Run)] = enemyRunLeftMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Run)] = enemyRunRightMaterials;

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Die)] = enemyDieDownMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Die)] = enemyDieUpMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Die)] = enemyDieLeftMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Die)] = enemyDieRightMaterials;

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Attack)] = enemyAttackDownMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Attack)] = enemyAttackUpMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Attack)] = enemyAttackLeftMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Attack)] = enemyAttackRightMaterials;

    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        LoadMaterials();

        Entity unitEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(unitPrefab, GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null));
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //define unit archetype
        unitArchetype = entityManager.CreateArchetype(
            typeof(PositionComponent),
            typeof(VelocityComponent),
            typeof(MovementSpeedComponent),
            typeof(HealthComponent),
            typeof(AttackComponent),
            typeof(AttackCooldownComponent),
            typeof(TargetComponent),
            typeof(AnimationComponent),
            typeof(IsDeadComponent),
            typeof(UnitMaterialComponent),
            typeof(Translation) 
            );


        //define commander archetype
        commanderArchetype = entityManager.CreateArchetype(
            typeof(PositionComponent),
            typeof(VelocityComponent),
            typeof(MovementSpeedComponent),
            typeof(HealthComponent),
            typeof(AttackComponent),
            typeof(AttackCooldownComponent),
            typeof(CommanderComponent),
            typeof(AnimationComponent),
            typeof(PlayerInputComponent),
            typeof(UnitMaterialComponent),
            typeof(Translation)
            );

        //SpawnCommander(unitEntityPrefab);

        SpawnUnits(10000, unitEntityPrefab);
    }

    private void SpawnUnits(int count, Entity unitEntityPrefab)
    {
        for (int i = 0; i < count; i++)
        {
            Entity unit = entityManager.CreateEntity(unitArchetype);
            float x = i % 4 * 2f;
            float y = i / 4 * 2f;
            entityManager.SetComponentData(unit, new PositionComponent { value = new float3(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-20f, 20f), 0) });
            entityManager.SetComponentData(unit, new HealthComponent { health = 100f, maxHealth = 100f });
            entityManager.SetComponentData(unit, new MovementSpeedComponent { value = 3f });
            entityManager.SetComponentData(unit, new AttackComponent { damage = 10f, range = 1f });
            entityManager.SetComponentData(unit, new AttackCooldownComponent { cooldownDuration = .525f, timeRemaining = 0f });
            entityManager.SetComponentData(unit,
                new AnimationComponent
                {
                    currentFrame = UnityEngine.Random.Range(0, 5),
                    frameCount = 6,
                    frameTimer = UnityEngine.Random.Range(0f, 1f),
                    frameTimerMax = .1f,

                    unitType = UnitType.Enemy,
                    direction = Direction.Right,
                    animationType = AnimationType.Run,
                    prevAnimationType = AnimationType.Run,
                    finishAnimation = false
                }
            );
        }
    }
    int GetMaterialIndex(string unitType)
    {
        switch (unitType)
        {
            case "Infantry":
                return 0;
            case "Cavalry":
                return 1;
            case "Archer":
                return 2;
            default:
            case "":
                return 0; // Default to Infantry if not found
        }
    }
    private void SpawnCommander(Entity unitEntityPrefab)
    {
        commanderEntity = entityManager.CreateEntity(commanderArchetype);

        //set intial data
        entityManager.SetComponentData(commanderEntity, new PositionComponent { value = new float3(0f, 0f, 0f) });
        entityManager.SetComponentData(commanderEntity, new VelocityComponent { velocity = new float3(0f, 0f, 0f) });
        entityManager.SetComponentData(commanderEntity, new HealthComponent { health = 100f, maxHealth = 100f });
        entityManager.SetComponentData(commanderEntity, new MovementSpeedComponent { value = 5f });
        entityManager.SetComponentData(commanderEntity, new CommanderComponent { isPlayerControlled = true });
        entityManager.SetComponentData(commanderEntity,
            new AnimationComponent
            {
                currentFrame = UnityEngine.Random.Range(0, 1),
                frameCount = 2,
                frameTimer = UnityEngine.Random.Range(0f, 1f),
                frameTimerMax = .1f
            }
        );

        // Set the material index based on the unit type
        int materialIndex = GetMaterialIndex("");
        entityManager.SetComponentData(commanderEntity, new UnitMaterialComponent { materialIndex = materialIndex });

    }
    // Update is called once per frame
    void Update()
    {

    }
}