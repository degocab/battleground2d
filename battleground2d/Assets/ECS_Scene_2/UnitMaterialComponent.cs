using Unity.Entities;

public struct UnitMaterialComponent : IComponentData
{
    public int materialIndex;  // Material index instead of directly holding the Material
}