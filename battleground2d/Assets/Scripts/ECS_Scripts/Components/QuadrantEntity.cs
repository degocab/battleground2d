using Unity.Entities;

public struct QuadrantEntity : IComponentData
{
    public TypeEnum typeEnum;

    public enum TypeEnum
    {
        Unit,
        Target
    }
}