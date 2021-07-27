using UnityEngine;

namespace RTSToolkit
{
    public class UseJobSystem : MonoBehaviour
    {
        public static bool useJobifiedKdtree_s = false;
        public static bool useJobifiedRenderMeshModels_s = false;

        public bool useOnlyWithBuild = false;
        public bool useJobifiedKdtree = false;
        public bool useJobifiedRenderMeshModels = false;

        void Start()
        {
            useJobifiedKdtree_s = useJobifiedKdtree;
            useJobifiedRenderMeshModels_s = useJobifiedRenderMeshModels;

#if UNITY_EDITOR
            if (useOnlyWithBuild)
            {
                useJobifiedKdtree_s = false;
                useJobifiedRenderMeshModels_s = false;
            }
#endif
            Destroy(this.gameObject);
        }
    }
}
