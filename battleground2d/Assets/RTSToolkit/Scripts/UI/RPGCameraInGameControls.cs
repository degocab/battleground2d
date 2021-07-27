using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class RPGCameraInGameControls : MonoBehaviour
    {
        public static RPGCameraInGameControls active;

        [HideInInspector] public Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();
        public Text rotateUp, rotateDown, rotateLeft, rotateRight;
        public Text zoomIn, zoomOut;

        GameObject currentKey;

        public Color regular;
        public Color selected;

        public Toggle zoomWithMouse;
        public Toggle flipZoom;

        public InputField rotationSpeedInputField;
        public InputField zoomSpeedInputField;
        public InputField maxZoomOutInputField;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            keys.Add("RotateUp", KeyCode.W);
            keys.Add("RotateDown", KeyCode.S);
            keys.Add("RotateLeft", KeyCode.A);
            keys.Add("RotateRight", KeyCode.D);

            keys.Add("ZoomIn", KeyCode.None);
            keys.Add("ZoomOut", KeyCode.None);

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

            if (currentKey.transform.parent.name == "RotateUp")
            {
                RPGCamera.active.rotateUp = kc;
            }

            if (currentKey.transform.parent.name == "RotateDown")
            {
                RPGCamera.active.rotateDown = kc;
            }

            if (currentKey.transform.parent.name == "RotateLeft")
            {
                RPGCamera.active.rotateLeft = kc;
            }

            if (currentKey.transform.parent.name == "RotateRight")
            {
                RPGCamera.active.rotateRight = kc;
            }

            if (currentKey.transform.parent.name == "ZoomIn")
            {
                RPGCamera.active.zoomInKey = kc;
            }

            if (currentKey.transform.parent.name == "ZoomOut")
            {
                RPGCamera.active.zoomOutKey = kc;
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

        public void ChangeZoomWithMouse()
        {
            RPGCamera.active.zoomWithMouse = zoomWithMouse.isOn;
        }

        public void ChangeFlipZoom()
        {
            RPGCamera.active.flipZoom = flipZoom.isOn;
        }

        public void ChangeRotationSpeed()
        {
            string str = rotationSpeedInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RPGCamera.active.rotationSpeed = Mathf.Clamp(f, 0, 10);
            }
            else
            {
                rotationSpeedInputField.text = RPGCamera.active.rotationSpeed.ToString();
            }
        }

        public void ChangeZoomSpeed()
        {
            string str = zoomSpeedInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RPGCamera.active.zoomSpeed = Mathf.Clamp(f, 0, 10);
            }
            else
            {
                zoomSpeedInputField.text = RPGCamera.active.zoomSpeed.ToString();
            }
        }

        public void ChangeMaximumZoomOut()
        {
            string str = maxZoomOutInputField.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RPGCamera.active.maxZoomOut = Mathf.Clamp(f, 10, 300);
            }
            else
            {
                maxZoomOutInputField.text = RPGCamera.active.maxZoomOut.ToString();
            }
        }
    }
}
