using UnityEngine;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class RTSCamera : MonoBehaviour
    {
        public static RTSCamera active;

        public float zoomSpeed = 1.0f;
        public float moveSpeed = 1.0f;
        public float rotationSpeed = 1.0f;

#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
        float ScrollEdge = 0.01f;
#endif

        public float minZoomHeight = 3.0f;
        public float maxZoomHeight = 200.0f;

        public float minCameraHeight = 58f;

        float camHeight;

        Transform camTransform;

        Terrain terrain;

        public bool followHeightmap = true;

        [HideInInspector] public int mobileCameraMode = 1;
        [HideInInspector] public bool isMultiplayer = false;

        public bool edgeMovement = false;

        public KeyCode moveRight = KeyCode.D;
        public KeyCode moveLeft = KeyCode.A;
        public KeyCode moveForward = KeyCode.W;
        public KeyCode moveBackward = KeyCode.S;

        public KeyCode rotateRight;
        public KeyCode rotateLeft;
        public KeyCode rotateUp;
        public KeyCode rotateDown;

        public KeyCode zoomInKey;
        public KeyCode zoomOutKey;

        Vector3 currentMovementVector = Vector3.zero;
        public float moveAcceleration = 0.05f;

        public bool rotateWithMouse = true;
        public bool zoomWithMouse = true;

        public bool flipHorizontalRotation = false;
        public bool flipVerticalRotation = false;
        public bool flipZoom = false;

        public float terrainLengthToFarClippingPlaneMultiplier = 3f;

        void Awake()
        {
            active = this;
            camTransform = transform;

#if UNITY_IPHONE || UNITY_ANDROID
		    Input.simulateMouseWithTouches = true;
#endif
        }

        public void DisableSinglePlayerCamera()
        {
            if (isMultiplayer)
            {
                GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

                for (int i = 0; i < allObjects.Length; i++)
                {
                    GameObject go = allObjects[i];

                    if (go.GetComponent<RTSCamera>() != null)
                    {
                        RTSCamera rtsc = go.GetComponent<RTSCamera>();

                        if (rtsc.isMultiplayer == false)
                        {
                            go.SetActive(false);
                        }
                    }
                }

                transform.position = new Vector3(100f, 100f, 100f);
            }
        }

        void Start()
        {
            Camera cam = GetComponent<Camera>();

            if (cam != null)
            {
                if (GenerateTerrain.active != null)
                {
                    cam.farClipPlane = terrainLengthToFarClippingPlaneMultiplier * GenerateTerrain.active.length;
                }
            }

            if (GenerateTerrain.active != null)
            {
                Terrain terrain2 = TerrainProperties.GetTerrainBellow(camTransform.position);

                if ((terrain2 != null) && (terrain2 != terrain))
                {
                    terrain = terrain2;
                }
            }

            CorrectForHeight();
        }

        void Update()
        {
            if (isMultiplayer == false)
            {
                CameraUpdate();
            }
        }

        public void CameraUpdate()
        {
            bool terrainSwithced = false;

            if (GenerateTerrain.active != null)
            {
                Terrain terrain2 = TerrainProperties.GetTerrainBellow(camTransform.position);

                if ((terrain2 != null) && (terrain2 != terrain))
                {
                    terrain = terrain2;
                    terrainSwithced = true;
                }
            }

            float gameSpeed = Time.timeScale;
            float dTime = Time.deltaTime;
            float dT = dTime / gameSpeed;

            if (gameSpeed == 0)
            {
                dT = 0;
            }

            if (terrainSwithced == false)
            {
                if (terrain != null)
                {
                    float terHeight = terrain.SampleHeight(camTransform.position);
                    camHeight = camTransform.position.y - terHeight - terrain.transform.position.y;
                }
                else
                {
                    camHeight = camTransform.position.y;
                }

                Vector3 moveNext = Vector3.zero;

                if (!Input.GetKey("mouse 2"))
                {
                    bool movePassed = false;

                    float slideSpeed = 1f;

#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
                    Vector3 v3 = Vector3.zero;

                    if (Input.GetKey(moveRight))
                    {
                        v3 = v3 + GetMovementVector(270f, 1f);
                    }

                    if (Input.GetKey(moveLeft))
                    {
                        v3 = v3 + GetMovementVector(90f, 1f);
                    }

                    if (Input.GetKey(moveForward))
                    {
                        v3 = v3 + GetMovementVector(0f, 1f);
                    }

                    if (Input.GetKey(moveBackward))
                    {
                        v3 = v3 + GetMovementVector(180f, 1f);
                    }

                    if (
                        (Input.mousePosition.x >= Screen.width * (1 - ScrollEdge)) ||
                        (Input.mousePosition.x <= Screen.width * ScrollEdge) ||
                        (Input.mousePosition.y >= Screen.height * (1 - ScrollEdge)) ||
                        (Input.mousePosition.y <= Screen.height * ScrollEdge)
                    )
                    {
                        if (edgeMovement)
                        {
                            v3 = v3 + GetMovementVector(GetMouseAngle(), 1f);
                        }
                    }

                    if (v3.sqrMagnitude > 0)
                    {
                        v3 = v3.normalized;
                    }

                    currentMovementVector = (1f - moveAcceleration) * currentMovementVector + moveAcceleration * v3;
                    movePassed = true;
                    moveNext = currentMovementVector * dT * moveSpeed * camHeight * slideSpeed;
#endif

#if UNITY_IPHONE || UNITY_ANDROID
                    movePassed = false;
                    float angleToRotate = 0f;
                    moveNext = Vector3.zero;

                    if (mobileCameraMode == 1)
                    {
                        Vector2 touchDeltaPosition = Vector2.zero;

                        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                        {
                            touchDeltaPosition = Input.GetTouch(0).deltaPosition;
                        }

#if UNITY_EDITOR
                        if (Input.GetMouseButton(0))
                        {
                            movePassed = false;
                            angleToRotate = 0f;
                            moveNext = Vector3.zero;
                            touchDeltaPosition = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                        }
#endif
                        if (touchDeltaPosition.sqrMagnitude > 0f)
                        {
                            movePassed = true;
                            angleToRotate = GetTouchMoveAngle(touchDeltaPosition);
                            moveNext = GetMovementVector(angleToRotate, dT * moveSpeed * camHeight * slideSpeed * touchDeltaPosition.magnitude);

                            if (moveNext.magnitude > dT * moveSpeed * camHeight * slideSpeed)
                            {
                                moveNext = moveNext.normalized * dT * moveSpeed * camHeight * slideSpeed;
                            }

                            slideSpeed = touchDeltaPosition.magnitude;
                        }
                    }
#endif
                    if (movePassed == true)
                    {
                        MoveCamera(moveNext);

                        if (followHeightmap == true)
                        {
                            if (terrain != null)
                            {
                                camTransform.position = new Vector3(
                                    camTransform.position.x,
                                    terrain.SampleHeight(camTransform.position) + terrain.transform.position.y + camHeight,
                                    camTransform.position.z
                                );
                            }
                        }
                    }
                }

                // ZOOM		
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
                if (zoomWithMouse)
                {
                    float msw = Input.GetAxis("Mouse ScrollWheel");

                    if (msw != 0)
                    {
                        if (SelectionManager.active.isMouseOnActiveScreen)
                        {
                            if (flipZoom)
                            {
                                msw = -msw;
                            }

                            CameraZoom(dT, msw / Mathf.Abs(msw));
                        }
                    }
                }

                if (Input.GetKey(zoomInKey))
                {
                    CameraZoom(dT, 1);
                }

                if (Input.GetKey(zoomOutKey))
                {
                    CameraZoom(dT, -1);
                }
#endif

#if UNITY_IPHONE || UNITY_ANDROID
                if (mobileCameraMode == 3)
                {
                    Vector2 touchDeltaPosition = Vector2.zero;

                    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        touchDeltaPosition = rotationSpeed * Input.GetTouch(0).deltaPosition;

                    }
#if UNITY_EDITOR
                    if (Input.GetMouseButton(0))
                    {
                        touchDeltaPosition = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                    }
#endif
                    if (touchDeltaPosition.sqrMagnitude != 0f)
                    {
                        CameraZoom(dT, touchDeltaPosition.y / Mathf.Abs(touchDeltaPosition.y));
                    }
                }
#endif
            }

            // ROTATION
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
            if (rotateWithMouse)
            {
                if (Input.GetMouseButton(1))
                {
                    float h = -rotationSpeed * Input.GetAxis("Mouse X");
                    float v = rotationSpeed * Input.GetAxis("Mouse Y");

                    if (flipHorizontalRotation)
                    {
                        h = -h;
                    }
                    if (flipVerticalRotation)
                    {
                        v = -v;
                    }

                    RotateCamera(v, h);
                }
            }

            if (Input.GetKey(rotateRight))
            {
                RotateCamera(0, rotationSpeed);
            }

            if (Input.GetKey(rotateLeft))
            {
                RotateCamera(0, -rotationSpeed);
            }

            if (Input.GetKey(rotateUp))
            {
                RotateCamera(-rotationSpeed, 0);
            }

            if (Input.GetKey(rotateDown))
            {
                RotateCamera(rotationSpeed, 0);
            }
#endif

#if UNITY_IPHONE || UNITY_ANDROID
            if (mobileCameraMode == 2)
            {
                Vector2 touchDeltaPosition = Vector2.zero;

                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    touchDeltaPosition = rotationSpeed * Input.GetTouch(0).deltaPosition;

                }
#if UNITY_EDITOR
                if (Input.GetMouseButton(0))
                {
                    touchDeltaPosition = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                }
#endif
                if (touchDeltaPosition.sqrMagnitude > 0f)
                {
                    RotateCamera(touchDeltaPosition.y, touchDeltaPosition.x);
                }
            }
