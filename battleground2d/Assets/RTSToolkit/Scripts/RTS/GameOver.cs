using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace RTSToolkit
{
    public class GameOver : MonoBehaviour
    {
        public static GameOver active;

        RTSMaster rtsm;
        [HideInInspector] public bool runUpdate = false;

        private bool isFirstStart = true;

        [HideInInspector] public bool isActive = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            runUpdate = true;
        }

        void Update()
        {
            CheckCentralBuildingExistance();
        }

        void LateUpdate()
        {
            if (triggerGameOverUpdate)
            {
                TriggerGameOverDelayed();
            }
        }

        public void CheckCentralBuildingExistance()
        {
            if (SelectionManager.active.selectedGoPars.Count == 0)
            {
                if (rtsm.nationPars.Count > 0)
                {
                    if (
                        (rtsm.nationPars[Diplomacy.active.playerNation].nationAI.spawnedBuildings.Count > 0) ||
                        (isFirstStart)
                    )
                    {
                        if (Diplomacy.active.playerNation < rtsm.numberOfUnitTypes.Count)
                        {
                            if (rtsm.numberOfUnitTypes[Diplomacy.active.playerNation][0] == 0)
                            {
                                if (isActive == false)
                                {
                                    if (rtsm.nationPars[Diplomacy.active.playerNation].IsHeroPresent())
                                    {
                                        SpawnGridUI.active.OpenBuildingMenu(-1);
                                        isActive = true;
                                    }
                                }
                            }

                            if (rtsm.numberOfUnitTypes[Diplomacy.active.playerNation][0] > 0)
                            {
                                if (isActive)
                                {
                                    SpawnGridUI.active.CloseBuildingMenu();
                                    isActive = false;
                                }
                            }
                        }
                    }
                }
            }

            if (SelectionManager.active.selectedGoPars.Count > 0)
            {
                if (isActive)
                {
                    SpawnGridUI.active.CloseBuildingMenu();
                    isActive = false;
                }
            }

            if (isActive)
            {
                if (rtsm.numberOfUnitTypes.Count <= 0)
                {
                    SpawnGridUI.active.CloseBuildingMenu();
                    isActive = false;
                }
                else
                {
                    if (rtsm.numberOfUnitTypes[Diplomacy.active.playerNation][20] <= 0)
                    {
                        SpawnGridUI.active.CloseBuildingMenu();
                        isActive = false;
                    }
                }
            }
        }

        bool isTriggerGameOverDelayedRunning = false;
        bool triggerGameOverUpdate = false;

        public void CheckIfHeroAndCentralBuildingAreDestroyed()
        {
            if (RTSMaster.active.numberOfUnitTypes[Diplomacy.active.playerNation][0] <= 0)
            {
                if (RTSMaster.active.numberOfUnitTypes[Diplomacy.active.playerNation][20] <= 0)
                {
                    if (isTriggerGameOverDelayedRunning == false)
                    {
                        isTriggerGameOverDelayedRunning = true;
                        StartCoroutine(TriggerGameOverDelayedCor());
                    }
                }
            }
        }

        IEnumerator TriggerGameOverDelayedCor()
        {
            yield return new WaitForSeconds(10f);
            triggerGameOverUpdate = true;
        }

        void TriggerGameOverDelayed()
        {
            triggerGameOverUpdate = false;
            SaveLoad.active.UnloadEverything();
            SceneManager.LoadSceneAsync("GameOver", LoadSceneMode.Single);
        }
    }
}
