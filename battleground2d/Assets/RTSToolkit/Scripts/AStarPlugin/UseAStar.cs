// #pragma warning disable 0618 // WebPlayer was removed in 5.4, consider using WebGL.

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

namespace RTSToolkit
{
    [ExecuteInEditMode]
    public class UseAStar : MonoBehaviour
    {
        public static UseAStar active;
        public static bool isAStarOn = false;

        public bool waitforcompile = false;
        public bool useAstar = false;
        public bool aStarSwitched = false;

        void Start()
        {

        }

        void OnGUI()
        {
            if (UseAStar.active == null)
            {
                UseAStar.active = this;
            }

            if (UseAStar.IfExists() == false)
            {
                if (useAstar)
                {
                    aStarSwitched = false;
                    SwitchUseAStar();
                }
            }
        }

        public static UseAStar GetActive()
        {
            if (UseAStar.active == null)
            {
                UseAStar.active = UnityEngine.Object.FindObjectOfType<UseAStar>();
            }

            return UseAStar.active;
        }

        public void Update_E()
        {
#if UNITY_EDITOR
#if ASTAR
            RTSMaster rtsm = RTSMaster.GetActive();

            if (useAstar)
            {
                if (rtsm.useAStar == false)
                {
                    rtsm.useAStar = true;
                    rtsm.SwitchPrefabsToAStar();
                    Animals animals = Animals.GetActive();

                    if (animals != null)
                    {
                        animals.SwitchPrefabsToAStar();
                    }

                    SceneScripts.SaveScene();
                }
            }
            else
            {
                if (rtsm.useAStar == true)
                {
                    rtsm.useAStar = false;
                    rtsm.SwitchPrefabsToUnityNavMesh();
                    Animals animals = Animals.GetActive();

                    if (animals != null)
                    {
                        animals.SwitchPrefabsToUnityNavMesh();
                    }

                    SceneScripts.SaveScene();
                }
            }
#endif
#endif
        }

        public void SwitchUseAStar()
        {
            if (useAstar)
            {
#if UNITY_EDITOR
                if (!PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains("ASTAR"))
                {
                    string symbols = AddAstarToString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
                    waitforcompile = true;
                }

                if (!PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS).Contains("ASTAR"))
                {
                    string symbols = AddAstarToString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, symbols);
                    waitforcompile = true;
                }

                if (!PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Contains("ASTAR"))
                {
                    string symbols = AddAstarToString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
                    waitforcompile = true;
                }

                if (!PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL).Contains("ASTAR"))
                {
                    string symbols = AddAstarToString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, symbols);
                    waitforcompile = true;
                }
#endif
            }
            else
            {
#if UNITY_EDITOR
                if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains("ASTAR"))
                {
#if ASTAR
		  		    AStarCompiler.GetActive().Clean();
#endif
                    string symbols = RemoveAstarFromString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
                    waitforcompile = true;
                }

                if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS).Contains("ASTAR"))
                {
#if ASTAR
		  		    AStarCompiler.GetActive().Clean();
#endif
                    string symbols = RemoveAstarFromString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, symbols);
                    waitforcompile = true;
                }

                if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Contains("ASTAR"))
                {
#if ASTAR
		  		    AStarCompiler.GetActive().Clean();
#endif
                    string symbols = RemoveAstarFromString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
                    waitforcompile = true;
                }

                if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL).Contains("ASTAR"))
                {
#if ASTAR
		  		    AStarCompiler.GetActive().Clean();
#endif

                    string symbols = RemoveAstarFromString(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, symbols);
                    waitforcompile = true;
                }
#endif
            }
        }

        string AddAstarToString(string symbols)
        {
            if (string.IsNullOrEmpty(symbols))
            {
                symbols = symbols + "ASTAR";
            }
            else
            {
                symbols = symbols + ";ASTAR";
            }

            return symbols;
        }

        string RemoveAstarFromString(string symbols)
        {
            symbols = symbols.Replace(";ASTAR;", ";");
            symbols = symbols.Replace("ASTAR;", "");
            symbols = symbols.Replace(";ASTAR", "");
            symbols = symbols.Replace("ASTAR", "");
            return symbols;
        }

        public static bool IfExists()
        {
            bool existsHere = false;
            var demoClass = System.Type.GetType("AIPath");

            if (demoClass != null)
            {
                existsHere = true;
            }

            return existsHere;
        }

        public static UseAStar GetCurrent()
        {
            UseAStar[] allObjects = UnityEngine.Object.FindObjectsOfType<UseAStar>();
            UseAStar obj = null;

            if (allObjects.Length > 0)
            {
                obj = allObjects[0];
            }

            return obj;
        }

#if UNITY_EDITOR
        [InitializeOnLoad]
        class EditorUpdater
        {
            static EditorUpdater()
            {
                EditorApplication.update += Update;
            }

            static void Update()
            {
                UseAStar uas = UseAStar.GetActive();
                if (uas != null)
                {
                    uas.Update_E();
                }
            }
        }
#endif
    }
}
