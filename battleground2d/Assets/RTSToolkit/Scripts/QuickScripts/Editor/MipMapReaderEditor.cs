using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(MipMapReader))]
    public class MipMapReaderEditor : Editor
    {
        public MipMapReader origin;

        public override void OnInspectorGUI()
        {
            origin = (MipMapReader)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Read"))
            {
                origin.Reader();
            }

            if (GUILayout.Button("Update"))
            {
                origin.Writer();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
