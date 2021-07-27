using UnityEngine;

namespace RTSToolkit
{
    [System.Serializable]
    public class LODNode
    {
        public float viewDistance;
        public GameObject lodGameObject;
        [HideInInspector] public GameObject lodGameObjectPrefab;
        [HideInInspector] public Animation animation;
    }
}
