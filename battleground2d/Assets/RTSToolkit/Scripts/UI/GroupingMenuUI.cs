using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class GroupingMenuUI : MonoBehaviour
    {
        public static GroupingMenuUI active;
        public GameObject cellPrefab;
        public GameObject grid;
        public Toggle formationToggle;
        public Toggle journeyToggle;

        List<GameObject> gridCells = new List<GameObject>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            RefreshGroups();
        }

        public void RefreshGroups()
        {
            Clean();

            UnitsGrouping ug = UnitsGrouping.active;

            if (ug != null)
            {
                for (int i = 0; i < ug.unitsGroups.Count; i++)
                {
                    GameObject go = Instantiate(cellPrefab);
                    go.transform.SetParent(grid.transform);
                    go.SetActive(true);
                    gridCells.Add(go);

                    SelectGroupActionUI sga = go.GetComponent<SelectGroupActionUI>();

                    sga.groupId = gridCells.Count;
                    sga.text.text = gridCells.Count.ToString();

                    if (ug.unitsGroups[i].formationMode == 1)
                    {
                        sga.SetFormationColor(true);
                    }
                    else
                    {
                        sga.SetFormationColor(false);
                    }

                    if (ug.unitsGroups[i].journeyMode == 1)
                    {
                        sga.SetJourneyColor();
                    }
                }
            }
        }

        public bool GetFormationMode()
        {
            return formationToggle.isOn;
        }

        public bool GetJourneyMode()
        {
            return journeyToggle.isOn;
        }

        public void Clean()
        {
            for (int i = 0; i < gridCells.Count; i++)
            {
                Destroy(gridCells[i]);
            }

            gridCells.Clear();
        }

        public void CleanAndRemove()
        {
            UnitsGrouping.active.CleanUpGroups();
            for (int i = 0; i < gridCells.Count; i++)
            {
                Destroy(gridCells[i]);
            }

            gridCells.Clear();
        }
    }
}
