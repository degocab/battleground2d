using UnityEngine;

namespace RTSToolkit
{
    public class CameraFollower : MonoBehaviour
    {
        Transform cam;

        void Start()
        {
            cam = Camera.main.transform;
        }

        void Update()
        {
            transform.position = new Vector3(cam.position.x, transform.position.y, cam.position.z);
        }
    }
}
