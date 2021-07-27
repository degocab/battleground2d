using UnityEngine;

namespace RTSToolkit
{
    public class MinimapFollow : MonoBehaviour
    {
        public RectTransform uiToRotate;
        Camera thisCamera;

        public float minOrthographicSize = 100f;
        public float maxOrthographicSize = 3000f;

        public float zoomSpeed = 5f;

        void Start()
        {
            thisCamera = GetComponent<Camera>();
        }

        void Update()
        {
            Transform cam = Camera.main.transform;
            transform.position = cam.position + new Vector3(0, 300, 0);
            uiToRotate.rotation = Quaternion.Euler(0, 0, cam.eulerAngles.y);

            if (thisCamera != null)
            {
                if (MinimapPointer.active != null)
                {
                    if (MinimapPointer.active.isPointerOnMinimap)
                    {
                        float msw = Input.GetAxis("Mouse ScrollWheel");

                        if (msw != 0)
                        {
                            thisCamera.orthographicSize = thisCamera.orthographicSize - zoomSpeed * msw;

                            if (thisCamera.orthographicSize > maxOrthographicSize)
                            {
                                thisCamera.orthographicSize = maxOrthographicSize;
                            }

                            if (thisCamera.orthographicSize < minOrthographicSize)
                            {
                                thisCamera.orthographicSize = minOrthographicSize;
                            }
                        }
                    }
                }
            }
        }
    }
}
