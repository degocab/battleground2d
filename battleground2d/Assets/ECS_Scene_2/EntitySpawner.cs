using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    public EntitySpawner instanceMain;
    public EntitySpawner instance;

    public  EntitySpawner GetInstance()
    {
        //if (instance == null)
        //{
        //    GameObject go = new GameObject();
        //    instance = instanceMain;
        //}
        return instanceMain;
    }


    private EntityManager entityManager;
    private EntityArchetype unitArchetype;

    private Entity commanderEntity;
    private EntityArchetype commanderArchetype;

    public Mesh quadMesh;      // Assign your quad mesh here
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

    public GameObject unitPrefab;  // Drag your prefab with MeshRenderer in Unity editor

    string GetPathForUnitType(string UnitType)
    {
        return "Material/" + UnitType;
    }

    public enum Direction { Up, Down, Left, Right}
    public enum AnimationType { Idle, Run, Die, Attack }
    public enum UnitType { Default, Enemy }

    public Dictionary<(UnitType, Direction, AnimationType), Material[]> materialDictionary; 
    //    new KeyValuePair<(UnitType, Direction, AnimationType), Material[]>((UnitType.Enemy, Direction.Down, AnimationType.Attack), enemyAttackDownMaterials), 
    ////    materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Attack)] = enemyAttackDownMaterials;
    ////materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Attack)] = enemyAttackUpMaterials;
    ////    materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Attack)] = enemyAttackLeftMaterials;
    ////        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Attack)] = enemyAttackRightMaterials;
    //};


    //public  Dictionary<(UnitType, Direction, AnimationType), Material[]> MaterialDictionary
    //{
    //    get
    //    {
    //        if (materialDictionary == null)
    //        {
    //            LoadMaterials();
    //        }
    //        return materialDictionary;
    //    }
    //}
        
        



    // Load all materials into the dictionary
    public  void LoadMaterials()
    {
            // Initialize the dictionary for Default and Enemy materials
            materialDictionary = new Dictionary<(UnitType, Direction, AnimationType), Material[]>();

            //// Default unit type materials
            //materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Default/IdleDown");
            //materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Default/IdleUp");
            //materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Default/IdleLeft");
            //materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Default/IdleRight");

            //materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Default/RunDown");
            //materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Default/RunUp");
            //materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Default/RunLeft");
            //materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Default/RunRight");

            //materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Default/DieDown");
            //materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Default/DieUp");
            //materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Default/DieLeft");
            //materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Default/DieRight");

            //materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Attack)] = Resources.LoadAll<Material>("Material/Default/AttackDown");
            //materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Attack)] = Resources.LoadAll<Material>("Material/Default/AttackUp");
            //materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Attack)] = Resources.LoadAll<Material>("Material/Default/AttackLeft");
            //materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Attack)] = Resources.LoadAll<Material>("Material/Default/AttackRight");

            //// Enemy unit type materials
            //materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Enemy/IdleDown");
            //materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Enemy/IdleUp");
            //materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Enemy/IdleLeft");
            //materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Idle)] = Resources.LoadAll<Material>("Material/Enemy/IdleRight");

            //materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Enemy/RunDown");
            //materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Enemy/RunUp");
            //materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Enemy/RunLeft");
            //materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Run)] = Resources.LoadAll<Material>("Material/Enemy/RunRight");

            //materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Enemy/DieDown");
            //materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Enemy/DieUp");
            //materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Enemy/DieLeft");
            //materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Die)] = Resources.LoadAll<Material>("Material/Enemy/DieRight");

            materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Attack)] = enemyAttackDownMaterials;
            materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Attack)] = enemyAttackUpMaterials;
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Attack)] = enemyAttackLeftMaterials;
            materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Attack)] = enemyAttackRightMaterials;

    }

    // Retrieve materials based on unit type, direction, and animation type

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
            typeof(CommanderComponent),
            typeof(AnimationComponent),
            typeof(PlayerInputComponent),
            typeof(UnitMaterialComponent),
            typeof(Translation)
            );

        SpawnCommander(unitEntityPrefab);

        SpawnUnits(10000, unitEntityPrefab);
    }

    private void SpawnUnits(int count, Entity unitEntityPrefab)
    {
        for (int i = 0; i < count; i++)
        {
            Entity unit = entityManager.CreateEntity(unitArchetype);
            float x = i % 4 * 2f;
            float y = i / 4 * 2f;
            //entityManager.SetComponentData(unit, new PositionComponent { position = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-2.5f, 2.5f), 0) });
            entityManager.SetComponentData(unit, new PositionComponent { position = new float3((i * .25f) + ((i %100 == 0) ? -5f : 0f), i * .25f + ((i % 100 == 0) ? -5f : 0f), 0) });
            entityManager.SetComponentData(unit, new HealthComponent { health = 100f, maxHealth = 100f });
            entityManager.SetComponentData(unit, new MovementSpeedComponent { speed = 3f });
            entityManager.SetComponentData(unit,
                new AnimationComponent
                {
                    currentFrame = UnityEngine.Random.Range(0, 5),
                    frameCount = 6,
                    frameTimer = UnityEngine.Random.Range(0f, 1f),
                    frameTimerMax = .1f
                }
            );
            entityManager.SetComponentData(unit,
                new Translation
                {
                    Value = new float3(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-30f, 30f), 0)
                });
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
        entityManager.SetComponentData(commanderEntity, new PositionComponent { position = new float3(0f, 0f, 0f) });
        entityManager.SetComponentData(commanderEntity, new VelocityComponent { velocity = new float3(0f, 0f, 0f) });
        entityManager.SetComponentData(commanderEntity, new HealthComponent { health = 100f, maxHealth = 100f });
        entityManager.SetComponentData(commanderEntity, new MovementSpeedComponent { speed = 5f });
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