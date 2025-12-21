#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class FormationDebugDrawer : MonoBehaviour
{
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
            return;

        var em = world.EntityManager;

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<FormationDebugComponent>()
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var debugData = query.ToComponentDataArray<FormationDebugComponent>(Unity.Collections.Allocator.Temp);

        // Base label style
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,                 // 🔹 bigger text
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white } // 🔹 main color
        };

        // Outline style (thickness effect)
        var outlineStyle = new GUIStyle(style)
        {
            normal = { textColor = Color.black } // outline color
        };

        Vector3[] outlineOffsets =
        {
        new Vector3(-0.03f, 0f, 0f),
        new Vector3( 0.03f, 0f, 0f),
        new Vector3( 0f,  0.03f, 0f),
        new Vector3( 0f, -0.03f, 0f),
    };

        for (int i = 0; i < entities.Length; i++)
        {
            var d = debugData[i];
            Vector3 pos = new Vector3(d.WorldPosition.x, 0f, d.WorldPosition.y)
                          + Vector3.up * 1.5f;

            string text = d.Status.ToString();

            // 🔸 Draw outline (fake thickness)
            foreach (var offset in outlineOffsets)
            {
                Handles.Label(pos + offset, text, outlineStyle);
            }

            // 🔹 Draw main label
            Handles.Label(pos, text, style);
        }
    }
}
#endif
