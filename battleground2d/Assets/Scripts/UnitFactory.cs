using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static EntitySpawner;

public class UnitFactory
{
    private readonly EntityManager entityManager;
    private readonly UnitArchetypeFactory archetypeFactory;

    public UnitFactory(EntityManager entityManager)
    {
        this.entityManager = entityManager;
        this.archetypeFactory = new UnitArchetypeFactory(entityManager);
    }

    //public void SpawnUnits(int count, UnitType unitType = UnitType.Enemy, Direction unitDirection = Direction.Right, CommandData? initialCommand = null, float2? spawnPosition = null, FormationGenerator.FormationType formationType = default)
    public void SpawnUnits(int count, UnitType unitType, Direction unitDirection, CommandData initialCommand, float2 spawnPosition, FormationGenerator.FormationType formationType)
    {
        var formationGenerator = new FormationGenerator();


        List<float2> positions = new List<float2>();
        switch (formationType)
        {
            case FormationGenerator.FormationType.Phalanx:
                positions = formationGenerator.GeneratePhalanxFormation(count, spawnPosition);
                break;
            case FormationGenerator.FormationType.Horde:
            default:
                positions = formationGenerator.GenerateHordeFormation(count, 20f, 1f, 0.275f, 12345, spawnPosition);
                break;
        }

        for (int i = 0; i < positions.Count; i++)
        {
            SpawnUnit(positions[i], unitType, unitDirection, GetRank(i), initialCommand, spawnPosition);
        }
    }

    //TODO: add bool for setting AI commander component
    public void SpawnCommander()
    {
        var commander = CreateUnitBase(new float2(0, 0), UnitType.Default, 7, Direction.Right, 100000f);
        entityManager.AddComponent<CommanderComponent>(commander);
        entityManager.SetComponentData(commander, new CommanderComponent { isPlayerControlled = true });
    }
    private Entity SpawnUnit(float2 position, UnitType unitType, Direction unitDirection, int rank, CommandData? initialCommand = null, float2? spawnPosition = null)
    {
        var unit = CreateUnitBase(position, unitType, rank, unitDirection, 100f);

        // Use provided command or create default move command
        CommandData command = initialCommand ?? CommandFactory.CreateMoveCommand(spawnPosition);
        entityManager.SetComponentData(unit, command);

        return unit;
    }

    // Overload for specific command types
    private Entity SpawnUnit(float2 position, UnitType unitType, Direction unitDirection, int rank, CommandData? initialCommand, CommandType commandType)
    {
        return SpawnUnit(position, unitType, unitDirection, rank, CommandFactory.CreateCommand(commandType));
    }

    private Entity CreateUnitBase(float2 position, UnitType unitType, int rank, Direction unitDirection, float health)
    {
        var archetype = archetypeFactory.GetArchetype(rank);
        var unit = entityManager.CreateEntity(archetype);

        // Set common components
        if (unitType == UnitType.Default)
            SetTransformComponents(unit, new float3(position.x, position.y, 0));
        else
            SetTransformComponents(unit, new float3( position.x, position.y, 0));

        SetCombatComponents(unit, health);
        SetPhysicsComponents(unit);
        SetAnimationComponent(unit, unitType, unitDirection);
        SetUnitIdentity(unit, unitType, rank);

        return unit;
    }

    private void SetTransformComponents(Entity entity, float3 position)
    {
        entityManager.SetComponentData(entity, new Translation { Value = position });
        entityManager.SetComponentData(entity, new PositionComponent { Value = position });
    }

    private void SetCombatComponents(Entity entity, float health)
    {
        entityManager.SetComponentData(entity, new CombatState { CurrentState = CombatState.State.Idle });
        entityManager.SetComponentData(entity, new HealthComponent { Health = health, MaxHealth = health });
        entityManager.SetComponentData(entity, new AttackComponent
        {
            Damage = 10f,
            Range = .2875f,
            isAttacking = false,
            isDefending = false,
            AttackRate = 2f, // have to match for initial 
            AttackRateRemaining = 0f  // have to match for initial 
        });
        entityManager.SetComponentData(entity, new DefenseComponent
        {
            IsBlocking = false,
        });
        entityManager.SetComponentData(entity, new AttackCooldownComponent
        {
            attackCoolDownDuration = .6f,
            attackCoolTimeRemaining = 0f,
            takeDamageCooldownDuration = .22f, // have to match for initial 
            takingDmgTimeRemaining = .22f // have to match for initial 
        });


    }

