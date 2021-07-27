using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(IconsBake))]
    public class IconsBakeEditor : Editor
    {
        public IconsBake origin = null;

        public override void OnInspectorGUI()
        {
            origin = (IconsBake)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load defaults"))
            {
                origin.LoadDefault();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
