using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(AStarCompiler))]
    public class AStarCompilerEditor : Editor
    {

        public AStarCompiler origin;

        public override void OnInspectorGUI()
        {
            origin = (AStarCompiler)target;

            DrawDefaultInspector();
#if ASTAR
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clean"))
            {
                origin.Clean();
            }
            if (GUILayout.Button("Generate"))
            {
                origin.Compile();
            }
            EditorGUILayout.EndHorizontal();
#endif
        }
    }
}
