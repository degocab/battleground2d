using UnityEngine;

namespace RTSToolkit
{
    public class CameraSwitcher : MonoBehaviour
    {
        public static CameraSwitcher active;

        [HideInInspector] public UnitPars followPars;

        [HideInInspector] public Vector3 lastRTSposition;
        [HideInInspector] public Quaternion lastRTSrotation;

        RTSCamera rtsCamera;
        RPGCamera rpgCamera;

        [HideInInspector] public Transform camTransform;

        [HideInInspector] public int mode = 1;

        int lookAtPointPassages = 0;
        int maxLookAtPointPassages = 30;
        bool isLookingToUnit = false;

        CameraLooker camLook;
        Quaternion camIniRot;

        float distance = 50f;
        float distanceOffset = 0f;

        float hAngleOffest = 0f;
        float vAngleOffest = 0f;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsCamera = RTSCamera.active;
            rpgCamera = RPGCamera.active;
            camTransform = Camera.main.transform;
        }

        void Update()
        {
            if (isLookingToUnit)
            {
                lookAtPointPassages = lookAtPointPassages + 1;

                if (lookAtPointPassages > maxLookAtPointPassages)
                {
                    isLookingToUnit = false;
                    lookAtPointPassages = 0;
                }

                camLook.LookAtTransform(followPars.transform.position, distance + distanceOffset, -camIniRot.eulerAngles.y + hAngleOffest, -25f + vAngleOffest);
            }
        }

        public void FlipSwitcher(UnitPars follower)
        {
            if (mode == 1)
            {
                followPars = follower;
                SwitchToRPG(follower);
            }
            else if (mode == 2)
            {
                SwitchToRTS();
            }
        }

        public void SwitchToRPG(UnitPars follower)
        {
            SwitchToRPG(follower, true);
        }

        public void SwitchToRPG(UnitPars follower, bool saveCameraPosition)
        {
            rtsCamera.enabled = false;

            if (saveCameraPosition)
            {
                lastRTSposition = camTransform.position;
                lastRTSrotation = camTransform.rotation;
            }

            rpgCamera.SetLooker(follower);
            rpgCamera.enabled = true;
            mode = 2;
        }

        public void SwitchToRTS()
        {
            rpgCamera.enabled = false;
            camTransform.position = lastRTSposition;
            camTransform.rotation = lastRTSrotation;
            rtsCamera.enabled = true;
            mode = 1;
        }

        public void LookRTSCameraToUnit(UnitPars up)
        {
            if (mode == 1)
            {
                camLook = new CameraLooker();
                camLook.camTransform = camTransform;
                followPars = up;
                float rad = up.rEnclosed;

                if (up.thisNMA != null)
                {
                    rad = up.thisNMA.radius;
                }

                distance = 5 * rad;
                distanceOffset = 0f;
                hAngleOffest = 0f;
                vAngleOffest = 0f;
                camIniRot = camTransform.rotation;
                isLookingToUnit = true;
            }
        }

        public void ResetFromUnit(UnitPars cand)
        {
            if (mode == 2)
            {
                if (cand == followPars)
                {
                    SwitchToRTS();
                }
            }
        }
    }

    public class CameraLooker
    {
        public float moveFraction = 0.1f;
        public Transform camTransform;

        public void LookAtTransform(Vector3 source, float dist, float hRot, float vRot)
        {
            PosRot look = LookAt(source, dist, hRot, vRot);
            camTransform.position = (1f - moveFraction) * camTransform.position + moveFraction * look.position;
            camTransform.rotation = Quaternion.Lerp(camTransform.rotation, look.rotation, 0.1f);

            Vector3 tpos = camTransform.position;
            Vector3 terVect = TerrainProperties.TerrainVectorProc(tpos);

            if (tpos.y - terVect.y < 5f)
            {
                camTransform.position = terVect + new Vector3(0f, 5f, 0f);
            }
        }

        PosRot LookAt(Vector3 source, float dist, float hRot, float vRot)
        {
            Vector3 norm = new Vector3(0f, 0f, 1f);
            norm = RotAround(vRot, norm, new Vector3(1f, 0f, 0f));
            norm = RotAround(hRot, norm, new Vector3(0f, 1f, 0f));

            norm = norm.normalized;

            Vector3 finalPos = source - dist * norm;
            Quaternion finalRot = Quaternion.Euler(-vRot, -hRot, 0f);

            PosRot pr = new PosRot();
            pr.position = finalPos;
            pr.rotation = finalRot;

            return pr;
        }

        Vector3 RotAround(float rotAngle, Vector3 original, Vector3 direction)
        {
            Vector3 cross1 = Vector3.Cross(original, direction);

            Vector3 pr = Vector3.Project(original, direction);
            Vector3 pr2 = original - pr;

            Vector3 cross2 = Vector3.Cross(pr2, cross1);
            Vector3 rotatedVector = (Quaternion.AngleAxis(rotAngle, cross2) * pr2) + pr;

            return rotatedVector;
        }
    }

    public struct PosRot
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}
