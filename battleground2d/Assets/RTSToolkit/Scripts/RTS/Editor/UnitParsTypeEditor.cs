using UnityEngine;
using UnityEditor;
using RTSToolkit;
using System.Collections.Generic;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(UnitParsType))]
    public class UnitParsTypeEditor : Editor
    {
        public UnitParsType origin;

        bool buildSequenceMeshes = false;
        bool destroySequenceMeshes = false;
        bool buildSequencePrefabs = false;
        bool destroySequencePrefabs = false;

        bool combatPropFoldout = false;
        bool combatStatFoldout = false;
        bool attackSoundsFoldout = false;
        bool deathSoundsFoldout = false;
        bool levels = false;
        bool smokes = false;
        bool costs = false;

        public override void OnInspectorGUI()
        {
            origin = (UnitParsType)target;

            Color curColor = GUI.color;
            GUI.color = new Color(1, 1, 0.9f, 1);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.color = curColor;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.unitName = EditorGUILayout.TextField("Unit name", origin.unitName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.buildTime = EditorGUILayout.FloatField("Build time", origin.buildTime);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.isBuilding = EditorGUILayout.ToggleLeft("Is building", origin.isBuilding);
            EditorGUILayout.EndHorizontal();

            if (origin.isBuilding)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.buildSequenceMeshMode = EditorGUILayout.ToggleLeft("Use build sequence meshes", origin.buildSequenceMeshMode);
                EditorGUILayout.EndHorizontal();

                if (origin.buildSequenceMeshMode)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    origin.buildSequenceMeshesPath = EditorGUILayout.TextField(origin.buildSequenceMeshesPath);

                    if (GUILayout.Button("Load"))
                    {
                        int nBuildFiles = 0;
                        System.IO.DirectoryInfo d = new System.IO.DirectoryInfo(Application.dataPath + "/" + origin.buildSequenceMeshesPath);

                        if (d.Exists)
                        {
                            System.IO.FileInfo[] files = d.GetFiles();

                            for (int i = 0; i < files.Length; i++)
                            {
                                System.IO.FileInfo fi = files[i];
                                if (fi.Name.Contains(".meta") == false)
                                {
                                    if (fi.Name.Contains("b.asset"))
                                    {
                                        nBuildFiles++;
                                    }
                                }
                            }

                            bool markSceneDirty = false;
                            if (nBuildFiles > 0)
                            {
                                origin.buildSequenceMeshes.Clear();
                                markSceneDirty = true;
                            }

                            Mesh msh = null;

                            for (int i = 0; i < nBuildFiles; i++)
                            {
                                msh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/" + origin.buildSequenceMeshesPath + (i + 1).ToString() + "b.asset", typeof(Mesh));

                                if (msh != null)
                                {
                                    origin.buildSequenceMeshes.Add(msh);
                                    markSceneDirty = true;
                                }
                            }

                            msh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/" + origin.buildSequenceMeshesPath + "f.asset", typeof(Mesh));

                            if (msh != null)
                            {
                                origin.buildSequenceMeshes.Add(msh);
                                markSceneDirty = true;
                            }

                            if (markSceneDirty)
                            {
                                SceneScripts.MarkDirtyScene();
                                return;
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    buildSequenceMeshes = EditorGUILayout.Foldout(buildSequenceMeshes, "Build sequence meshes");
                    EditorGUILayout.EndHorizontal();

                    if (buildSequenceMeshes)
                    {
                        curColor = GUI.color;
                        GUI.color = new Color(1f, 0.95f, 0.95f, 1);
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        GUI.color = curColor;

                        for (int i = 0; i < origin.buildSequenceMeshes.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            origin.buildSequenceMeshes[i] = (Mesh)EditorGUILayout.ObjectField(i.ToString(), origin.buildSequenceMeshes[i], typeof(Mesh), true);

                            if (GUILayout.Button("X"))
                            {
                                origin.buildSequenceMeshes.RemoveAt(i);
                                SceneScripts.MarkDirtyScene();
                                return;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("Add mesh"))
                        {
                            origin.buildSequenceMeshes.Add(null);
                            SceneScripts.MarkDirtyScene();
                            return;
                        }

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.destroySequenceMeshMode = EditorGUILayout.ToggleLeft("Use destroy sequence meshes", origin.destroySequenceMeshMode);
                EditorGUILayout.EndHorizontal();

                if (origin.destroySequenceMeshMode)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    origin.destroySequenceMeshesPath = EditorGUILayout.TextField(origin.destroySequenceMeshesPath);

                    if (GUILayout.Button("Load"))
                    {
                        int nDestroyFiles = 0;
                        System.IO.DirectoryInfo d = new System.IO.DirectoryInfo(Application.dataPath + "/" + origin.destroySequenceMeshesPath);

                        if (d.Exists)
                        {
                            System.IO.FileInfo[] files = d.GetFiles();

                            for (int i = 0; i < files.Length; i++)
                            {
                                System.IO.FileInfo fi = files[i];

                                if (fi.Name.Contains(".meta") == false)
                                {
                                    if (fi.Name.Contains("d.asset"))
                                    {
                                        nDestroyFiles++;
                                    }
                                }
                            }

                            bool markSceneDirty = false;

                            if (nDestroyFiles > 0)
                            {
                                origin.destroySequenceMeshes.Clear();
                                markSceneDirty = true;
                            }

                            Mesh msh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/" + origin.buildSequenceMeshesPath + "f.asset", typeof(Mesh));

                            if (msh != null)
                            {
                                origin.destroySequenceMeshes.Add(msh);
                                markSceneDirty = true;
                            }

                            for (int i = 0; i < nDestroyFiles; i++)
                            {
                                msh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/" + origin.destroySequenceMeshesPath + (i + 1).ToString() + "d.asset", typeof(Mesh));

                                if (msh != null)
                                {
                                    origin.destroySequenceMeshes.Add(msh);
                                    markSceneDirty = true;
                                }
                            }

                            if (markSceneDirty)
                            {
                                SceneScripts.MarkDirtyScene();
                                return;
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    destroySequenceMeshes = EditorGUILayout.Foldout(destroySequenceMeshes, "Destroy sequence meshes");
                    EditorGUILayout.EndHorizontal();

                    if (destroySequenceMeshes)
                    {
                        curColor = GUI.color;
                        GUI.color = new Color(1f, 0.95f, 0.95f, 1);
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        GUI.color = curColor;

                        for (int i = 0; i < origin.destroySequenceMeshes.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            origin.destroySequenceMeshes[i] = (Mesh)EditorGUILayout.ObjectField(i.ToString(), origin.destroySequenceMeshes[i], typeof(Mesh), true);

                            if (GUILayout.Button("X"))
                            {
                                origin.destroySequenceMeshes.RemoveAt(i);
                                SceneScripts.MarkDirtyScene();
                                return;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("Add mesh"))
                        {
                            origin.destroySequenceMeshes.Add(null);
                            SceneScripts.MarkDirtyScene();
                            return;
                        }

                        EditorGUILayout.EndVertical();
                    }
                }

                if (origin.buildSequenceMeshMode == false)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    buildSequencePrefabs = EditorGUILayout.Foldout(buildSequencePrefabs, "Build sequence prefabs");
                    EditorGUILayout.EndHorizontal();

                    if (buildSequencePrefabs)
                    {
                        curColor = GUI.color;
                        GUI.color = new Color(1f, 0.95f, 0.95f, 1);
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        GUI.color = curColor;

                        for (int i = 0; i < origin.buildSequencePrefabs.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            origin.buildSequencePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(i.ToString(), origin.buildSequencePrefabs[i], typeof(GameObject), true);

                            if (GUILayout.Button("X"))
                            {
                                origin.buildSequencePrefabs.RemoveAt(i);
                                SceneScripts.MarkDirtyScene();
                                return;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("Add prefab"))
                        {
                            origin.buildSequencePrefabs.Add(null);
                            SceneScripts.MarkDirtyScene();
                            return;
                        }

                        EditorGUILayout.EndVertical();
                    }
                }

                if (origin.destroySequenceMeshMode == false)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    destroySequencePrefabs = EditorGUILayout.Foldout(destroySequencePrefabs, "Destroy sequence prefabs");
                    EditorGUILayout.EndHorizontal();

                    if (destroySequencePrefabs)
                    {
                        curColor = GUI.color;
                        GUI.color = new Color(1f, 0.95f, 0.95f, 1);
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        GUI.color = curColor;

                        for (int i = 0; i < origin.destroySequencePrefabs.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            origin.destroySequencePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(i.ToString(), origin.destroySequencePrefabs[i], typeof(GameObject), true);

                            if (GUILayout.Button("X"))
                            {
                                origin.destroySequencePrefabs.RemoveAt(i);
                                SceneScripts.MarkDirtyScene();
                                return;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("Add prefab"))
                        {
                            origin.destroySequencePrefabs.Add(null);
                            SceneScripts.MarkDirtyScene();
                            return;
                        }

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.isArcher = EditorGUILayout.ToggleLeft("Is archer", origin.isArcher);
            EditorGUILayout.EndHorizontal();

            if (origin.isArcher)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.arrow = (GameObject)EditorGUILayout.ObjectField("Arrow", origin.arrow, typeof(GameObject), true);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.arrowOffset = EditorGUILayout.Vector3Field("Arrow offset", origin.arrowOffset);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.velArrow = EditorGUILayout.FloatField("Arrow velocity", origin.velArrow);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.isWizzard = EditorGUILayout.ToggleLeft("Is wizzard", origin.isWizzard);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.hasBullets = EditorGUILayout.ToggleLeft("Has bullets", origin.hasBullets);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.isWorker = EditorGUILayout.ToggleLeft("Is worker", origin.isWorker);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            combatPropFoldout = EditorGUILayout.Foldout(combatPropFoldout, "Combat properties");
            EditorGUILayout.EndHorizontal();

            if (combatPropFoldout)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.searchDistance = EditorGUILayout.FloatField("Target search distance", origin.searchDistance);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.maxAttackers = EditorGUILayout.IntField("Max attackers", origin.maxAttackers);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.stopDistOut = EditorGUILayout.FloatField("Outer stop distance", origin.stopDistOut);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.critFailedR = EditorGUILayout.IntField("Critical failed R", origin.critFailedR);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.attackWaiter = EditorGUILayout.FloatField("Attack waiter", origin.attackWaiter);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.attackDelay = EditorGUILayout.FloatField("Attack delay", origin.attackDelay);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.damageCoolDownTime = EditorGUILayout.FloatField("Damage cool down time", origin.damageCoolDownTime);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.damageCoolDownMin = EditorGUILayout.FloatField("Minimum damage cool down", origin.damageCoolDownMin);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.damageCoolDownMax = EditorGUILayout.FloatField("Maximum damage cool down", origin.damageCoolDownMax);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            combatStatFoldout = EditorGUILayout.Foldout(combatStatFoldout, "Combat stats");
            EditorGUILayout.EndHorizontal();

            if (combatStatFoldout)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.health = EditorGUILayout.FloatField("Health", origin.health);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.maxHealth = EditorGUILayout.FloatField("Maximum health", origin.maxHealth);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.selfHealFactor = EditorGUILayout.FloatField("Self-healing factor", origin.selfHealFactor);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.strength = EditorGUILayout.FloatField("Strength", origin.strength);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.defence = EditorGUILayout.FloatField("Defence", origin.defence);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            attackSoundsFoldout = EditorGUILayout.Foldout(attackSoundsFoldout, "Attack sounds");
            EditorGUILayout.EndHorizontal();

            if (attackSoundsFoldout)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                for (int i = 0; i < origin.attackSounds.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    origin.attackSounds[i] = (AudioClip)EditorGUILayout.ObjectField(origin.attackSounds[i], typeof(AudioClip), true);
                    if (GUILayout.Button("X"))
                    {
                        origin.attackSounds.RemoveAt(i);
                        SceneScripts.MarkDirtyScene();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add attack sound"))
                {
                    origin.attackSounds.Add(null);
                    SceneScripts.MarkDirtyScene();
                    return;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            deathSoundsFoldout = EditorGUILayout.Foldout(deathSoundsFoldout, "Death sounds");
            EditorGUILayout.EndHorizontal();

            if (deathSoundsFoldout)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                for (int i = 0; i < origin.deathSounds.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    origin.deathSounds[i] = (AudioClip)EditorGUILayout.ObjectField(origin.deathSounds[i], typeof(AudioClip), true);

                    if (GUILayout.Button("X"))
                    {
                        origin.deathSounds.RemoveAt(i);
                        SceneScripts.MarkDirtyScene();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add death sound"))
                {
                    origin.deathSounds.Add(null);
                    SceneScripts.MarkDirtyScene();
                    return;
                }

                EditorGUILayout.EndVertical();
            }


            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.maxDeathCalls = EditorGUILayout.IntField("Max death calls", origin.maxDeathCalls);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.rEnclosed = EditorGUILayout.FloatField("Enclosed radius", origin.rEnclosed);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.unitCenter = EditorGUILayout.Vector3Field("Unit center", origin.unitCenter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            origin.unitSize = EditorGUILayout.FloatField("Unit size", origin.unitSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            levels = EditorGUILayout.Foldout(levels, "Unit levels");
            EditorGUILayout.EndHorizontal();

            if (levels)
            {
                for (int i = 0; i < origin.levelNames.Count; i++)
                {
                    curColor = GUI.color;
                    GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = curColor;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    origin.levelNames[i] = EditorGUILayout.TextField("Level", origin.levelNames[i]);

                    if (GUILayout.Button("X"))
                    {
                        origin.levelNames.RemoveAt(i);
                        origin.levelExpTimeGain.RemoveAt(i);
                        SceneScripts.MarkDirtyScene();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    origin.levelExpTimeGain[i] = EditorGUILayout.Vector2Field("Level exp time gain", origin.levelExpTimeGain[i]);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add unit level"))
                {
                    origin.levelNames.Add("");
                    origin.levelExpTimeGain.Add(new Vector2(0, 0));
                    SceneScripts.MarkDirtyScene();
                    return;
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            smokes = EditorGUILayout.Foldout(smokes, "Smokes");
            EditorGUILayout.EndHorizontal();

            if (smokes)
            {
                for (int i = 0; i < origin.smokes.Count; i++)
                {
                    curColor = GUI.color;
                    GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = curColor;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    origin.smokes[i] = (ParticleSystem)EditorGUILayout.ObjectField("Smoke", origin.smokes[i], typeof(ParticleSystem), true);

                    if (GUILayout.Button("X"))
                    {
                        origin.smokes.RemoveAt(i);
                        SceneScripts.MarkDirtyScene();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add smoke"))
                {
                    origin.smokes.Add(null);
                    SceneScripts.MarkDirtyScene();
                    return;
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            if (GUILayout.Button("..."))
            {
                List<ParticleSystem> pSystems = new List<ParticleSystem>();

                foreach (Transform child in origin.gameObject.transform)
                {
                    ParticleSystem ps = child.gameObject.GetComponent<ParticleSystem>();

                    if (ps != null)
                    {
                        pSystems.Add(ps);
                    }
                }

                if(pSystems.Count > 0)
                {
                    origin.smokes = pSystems;
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (origin.smokes != null)
            {
                curColor = GUI.color;
                GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.color = curColor;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.smokeMinUpdateTime = EditorGUILayout.FloatField("Smoke minimum update time", origin.smokeMinUpdateTime);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                origin.smokeMaxUpdateTime = EditorGUILayout.FloatField("Smoke maximum update time", origin.smokeMaxUpdateTime);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            costs = EditorGUILayout.Foldout(costs, "Costs");
            EditorGUILayout.EndHorizontal();

            if (costs)
            {
                for (int i = 0; i < origin.costs.Count; i++)
                {
                    curColor = GUI.color;
                    GUI.color = new Color(0.95f, 1f, 0.95f, 1);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = curColor;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    origin.costs[i].name = EditorGUILayout.TextField("Resource", origin.costs[i].name);

                    if (GUILayout.Button("X"))
                    {
                        origin.costs.RemoveAt(i);
                        SceneScripts.MarkDirtyScene();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    origin.costs[i].amount = EditorGUILayout.IntField("Amount", origin.costs[i].amount);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add cost"))
                {
                    origin.costs.Add(new EconomyResourceUnitPars());
                    SceneScripts.MarkDirtyScene();
                    return;
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
