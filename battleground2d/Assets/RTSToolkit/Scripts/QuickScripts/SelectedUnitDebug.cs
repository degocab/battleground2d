using UnityEngine;

namespace RTSToolkit
{
    public class SelectedUnitDebug : MonoBehaviour
    {
        public KeyCode key = KeyCode.C;
        public bool useCustomDebug = false;

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                Debug.Log(useCustomDebug);

                if (useCustomDebug == false)
                {
                    if (Diplomacy.active != null)
                    {
                        Debug.Log("Player nation: " + Diplomacy.active.playerNation);
                        Debug.Log(" ----Nations list---- ");

                        for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                        {
                            if (RTSMaster.active.nationPars[i] != null)
                            {
                                Debug.Log(RTSMaster.active.nationPars[i].GetNationName() + " " + i);
                            }
                        }
                    }

                    Debug.Log(" ------------ ");
                }

                for (int i = 0; i < SelectionManager.active.selectedGoPars.Count; i++)
                {
                    if (useCustomDebug)
                    {
                        CustomDebugUnit(SelectionManager.active.selectedGoPars[i]);
                    }
                    else
                    {
                        DebugUnit(SelectionManager.active.selectedGoPars[i]);
                    }
                }
            }
        }

        void DebugUnit(UnitPars up)
        {
            int id = RTSMaster.active.allUnits.IndexOf(up);
            Debug.Log("Unit list id = " + id);
            Debug.Log(" ------------ ");

            Debug.Log("rtsUnitId = " + up.rtsUnitId);
            Debug.Log("militaryMode = " + up.militaryMode);

            bool targParsReady = false;
            if (up.targetUP != null)
            {
                targParsReady = true;
            }
            Debug.Log("targParsReady = " + targParsReady);

            if (UnitsMover.active.militaryAvoiders.Contains(up))
            {
                Debug.Log("militaryAvoiders = " + true);
            }

            if (up.unitParsType.isBuilding == false)
            {
                Debug.Log(" position " + up.transform.position);
                Debug.Log(" destination " + up.thisNMA.destination);
                Debug.Log(" distanceToDestination " + (up.thisNMA.destination - up.transform.position).magnitude.ToString());

                if ((up.targetUP != null) && (up.targetUP.unitParsType.isBuilding))
                {
                    Debug.Log("GetClosestBuildingPoint = " + (BattleSystem.GetClosestBuildingPoint(up, up.targetUP) - up.transform.position).magnitude.ToString());
                    Debug.Log("up.thisNMA.radius = " + up.thisNMA.radius);
                }
                else if ((up.targetUP != null) && (up.targetUP.unitParsType.isBuilding))
                {
                    Debug.Log("distanceToTarget = " + (up.transform.position - up.targetUP.transform.position).magnitude);
                }
            }

            if (up.targetUP != null)
            {
                Debug.Log(" up.targetUP.isBuilding = " + up.targetUP.unitParsType.isBuilding);
                Debug.Log(" up.targetUP.health = " + up.targetUP.health);
                Debug.Log(" up.targetUP.isDying = " + up.targetUP.isDying);
                Debug.Log(" up.targetUP.isSinking = " + up.targetUP.isSinking);
                Debug.Log(" up.attackers.Count = " + up.attackers.Count);
            }

            Debug.Log("wanderingMode = " + up.wanderingMode);

            Debug.Log("strictApproachMode = " + up.strictApproachMode);

            Debug.Log("isAttackable = " + up.isAttackable);
            Debug.Log("isDying = " + up.isDying);
            Debug.Log("isSinking = " + up.isSinking);

            Debug.Log("up.velocityVector = " + up.velocityVector);
            Debug.Log("up.velocityVector.magnitude = " + up.velocityVector.magnitude);

            Debug.Log("hasPath = " + up.hasPath);

            Debug.Log("resourceType = " + up.resourceType);
            Debug.Log("resourceAmount = " + up.resourceAmount);
            Debug.Log("chopTreePhase = " + up.chopTreePhase);
            Debug.Log("deliveryPointId= " + up.deliveryPointId);

            Debug.Log("nation = " + up.nation);
            Debug.Log("nationName = " + up.nationName);

            SpawnPoint sp = up.GetComponent<SpawnPoint>();
            if (sp != null)
            {
                Debug.Log("SpawnPoint " + sp.nationName + " " + sp.nation);
            }

            Debug.Log("Time.time-attPars.timeMark = " + (Time.time - up.timeMark).ToString());
            Debug.Log("attPars.attackWaiter = " + up.unitParsType.attackWaiter);

            Debug.Log(" ------------ ");
            Debug.Log(" ------------ ");
        }

        void CustomDebugUnit(UnitPars up)
        {
            Debug.Log(up.attackers.Count);

            for (int i = 0; i < up.attackers.Count; i++)
            {
                UnitPars att = up.attackers[i];
                if (att == null)
                {
                    Debug.Log("att == null");
                }
                else
                {
                    if (att.targetUP != up)
                    {
                        Debug.Log("att.targetUP != up");
                    }
                }
            }
        }
    }
}
