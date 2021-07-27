using RTSToolkit;
using UnityEditor;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(NationSpawner))]
    public class NationSpawnerEditor : Editor
    {
        public NationSpawner origin;

        public override void OnInspectorGUI()
        {
            origin = (NationSpawner)target;
            DrawDefaultInspector();
        }
    }
}
