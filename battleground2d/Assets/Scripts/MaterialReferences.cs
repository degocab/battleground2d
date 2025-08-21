using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Material References", fileName = "MaterialReferences.asset")]
public class MaterialReferences : ScriptableObject
{
    [Header("Instanced Materials (GPU Slicing)")]
    public Material WalkingSpriteSheetMaterial;

    [Header("Optional: Fallback Materials for Debug")]
    public Material DefaultMaterial;
    public Material EnemyMaterial;

    // That's it! No complex dictionaries needed
}