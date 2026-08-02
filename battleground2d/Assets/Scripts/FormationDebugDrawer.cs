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

        if (EntitySpawner.Instance == null)
            return;

        if (!EntitySpawner.Instance.EnableDebugDrawing)
            return;


        var em = world.EntityManager;

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<FormationCaptainComponent>(),
            ComponentType.ReadOnly<FormationGroupComponent>(),
            ComponentType.ReadOnly<FormationBehaviorComponent>(),
            ComponentType.ReadOnly<OrderData>()
        );

        var captains = query.ToComponentDataArray<FormationCaptainComponent>(Unity.Collections.Allocator.Temp);
        var groups = query.ToComponentDataArray<FormationGroupComponent>(Unity.Collections.Allocator.Temp);
        var behaviors = query.ToComponentDataArray<FormationBehaviorComponent>(Unity.Collections.Allocator.Temp);
        var orders = query.ToComponentDataArray<OrderData>(Unity.Collections.Allocator.Temp);

        // Base label style
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        // Outline
        //var outlineStyle = new GUIStyle(style)
        //{
        //    normal = { textColor = Color.black }
        //};
        GUIStyle outlineStyle = new GUIStyle(style);

        Vector3[] outlineOffsets =
        {
    new Vector3(-0.05f,  0.00f, 0f),
    new Vector3( 0.05f,  0.00f, 0f),
    new Vector3( 0.00f, -0.05f, 0f),
    new Vector3( 0.00f,  0.05f, 0f),

    // Optional corners for a thicker outline
    new Vector3(-0.05f, -0.05f, 0f),
    new Vector3(-0.05f,  0.05f, 0f),
    new Vector3( 0.05f, -0.05f, 0f),
    new Vector3( 0.05f,  0.05f, 0f),
};


        for (int i = 0; i < captains.Length; i++)
        {
            var captain = captains[i];
            var group = groups[i];
            var behavior = behaviors[i];
            var order = orders[i];

            // formation center
            float2 center = (group.BoundsMin + group.BoundsMax) * 0.5f;

            Vector3 pos = new Vector3(center.x, center.y, 2f);
            //+ Vector3.up * 2f;

            string text =
                $"P:{captain.State}\n" +
                $"C:{captain.Control:F2}\n" +
                $"I:{captain.Intensity:F2}\n" +
                $"M:{captain.Morale:F2}\n" +
                $"Behavior:{behavior.Type}\n" +
                $"Order:{order.CurrentOrder}\n" +
                $"State From FCD:{behavior.State}\n" +
                $"State From Captain:{captain.State}\n";
            // outline
            //foreach (var offset in outlineOffsets)
            //{
            //    Handles.Label(pos + offset, text, outlineStyle);
            //}

            //if (group.UnitType == EntitySpawner.UnitType.Ally)
            //{
            //    //style = new GUIStyle(EditorStyles.boldLabel)
            //    //{
            //    //    fontSize = 14,
            //    //    alignment = TextAnchor.MiddleCenter,
            //    //    normal = { textColor = Color.white }
            //    //};
            //    style.normal.textColor = Color.green;
            //}
            //else
            //{
            //    style.normal.textColor = Color.red;
            //}
            //// main text
            //Handles.Label(pos, text, style);

            outlineStyle.normal.textColor = Color.black;


            foreach (var offset in outlineOffsets)
            {
                Handles.Label(pos + offset, text, outlineStyle);
            }

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

        DrawCommandAwareness(em, style);
    }

    private static void DrawCommandAwareness(EntityManager em, GUIStyle labelStyle)
    {
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<CommandComponent>(),
            ComponentType.ReadOnly<CommandAwarenessConfig>(),
            ComponentType.ReadOnly<CommandAwareness>(),
            ComponentType.ReadOnly<CommandKnownSlice>(),
            ComponentType.ReadOnly<CommandKnownFormation>());

        var commandEntities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var commands = query.ToComponentDataArray<CommandComponent>(Unity.Collections.Allocator.Temp);
        var configs = query.ToComponentDataArray<CommandAwarenessConfig>(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < commandEntities.Length; i++)
        {
            CommandComponent command = commands[i];
            if (command.AwarenessOrigin == Entity.Null || !em.HasComponent<Translation>(command.AwarenessOrigin))
                continue;

            float2 origin = em.GetComponentData<Translation>(command.AwarenessOrigin).Value.xy;
            Vector3 origin3 = new Vector3(origin.x, origin.y, 0f);

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(origin3, Vector3.forward, configs[i].ObservationRadius);

            DynamicBuffer<CommandKnownSlice> slices = em.GetBuffer<CommandKnownSlice>(commandEntities[i]);
            for (int sliceIndex = 0; sliceIndex < slices.Length; sliceIndex++)
            {
                CommandKnownSlice slice = slices[sliceIndex];
                int2 cell = SliceGridUtil.DecodeKey(slice.SliceKey);
                SliceGridUtil.CellBounds(cell, new float2(-32f, -16f), out float2 min, out float2 max);
                Color color = slice.Source == AwarenessSource.DirectObservation
                    ? new Color(0f, 1f, 1f, 0.9f)
                    : new Color(0.4f, 0.7f, 0.7f, math.max(0.15f, slice.Confidence));
                DrawAabb(min, max, color);
            }

            DynamicBuffer<CommandKnownFormation> formations = em.GetBuffer<CommandKnownFormation>(commandEntities[i]);
            for (int formationIndex = 0; formationIndex < formations.Length; formationIndex++)
            {
                CommandKnownFormation formation = formations[formationIndex];
                Color color = formation.Faction == command.FactionType ? Color.green : Color.red;
                if (formation.Source == AwarenessSource.Memory)
                    color = new Color(color.r, color.g, color.b, math.max(0.15f, formation.Confidence));

                DrawAabb(formation.BoundsMin, formation.BoundsMax, color);
                float2 formationCenter = (formation.BoundsMin + formation.BoundsMax) * 0.5f;
                Gizmos.color = color;
                Gizmos.DrawLine(origin3, new Vector3(formationCenter.x, formationCenter.y, 0f));
            }

            CommandAwareness awareness = em.GetComponentData<CommandAwareness>(commandEntities[i]);
            labelStyle.normal.textColor = Color.cyan;
            Handles.Label(
                origin3 + Vector3.up,
                $"Awareness R:{configs[i].ObservationRadius:F0}\n" +
                $"Slices:{awareness.KnownSliceCount} Friendly:{awareness.FriendlyFormationCount} Enemy:{awareness.EnemyFormationCount}\n" +
                $"Pressure:{awareness.Pressure} C:{awareness.Control:F2} I:{awareness.Intensity:F2}",
                labelStyle);
        }

        Handles.color = Color.white;
        configs.Dispose();
        commands.Dispose();
        commandEntities.Dispose();
    }

    private static void DrawAabb(float2 min, float2 max, Color color)
    {
        Vector3 bottomLeft = new Vector3(min.x, min.y, 0f);
        Vector3 bottomRight = new Vector3(max.x, min.y, 0f);
        Vector3 topRight = new Vector3(max.x, max.y, 0f);
        Vector3 topLeft = new Vector3(min.x, max.y, 0f);

        Gizmos.color = color;
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}
#endif
