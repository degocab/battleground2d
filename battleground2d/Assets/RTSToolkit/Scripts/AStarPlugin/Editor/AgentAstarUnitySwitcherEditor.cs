using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(AgentAstarUnitySwitcher))]
    public class AgentAstarUnitySwitcherEditor : Editor
    {

        public AgentAstarUnitySwitcher origin;

        public override void OnInspectorGUI()
        {
            origin = (AgentAstarUnitySwitcher)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
#if ASTAR
            if (GUILayout.Button("Switch To Unity Nav"))
            {
                origin.SwitchThisToUnityNavMesh();
            }
            if (GUILayout.Button("Switch To AStar"))
            {
                origin.SwitchThisToAStar();
            }
#endif
            EditorGUILayout.EndHorizontal();
        }
    }
}
