using UnityEngine;
using UnityEditor;
using RTSToolkit;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(TimeOfDay))]
    public class TimeOfDayEditor : Editor
    {
        public TimeOfDay origin = null;

        public override void OnInspectorGUI()
        {
            origin = (TimeOfDay)target;
            DrawDefaultInspector();

            if (RenderSettings.skybox.name == "TOD_SYSTEM_FREE_SKY")
            {
                origin.moonTexture = (Texture)EditorGUILayout.ObjectField("Moon texture", origin.moonTexture, typeof(Texture), true);
                origin.starsTexture = (Texture)EditorGUILayout.ObjectField("Stars cubemap", origin.starsTexture, typeof(Texture), true);
                origin.starsNoiseTexture = (Texture)EditorGUILayout.ObjectField("Stars noise cubemap", origin.starsNoiseTexture, typeof(Texture), true);
            }
        }
    }
}
