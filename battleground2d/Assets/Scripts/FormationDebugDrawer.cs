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
            ComponentType.ReadOnly<FormationCaptainComponent>(),
            ComponentType.ReadOnly<FormationGroupComponent>()
        );

        var captains = query.ToComponentDataArray<FormationCaptainComponent>(Unity.Collections.Allocator.Temp);
        var groups = query.ToComponentDataArray<FormationGroupComponent>(Unity.Collections.Allocator.Temp);

        // Base label style
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        // Outline
        var outlineStyle = new GUIStyle(style)
        {
            normal = { textColor = Color.black }
        };

        Vector3[] outlineOffsets =
        {
        new Vector3(-0.03f, 0f, 0f),
        new Vector3( 0.03f, 0f, 0f),
        new Vector3( 0f,  0.03f, 0f),
        new Vector3( 0f, -0.03f, 0f),
    };

        for (int i = 0; i < captains.Length; i++)
        {
            var captain = captains[i];
            var group = groups[i];

            // formation center
            float2 center = (group.BoundsMin + group.BoundsMax) * 0.5f;

            Vector3 pos = new Vector3(center.x, center.y, 2f);
                          //+ Vector3.up * 2f;

            string text =
                $"P:{captain.State}\n"+
                $"C:{captain.Control}\n"+
                $"I:{captain.Intensity01}";

            // outline
            //foreach (var offset in outlineOffsets)
            //{
            //    Handles.Label(pos + offset, text, outlineStyle);
            //}

            if (group.UnitType == EntitySpawner.UnitType.Ally)
            {
                //style = new GUIStyle(EditorStyles.boldLabel)
                //{
                //    fontSize = 14,
                //    alignment = TextAnchor.MiddleCenter,
                //    normal = { textColor = Color.white }
                //};
                style.normal.textColor = Color.green;
            }
            else
            {
                style.normal.textColor = Color.red;
            }
            // main text
            Handles.Label(pos, text, style);
        }

        captains.Dispose();
        groups.Dispose();
    }
}
#endif
