using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class SpawnGridUI : MonoBehaviour
    {
        public static SpawnGridUI active;

        public GameObject restoreButton;
        public GameObject destroyButton;

        public List<BuildingSpawnMenu> grids = new List<BuildingSpawnMenu>();
        public List<GameObject> elements;

        [HideInInspector] public bool isAnyGirdEnabled = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void OpenBuildingMenu(int rtsId)
        {
            for (int i = 0; i < grids.Count; i++)
            {
                if (grids[i].rtsUnitId == rtsId)
                {
                    ToggleGrid();
                }
            }

            if (rtsId > -1)
            {
                if (SelectionManager.active.selectedGoPars.Count == 1)
                {
                    UnitPars up = SelectionManager.active.selectedGoPars[0];

                    if (up.health < up.maxHealth)
                    {
                        restoreButton.SetActive(true);
                    }
                }

                destroyButton.SetActive(true);
            }
        }

        public void CloseBuildingMenu()
        {
            DisableAllGrids();
            restoreButton.SetActive(false);
            destroyButton.SetActive(false);
        }

        public void DisableAllGrids()
        {
            for (int i = 0; i < grids.Count; i++)
            {
                BuildingSpawnMenu grid = grids[i];
                grid.grid.SetActive(false);
                isAnyGirdEnabled = false;
            }

            if (SpawnNumberUI.active != null)
            {
                SpawnNumberUI.active.DisableScrollMode();
            }

            if (FormationNumberUI.active != null)
            {
                FormationNumberUI.active.DeActivate();
            }
        }

        public void ToggleGrid()
        {
            BuildMark.active.DisableProjector();

            if (SelectionManager.active.selectedGoPars.Count == 1)
            {
                ToggleGrid(SelectionManager.active.HeroCheckId(SelectionManager.active.selectedGoPars[0].rtsUnitId));
            }

            if (SelectionManager.active.selectedGoPars.Count == 0)
            {
                ToggleGrid(-1);
            }
        }

        public void RestoreBuilding()
        {
            if (SelectionManager.active.selectedGoPars.Count == 1)
            {
                UnitPars up = SelectionManager.active.selectedGoPars[0];
                restoreButton.SetActive(false);
                up.RestoreBuilding();
            }
        }

        public void ToggleGrid(int rtsId)
        {
            for (int i = 0; i < grids.Count; i++)
            {
                if (grids[i].rtsUnitId == rtsId)
                {
                    grids[i].grid.SetActive(true);
                    isAnyGirdEnabled = true;
                }
            }
        }

        public void EnableElement(int id)
        {
            if (id < elements.Count)
            {
                if (elements[id] != null)
                {
                    elements[id].SetActive(true);
                }
            }
        }

        public void DisableElement(int id)
        {
            if (id < elements.Count)
            {
                if (elements[id] != null)
                {
                    elements[id].SetActive(false);
                }
            }
        }

        public void DestroySelected()
        {
            if (SelectionManager.active.selectedGoPars.Count == 1)
            {
                SelectionManager.active.selectedGoPars[0].UpdateHealth(-10f);
            }
        }

        [System.Serializable]
        public class BuildingSpawnMenu
        {
            public GameObject grid;
            public int rtsUnitId;
        }
    }
}
