using Unity.Entities;

public struct FormationComponent : IComponentData
{
    public int formationType;  // 0 = Line, 1 = Grid, 2 = Wedge
}