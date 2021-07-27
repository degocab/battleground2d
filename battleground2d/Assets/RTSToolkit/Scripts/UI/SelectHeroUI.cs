using UnityEngine;

namespace RTSToolkit
{
    public class SelectHeroUI : MonoBehaviour
    {
        void Start()
        {

        }

        public void SelectHero()
        {
            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars up = RTSMaster.active.allUnits[i];

                if (up.nation == Diplomacy.active.playerNation)
                {
                    if (up.rtsUnitId == 20)
                    {
                        SelectionManager.active.SelectObject(up);
                        SelectionManager.active.ActivateUnitsMenu();
                        SelectionManager.active.SelectedUnitsInfo();
                        SelectionManager.active.PlaySelectSound();
                        BottomBarUI.active.DisableAll();
                        SelectionManager.active.LockLeftClickSelectionOneFrame();

                        return;
                    }
                }
            }
        }

        public void PointerEnter()
        {
            BottomBarUI.active.DisplayMessage("Select hero");
        }

        public void PointerExit()
        {
            BottomBarUI.active.DisableAll();
        }
    }
}
