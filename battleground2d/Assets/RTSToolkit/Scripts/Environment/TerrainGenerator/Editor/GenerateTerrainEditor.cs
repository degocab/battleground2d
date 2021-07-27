using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(GenerateTerrain))]
    public class GenerateTerrainEditor : Editor
    {
        public GenerateTerrain origin;
        string[] updateTypes = new string[] { "Do not update", "Distance to the center", "Number of tiles", "Scan" };

        public override void OnInspectorGUI()
        {
            origin = (GenerateTerrain)target;
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            origin.updateTypeIndex = EditorGUILayout.Popup("Update mode", origin.updateTypeIndex, updateTypes);
            origin.updateType = updateTypes[origin.updateTypeIndex];

            EditorGUILayout.EndHorizontal();

            if (origin.updateTypeIndex == 1)
            {
                EditorGUILayout.BeginHorizontal();
                origin.radiusInner = EditorGUILayout.FloatField("Radius to load", origin.radiusInner);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                origin.radiusOuter = EditorGUILayout.FloatField("Radius to unload", origin.radiusOuter);
                EditorGUILayout.EndHorizontal();

                if (origin.radiusInner > origin.radiusOuter - 1f)
                {
                    origin.radiusInner = origin.radiusOuter - 1f;
                }

                if (origin.radiusInner < 10f)
                {
                    origin.radiusInner = 10f;
                }

                if (origin.radiusOuter < 11f)
                {
                    origin.radiusOuter = 11f;
                }
            }
            else if (origin.updateTypeIndex == 2)
            {
                EditorGUILayout.BeginHorizontal();
                origin.nTilesRadius = EditorGUILayout.IntField("Number of tiles", origin.nTilesRadius);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clean Up"))
            {
                if (Rivers.GetActive() != null)
                {
                    Rivers.GetActive().Clean();
                }

                origin.CleanUp();
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            }

            if (GUILayout.Button("Generate"))
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                origin.Generate();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
