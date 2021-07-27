using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class PlayerAIUI : MonoBehaviour
    {
        public Toggle buildTownBuildingsToggle;
        public Toggle buildMiningPointsToggle;
        public Toggle makeWorkersToggle;
        public Toggle sendWorkersToCollectResourcesToggle;
        public Toggle restoreDamagedBuildingsToggle;
        public Toggle createMilitaryUnitsToggle;
        public Toggle distrubuteMilitaryForGuarding;
        public Toggle wanderOtherNation;

        void Start()
        {

        }

        public void BuildTownBuildings()
        {
            NationAI nai = GetPlayerNationAI();

            if (nai != null)
            {
                if (buildTownBuildingsToggle.isOn)
                {
                    if (nai.enabled == false)
                    {
                        nai.enabled = true;
                    }

                    nai.runTownBuilder = true;
                }
                else
                {
                    nai.runTownBuilder = false;
                }
            }
        }

        public void BuildMiningPoints()
        {
            NationAI nai = GetPlayerNationAI();

            if (nai != null)
            {
                if (buildMiningPointsToggle.isOn)
                {
                    if (nai.enabled == false)
                    {
                        nai.enabled = true;
                    }

                    nai.runMiningPointsControl = true;
                }
                else
                {
                    nai.runMiningPointsControl = false;
                }
            }
        }

        public void MakeWorkers()
        {
            NationAI nai = GetPlayerNationAI();

            if (nai != null)
            {
                if (makeWorkersToggle.isOn)
                {
                    if (nai.enabled == false)
                    {
                        nai.enabled = true;
                    }

                    nai.runWorkersSpawner = true;
                }
                else
                {
                    nai.runWorkersSpawner = false;
                }
            }
        }

        public void SendWorkersToCollectResources()
        {
            NationAI nai = GetPlayerNationAI();

            if (nai != null)
            {
                if (sendWorkersToCollectResourcesToggle.isOn)
                {
                    if (nai.enabled == false)
                    {
                        nai.enabled = true;
                    }

                    nai.runSetWorkers = true;
                }
                else
                {
                    nai.runSetWorkers = false;
                }
            }
        }

        public void RestoreDamagedBuildings()
        {
            NationAI nai = GetPlayerNationAI();

            if (nai != null)
            {
                if (restoreDamagedBuildingsToggle.isOn)
                {
                    if (nai.enabled == false)
                    {
                        nai.enabled = true;
                    }

                    nai.runRestoreDamagedBuildings = true;
                }
                else
                {
                    nai.runRestoreDamagedBuildings = false;
                }
            }
        }

        public void CreateMilitaryUnits()
        {
            NationAI nai = GetPlayerNationAI();

            if (nai != null)
            {
                if (createMilitaryUnitsToggle.isOn)
                {
                    if (nai.enabled == false)
                    {
                        nai.enabled = true;
                    }

                    nai.runTroopsControl = true;
                }
                else
                {
                    nai.runTroopsControl = false;
                }
            }
        }

        public void DistrubuteMilitaryForGuarding()
        {
            WandererAI wai = GetPlayerWandererAI();

            if (wai != null)
            {
                if (distrubuteMilitaryForGuarding.isOn)
                {
                    if (wai.enabled == false)
                    {
                        wai.enabled = true;
                    }

                    wai.runReturnGuardsToTheirPositions = true;
                }
                else
                {
                    wai.runReturnGuardsToTheirPositions = false;
                }
            }
        }

        public void WanderOtherNations()
        {
            WandererAI wai = GetPlayerWandererAI();

            if (wai != null)
            {
                if (wanderOtherNation.isOn)
                {
                    if (wai.enabled == false)
                    {
                        wai.enabled = true;
                    }

                    wai.runWandererPhase = true;
                }
                else
                {
                    wai.runWandererPhase = false;
                }
            }
        }

        NationPars GetPlayerNationPars()
        {
            if ((Diplomacy.active.playerNation > -1) && (Diplomacy.active.playerNation < RTSMaster.active.nationPars.Count))
            {
                return RTSMaster.active.nationPars[Diplomacy.active.playerNation];
            }

            return null;
        }

        NationAI GetPlayerNationAI()
        {
            NationPars np = GetPlayerNationPars();

            if (np != null)
            {
                if (np.gameObject != null)
                {
                    return np.gameObject.GetComponent<NationAI>();
                }
            }

            return null;
        }

        WandererAI GetPlayerWandererAI()
        {
            NationPars np = GetPlayerNationPars();

            if (np != null)
            {
                if (np.gameObject != null)
                {
                    return np.gameObject.GetComponent<WandererAI>();
                }
            }

            return null;
        }
    }
}
