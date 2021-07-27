using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SpawnElementUI : MonoBehaviour
    {
        public int rtsUnitId = 0;
        UnitPars model;

        public Sprite activeIcon;
        public Sprite inactiveIcon;
        public Image image;

        bool runUpdate = false;
        bool isModelSet = false;

        bool isModelCurrentlyActive = true;

        void Start()
        {
            if (model == null)
            {
                model = RTSMaster.active.rtsUnitTypePrefabs[rtsUnitId].GetComponent<UnitPars>();
                isModelSet = false;

                if (model != null)
                {
                    isModelSet = true;
                }
            }

            if ((activeIcon != null) && (inactiveIcon != null) && (image != null))
            {
                runUpdate = true;
                Update();
            }
        }

        void Update()
        {
            if (runUpdate)
            {
                if (isModelSet)
                {
                    bool isEnoughResources = model.IsEnoughResources(Diplomacy.active.playerNation);

                    if (isEnoughResources)
                    {
                        if (isModelCurrentlyActive == false)
                        {
                            // Set to enable
                            image.sprite = activeIcon;
                            isModelCurrentlyActive = true;
                        }
                    }
                    else
                    {
                        if (isModelCurrentlyActive)
                        {
                            // Set to disable
                            image.sprite = inactiveIcon;
                            isModelCurrentlyActive = false;
                        }
                    }
                }
            }
        }

        public void PointerEnter()
        {
            if (isModelSet == false)
            {
                model = RTSMaster.active.rtsUnitTypePrefabs[rtsUnitId].GetComponent<UnitPars>();

                if (model != null)
                {
                    isModelSet = true;
                }
            }

            if (isModelSet)
            {
                BottomBarUI.active.DisplayCost(model);
            }
        }

        public void PointerExit()
        {
            BottomBarUI.active.DisableAll();
        }

        public void TriggerSpawn()
        {
            if (isModelSet)
            {
                bool resourcePass = true;

                if (runUpdate)
                {
                    if (isModelCurrentlyActive == false)
                    {
                        resourcePass = false;
                    }
                }

                if (resourcePass)
                {
                    if (RTSMaster.active.rtsUnitTypePrefabsUpt[model.rtsUnitId].isBuilding)
                    {
                        BuildMark.active.objectToSpawn = model.gameObject;
                        BuildMark.active.up_objectToSpawn = model;
                        BuildMark.active.enabled = true;
                        BuildMark.active.ActivateProjector();

                        SpawnGridUI.active.DisableAllGrids();

                        if (MobileButtonsUI.active.isMobileMode)
                        {
                            MobileButtonsUI.active.ActivateBuildMode(model);
                        }

                        BottomBarUI.active.DisableAll();
                    }
                    else
                    {
                        if ((SpawnNumberUI.active.scrollMode == false) && (model.rtsUnitId != 20))
                        {
                            SpawnNumberUI.active.EnableScrollMode();

                            if (RTSMaster.active.rtsUnitTypePrefabsUpt[model.rtsUnitId].isWorker == false)
                            {
                                FormationNumberUI.active.Activate();
                            }

                            if (MobileButtonsUI.active.isMobileMode)
                            {
                                MobileButtonsUI.active.ActivateBuildMode(model);
                            }
                        }
                        else
                        {
                            SpawnElementUI.StartUnitSpawn(model);

                            if (MobileButtonsUI.active.isMobileMode)
                            {
                                MobileButtonsUI.active.DeActivateBuildMode();
                            }
                        }
                    }
                }
            }

            SelectionManager.active.LockLeftClickSelectionOneFrame();
        }

        public static void StartUnitSpawn(UnitPars mdl)
        {
            SpawnNumberUI.active.StartSpawning(mdl);
            SpawnGridUI.active.CloseBuildingMenu();
            CancelSpawnUI.active.Activate();
            SpawnNumberUI.active.DisableScrollMode();
            FormationNumberUI.active.DeActivate();
            BottomBarUI.active.DisableAll();
        }
    }
}