#endif
            if (camTransform.position.y < minCameraHeight)
            {
                camTransform.position = new Vector3(camTransform.position.x, minCameraHeight, camTransform.position.z);
            }
        }

        void RotateCamera(float v, float h)
        {
            camTransform.Rotate(0, h, 0, Space.World);
            camTransform.Rotate(v, 0, 0);

            if ((camTransform.rotation.eulerAngles.x >= 90) && (camTransform.rotation.eulerAngles.x <= 180))
            {
                camTransform.Rotate(-v, 0, 0);
            }

            if (((camTransform.rotation.eulerAngles.x >= 180) && (camTransform.rotation.eulerAngles.x <= 270)) || (camTransform.rotation.eulerAngles.x < 0))
            {
                camTransform.Rotate(-v, 0, 0);
            }

            if ((camTransform.rotation.eulerAngles.z >= 160) && (camTransform.rotation.eulerAngles.z <= 200))
            {
                camTransform.Rotate(-v, 0, 0);
            }
        }

        void MoveCamera(float angle, float v)
        {
            MoveCamera(GetMovementVector(angle, v));
        }

        void MoveCamera(Vector3 dir)
        {
            camTransform.position = camTransform.position + dir;
        }

        public Vector3 GetMovementVector(float angle, float v)
        {
            Vector3 fwd = camTransform.TransformDirection(Vector3.forward);
            Vector3 dir = (GenericMath.RotAround(angle, new Vector3(fwd.x, 0f, fwd.z), new Vector3(0f, 1f, 0f))).normalized;
            return (v * dir);
        }

        float GetMouseAngle()
        {
            Vector2 mPos = Input.mousePosition;
            Vector2 cPos = new Vector2(Screen.width / 2, Screen.height / 2);
            Vector2 hPos = new Vector2(Screen.width / 2, Screen.height);

            return SignedAngleBetween2d((mPos - cPos), (cPos - hPos));
        }

        float GetTouchMoveAngle(Vector2 touchDeltaPosition)
        {
            return SignedAngleBetween2d(touchDeltaPosition, new Vector2(0f, 1f));
        }

        void CameraZoom(float dT, float dr)
        {
            int dr1 = 1;

            if (dr < 0)
            {
                dr1 = -1;
            }

            camTransform.position = camTransform.position + 2f * dr1 * dT * zoomSpeed * camHeight * camTransform.forward;
            CorrectForHeight();
        }

        void CorrectForHeight()
        {
            if (terrain != null)
            {
                float terHeight = terrain.SampleHeight(camTransform.position);
                camHeight = camTransform.position.y - terHeight - terrain.transform.position.y;

                if (camHeight < minZoomHeight)
                {
                    camTransform.position = new Vector3(camTransform.position.x, terHeight + minZoomHeight, camTransform.position.z);
                }

                if (camHeight > maxZoomHeight)
                {
                    camTransform.position = new Vector3(camTransform.position.x, terHeight + maxZoomHeight, camTransform.position.z);
                }
            }
        }

        Terrain GetTerrainBellow()
        {
            // Gets terrain bellow camera using raycast
            Terrain locTerrain = null;

            Ray ray = new Ray();
            ray.direction = new Vector3(0f, -1f, 0f);
            ray.origin = camTransform.position;

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.gameObject.GetComponent<Terrain>() != null)
                    {
                        locTerrain = hit.collider.gameObject.GetComponent<Terrain>();
                        camHeight = hit.distance;
                    }
                }
            }

            return locTerrain;
        }

        float AverageHeight()
        {
            float avHeight = 0f;
            float dx = 10f;
            float kk = 0f;

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    Vector3 pos = new Vector3(camTransform.position.x + i * dx, 0f, camTransform.position.z + j * dx);
                    avHeight = avHeight + camTransform.position.y - terrain.SampleHeight(pos) - terrain.transform.position.y;
                    kk = kk + 1f;
                }
            }

            avHeight = avHeight / kk;

            return avHeight;
        }

        float SignedAngleBetween2d(Vector2 a, Vector2 b)
        {
            float angle = Vector2.Angle(a, b);
            float sign = Mathf.Sign(a.y * b.x - a.x * b.y);

            // angle in [-179,180]
            float signed_angle = angle * sign;

            // angle in [0,360] (not used but included here for completeness)
            float angle360 = (signed_angle + 180) % 360;

            return angle360;
        }

        public float ReactionTime(Vector3 pos, float v)
        {
            float t1 = DistanceReactionTime(0.6f, pos, 10f);
            float t2 = VelocityReactionTime(0.03f, v);
            float tf = Mathf.Max(t1, t2);

            if (tf > 0.1f)
            {
                tf = 0.1f + Random.Range(-0.01f, 0.01f);
            }

            return tf;
        }

        public float DistanceReactionTime(float alpha, Vector3 pos, float max_v)
        {
            float d = (transform.position - pos).magnitude * Mathf.Tan(Mathf.Deg2Rad * alpha);
            return d / max_v;
        }

        public float VelocityReactionTime(float min_d, float v)
        {
            return min_d / v;
        }

        [System.Serializable]
        public class RTSKeySet
        {
            public KeyCode key;
            [HideInInspector] public float currentSpeed = 0f;
            public float maxSpeed = 1f;
            public float acceleration = 1f;
            public float deceleration = 1f;

            public void UpdateCurrentSpeed()
            {
                if (Input.GetKey(key))
                {
                    currentSpeed = (1f - acceleration) * currentSpeed + acceleration * maxSpeed;
                }
                else
                {
                    currentSpeed = (1f - deceleration) * currentSpeed;
                }
            }
        }
    }
}
