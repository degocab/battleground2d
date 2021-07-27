using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(FontSwitcher))]
    public class FontSwitcherEditor : Editor
    {
        public FontSwitcher origin = null;

        public override void OnInspectorGUI()
        {
            origin = (FontSwitcher)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Switch"))
            {
                origin.GetAllSceneGameObjects();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
