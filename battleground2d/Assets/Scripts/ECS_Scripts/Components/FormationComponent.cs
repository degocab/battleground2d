using Unity.Entities;
using Unity.Mathematics;
using static EntitySpawner;

public struct FormationComponent : IComponentData
{
    public int FormationID;
    public int SlotIndex;
    public float2 LocalOffset; // Position relative to leader
    public float2 FormationPosition;
    public FormationType FormationType;
    //public FormationStatusEnum Status;
    public FormationColliderStatus ColliderStatus; 
    public Entity? FormationGroupEntity;
    public FormationColliderStatus PreviousColliderStatus;
    public float FormationWeight;
    public Direction Direction;
}

/// <summary>
/// Formation collider status to turn on or off unit/group collision
/// </summary>
public enum FormationColliderStatus
{
    Group,       // Use Group collider
    Individual,       // User unit collider
}
public enum FormationStatusEnum
{
    //None,       // No formation behavior
    Hold,       // Maintain position in formation
    Engaged,    // In formation but actively fighting
    Broken,     // Formation broken, individual combat
    Disengaging   // Returning to formation position
}

public enum FormationType
{
    Phalanx,
    SinglePhalanx,
    Horde,
    Wedge,
    Testudo
}

public struct FormationGroupComponent : IComponentData
{
    public int FormationID;
    public float2 AnchorPosition;
    public int UnitsPerRow;
    public float UnitSpacing;
    public UnitType UnitType;//todo: change thsi to faction type?
    public AABB GroupBounds;
    public float2 BoundsMin; // AABB min corner
    public float2 BoundsMax; // AABB max corner
    public bool isColliding;
    public FormationStatusEnum FormationGroupStatus;
    public OrderType CurrentOrder;
    public bool ShouldUpdateAnchorToCurrentPosition; // NEW
    public Entity FormationGroupEntity;

    public float2 CurrentUnitAveragePosition;
    public int CurrentUnitCount;
    public int PriorGroupCount;
    public bool ReIndexSlots;

    public EntitySpawner.Direction FormationFacingDirection;
    public EntitySpawner.UnitType FormationUnitType;
    public bool InitialOrder;
    public bool AnchorLocked;

    public FormationType FormationType;

    public float AnchorResetTimer;
}

public struct FormationDebugComponent : IComponentData
{
    public OrderType Status;
    public float2 WorldPosition;
}
