using UnityEngine;

// This attribute creates the menu item
[CreateAssetMenu(menuName = "RTS/Spawn Config", fileName = "NewSpawnConfig.asset")]
public class SpawnConfig : ScriptableObject
{
    [Range(1, 20000)] public int UnitCountToSpawn = 256;
    public int UnitsPerPhalanx = 256;
    public float UnitSpacing = 0.4f;
    public float PhalanxSpacing = 1f;
}
