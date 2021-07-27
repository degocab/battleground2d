using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class BottomBarUI : MonoBehaviour
    {
        public static BottomBarUI active;

        public GameObject grid;
        public GameObject inactiveGrid;

        public GameObject resSlot;

        List<GameObject> resSlotInstances = new List<GameObject>();
        public Text infoText;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void DisableAll()
        {
            infoText.gameObject.SetActive(false);

            for (int i = 0; i < resSlotInstances.Count; i++)
            {
                Destroy(resSlotInstances[i]);
            }

            resSlotInstances.Clear();
        }

        public void DisplayCost(UnitPars up)
        {
            DisableAll();
            infoText.gameObject.SetActive(true);
            UnitParsType upt = RTSMaster.active.rtsUnitTypePrefabsUpt[up.rtsUnitId];

            string createPrefix = "Create ";

            if (upt.isBuilding)
            {
                createPrefix = "Build ";
            }

            infoText.text = createPrefix + upt.unitName;

            for (int i = 0; i < upt.costs.Count; i++)
            {
                EconomyResource er = upt.costs[i].GetCorrespondingResource();

                if (er != null)
                {
                    GameObject go = Instantiate(resSlot, grid.transform);
                    ResourceSlotUI rcui = go.GetComponent<ResourceSlotUI>();
                    rcui.image.sprite = er.icon;
                    rcui.text.text = upt.costs[i].amount.ToString();
                    resSlotInstances.Add(go);
                }
            }
        }

        public void DisplayMessage(string text)
        {
            DisableAll();
            infoText.gameObject.SetActive(true);
            infoText.text = text;
        }
    }
}
