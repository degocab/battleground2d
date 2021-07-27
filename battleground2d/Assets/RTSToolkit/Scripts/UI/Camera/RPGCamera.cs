using UnityEngine;

namespace RTSToolkit
{
    public class RPGCamera : MonoBehaviour
    {
        public static RPGCamera active;

        public float zoomSpeed = 1.0f;
        public float rotationSpeed = 1.0f;

        [HideInInspector] public UnitPars followPars = null;
        public CameraLooker camLook = null;

        [HideInInspector] public Transform camTransform = null;

        [HideInInspector] public float distance = 50f;
        [HideInInspector] public float distanceOffset = 0f;

        [HideInInspector] public float hAngleOffest = 0f;
        [HideInInspector] public float vAngleOffest = 0f;

        public float maxZoomOut = 200f;

        public KeyCode rotateRight = KeyCode.D;
        public KeyCode rotateLeft = KeyCode.A;
        public KeyCode rotateUp = KeyCode.W;
        public KeyCode rotateDown = KeyCode.S;

        public KeyCode zoomInKey;
        public KeyCode zoomOutKey;

        public bool zoomWithMouse = true;
        public bool flipZoom = false;

        void Awake()
        {
            active = this;
            camTransform = Camera.main.transform;
        }

        void Start()
        {

        }

        public static RPGCamera GetActive()
        {
            if (RPGCamera.active == null)
            {
                RPGCamera.active = UnityEngine.Object.FindObjectOfType<RPGCamera>();
            }

            return RPGCamera.active;
        }

        public void SetLooker(UnitPars follower)
        {
            camLook = new CameraLooker();
            camLook.camTransform = camTransform;
            followPars = follower;
            camTransform = Camera.main.transform;
            float rad = follower.rEnclosed;

            if (follower.thisNMA != null)
            {
                rad = follower.thisNMA.radius;
            }

            distance = 5 * rad;
            distanceOffset = 0f;
            hAngleOffest = 0f;
            vAngleOffest = 0f;
        }

        void Update()
        {
            if (followPars == null)
            {
                if (CameraSwitcher.active != null)
                {
                    CameraSwitcher.active.SwitchToRTS();
                    return;
                }
            }

            camLook.LookAtTransform(followPars.transform.position, distance + distanceOffset, -followPars.transform.rotation.eulerAngles.y + hAngleOffest, -25f + vAngleOffest);
            float rad = followPars.rEnclosed;

            if (followPars.thisNMA != null)
            {
                rad = followPars.thisNMA.radius;
            }

            if (((Input.GetAxis("Mouse ScrollWheel") > 0) && (zoomWithMouse) && (flipZoom == false)) || (Input.GetKey(zoomInKey)))
            {
                if ((distance + distanceOffset) > rad)
                {
                    distanceOffset = distanceOffset - Time.deltaTime * zoomSpeed * (distance + distanceOffset);
                }
            }
            else if (((Input.GetAxis("Mouse ScrollWheel") < 0) && (zoomWithMouse) && (flipZoom == false)) || (Input.GetKey(zoomOutKey)))
            {
                if ((distance + distanceOffset) < maxZoomOut)
                {
                    distanceOffset = distanceOffset + Time.deltaTime * zoomSpeed * (distance + distanceOffset);
                }
            }
            else if (((Input.GetAxis("Mouse ScrollWheel") > 0) && (zoomWithMouse) && (flipZoom)) || (Input.GetKey(zoomInKey)))
            {
                if ((distance + distanceOffset) < maxZoomOut)
                {
                    distanceOffset = distanceOffset + Time.deltaTime * zoomSpeed * (distance + distanceOffset);
                }
            }
            else if (((Input.GetAxis("Mouse ScrollWheel") < 0) && (zoomWithMouse) && (flipZoom)) || (Input.GetKey(zoomOutKey)))
            {
                if ((distance + distanceOffset) > rad)
                {
                    distanceOffset = distanceOffset - Time.deltaTime * zoomSpeed * (distance + distanceOffset);
                }
            }

            if (Input.GetKey(rotateRight))
            {
                hAngleOffest = hAngleOffest + rotationSpeed;
                if (hAngleOffest > 360f)
                {
                    hAngleOffest = hAngleOffest - 360f;
                }
            }

            if (Input.GetKey(rotateLeft))
            {
                hAngleOffest = hAngleOffest - rotationSpeed;
                if (hAngleOffest < -360f)
                {
                    hAngleOffest = hAngleOffest + 360f;
                }
            }

            if (Input.GetKey(rotateDown))
            {
                if ((vAngleOffest - 25f) < 0f)
                {
                    vAngleOffest = vAngleOffest + rotationSpeed;
                }
            }

            if (Input.GetKey(rotateUp))
            {
                if ((vAngleOffest - 25f) > -90f)
                {
                    vAngleOffest = vAngleOffest - rotationSpeed;
                }
            }
        }
    }
}
