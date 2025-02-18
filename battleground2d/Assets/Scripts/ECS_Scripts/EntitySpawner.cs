using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.UI.CanvasScaler;

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
    public UnityEngine.Material walkingSpriteSheetMaterial;  // Drag your prefab with MeshRenderer in Unity editor


    #region Materials
    public UnityEngine.Material[] defaultIdleDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultIdleLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultIdleRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultIdleUpMaterials;  // Assign the UnityEngine.Material here

    public UnityEngine.Material[] defaultDieDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultDieLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultDieRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultDieUpMaterials;  // Assign the UnityEngine.Material here

    public UnityEngine.Material[] defaultRunDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultRunLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultRunRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultRunUpMaterials;  // Assign the UnityEngine.Material here


    public UnityEngine.Material[] defaultAttackDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultAttackLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultAttackRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] defaultAttackUpMaterials;  // Assign the UnityEngine.Material here


    public UnityEngine.Material[] enemyIdleDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyIdleLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyIdleRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyIdleUpMaterials;  // Assign the UnityEngine.Material here

    public UnityEngine.Material[] enemyDieDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyDieLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyDieRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyDieUpMaterials;  // Assign the UnityEngine.Material here

    public UnityEngine.Material[] enemyRunDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyRunLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyRunRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyRunUpMaterials;  // Assign the UnityEngine.Material here


    public UnityEngine.Material[] enemyAttackDownMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyAttackLeftMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyAttackRightMaterials;  // Assign the UnityEngine.Material here
    public UnityEngine.Material[] enemyAttackUpMaterials;  // Assign the UnityEngine.Material here 
    #endregion

    public GameObject unitPrefab;  // Drag your prefab with MeshRenderer in Unity editor
    public enum Direction { Up, Down, Left, Right }
    public enum AnimationType { Idle, Run, Die, Attack, Walk, Defend, Block, TakeDamage }
    public enum UnitType { Default, Enemy }

    public Dictionary<(UnitType, Direction, AnimationType), UnityEngine.Material[]> materialDictionary;

    /// <summary>
    /// Load all animation materials into the material Dictionary
    /// </summary>
    public void LoadMaterials()
    {
        // Initialize the dictionary for Default and Enemy materials
        materialDictionary = new Dictionary<(UnitType, Direction, AnimationType), UnityEngine.Material[]>();

        //// Default unit type materials
        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleDown"); //defaultIdleDownMaterials;
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleUp"); //defaultIdleUpMaterials;
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleLeft"); //defaultIdleLeftMaterials;
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleRight"); //defaultIdleRightMaterials;

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunLeft"); //defaultRunLeftMaterials;
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunRight"); //defaultRunRightMaterials;

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieLeft");
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieRight");

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackLeft");
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackRight");

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkLeft");
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkRight");

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendLeft");
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendRight");

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockLeft");
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockRight");

        materialDictionary[(UnitType.Default, Direction.Down, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageDown");
        materialDictionary[(UnitType.Default, Direction.Up, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageUp");
        materialDictionary[(UnitType.Default, Direction.Left, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageLeft");
        materialDictionary[(UnitType.Default, Direction.Right, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageRight");


        //// Enemy unit type materials
        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockRight");

        materialDictionary[(UnitType.Enemy, Direction.Down, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageDown");
        materialDictionary[(UnitType.Enemy, Direction.Up, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageUp");
        materialDictionary[(UnitType.Enemy, Direction.Left, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageLeft");
        materialDictionary[(UnitType.Enemy, Direction.Right, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageRight");

    }
    UnityEngine.Material[] LoadMaterialArray(string path)
    {
        return Resources.LoadAll<UnityEngine.Material>(path);
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        LoadMaterials();

        //Entity unitEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(unitPrefab, GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null));
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
            typeof(TargetPositionComponent),
            typeof(AnimationComponent),
            typeof(IsDeadComponent),
            typeof(UnitMaterialComponent),
            typeof(Translation),
            typeof(Unit),
            typeof(GridID),
            typeof(CollisionBounds),
                        typeof(PhysicsPosition),    // Position component
            typeof(PhysicsVelocity),    // Velocity component
            typeof(PhysicsForce),       // Force component
            typeof(PhysicsRadius)       // Radius componen
            , typeof(PhysicsColliderComponent),   // Collider for physics
            typeof(HitDetectionComponent)      // Hit detection component
            );


        //define commander archetype
        commanderArchetype = entityManager.CreateArchetype(
            typeof(CommanderComponent),
            typeof(PlayerInputComponent),
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
            typeof(Translation),
            typeof(Unit),
            typeof(GridID),
            typeof(CollisionBounds),
                        typeof(PhysicsPosition),    // Position component
            typeof(PhysicsVelocity),    // Velocity component
            typeof(PhysicsForce),       // Force component
            typeof(PhysicsRadius)       // Radius componen
                        , typeof(PhysicsColliderComponent),   // Collider for physics
            typeof(HitDetectionComponent)      // Hit detection component
            );

        SpawnCommander();

        SpawnUnits(UnitCountToSpawn);
    }

    int phalanxSize = 100; // 10x10 formation
    float unitSpacing = 0.25f; // Spacing between units within a phalanx
    float formationSpacing = 4f; // Space between phalanxes

    int totalUnits = 10000; // Total number of units
    int unitsPerPhalanx = 1000; // 100 units per phalanx (10x10 grid)
    int numPhalanxes = 100; // Number of phalanxes (100)

    private void SpawnUnits(int count)
    {



        //phalanxCap

        //[0,0] starting x,y - first unit
        //[.25,0] second unit x,y
        //[.5,0] third unit x,y
        //....
        //[25,25] 1000th unit x,y

        // next phalanx
        // gap of 4f vertically (final unit y coordinate = 25) + 4f spacing = 29
        //start of next phalanx coordinate
        //[0,29] first unit of second phalanx x,y
        //[0,29.25] second unit of second phalanx x,y
        //.....
        //[25,50] 1000th unit of second phalanx x,y

        // next phalanx
        // gap of 4f vertically (final unit y coordinate = 50) + 4f spacing = 54
        // start of next phalanx coordinate
        //[0,54] first unit of third phalanx x,y
        //[0,54.25] second unit of third phalanx x,y
        //.....
        //[25,75] 1000th unit of third phalanx x,y
        //etc...

        totalUnits = count;
        int unitsPerPhalanx = 256; // Number of units in each phalanx (e.g., 16x16 grid)
        float unitSpacing = 0.25f; // Spacing between units within a phalanx
        float phalanxSpacing = 1f; // Vertical spacing between each phalanx

        // Determine how many full phalanxes we have and the remainder
        int numFullPhalanxes = totalUnits / unitsPerPhalanx;
        int remainingUnits = totalUnits % unitsPerPhalanx;

        // Create the array for phalanxes, storing the number of units per phalanx
        int[] phalanxSizes = GeneratePhalanxSizes(numFullPhalanxes, remainingUnits, unitsPerPhalanx);

        // Track vertical position for spawning units
        float yTracker = 0f;

        // Spawn the units in phalanx formation
        foreach (int phalanxSize in phalanxSizes)
        {


            // Calculate the dimensions of the current phalanx based on the number of units
            int sqrtSize = Mathf.CeilToInt(Mathf.Sqrt(phalanxSize));

            // Spawn the units in a grid within the phalanx
            SpawnPhalanxUnits(sqrtSize, unitSpacing, yTracker, phalanxSize);
            // Update the Y position to account for vertical spacing between phalanxes
            yTracker += Mathf.CeilToInt(Mathf.Sqrt(phalanxSize)) * unitSpacing + phalanxSpacing; // Apply vertical gap
        }

    }


    /// <summary>
    /// Generate the sizes of each phalanx, considering full phalanxes and the remaining units for the last group
    /// </summary>
    /// <param name="numFullPhalanxes"></param>
    /// <param name="remainingUnits"></param>
    /// <param name="unitsPerPhalanx"></param>
    /// <returns></returns>
    private int[] GeneratePhalanxSizes(int numFullPhalanxes, int remainingUnits, int unitsPerPhalanx)
    {
        int[] phalanxSizes = new int[numFullPhalanxes + (remainingUnits > 0 ? 1 : 0)];

        for (int i = 0; i < numFullPhalanxes; i++)
        {
            phalanxSizes[i] = unitsPerPhalanx; // Fill full phalanxes
        }

        if (remainingUnits > 0)
        {
            phalanxSizes[numFullPhalanxes] = remainingUnits; // Fill the last group with remaining units
        }

        return phalanxSizes;
    }

    /// <summary>
    /// Spawn units in a given phalanx based on its size (calculated dimensions of the grid)
    /// </summary>
    /// <param name="sqrtSize"></param>
    /// <param name="unitSpacing"></param>
    /// <param name="yTracker"></param>
    /// <param name="phalanxSize"></param>
    private void SpawnPhalanxUnits(int sqrtSize, float unitSpacing, float yTracker, int phalanxSize)
    {

        // For each unit in the phalanx (calculated by sqrtSize to make it a square-like formation)
        for (int row = 0; row < sqrtSize; row++)
        {
            for (int col = 0; col < sqrtSize; col++)
            {
                // Ensure we don't spawn more units than we have
                if (row * sqrtSize + col >= phalanxSize)
                    return;

                // Calculate the unit's position
                float xCoord = col * unitSpacing;
                float yCoord = yTracker + row * unitSpacing;

                // Set the position of the unit
                float3 unitPosition = new float3(xCoord, yCoord, 0);

                // Create the entity for the unit and set its position
                Entity unit = entityManager.CreateEntity(unitArchetype);
                entityManager.SetComponentData(unit, new Translation { Value = unitPosition });
                entityManager.SetComponentData(unit, new PositionComponent { value = unitPosition });
                UnitType unitType;

                // Set common components for each unit
                entityManager.SetComponentData(unit, new PhysicsPosition { Value = unitPosition });
                entityManager.SetComponentData(unit, new PhysicsVelocity { Value = new float3(0, 0, 0) });
                entityManager.SetComponentData(unit, new PhysicsForce { Value = new float3(0, 0, 0) });
                entityManager.SetComponentData(unit, new PhysicsRadius { Value = 0.25f }); // Set radius for collision


                ///
                /// randomoing range to test unit materials
                ///
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                {
                    unitType = UnitType.Enemy;
                }
                else
                {
                    unitType = UnitType.Default;
                }

                // Set additional unit components such as Health, Movement, etc.
                SetUnitComponents(unit, unitPosition, unitType);
            }
        }
    }



    private void SetUnitComponents(Entity unit, float3 unitPosition, UnitType unitType)
    {
        entityManager.SetComponentData(unit, new HealthComponent { health = 100f, maxHealth = 100f });
        entityManager.SetComponentData(unit, new MovementSpeedComponent { value = 3f, isBlocked = false, isKnockedBack = false });
        entityManager.SetComponentData(unit, new AttackComponent { damage = 10f, range = 1f, isAttacking = false, isDefending = false });
        entityManager.SetComponentData(unit, new AttackCooldownComponent { cooldownDuration = .525f, timeRemaining = 0f, takeDamageCooldownDuration = .225f });
        entityManager.SetComponentData(unit, new TargetPositionComponent { targetPosition = new float3(unitPosition.x + 2f, unitPosition.y, 0f) });
        entityManager.SetComponentData(unit, new Unit { isMounted = false });
        entityManager.SetComponentData(unit, new GridID { value = 0 });
        entityManager.SetComponentData(unit, new CollisionBounds { radius = .25f });
        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = unitPosition,  // Center of the sphere collider (relative to the entity's position)
            Radius = 0.25f         // Radius of the sphere collider
        };

        // Create the PhysicsCollider component with the SphereGeometry
        PhysicsCollider physicsCollider = new PhysicsCollider
        {
            Value = Unity.Physics.SphereCollider.Create(sphereGeometry)  // Create the actual collider
        };
        var t = new Unity.Physics.SphereCollider();
        t.Geometry = sphereGeometry;
        entityManager.SetComponentData(unit, new PhysicsColliderComponent { Collider = t });

        // Initialize hit detection (for example, set radius)
        entityManager.SetComponentData(unit, new HitDetectionComponent { Radius = 0.5f });


        entityManager.SetComponentData(unit,
            new AnimationComponent
            {
                frameCount = 2,
                currentFrame = 0,
                frameTimerMax = .0875f,
                frameTimer = 0f,
                animationHeightOffset = 0,
                animationWidthOffset = 1,
                unitType = unitType,
                direction = Direction.Right,
                animationType = AnimationType.Idle,
                prevAnimationType = AnimationType.Idle,
                finishAnimation = false
            });

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
    private void SpawnCommander()
    {
        commanderEntity = entityManager.CreateEntity(commanderArchetype);
        float3 pos = new float3 (-2f,0f,0f );
        entityManager.SetComponentData(commanderEntity, new Translation { Value = pos });
        entityManager.SetComponentData(commanderEntity, new PositionComponent { value = pos });

        entityManager.SetComponentData(commanderEntity, new PhysicsPosition { Value = pos });
        entityManager.SetComponentData(commanderEntity, new PhysicsVelocity { Value = new float3(0, 0, 0) });
        entityManager.SetComponentData(commanderEntity, new PhysicsForce { Value = new float3(0, 0, 0) });
        entityManager.SetComponentData(commanderEntity, new PhysicsRadius { Value = 0.5f }); // Set radius for collision

        entityManager.SetComponentData(commanderEntity, new HealthComponent { health = 10000f, maxHealth = 10000f });
        entityManager.SetComponentData(commanderEntity, new MovementSpeedComponent { value = 3f, isBlocked = false, isKnockedBack = false });
        entityManager.SetComponentData(commanderEntity, new AttackComponent { damage = 10f, range = 1f, isAttacking = false, isDefending = false });
        entityManager.SetComponentData(commanderEntity, new AttackCooldownComponent { cooldownDuration = .525f, timeRemaining = 0f, takeDamageCooldownDuration = .225f });
        entityManager.SetComponentData(commanderEntity, new CommanderComponent { isPlayerControlled = true });
        entityManager.SetComponentData(commanderEntity, new Unit { isMounted = false });
        entityManager.SetComponentData(commanderEntity, new GridID { value = 0 });
        entityManager.SetComponentData(commanderEntity, new CollisionBounds { radius = .25f });


        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = pos,  // Center of the sphere collider (relative to the entity's position)
            Radius = 0.25f         // Radius of the sphere collider
        };

        // Create the PhysicsCollider component with the SphereGeometry
        PhysicsCollider physicsCollider = new PhysicsCollider
        {
            Value = Unity.Physics.SphereCollider.Create(sphereGeometry)  // Create the actual collider
        };
        var t = new Unity.Physics.SphereCollider();
        t.Geometry = sphereGeometry;
        entityManager.SetComponentData(commanderEntity, new PhysicsColliderComponent { Collider = t });


        entityManager.SetComponentData(commanderEntity,
            new AnimationComponent
            {
                currentFrame = UnityEngine.Random.Range(0, 5),
                frameCount = 2,
                frameTimer = UnityEngine.Random.Range(0f, 1f),
                frameTimerMax = .1f,
                animationHeightOffset = 0,
                animationWidthOffset = 1,
                unitType = UnitType.Default,
                direction = Direction.Right,
                animationType = AnimationType.Idle,
                prevAnimationType = AnimationType.Idle,
                finishAnimation = false
            }
        );

    }
    // Update is called once per frame
    void Update()
    {

    }
    public static void UpdateAnimationFields(ref AnimationComponent animationComponent, Unity.Mathematics.Random? walkRandom = null, Unity.Mathematics.Random? runRandom = default)
    {



        // Depending on the animationType, set the specific frame-related values
        switch (animationComponent.animationType)
        {
            case EntitySpawner.AnimationType.Attack:
                animationComponent.finishAnimation = true;
                animationComponent.frameCount = 6; // Example: 6 frames for the attack animation
                animationComponent.currentFrame = 0; // Start at the first frame
                animationComponent.frameTimerMax = 0.12f; // Example: 0.2 seconds per frame
                animationComponent.frameTimer = 0f; // Reset the frame timer
                animationComponent.animationHeightOffset = 7;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.Run:
                animationComponent.frameCount = 6;
                animationComponent.currentFrame = runRandom.Value.NextInt(0, 5);
                animationComponent.frameTimerMax = .1f;
                animationComponent.frameTimer = 0f; // Reset the frame timer
                animationComponent.animationHeightOffset = 5;
                //animationComponent.animationWidthOffset =  horizontalMultiplier;
                break;
            default:
            case EntitySpawner.AnimationType.Idle:
                animationComponent.frameCount = 2;
                animationComponent.currentFrame = 0;
                animationComponent.frameTimerMax = .0875f;
                animationComponent.frameTimer = 0f; // Reset the frame timer
                animationComponent.animationHeightOffset = 0;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.Walk:
                animationComponent.frameCount = 4;
                animationComponent.currentFrame = walkRandom.Value.NextInt(0, 3);
                animationComponent.frameTimerMax = 0.15f;
                animationComponent.frameTimer = 0f;
                animationComponent.animationHeightOffset = 1;
                //animationComponent.animationWidthOffset = horizontalMultiplier;

                break;
            case EntitySpawner.AnimationType.Defend:
                animationComponent.frameCount = 3;
                animationComponent.currentFrame = 0;
                animationComponent.frameTimerMax = .1f;
                animationComponent.frameTimer = 0f;
                animationComponent.animationHeightOffset = 2;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.Block:
                animationComponent.frameCount = 3;
                animationComponent.currentFrame = 0;
                animationComponent.frameTimerMax = .0875f;
                animationComponent.frameTimer = 0f;
                animationComponent.animationHeightOffset = 3;
                //animationComponent.animationWidthOffset = horizontalMultiplier;
                break;
            case EntitySpawner.AnimationType.TakeDamage:
                animationComponent.frameCount = 3;
                animationComponent.currentFrame = 0;
                animationComponent.frameTimerMax = .0875f;
                animationComponent.frameTimer = 0f;
                animationComponent.animationHeightOffset = 6;
                //animationComponent.animationWidthOffset = horizontalMultiplier;

                break;
            case EntitySpawner.AnimationType.Die:
                animationComponent.frameCount = 6;
                animationComponent.currentFrame = 0;
                animationComponent.frameTimerMax = 0.12f;
                animationComponent.frameTimer = 0f;
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