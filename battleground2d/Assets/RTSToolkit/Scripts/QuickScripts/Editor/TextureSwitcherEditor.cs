using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(TextureSwitcher))]
    public class TextureSwitcherEditor : Editor
    {
        public TextureSwitcher origin = null;

        public override void OnInspectorGUI()
        {
            origin = (TextureSwitcher)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Flip"))
            {
                origin.SwitchBetweenOldAndNew();
            }

            if (GUILayout.Button("Change textures"))
            {
                origin.SwitchMaterialTextures();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
