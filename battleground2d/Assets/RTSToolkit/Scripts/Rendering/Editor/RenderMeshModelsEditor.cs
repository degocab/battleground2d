using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(RenderMeshModels))]
    public class RenderMeshModelsEditor : Editor
    {
        public RenderMeshModels origin = null;

        public override void OnInspectorGUI()
        {
            origin = (RenderMeshModels)target;

            EditorGUILayout.BeginHorizontal();
            origin.modelsWrapperOpen = EditorGUILayout.Foldout(origin.modelsWrapperOpen, "Models");
            EditorGUILayout.EndHorizontal();

            if (origin.modelsWrapperOpen)
            {
                for (int i = 0; i < origin.renderModels.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Space(10);
                    origin.renderModels[i].wrapperOpen = EditorGUILayout.Foldout(origin.renderModels[i].wrapperOpen, ("(" + origin.renderModels[i].renderAnimationsWrapper.Count + ")"));
                    origin.renderModels[i].modelName = EditorGUILayout.TextField(origin.renderModels[i].modelName);

                    if (GUILayout.Button("X"))
                    {
                        origin.renderModels.RemoveAt(i);
                        SceneScripts.MarkDirtyScene();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (origin.renderModels[i].wrapperOpen)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        EditorGUILayout.EndHorizontal();

                        for (int j = 0; j < origin.renderModels[i].renderAnimationsWrapper.Count; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(20);

                            origin.renderModels[i].renderAnimationsWrapper[j].lodWrapperOpen = EditorGUILayout.Foldout(origin.renderModels[i].renderAnimationsWrapper[j].lodWrapperOpen, "");
                            origin.renderModels[i].renderAnimationsWrapper[j].model = (GameObject)EditorGUILayout.ObjectField(origin.renderModels[i].renderAnimationsWrapper[j].model, typeof(GameObject), true);

                            if (GUILayout.Button("X"))
                            {
                                origin.renderModels[i].renderAnimationsWrapper.RemoveAt(j);
                                SceneScripts.MarkDirtyScene();
                                return;
                            }

                            EditorGUILayout.EndHorizontal();

                            if (origin.renderModels[i].renderAnimationsWrapper[j].lodWrapperOpen)
                            {
                                EditorGUILayout.BeginHorizontal();

                                GUILayout.Space(30);
                                origin.renderModels[i].renderAnimationsWrapper[j].distance = EditorGUILayout.Vector2Field("Distances", origin.renderModels[i].renderAnimationsWrapper[j].distance);

                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.BeginHorizontal();

                                GUILayout.Space(30);
                                origin.renderModels[i].renderAnimationsWrapper[j].offset = EditorGUILayout.Vector3Field("Offset", origin.renderModels[i].renderAnimationsWrapper[j].offset);

                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.BeginHorizontal();

                                GUILayout.Space(30);
                                origin.renderModels[i].renderAnimationsWrapper[j].lodMode = EditorGUILayout.IntField("LOD mode", origin.renderModels[i].renderAnimationsWrapper[j].lodMode);

                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.BeginHorizontal();

                                GUILayout.Space(30);
                                origin.renderModels[i].renderAnimationsWrapper[j].numberFramesToBake = EditorGUILayout.IntField("Number of frames to bake", origin.renderModels[i].renderAnimationsWrapper[j].numberFramesToBake);

                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                                EditorGUILayout.EndHorizontal();
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        origin.renderModels[i].useNationColor = EditorGUILayout.ToggleLeft("Use nation colors", origin.renderModels[i].useNationColor);
                        EditorGUILayout.EndHorizontal();

                        if (origin.renderModels[i].useNationColor)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            origin.renderModels[i].nationColorOverwiteSubmeshIndex = EditorGUILayout.IntField("Nation color submesh index", origin.renderModels[i].nationColorOverwiteSubmeshIndex);
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);

                        if (GUILayout.Button("Add LOD"))
                        {
                            RenderMeshLODs.RenderMeshLODsWrapper newWrapper = new RenderMeshLODs.RenderMeshLODsWrapper();
                            newWrapper.model = null;

                            if (origin.renderModels[i].renderAnimationsWrapper.Count == 0)
                            {
                                newWrapper.distance = new Vector2(0f, 1f);
                            }
                            else
                            {
                                newWrapper.distance = new Vector2(origin.renderModels[i].renderAnimationsWrapper[origin.renderModels[i].renderAnimationsWrapper.Count - 1].distance.y, origin.renderModels[i].renderAnimationsWrapper[origin.renderModels[i].renderAnimationsWrapper.Count - 1].distance.y + 1f);
                            }

                            newWrapper.lodMode = 0;
                            newWrapper.lodWrapperOpen = true;

                            origin.renderModels[i].renderAnimationsWrapper.Add(newWrapper);

                            SceneScripts.MarkDirtyScene();
                            return;
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (origin.modelsWrapperOpen)
            {
                if (GUILayout.Button("Add model"))
                {
                    origin.renderModels.Add(new RenderMeshLODs());
                    SceneScripts.MarkDirtyScene();
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            origin.useMeshInstancing = EditorGUILayout.ToggleLeft("Use mesh instancing", origin.useMeshInstancing);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            origin.useMeshInstancingIndirect = EditorGUILayout.ToggleLeft("Use mesh instancing indirect", origin.useMeshInstancingIndirect);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            origin.lodDistancesFactor = EditorGUILayout.FloatField("LOD distances factor", origin.lodDistancesFactor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            origin.adjustLODDistanceFactorRuntime = EditorGUILayout.ToggleLeft("Adjust LOD distances factor on runtime", origin.adjustLODDistanceFactorRuntime);
            EditorGUILayout.EndHorizontal();

            if (origin.adjustLODDistanceFactorRuntime)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                origin.minLodDistancesFactor = EditorGUILayout.FloatField("Minimum LOD distances factor", origin.minLodDistancesFactor);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                origin.maxLodDistancesFactor = EditorGUILayout.FloatField("Maxmimum LOD distances factor", origin.maxLodDistancesFactor);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                origin.minVertsCount = EditorGUILayout.FloatField("Minimum number of vertices", origin.minVertsCount);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                origin.maxVertsCount = EditorGUILayout.FloatField("Maximum number of vertices", origin.maxVertsCount);
                EditorGUILayout.EndHorizontal();
            }

            if (GUI.changed)
            {
                SceneScripts.MarkDirtyScene();
            }
        }
    }
}
