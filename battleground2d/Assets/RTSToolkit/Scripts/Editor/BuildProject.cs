using System.IO;
using UnityEditor;
using System.Collections.Generic;

namespace RTSToolkitEditor
{
    public static class BuildProject
    {
        const string version = "2018_2";

        [MenuItem("File/BuildProject/MacOS Run")]
        static void MacOSBuildRun()
        {
            List<string> levels = GetLevels();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            BuildPipeline.BuildPlayer(levels.ToArray(), "Builds/MacOS/RTSToolkitMac.app", BuildTarget.StandaloneOSX, BuildOptions.AutoRunPlayer);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("File/BuildProject/MacOS")]
        static void MacOSBuild()
        {
            List<string> levels = GetLevels();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            BuildPipeline.BuildPlayer(levels.ToArray(), "Builds/MacOS/RTSToolkitMac.app", BuildTarget.StandaloneOSX, BuildOptions.CompressWithLz4HC);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("File/BuildProject/WebGL Facebook")]
        static void WebGLFacebookBuild()
        {

            if (Directory.Exists("Builds/RTSToolkitFB/WebGL/"))
            {
                Directory.Delete("Builds/RTSToolkitFB/WebGL", true);
            }

            List<string> levels = GetLevels();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            BuildPipeline.BuildPlayer(levels.ToArray(), "Builds/RTSToolkitFB/WebGL", BuildTarget.WebGL, BuildOptions.CompressWithLz4HC);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("File/BuildProject/Itchio Mac")]
        static void ItchioMacOSBuild()
        {
            List<string> levels = GetLevels();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            BuildPipeline.BuildPlayer(levels.ToArray(), "Builds/Itchio/MacOS_" + version + "/RTSToolkitMac.app", BuildTarget.StandaloneOSX, BuildOptions.CompressWithLz4HC);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("File/BuildProject/Itchio Windows")]
        static void ItchioWindowsBuild()
        {
            List<string> levels = GetLevels();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
            BuildPipeline.BuildPlayer(levels.ToArray(), "Builds/Itchio/Windows_" + version + "/RTSToolkitWin.exe", BuildTarget.StandaloneWindows, BuildOptions.CompressWithLz4HC);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("File/BuildProject/Itchio Linux")]
        static void ItchioLinuxBuild()
        {
            List<string> levels = GetLevels();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
            BuildPipeline.BuildPlayer(levels.ToArray(), "Builds/Itchio/Linux_" + version + "/RTSToolkitLin", BuildTarget.StandaloneLinux64, BuildOptions.CompressWithLz4HC);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("File/BuildProject/Itchio non mobile")]
        static void ItchioNonMobileBuild()
        {
            ItchioMacOSBuild();
            ItchioWindowsBuild();
            ItchioLinuxBuild();
        }

        static List<string> GetLevels()
        {
            List<string> levels = new List<string>();

            levels.Add("Assets/RTSToolkit/Scenes/OpenLobby.unity");
            levels.Add("Assets/RTSToolkit/Scenes/Main.unity");
            levels.Add("Assets/RTSToolkit/Scenes/GameOver.unity");

            return levels;
        }
    }
}
