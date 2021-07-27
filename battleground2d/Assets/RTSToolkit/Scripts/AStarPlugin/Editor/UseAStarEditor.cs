using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(UseAStar))]
    public class UseAStarEditor : Editor
    {
        public UseAStar origin;

        public override void OnInspectorGUI()
        {
            origin = (UseAStar)target;

            if (origin.waitforcompile)
            {
                if (EditorApplication.isCompiling == false)
                {
                    origin.waitforcompile = false;
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (UseAStar.IfExists())
            {
                origin.useAstar = GUILayout.Toggle(origin.useAstar, "Use AStar");
                if (origin.useAstar != origin.aStarSwitched)
                {
                    origin.aStarSwitched = origin.useAstar;
                    origin.SwitchUseAStar();
                }
            }
            else
            {
                if (origin.useAstar)
                {
                    origin.aStarSwitched = false;
                    origin.SwitchUseAStar();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
