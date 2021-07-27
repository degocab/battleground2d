using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class CameraMovements : MonoBehaviour
    {
        public static CameraMovements active;

        public string key = "c";
        bool movementOn = false;

        public List<Vector2> centers;

        public Vector2 cameraVelocity;
        Vector2 dv;

        Vector3 previousLook;

        public float attractionForce = 0.05f;
        public float dragCoefficient = 0.00001f;

        public float angularVelocity = 90f;
        public bool lookToCenters = false;

        float height = 40f;

        public float minHeight = 5f;
        public float maxHeight = 40f;

        public float velocityHeightCoefficient = 40f;

        public float centersRandomMoveFactor = 10f;

        Transform cam;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                movementOn = !movementOn;
                cam = Camera.main.transform;
            }

            if (movementOn)
            {
                MoveCamera();
                RotateCamera();
                MoveCenter();
            }
        }

        void MoveCamera()
        {
            cam = Camera.main.transform;
            Vector2 pos2d = new Vector2(cam.position.x, cam.position.z);

            dv = Vector2.zero;
            for (int i = 0; i < centers.Count; i++)
            {
                dv = dv + (pos2d - centers[i]);
            }
            if (centers.Count > 0)
            {
                dv = attractionForce * Time.deltaTime * dv.normalized;
            }

            height = velocityHeightCoefficient * cameraVelocity.magnitude;
            if (height < minHeight)
            {
                height = minHeight;
            }
            if (height > maxHeight)
            {
                height = maxHeight;
            }

            cameraVelocity = cameraVelocity - dv;
            Vector2 drag = dragCoefficient * cameraVelocity * cameraVelocity.magnitude;
            cameraVelocity = cameraVelocity - drag;

            cam.position = TerrainProperties.TerrainVectorProc(cam.position + new Vector3(cameraVelocity.x, 0, cameraVelocity.y)) + new Vector3(0f, height, 0f);
        }

        void RotateCamera()
        {
            Vector3 lookVect = new Vector3(cameraVelocity.x, 0, cameraVelocity.y);

            if (lookToCenters)
            {
                Vector2 pos2d = new Vector2(cam.position.x, cam.position.z);
                dv = Vector2.zero;

                for (int i = 0; i < centers.Count; i++)
                {
                    dv = dv + (pos2d - centers[i]);
                }

                if (centers.Count > 0)
                {
                    dv = dv.normalized;
                }

                lookVect = new Vector3(-dv.x, 0, -dv.y);
            }

            float angle = GenericMath.SignedAngle(lookVect, previousLook, Vector3.up);
            float critAngle = angularVelocity * Time.deltaTime;

            if (Mathf.Abs(angle) > critAngle)
            {
                if (previousLook != Vector3.zero)
                {
                    lookVect = GenericMath.RotAround(Mathf.Sign(angle) * critAngle, previousLook, Vector3.up);
                }
            }

            if (lookVect != Vector3.zero)
            {
                cam.rotation = Quaternion.LookRotation(lookVect);
            }

            previousLook = lookVect;
        }

        void MoveCenter()
        {
            for (int i = 0; i < centers.Count; i++)
            {
                centers[i] = centers[i] + centersRandomMoveFactor * Random.insideUnitCircle;
            }
        }
    }
}
