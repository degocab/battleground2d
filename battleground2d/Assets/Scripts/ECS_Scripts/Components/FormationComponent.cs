using Unity.Entities;
using Unity.Mathematics;
using static EntitySpawner;

public struct FormationComponent : IComponentData
{
    public Entity FormationLeader;
    public int FormationID;
    public int SlotIndex;
    public float2 LocalOffset; // Position relative to leader
    public float2 FormationPosition;
    public FormationType FormationType;
    public FormationStatus Status;
    public FormationColliderStatus ColliderStatus; 
    public Entity PreviousTarget; // Store previous target for returning
    public float2 PreviousPosition; // Store previous position for returning
    public float2 AnchorPosition;
    public int UnitsPerRow;
    public float UnitSpacing;
    public Entity? FormationGroupEntity;
    public bool UnitCollision;
    public bool WasJustAssignedToGroup; // NEW: Track fresh assignments
    public FormationColliderStatus PreviousColliderStatus;
    public float FormationWeight;
}

/// <summary>
/// Formation collider status to turn on or off unit/group collision
/// </summary>
public enum FormationColliderStatus
{
    Group,       // Use Group collider
    Individual,       // User unit collider
}
public enum FormationStatus
{
    None,       // No formation behavior
    Hold,       // Maintain position in formation
    Engaged,    // In formation but actively fighting
    Broken,     // Formation broken, individual combat
    Returning   // Returning to formation position
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
    public FormationStatus FormationGroupStatus;
    public CommandType CurrentCommand;
    public bool ShouldUpdateAnchorToCurrentPosition; // NEW
    public Entity FormationGroupEntity;

    public float2 CurrentUnitAveragePosition;
    public int PriorGroupCount;
    public bool ReIndexSlots;
}