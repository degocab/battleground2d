using UnityEngine;

namespace RTSToolkit
{
    public class AttackRandomTargets : MonoBehaviour
    {
        public KeyCode key = KeyCode.P;

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                Attack();
            }
        }

        void Attack()
        {
            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars up = RTSMaster.active.allUnits[i];

                if (up.militaryMode == 10)
                {
                    int itarg = Random.Range(0, RTSMaster.active.allUnits.Count);
                    UnitPars targ = RTSMaster.active.allUnits[itarg];

                    if (targ != up)
                    {
                        up.AssignTarget(targ, true);
                    }
                }
            }
        }
    }
}
