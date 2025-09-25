using Unity.Entities;

public struct AttackEventBuffer : IBufferElementData
{
    public Entity Attacker;
    public float Damage;
    public int DamageType; // use int for enum-like simplicity
}