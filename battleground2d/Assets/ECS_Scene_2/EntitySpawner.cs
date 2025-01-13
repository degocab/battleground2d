using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    private static EntitySpawner instance;

    public static EntitySpawner GetInstance()
    {
        return instance;
    }


    private EntityManager entityManager;
    private EntityArchetype unitArchetype;

    private Entity commanderEntity;
    private EntityArchetype commanderArchetype;

    public Mesh quadMesh;      // Assign your quad mesh here
    public Material[] defaultIdleDownMaterials;  // Assign the material here
    public Material[] defaultRunDownMaterials;  // Assign the material here

    public GameObject unitPrefab;  // Drag your prefab with MeshRenderer in Unity editor

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        defaultIdleDownMaterials = Resources.LoadAll<Material>("Material/Default/IdleDown");
        defaultRunDownMaterials = Resources.LoadAll<Material>("Material/Default/RunDown");

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