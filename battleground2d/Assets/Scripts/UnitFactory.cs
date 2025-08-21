using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
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

    public void SpawnUnits(int count, UnitType unitType = UnitType.Enemy, CommandData? initialCommand = null)
    {
        var formationGenerator = new FormationGenerator();
        var positions = formationGenerator.GeneratePhalanxFormation(count);

        for (int i = 0; i < positions.Count; i++)
        {
            SpawnUnit(positions[i], unitType, GetRank(i), initialCommand);
        }
    }

    public void SpawnCommander()
    {
        var commander = CreateUnitBase(new float3(0, 0, 0), UnitType.Default, 7);
        entityManager.AddComponent<CommanderComponent>(commander);
        entityManager.SetComponentData(commander, new CommanderComponent { isPlayerControlled = true });
    }
    private Entity SpawnUnit(float3 position, UnitType unitType, int rank, CommandData? initialCommand = null)
    {
        var unit = CreateUnitBase(position, unitType, rank);

        // Use provided command or create default move command
        CommandData command = initialCommand ?? CommandFactory.CreateMoveCommand(position);
        entityManager.SetComponentData(unit, command);

        return unit;
    }

    // Overload for specific command types
    private Entity SpawnUnit(float3 position, UnitType unitType, int rank, CommandType commandType)
    {
        return SpawnUnit(position, unitType, rank, CommandFactory.CreateCommand(commandType));
    }

    private Entity CreateUnitBase(float3 position, UnitType unitType, int rank)
    {
        var archetype = archetypeFactory.GetArchetype(rank);
        var unit = entityManager.CreateEntity(archetype);

        // Set common components
        if (unitType == UnitType.Default)
            SetTransformComponents(unit, new float3(position.x + 5f, position.y, 0));
        else
            SetTransformComponents(unit, new float3( position.x - 3f, position.y, 0));

        SetCombatComponents(unit);
        SetPhysicsComponents(unit);
        SetAnimationComponent(unit, unitType);
        SetUnitIdentity(unit, unitType, rank);

        return unit;
    }

    private void SetTransformComponents(Entity entity, float3 position)
    {
        entityManager.SetComponentData(entity, new Translation { Value = position });
        entityManager.SetComponentData(entity, new PositionComponent { Value = position });
    }

    private void SetCombatComponents(Entity entity)
    {
        entityManager.SetComponentData(entity, new CombatState { CurrentState = CombatState.State.Idle });
        entityManager.SetComponentData(entity, new HealthComponent { Health = 100f, MaxHealth = 100f });
        entityManager.SetComponentData(entity, new AttackComponent
        {
            damage = 10f,
            range = 1f,
            isAttacking = false,
            isDefending = false
        });
        entityManager.SetComponentData(entity, new AttackCooldownComponent
        {
            cooldownDuration = .525f,
            timeRemaining = 0f,
            takeDamageCooldownDuration = .225f
        });
    }

    private void SetPhysicsComponents(Entity entity)
    {
        entityManager.SetComponentData(entity, new QuadrantEntity { typeEnum = QuadrantEntity.TypeEnum.Unit });
        entityManager.SetComponentData(entity, new ECS_CircleCollider2DAuthoring { Radius = .125f });
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

    private void SetAnimationComponent(Entity entity, UnitType unitType)
    {
        entityManager.SetComponentData(entity, new AnimationComponent
        {
            UnitType = unitType,
            Direction = Direction.Right,
            AnimationType = AnimationType.Idle,
            CurrentFrame = UnityEngine.Random.Range(0, 5),
            FrameCount = 2,
            FrameTimer = UnityEngine.Random.Range(0f, 1f),
            FrameTimerMax = 0.1f,
            animationHeightOffset = 0,
            animationWidthOffset = 1,
            prevAnimationType = AnimationType.Idle,
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
            typeof(ECS_Velocity2D), typeof(CollidableTag)
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
            typeof(ECS_Velocity2D), typeof(CollidableTag)
        );
    }
}