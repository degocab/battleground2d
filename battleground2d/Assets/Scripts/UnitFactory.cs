using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static EntitySpawner;
using static UnityEngine.EventSystems.EventTrigger;

public class UnitFactory
{
    private readonly EntityManager entityManager;
    private readonly UnitArchetypeFactory archetypeFactory;

    public UnitFactory(EntityManager entityManager)
    {
        this.entityManager = entityManager;
        this.archetypeFactory = new UnitArchetypeFactory(entityManager);
    }
    private int _nextFormationID = 0;
    //public void SpawnUnits(int count, UnitType unitType = UnitType.Enemy, Direction unitDirection = Direction.Right, CommandData? initialCommand = null, float2? spawnPosition = null, FormationGenerator.FormationType formationType = default)
    public Entity SpawnUnits(int count, UnitType unitType, Direction unitDirection, OrderData initialCommand, float2 spawnPosition, FormationType formationType)
    {
        int formationID = _nextFormationID++;


        // FORMATION GROUP UPDATE //
        Entity groupEntity = entityManager.CreateEntity();

        // Create the shared FormationGroupComponent
        var formationGroup = new FormationGroupComponent
        {
            FormationGroupEntity = groupEntity,
            FormationID = formationID,
            AnchorPosition = spawnPosition,
            UnitType = unitType,
            FormationType = formationType
        };
        var formationCaptain = new FormationCaptainComponent
        {
            
        };
        List<float2> positions = new List<float2>();
        switch (formationType)
        {
            case FormationType.Phalanx:
                //positions = FormationGenerator.GeneratePhalanxFormation(count, spawnPosition, 256 , .275f, 1);
                positions = FormationGenerator.GenerateSinglePhalanx(count , .275f, spawnPosition.y, spawnPosition.x);
                formationGroup.UnitSpacing = .275f;
                formationGroup.UnitsPerRow = 16;
                break;
            case FormationType.Horde:
            default:
                positions = FormationGenerator.GenerateHordeFormation(count, 20f, 1f, 0.275f, 12345, spawnPosition);
                formationGroup.UnitSpacing = .275f;
                formationGroup.UnitsPerRow = 20;
                break;
        }
        entityManager.AddComponentData(groupEntity, new QuadrantEntity { typeEnum = QuadrantEntity.TypeEnum.Target});
        entityManager.AddComponentData(groupEntity, new FormationDebugComponent { });
        if (unitType == UnitType.Ally)
        {
            entityManager.AddComponentData(groupEntity, new AllyTag { }); 
        }
        entityManager.AddComponentData(groupEntity, formationGroup);
        entityManager.AddComponentData(groupEntity, formationCaptain);
        entityManager.AddComponentData(groupEntity, new FormationBehaviorComponent());
        entityManager.AddComponentData(groupEntity, initialCommand);
        entityManager.AddComponentData(groupEntity, new FormationStatus());
        entityManager.AddComponentData(groupEntity, new FormationOrderIntent());
        for (int i = 0; i < positions.Count; i++)
        {
            //SpawnUnit(positions[i], unitType, unitDirection, GetRank(i), initialCommand, spawnPosition);
            SpawnUnit(i, positions[i], unitType, unitDirection, GetRank(i), initialCommand, formationID, positions[i] - spawnPosition, groupEntity);
        }
        return groupEntity;
    }

    private Entity SpawnUnit(int i, float2 position, UnitType unitType, Direction unitDirection, int rank, OrderData? initialCommand = null, int formationID = 0, float2 formationOffset = default, Entity? formationGroupEntity = null)
    {
        if (formationGroupEntity == null || formationGroupEntity == Entity.Null)
        {
            Debug.Log("null group entity ref");
        }

        var unit = CreateUnitBase(position, unitType, rank, unitDirection, 200f);//, formationID, formationOffset);
        entityManager.AddComponentData(unit, new FormationComponent
        {
            FormationID = formationID,
            LocalOffset = formationOffset,
            FormationPosition = position
            ,FormationGroupEntity = formationGroupEntity,
            SlotIndex = i,
        });
        OrderData order = initialCommand ?? OrderFactory.CreateMoveOrder(position);
        entityManager.SetComponentData(unit, order);
        //_entityManager.AddSharedComponentData(unit, formationGroup.Value);

        return unit;
    }

