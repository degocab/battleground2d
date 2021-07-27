using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(PSpriteLoader))]
    public class PSpriteLoaderEditor : Editor
    {
        public PSpriteLoader origin = null;

        public override void OnInspectorGUI()
        {
            origin = (PSpriteLoader)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear download cache"))
            {
                origin.ClearDownloadsCache();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
