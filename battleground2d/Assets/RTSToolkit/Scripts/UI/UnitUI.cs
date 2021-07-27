using UnityEngine;

namespace RTSToolkit
{
    public class UnitUI : MonoBehaviour
    {
        public static UnitUI active;

        public GameObject unit_buttons;

        public GameObject unit_attackButton;

        public GameObject unit_mineButton;
        public GameObject unit_returnMiningResourcesButton;

        public GameObject unit_chopButton;
        public GameObject unit_returnLumberButton;

        public GameObject unit_buildCentralBuildingButton;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void ActivateWorker()
        {
            DeActivate();

            int n_nonWorkers = 0;
            int nEmpty = 0;
            int nMining = 0;
            int nLumber = 0;
            int nHeroes = 0;

            if (SelectionManager.active.allUnits.Count == 0)
            {
                return;
            }

            for (int i = 0; i < SelectionManager.active.allUnits.Count; i++)
            {
                if (SelectionManager.active.allUnits[i].nation == Diplomacy.active.playerNation)
                {
                    if (SelectionManager.active.allUnits[i].isSelected)
                    {
                        if (SelectionManager.active.allUnits[i].unitParsType.isWorker)
                        {
                            if (SelectionManager.active.allUnits[i].resourceAmount > 0)
                            {
                                if ((SelectionManager.active.allUnits[i].resourceType > -1) && (SelectionManager.active.allUnits[i].resourceType < 2))
                                {
                                    nMining++;
                                }
                                else if (SelectionManager.active.allUnits[i].resourceType == 2)
                                {
                                    nLumber++;
                                }
                                else
                                {
                                    nEmpty++;
                                }
                            }
                            else
                            {
                                nEmpty++;
                            }
                        }
                        else
                        {
                            n_nonWorkers++;

                            if (SelectionManager.active.allUnits[i].rtsUnitId == 20)
                            {
                                nHeroes++;
                            }
                        }
                    }
                }
            }

            unit_buildCentralBuildingButton.SetActive(false);

            if (n_nonWorkers > 0)
            {
                unit_attackButton.SetActive(true);

                unit_mineButton.SetActive(false);
                unit_returnMiningResourcesButton.SetActive(false);

                unit_chopButton.SetActive(false);
                unit_returnLumberButton.SetActive(false);

                unit_buildCentralBuildingButton.SetActive(false);

                if (nHeroes == 1)
                {
                    if (n_nonWorkers == 1)
                    {
                        if (Diplomacy.active.playerNation < RTSMaster.active.numberOfUnitTypes.Count)
                        {
                            if (RTSMaster.active.numberOfUnitTypes[Diplomacy.active.playerNation][0] == 0)
                            {
                                unit_buildCentralBuildingButton.SetActive(true);
                            }
                        }
                    }
                }
            }
            else
            {
                if (nEmpty > 0 && nMining == 0 && nLumber == 0)
                {
                    unit_attackButton.SetActive(true);

                    unit_mineButton.SetActive(true);
                    unit_returnMiningResourcesButton.SetActive(false);

                    unit_chopButton.SetActive(true);
                    unit_returnLumberButton.SetActive(false);
                }
                else if (nEmpty == 0 && nMining > 0 && nLumber == 0)
                {
                    unit_attackButton.SetActive(false);

                    unit_mineButton.SetActive(false);
                    unit_returnMiningResourcesButton.SetActive(true);

                    unit_chopButton.SetActive(false);
                    unit_returnLumberButton.SetActive(false);
                }
                else if (nEmpty == 0 && nMining == 0 && nLumber > 0)
                {
                    unit_attackButton.SetActive(false);

                    unit_mineButton.SetActive(false);
                    unit_returnMiningResourcesButton.SetActive(false);

                    unit_chopButton.SetActive(false);
                    unit_returnLumberButton.SetActive(true);
                }
            }

            unit_buttons.SetActive(true);
        }

        public void DeActivate()
        {
            unit_buttons.SetActive(false);
        }

        public void SendWorkersToDeliveryPoint()
        {
            for (int i = 0; i < SelectionManager.active.allUnits.Count; i++)
            {
                if (SelectionManager.active.allUnits[i].unitParsType.isWorker)
                {
                    if (SelectionManager.active.allUnits[i].resourceAmount > 0)
                    {
                        if (SelectionManager.active.allUnits[i].isSelected)
                        {
                            int nat = SelectionManager.active.allUnits[i].nation;

                            if (nat == Diplomacy.active.playerNation)
                            {
                                if (nat > -1 && nat < RTSMaster.active.nationPars.Count)
                                {
                                    if (RTSMaster.active.nationPars[nat].resourcesCollection != null)
                                    {
                                        RTSMaster.active.nationPars[nat].resourcesCollection.SendWorkerToDeliveryPoint(SelectionManager.active.allUnits[i]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void Stop()
        {
            SelectionManager.active.StopDestinationsF();
            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void Move()
        {
            SelectionManager.active.movementButtonMode = SelectionManager.active.pointerMoveToMode;
            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void Attack()
        {
            SelectionManager.active.movementButtonMode = SelectionManager.active.pointerButtonClickAttackActiveMode;
            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void Group()
        {
            UnitsGrouping.active.GroupSelected();

            if (GroupingMenuUI.active != null)
            {
                GroupingMenuUI.active.RefreshGroups();
            }

            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void RPGMode()
        {
            CameraSwitcher.active.FlipSwitcher(SelectionManager.active.selectedGoPars[0]);
            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void WorkInMiningPoint()
        {
            SelectionManager.active.movementButtonMode = SelectionManager.active.pointerMiningPointActiveMode;
            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void ChopLumber()
        {
            SelectionManager.active.movementButtonMode = SelectionManager.active.pointerChopWoodActiveMode;
            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public void BuildCentralBuilding()
        {

        }
    }
}
