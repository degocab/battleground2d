using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static EntitySpawner;
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
        materialDictionary = new Dictionary<(UnitType, Direction, AnimationType), UnityEngine.Material[]>
        {
            //// Default unit type materials
            [(UnitType.Default, Direction.Down, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleDown"), //defaultIdleDownMaterials;
            [(UnitType.Default, Direction.Up, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleUp"), //defaultIdleUpMaterials;
            [(UnitType.Default, Direction.Left, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleLeft"), //defaultIdleLeftMaterials;
            [(UnitType.Default, Direction.Right, AnimationType.Idle)] = LoadMaterialArray("Material/Default/IdleRight"), //defaultIdleRightMaterials;

            [(UnitType.Default, Direction.Down, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunDown"),
            [(UnitType.Default, Direction.Up, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunUp"),
            [(UnitType.Default, Direction.Left, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunLeft"), //defaultRunLeftMaterials;
            [(UnitType.Default, Direction.Right, AnimationType.Run)] = LoadMaterialArray("Material/Default/RunRight"), //defaultRunRightMaterials;

            [(UnitType.Default, Direction.Down, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieDown"),
            [(UnitType.Default, Direction.Up, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieUp"),
            [(UnitType.Default, Direction.Left, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieLeft"),
            [(UnitType.Default, Direction.Right, AnimationType.Die)] = LoadMaterialArray("Material/Default/DieRight"),

            [(UnitType.Default, Direction.Down, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackDown"),
            [(UnitType.Default, Direction.Up, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackUp"),
            [(UnitType.Default, Direction.Left, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackLeft"),
            [(UnitType.Default, Direction.Right, AnimationType.Attack)] = LoadMaterialArray("Material/Default/AttackRight"),

            [(UnitType.Default, Direction.Down, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkDown"),
            [(UnitType.Default, Direction.Up, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkUp"),
            [(UnitType.Default, Direction.Left, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkLeft"),
            [(UnitType.Default, Direction.Right, AnimationType.Walk)] = LoadMaterialArray("Material/Default/WalkRight"),

            [(UnitType.Default, Direction.Down, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendDown"),
            [(UnitType.Default, Direction.Up, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendUp"),
            [(UnitType.Default, Direction.Left, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendLeft"),
            [(UnitType.Default, Direction.Right, AnimationType.Defend)] = LoadMaterialArray("Material/Default/DefendRight"),

            [(UnitType.Default, Direction.Down, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockDown"),
            [(UnitType.Default, Direction.Up, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockUp"),
            [(UnitType.Default, Direction.Left, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockLeft"),
            [(UnitType.Default, Direction.Right, AnimationType.Block)] = LoadMaterialArray("Material/Default/BlockRight"),

            [(UnitType.Default, Direction.Down, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageDown"),
            [(UnitType.Default, Direction.Up, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageUp"),
            [(UnitType.Default, Direction.Left, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageLeft"),
            [(UnitType.Default, Direction.Right, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Default/TakeDamageRight"),


            //// Enemy unit type materials
            [(UnitType.Enemy, Direction.Down, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Idle)] = LoadMaterialArray("Material/Enemy/IdleRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Run)] = LoadMaterialArray("Material/Enemy/RunRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Die)] = LoadMaterialArray("Material/Enemy/DieRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Attack)] = LoadMaterialArray("Material/Enemy/AttackRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Walk)] = LoadMaterialArray("Material/Enemy/WalkRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Defend)] = LoadMaterialArray("Material/Enemy/DefendRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.Block)] = LoadMaterialArray("Material/Enemy/BlockRight"),

            [(UnitType.Enemy, Direction.Down, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageDown"),
            [(UnitType.Enemy, Direction.Up, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageUp"),
            [(UnitType.Enemy, Direction.Left, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageLeft"),
            [(UnitType.Enemy, Direction.Right, AnimationType.TakeDamage)] = LoadMaterialArray("Material/Enemy/TakeDamageRight")
        };

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

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        //define unit archetype
        unitArchetype = entityManager.CreateArchetype(
            typeof(PositionComponent),
            typeof(VelocityComponent),
            typeof(MovementSpeedComponent),
            typeof(HealthComponent),
            typeof(AttackComponent),
            typeof(AttackCooldownComponent),

            typeof(TargetPositionComponent),
            typeof(AnimationComponent),
            typeof(IsDeadComponent),
            typeof(UnitMaterialComponent),
            typeof(Translation),
            typeof(Unit),
            typeof(GridID),
            typeof(CircleCollider2D),
            typeof(ECS_CircleCollider2DAuthoring),
            typeof(ECS_PhysicsBody2DAuthoring),
            typeof(CollidableTag),
            typeof(CommandData),
            typeof(ECS_Velocity2D)
            //,typeof(Radius)
            //,typeof(HasNeighbor)
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

            typeof(AnimationComponent),
            typeof(IsDeadComponent),
            typeof(UnitMaterialComponent),
            typeof(Translation),
            typeof(Unit),
            typeof(GridID),
            typeof(ECS_CircleCollider2DAuthoring),
            typeof(ECS_PhysicsBody2DAuthoring),
            typeof(CollidableTag),
            typeof(ECS_Velocity2D)
            //,typeof(Radius)
            //,typeof(HasNeighbor)
            );

        SpawnCommander();

        SpawnUnits(UnitCountToSpawn);

        for (int i = 0; i < 100; i++)
        {
            SpawnTargets(); 
        }

    }

    private void SpawnTargets()
    {
        EntityArchetype targetArchetype = entityManager.CreateArchetype(
             typeof(PositionComponent)
            , typeof(AnimationComponent)
            , typeof(Translation)
            , typeof(TargetComponent)
            );


        Entity entity = entityManager.CreateEntity(targetArchetype);
        float3 pos = new float3(UnityEngine.Random.Range(-2f, -.75f), UnityEngine.Random.Range(-2f, 20), 0f);
        entityManager.SetComponentData(entity, new PositionComponent {  Value = pos});
        entityManager.SetComponentData(entity, new Translation {  Value = pos});
        entityManager.SetComponentData(entity,
            new AnimationComponent
            {
                currentFrame = UnityEngine.Random.Range(0, 5),
                frameCount = 2,
                frameTimer = UnityEngine.Random.Range(0f, 1f),
                frameTimerMax = .1f,
                animationHeightOffset = 0,
                animationWidthOffset = 1,
                unitType = UnitType.Enemy,
                direction = Direction.Right,
                animationType = AnimationType.Idle,
                prevAnimationType = AnimationType.Idle,
                finishAnimation = false
            });
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

        Entity leaderEntity = Entity.Null; // The leader of the phalanx
        int rank = 1; // Start with Rank 1 for the basic soldier
                      // Define the total number of soldiers we want to spawn per phalanx or army
        int soldierCount = 0;
        int officerCount = 0;
        int captainCount = 0;
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
                entityManager.SetComponentData(unit, new PositionComponent { Value = unitPosition });
                UnitType unitType;



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


                //TODO: not working figure it out
                if (soldierCount * row % 15 != 0)
                {
                    if (captainCount == 1)
                    {
                        rank = 2;//reset
                    }
                    else
                    {
                        rank = 4;
                        captainCount++;
                    }
                }
                else
                {
                    rank = 1;
                    soldierCount++;
                }
                if (soldierCount == 225)
                {
                    rank = 3;
                    officerCount++;
                }



                // Determine the rank of the unit based on soldier count and position
                //if (rank == 1) // Soldier/Hoplite
                //{
                //    rank = 1;
                //    soldierCount++;
                //}
                //else if (rank == 2) // Elite Soldier (Commander's Guard)
                //{
                //    rank = 2;
                //    soldierCount++;
                //}
                //else if (rank == 3 && officerCount < phalanxSize / 15) // Officer (commands 15 soldiers)
                //{
                //    rank = 3; 
                //    officerCount++;
                //}
                //else if (rank == 4 && captainCount == 0) // Captain (commands the phalanx)
                //{
                //    rank = 4;
                //    captainCount++;
                //}
                //else if (rank == 5 && leaderEntity == Entity.Null) // General (commanding multiple phalanxes)
                //{
                //    rank = 5;
                //    leaderEntity = unit; // Set the first general as the leader
                //}
                //else if (rank == 6 && leaderEntity == Entity.Null) // Commander (player or AI)
                //{
                //    rank = 6;
                //    leaderEntity = unit; // Set the commander as the overall leader
                //}
                else // Increment rank for the next soldier
                {
                    //rank++; // Increment rank for the next soldier
                }


                // Set additional unit components such as Health, Movement, etc.
                entityManager.SetComponentData(unit, new TargetPositionComponent { targetPosition = new float3(unitPosition.x + 2f, unitPosition.y, 0f) });
                
                //unit commands
                entityManager.SetComponentData(unit, new CommandData
                {
                    Command = CommandType.Idle,
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero
                });

                SetUnitComponents(unit, unitPosition, unitType, rank);
                entityManager.SetComponentData(unit,
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
        }
    }



    private void SetUnitComponents(Entity unit, float3 unitPosition, UnitType unitType, int rank)
    {
        entityManager.SetComponentData(unit, new HealthComponent { health = 100f, maxHealth = 100f });
        entityManager.SetComponentData(unit, new MovementSpeedComponent { velocity = 3f, isBlocked = false, isKnockedBack = false });
        entityManager.SetComponentData(unit, new AttackComponent { damage = 10f, range = 1f, isAttacking = false, isDefending = false });
        entityManager.SetComponentData(unit, new AttackCooldownComponent { cooldownDuration = .525f, timeRemaining = 0f, takeDamageCooldownDuration = .225f });
        entityManager.SetComponentData(unit, new Unit { isMounted = false, rank = rank });
        entityManager.SetComponentData(unit, new ECS_CircleCollider2DAuthoring
        {
            Radius = .125f
        });
        entityManager.SetComponentData(unit, new ECS_PhysicsBody2DAuthoring
        {
            initialVelocity = new float2(0, 0),
            mass = 1,
            isStatic = false
        });
        entityManager.SetComponentData(unit, new ECS_Velocity2D
        {
            Value = new float2(0, 0),
            PrevValue = new float2(0, 0)
        });

        //entityManager.SetComponentData(unit, new Radius { Value = 0.125f });
        //entityManager.SetComponentData(unit, new HasNeighbor { Value = false });
        //entityManager.SetComponentData(unit, new GridID { Value = 0 });


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
        float3 unitPosition = new float3(-2f, -3f, 0f);

        entityManager.SetComponentData(commanderEntity, new Translation { Value = unitPosition });
        entityManager.SetComponentData(commanderEntity, new PositionComponent { Value = unitPosition });
        entityManager.SetComponentData(commanderEntity, new CommanderComponent { isPlayerControlled = true });

        SetUnitComponents(commanderEntity, unitPosition, UnitType.Default, 6);


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