    private Entity CreateUnitBase(float2 position, UnitType unitType, int rank, Direction unitDirection, float v, int formationID, float2 formationOffset)
    {
        throw new NotImplementedException();
    }

    //TODO: add bool for setting AI commander component
    public Entity SpawnCommander(UnitType unitType, float2 spawnLocation, float health, bool isPlayerControlled)
    {
        var commander = CreateUnitBase(spawnLocation, unitType, 7, Direction.Right, health);
        entityManager.AddComponent<CommanderComponent>(commander);
        entityManager.SetComponentData(commander, new CommanderComponent { isPlayerControlled = isPlayerControlled });
        return commander;
    }


    // Overload for specific command types
    //private Entity SpawnUnit(float2 position, UnitType unitType, Direction unitDirection, int rank, CommandData? initialCommand, CommandType commandType)
    //{
    //    return SpawnUnit(position, unitType, unitDirection, rank, CommandFactory.CreateOrder(commandType));
    //}

    private Entity CreateUnitBase(float2 position, UnitType unitType, int rank, Direction unitDirection, float health)
    {
        var archetype = archetypeFactory.GetArchetype(rank);
        var unit = entityManager.CreateEntity(archetype);

        // Set common components
        if (unitType == UnitType.Ally)
            SetTransformComponents(unit, new float3(position.x, position.y, 0));
        else
            SetTransformComponents(unit, new float3( position.x, position.y, 0));

        SetCombatComponents(unit, health);
        SetPhysicsComponents(unit);
        SetAnimationComponent(unit, unitType, unitDirection);
        SetUnitIdentity(unit, unitType, rank);

        if (unitType == UnitType.Ally)
        {
            entityManager.AddComponentData(unit, new AllyTag { });

        }

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
        entityManager.SetComponentData(entity, new HealthComponent { Health = health, MaxHealth = health, deathAnimationDuration = 1000f});
        entityManager.SetComponentData(entity, new AttackComponent
        {
            Damage = 10f,
            Range = .5f,//.275f,//.2875f,
            isAttacking = false,
            //isDefending = false,
            AttackRate = 2f, // have to match for initial 
            AttackRateRemaining = 0f  // have to match for initial 
            , DefendDuration = 0.1f
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
        entityManager.SetComponentData(entity, new ECS_CircleCollider2DAuthoring { Radius = 0.2f/*0.1375f*/ });
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
            CurrentFrame = UnityEngine.Random.Range(0, 2),
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

    private OrderData CreateMoveCommand(float3 startPosition)
    {
        return new OrderData
        {
            CurrentOrder = OrderType.MoveTo,
            TargetPosition = new float2(startPosition.x - 1.0f, startPosition.y + UnityEngine.Random.Range(-0.1f, 0.1f))
        };
    }

    private int GetRank(int index) => 1; // Simple rank assignment for now
    private UnitType GetUnitType(int unitType) => unitType == 1 ? UnitType.Ally : UnitType.Enemy;
}

public struct AllyTag:IComponentData
{
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
            typeof(OrderData), typeof(ECS_CircleCollider2DAuthoring), typeof(ECS_PhysicsBody2DAuthoring),
            typeof(ECS_Velocity2D), typeof(CollidableTag), typeof(TargetComponent), typeof(DefenseComponent), typeof(AttackPhasesComponent), typeof(FormationSlotGoal)
            , typeof(MovementGoal), typeof(MovementStatus), typeof(CombatTarget)
        );
    }

    private EntityArchetype CreateCommanderArchetype()
    {
        return entityManager.CreateArchetype(
            typeof(CommanderComponent),
            typeof(PositionComponent), typeof(Translation), typeof(MovementSpeedComponent),
            typeof(HealthComponent), typeof(AttackComponent), typeof(AttackCooldownComponent),
            typeof(CombatState), typeof(AnimationComponent), typeof(Unit), typeof(QuadrantEntity),
            typeof(ECS_CircleCollider2DAuthoring), typeof(ECS_PhysicsBody2DAuthoring),
            typeof(ECS_Velocity2D), typeof(CollidableTag), typeof(DefenseComponent), typeof(AttackPhasesComponent)
            ,typeof(MovementGoal),typeof(MovementStatus), typeof(CommandPerception)
        //, typeof(TargetComponent)
        );
    }
}
