using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class JourneysPosCellUI : MonoBehaviour
    {
        public InputField northPos;
        public InputField eastPos;

        public Dropdown dropdown;

        void Start()
        {
            FillDropdown();
        }

        public void RemoveThis()
        {
            JourneysUI.active.RemovePosition(this);
        }

        public void GetNationPosition()
        {
            int id = dropdown.value;
            string text = dropdown.options[id].text;

            if (text == "Home")
            {
                Vector3 meanPos = JourneysUI.active.openJourney.GetMeanPosition();
                northPos.text = (meanPos.x).ToString();
                eastPos.text = (-meanPos.z).ToString();
            }
            else
            {
                for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                {
                    if (text == RTSMaster.active.nationPars[i].GetNationName())
                    {
                        Vector3 natPos = RTSMaster.active.nationPars[i].transform.position;
                        northPos.text = (natPos.x).ToString();
                        eastPos.text = (-natPos.z).ToString();

                        return;
                    }
                }
            }
        }

        void FillDropdown()
        {
            dropdown.ClearOptions();
            List<string> options = new List<string>();
            options.Add("None");
            options.Add("Home");

            List<string> nationNames = Diplomacy.GetNationsDiscoveredByPlayerNames();
            for (int i = 0; i < nationNames.Count; i++)
            {
                options.Add(nationNames[i]);
            }

            dropdown.AddOptions(options);
        }
    }
}
