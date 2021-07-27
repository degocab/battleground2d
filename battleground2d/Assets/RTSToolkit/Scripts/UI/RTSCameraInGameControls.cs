using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class RTSCameraInGameControls : MonoBehaviour
    {
        public static RTSCameraInGameControls active;

        [HideInInspector] public Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();
        public Text moveForward, moveBackward, moveLeft, moveRight;
        public Text rotateUp, rotateDown, rotateLeft, rotateRight;
        public Text zoomIn, zoomOut;

        GameObject currentKey;

        public Color regular;
        public Color selected;

        public Toggle edgeMovement;
        public Toggle rotateWithMouse;
        public Toggle zoomWithMouse;
        public Toggle flipHorizontalRotation;
        public Toggle flipVerticalRotation;
        public Toggle flipZoom;

        public InputField moveSpeedInputField;
        public InputField moveAccelerationInputField;
        public InputField rotationSpeedInputField;
        public InputField zoomSpeedInputField;
        public InputField minimumTerrainHeightInputField;
        public InputField maximumTerrainHeightInputField;
        public InputField minimumAbsoluteHeightInputField;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            keys.Add("MoveForward", KeyCode.W);
            keys.Add("MoveBackward", KeyCode.S);
            keys.Add("MoveLeft", KeyCode.A);
            keys.Add("MoveRight", KeyCode.D);

            keys.Add("RotateUp", KeyCode.None);
            keys.Add("RotateDown", KeyCode.None);
            keys.Add("RotateLeft", KeyCode.None);
            keys.Add("RotateRight", KeyCode.None);

            keys.Add("ZoomIn", KeyCode.None);
            keys.Add("ZoomOut", KeyCode.None);


            moveForward.text = keys["MoveForward"].ToString();
            moveBackward.text = keys["MoveBackward"].ToString();
            moveLeft.text = keys["MoveLeft"].ToString();
            moveRight.text = keys["MoveRight"].ToString();

            rotateUp.text = keys["RotateUp"].ToString();
            rotateDown.text = keys["RotateDown"].ToString();
            rotateLeft.text = keys["RotateLeft"].ToString();
            rotateRight.text = keys["RotateRight"].ToString();

            zoomIn.text = keys["ZoomIn"].ToString();
            zoomOut.text = keys["ZoomOut"].ToString();
        }

        void Update()
        {

        }

        void OnGUI()
        {
            if (currentKey != null)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    ChangeKeyCode(KeyCode.None);
                    return;
                }

                Event e = Event.current;

                if (e.isKey)
                {
                    ChangeKeyCode(e.keyCode);
                }
            }
        }

        public void ChangeKeyCode(KeyCode kc)
        {
            keys[currentKey.transform.parent.name] = kc;

            currentKey.transform.GetChild(0).GetComponent<Text>().text = kc.ToString();
            currentKey.GetComponent<Image>().color = regular;

            if (currentKey.transform.parent.name == "MoveForward")
            {
                RTSCamera.active.moveForward = kc;
            }

            if (currentKey.transform.parent.name == "MoveBackward")
            {
                RTSCamera.active.moveBackward = kc;
            }

            if (currentKey.transform.parent.name == "MoveLeft")
            {
                RTSCamera.active.moveLeft = kc;
            }

            if (currentKey.transform.parent.name == "MoveRight")
            {
                RTSCamera.active.moveRight = kc;
            }

            if (currentKey.transform.parent.name == "RotateUp")
            {
                RTSCamera.active.rotateUp = kc;
            }

            if (currentKey.transform.parent.name == "RotateDown")
            {
                RTSCamera.active.rotateDown = kc;
            }

            if (currentKey.transform.parent.name == "RotateLeft")
            {
                RTSCamera.active.rotateLeft = kc;
            }

            if (currentKey.transform.parent.name == "RotateRight")
            {
                RTSCamera.active.rotateRight = kc;
            }

            if (currentKey.transform.parent.name == "ZoomIn")
            {
                RTSCamera.active.zoomInKey = kc;
            }

            if (currentKey.transform.parent.name == "ZoomOut")
            {
                RTSCamera.active.zoomOutKey = kc;
            }

            currentKey = null;
        }

        public void ChangeKey(GameObject clicked)
        {
            if (currentKey != null)
            {
                currentKey.GetComponent<Image>().color = regular;
            }

            currentKey = clicked;
            currentKey.GetComponent<Image>().color = selected;
        }

        public void ChangeEdgeMovement()
        {
            RTSCamera.active.edgeMovement = edgeMovement.isOn;
        }

        public void ChangeRotateWithMouse()
        {
            RTSCamera.active.rotateWithMouse = rotateWithMouse.isOn;
        }

        public void ChangeZoomWithMouse()
        {
            RTSCamera.active.zoomWithMouse = zoomWithMouse.isOn;
        }

        public void ChangeFlipHorizontalRotation()
        {
            RTSCamera.active.flipHorizontalRotation = flipHorizontalRotation.isOn;
        }
        public void ChangeFlipVerticalRotation()
        {
            RTSCamera.active.flipVerticalRotation = flipVerticalRotation.isOn;
        }
        public void ChangeFlipZoom()
        {
            RTSCamera.active.flipZoom = flipZoom.isOn;
        }

        public void ChangeMoveSpeed()
        {
            string str = moveSpeedInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RTSCamera.active.moveSpeed = Mathf.Clamp(f, 0, 10);
            }
            else
            {
                moveSpeedInputField.text = RTSCamera.active.moveSpeed.ToString();
            }
        }

        public void ChangeAcceleration()
        {
            string str = moveAccelerationInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RTSCamera.active.moveAcceleration = Mathf.Clamp(f, 0, 1);
            }
            else
            {
                moveAccelerationInputField.text = RTSCamera.active.moveAcceleration.ToString();
            }
        }

        public void ChangeRotationSpeed()
        {
            string str = rotationSpeedInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RTSCamera.active.rotationSpeed = Mathf.Clamp(f, 0, 10);
            }
            else
            {
                rotationSpeedInputField.text = RTSCamera.active.rotationSpeed.ToString();
            }
        }

        public void ChangeZoomSpeed()
        {
            string str = zoomSpeedInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RTSCamera.active.zoomSpeed = Mathf.Clamp(f, 0, 10);
            }
            else
            {
                zoomSpeedInputField.text = RTSCamera.active.zoomSpeed.ToString();
            }
        }

        public void ChangeMinimumTerrainHeight()
        {
            string str = minimumTerrainHeightInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                float f1 = Mathf.Clamp(f, 0.1f, 300);

                if (f1 >= RTSCamera.active.maxZoomHeight)
                {
                    f1 = RTSCamera.active.maxZoomHeight - 1f;

                    if (f1 < 0)
                    {
                        f1 = 0;
                    }

                    minimumTerrainHeightInputField.text = f1.ToString();
                }

                RTSCamera.active.minZoomHeight = f1;
            }
            else
            {
                minimumTerrainHeightInputField.text = RTSCamera.active.minZoomHeight.ToString();
            }
        }

        public void ChangeMaximumTerrainHeight()
        {
            string str = maximumTerrainHeightInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                float f1 = Mathf.Clamp(f, 0.1f, 300);

                if (f1 <= RTSCamera.active.minZoomHeight)
                {
                    f1 = RTSCamera.active.minZoomHeight + 1f;
                    maximumTerrainHeightInputField.text = f1.ToString();
                }

                RTSCamera.active.maxZoomHeight = f1;
            }
            else
            {
                maximumTerrainHeightInputField.text = RTSCamera.active.maxZoomHeight.ToString();
            }
        }

        public void ChangeMinimumAbsoluteHeight()
        {
            string str = minimumAbsoluteHeightInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RTSCamera.active.minCameraHeight = Mathf.Clamp(f, -50, 300);
            }
            else
            {
                minimumAbsoluteHeightInputField.text = RTSCamera.active.minCameraHeight.ToString();
            }
        }
    }
}