    private void SetPhysicsComponents(Entity entity)
    {
        entityManager.SetComponentData(entity, new QuadrantEntity { typeEnum = QuadrantEntity.TypeEnum.Unit });
        entityManager.SetComponentData(entity, new ECS_CircleCollider2DAuthoring { Radius = 0.1375f });
        entityManager.SetComponentData(entity, new ECS_PhysicsBody2DAuthoring
        {
            initialVelocity = new float2(0, 0),
            mass = 1,
            isStatic = false
        });
        entityManager.SetComponentData(entity, new ECS_Velocity2D
        {
            Value = new float2(0, 0),
            PrevValue = new float2(0, 0)
        });
    }

    private void SetAnimationComponent(Entity entity, UnitType unitType, Direction unitDirection)
    {
        entityManager.SetComponentData(entity, new AnimationComponent
        {
            UnitType = unitType,
            Direction = unitDirection,
            prevDirection = unitDirection,
            AnimationType = AnimationType.Idle,
            CurrentFrame = UnityEngine.Random.Range(0, 5),
            FrameCount = 2,
            FrameTimer = UnityEngine.Random.Range(0f, 1f),
            FrameTimerMax = 0.1f,
            animationHeightOffset = 0,
            animationWidthOffset = 1,
            PrevAnimationType = AnimationType.Idle,
            finishAnimation = false
        });
    }

    private void SetUnitIdentity(Entity entity, UnitType unitType, int rank)
    {
        entityManager.SetComponentData(entity, new Unit
        {
            isMounted = false,
            Rank = rank
        });
    }

    private CommandData CreateMoveCommand(float3 startPosition)
    {
        return new CommandData
        {
            Command = CommandType.MoveTo,
            TargetPosition = new float2(startPosition.x - 1.0f, startPosition.y + UnityEngine.Random.Range(-0.1f, 0.1f))
        };
    }

    private int GetRank(int index) => 1; // Simple rank assignment for now
    private UnitType GetUnitType(int unitType) => unitType == 1 ? UnitType.Default : UnitType.Enemy;
}

// Separate class for archetype management
public class UnitArchetypeFactory
{
    private readonly EntityManager entityManager;
    private readonly EntityArchetype regularUnitArchetype;
    private readonly EntityArchetype commanderArchetype;

    public UnitArchetypeFactory(EntityManager entityManager)
    {
        this.entityManager = entityManager;
        this.regularUnitArchetype = CreateRegularUnitArchetype();
        this.commanderArchetype = CreateCommanderArchetype();
    }

    public EntityArchetype GetArchetype(int rank) => rank == 7 ? commanderArchetype : regularUnitArchetype;

    private EntityArchetype CreateRegularUnitArchetype()
    {
        return entityManager.CreateArchetype(
            typeof(PositionComponent), typeof(Translation), typeof(MovementSpeedComponent),
            typeof(HealthComponent), typeof(AttackComponent), typeof(AttackCooldownComponent),
            typeof(CombatState), typeof(AnimationComponent), typeof(Unit), typeof(QuadrantEntity),
            typeof(CommandData), typeof(ECS_CircleCollider2DAuthoring), typeof(ECS_PhysicsBody2DAuthoring),
            typeof(ECS_Velocity2D), typeof(CollidableTag), typeof(TargetComponent), typeof(DefenseComponent), typeof(AttackPhasesComponent)
        );
    }

    private EntityArchetype CreateCommanderArchetype()
    {
        return entityManager.CreateArchetype(
            typeof(CommanderComponent), typeof(PlayerInputComponent),
            typeof(PositionComponent), typeof(Translation), typeof(MovementSpeedComponent),
            typeof(HealthComponent), typeof(AttackComponent), typeof(AttackCooldownComponent),
            typeof(CombatState), typeof(AnimationComponent), typeof(Unit), typeof(QuadrantEntity),
            typeof(ECS_CircleCollider2DAuthoring), typeof(ECS_PhysicsBody2DAuthoring),
            typeof(ECS_Velocity2D), typeof(CollidableTag), typeof(DefenseComponent), typeof(AttackPhasesComponent)
        //, typeof(TargetComponent)
        );
    }
}