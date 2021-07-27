using UnityEngine;

namespace RTSToolkit
{
    public class MiningPointDrainer : MonoBehaviour
    {
        public float drainFactor = 0.5f;
        public KeyCode key;

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
                {
                    UnitPars up = RTSMaster.active.allUnits[i];

                    if (up.rtsUnitId == 5)
                    {
                        int resToTake = (int)(drainFactor * up.resourceAmount);
                        up.TakeResourcesFromMiningPoint(resToTake);
                    }
                }
            }
        }
    }
}
