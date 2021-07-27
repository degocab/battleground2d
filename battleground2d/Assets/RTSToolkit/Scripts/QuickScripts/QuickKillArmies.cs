using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class QuickKillArmies : MonoBehaviour
    {
        RTSMaster rtsm;
        public int nation = 0;
        public int unitType = 11;
        public KeyCode key = KeyCode.E;
        public bool fullDeath = true;

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                List<UnitPars> unitsToKill = new List<UnitPars>();

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    UnitPars up = rtsm.allUnits[i];

                    if (up.nation == nation)
                    {
                        if (up.rtsUnitId == unitType)
                        {
                            unitsToKill.Add(up);
                        }
                    }
                }

                for (int i = 0; i < unitsToKill.Count; i++)
                {
                    if (fullDeath)
                    {
                        unitsToKill[i].UpdateHealth(-10f);
                    }
                    else
                    {
                        rtsm.DestroyUnit(unitsToKill[i]);
                    }
                }
            }
        }
    }
}
