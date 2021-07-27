using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class QuickDamageArmies : MonoBehaviour
    {
        RTSMaster rtsm;
        public int nation = 0;
        public int unitType = 11;
        public KeyCode key = KeyCode.F;

        public float randomDamageMin = 0.1f;
        public float randomDamageMax = 1f;

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                List<UnitPars> unitsToDamage = new List<UnitPars>();

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    UnitPars up = rtsm.allUnits[i];

                    if (up.nation == nation)
                    {
                        if (up.rtsUnitId == unitType)
                        {
                            unitsToDamage.Add(up);
                        }
                    }
                }

                for (int i = 0; i < unitsToDamage.Count; i++)
                {
                    float rand = Random.Range(randomDamageMin, randomDamageMax);
                    unitsToDamage[i].UpdateHealth(rand * unitsToDamage[i].health);
                }
            }
        }
    }
}
