using Unity.Entities;
using Unity.Mathematics;

//public struct FormationComponent : IComponentData
//{
//    public int formationType;  // 0 = Line, 1 = Grid, 2 = Wedge
//}
public struct FormationComponent : IComponentData
{
    public Entity FormationLeader;
    public int FormationID;
    public int SlotIndex;
    public float2 LocalOffset; // Position relative to leader
    public float2 FormationPosition;
    public FormationType FormationType;
    public FormationStatus Status;
    public Entity PreviousTarget; // Store previous target for returning
    public float2 PreviousPosition; // Store previous position for returning
    public float2 AnchorPosition;
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

public struct FormationGroupComponent : ISharedComponentData
{
    public int FormationID;
    public float2 AnchorPosition;
    public FormationType FormationType;
    public int UnitsPerRow;
    public float UnitSpacing;
}