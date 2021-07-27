using RTSToolkit;
using UnityEditor;

namespace RTSToolkitEditor
{
    [CustomEditor(typeof(UnitPars))]
    public class UnitParsEditor : Editor
    {

        public UnitPars origin;

        public override void OnInspectorGUI()
        {
            origin = (UnitPars)target;
            DrawDefaultInspector();
        }
    }
}
