using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(TextScaler))]
    public class TextScalerEditor : Editor
    {
        public TextScaler origin = null;

        public override void OnInspectorGUI()
        {
            origin = (TextScaler)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Scale"))
            {
                origin.GetAllSceneGameObjects();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
