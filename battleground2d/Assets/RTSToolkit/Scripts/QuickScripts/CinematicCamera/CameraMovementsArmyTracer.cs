using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class CameraMovementsArmyTracer : MonoBehaviour
    {
        public float updateTime = 0.2f;
        public List<int> militaryModesToTrace;

        void Start()
        {
            StartCoroutine(Updater());
        }

        IEnumerator Updater()
        {
            RTSMaster rtsm = RTSMaster.active;

            while (true)
            {
                CameraMovements.active.centers.Clear();

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    UnitPars up = rtsm.allUnits[i];

                    for (int j = 0; j < militaryModesToTrace.Count; j++)
                    {
                        if (up.militaryMode == militaryModesToTrace[j])
                        {
                            Vector2 v2 = new Vector2(up.transform.position.x, up.transform.position.z);
                            CameraMovements.active.centers.Add(v2);
                        }
                    }
                }

                yield return new WaitForSeconds(updateTime);
            }
        }
    }
}
