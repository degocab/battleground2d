using UnityEngine;

namespace RTSToolkit
{
    public class CameraTeleport : MonoBehaviour
    {
        Vector3 prevPos = Vector3.zero;
        public Vector3 teleportPosition = Vector3.zero;
        bool isTeleported = false;
        public KeyCode teleportKey = KeyCode.B;

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(teleportKey))
            {
                TeleportCamera();
            }
        }

        void TeleportCamera()
        {
            if (isTeleported == false)
            {
                isTeleported = true;
                prevPos = RTSCamera.active.transform.position;
                RTSCamera.active.transform.position = teleportPosition;
            }
            else
            {
                isTeleported = false;
                RTSCamera.active.transform.position = prevPos;
            }
        }
    }
}
