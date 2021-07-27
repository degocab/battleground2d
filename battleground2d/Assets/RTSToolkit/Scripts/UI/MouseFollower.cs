using UnityEngine;

namespace RTSToolkit
{
    public class MouseFollower : MonoBehaviour
    {
        RectTransform rt;
        public Vector2 offset;

        void Start()
        {
            rt = GetComponent<RectTransform>();
        }

        void Update()
        {
            rt.anchoredPosition3D = new Vector3(Input.mousePosition.x + offset.x, Input.mousePosition.y + offset.y, 0);
        }
    }
}
