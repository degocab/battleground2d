using UnityEngine;

namespace RTSToolkit
{
    public class RotatingPart : MonoBehaviour
    {
        public GameObject rotatingPart;
        public int rotationAxis = 0;
        public bool flipRotationDirection = false;
        float rotAngle = 0f;

        public bool useWind = true;
        public bool allignToWind = false;

        WindChanger windChanger;

        float deltaRot = 0f;

        void Start()
        {
            windChanger = WindChanger.active;
        }

        void Update()
        {
            if (useWind)
            {
                if (windChanger != null)
                {
                    Vector3 rotOrig = transform.rotation.eulerAngles;
                    Vector3 rotNew = new Vector3(rotOrig.x, rotOrig.y, rotOrig.z);
                    float angle = Quaternion.Angle(Quaternion.Euler(rotNew), windChanger.transform.rotation);

                    float direction = 1f;
                    if (angle > 90f)
                    {
                        direction = -1f;
                    }

                    deltaRot = deltaRot + 0.03f * windChanger.currentSpeed * direction * Mathf.Abs(Mathf.Cos(angle));
                    deltaRot = 0.994f * deltaRot;

                    if (deltaRot > 7f)
                    {
                        deltaRot = 7f;
                    }
                    if (deltaRot < -7f)
                    {
                        deltaRot = -7f;
                    }
                }
            }
            else
            {
                deltaRot = 3f;
            }

            if (flipRotationDirection)
            {
                rotAngle = rotAngle + deltaRot;
            }
            else
            {
                rotAngle = rotAngle - deltaRot;
            }

            if (allignToWind)
            {
                if (windChanger != null)
                {
                    float windRotY = windChanger.transform.rotation.eulerAngles.y;

                    if (rotationAxis == 0)
                    {
                        rotatingPart.transform.rotation = Quaternion.Euler(windRotY, 0f, 0f);
                    }
                    else if (rotationAxis == 1)
                    {
                        rotatingPart.transform.rotation = Quaternion.Euler(0f, windRotY, 0f);
                    }
                    else if (rotationAxis == 2)
                    {
                        rotatingPart.transform.rotation = Quaternion.Euler(0f, 0f, windRotY);
                    }
                }
            }
            else
            {
                if (rotationAxis == 0)
                {
                    rotatingPart.transform.rotation = transform.rotation * Quaternion.Euler(rotAngle, 0f, 0f);
                }
                else if (rotationAxis == 1)
                {
                    rotatingPart.transform.rotation = transform.rotation * Quaternion.Euler(0f, rotAngle, 0f);
                }
                else if (rotationAxis == 2)
                {
                    rotatingPart.transform.rotation = transform.rotation * Quaternion.Euler(0f, 0f, rotAngle);
                }
            }
        }
    }
}
