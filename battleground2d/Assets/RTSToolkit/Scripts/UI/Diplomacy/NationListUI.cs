using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class NationListUI : MonoBehaviour
    {
        public static NationListUI active;

        public GameObject nationList;
        public GameObject nationListCellPrefab;

        [HideInInspector] public List<NationListCellUI> nationCells = new List<NationListCellUI>();

        public List<Sprite> nationIcons = new List<Sprite>();
        public Sprite wizzardIcon;

        public Sprite peaceIcon;
        public Sprite warIcon;
        public Sprite slavedIcon;
        public Sprite masterIcon;
        public Sprite allianceIcon;

        [HideInInspector] public bool isActive = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void Activate()
        {
            nationList.SetActive(true);
            isActive = true;
        }

        public void DeActivate()
        {
            nationList.SetActive(false);
            isActive = false;
        }

        public void DeActivateCells()
        {
            for (int i = 0; i < nationCells.Count; i++)
            {
                nationCells[i].gameObject.SetActive(false);
            }
        }

        public void CloseAllDiplomacy()
        {
            DeActivate();
            DeActivateCells();
            OurProposalsUI.active.DeActivate();
            TheirProposalsUI.active.DeActivate();
            OurAnswersToTheirProposalsUI.active.DeActivate();
            DiplomacyReportsUI.active.CloseAll();
        }

        public void FlipActivation()
        {
            nationList.SetActive(!nationList.activeSelf);

            if (nationList.activeSelf == true)
            {
                DeActivateCells();

                for (int i = 0; i < nationCells.Count; i++)
                {
                    int nation = Diplomacy.active.GetNationIdFromName(nationCells[i].nationName.text);

                    if (nation != Diplomacy.active.playerNation)
                    {
                        int relation = Diplomacy.active.relations[Diplomacy.active.playerNation][nation];

                        if (relation > -1)
                        {
                            nationCells[i].gameObject.SetActive(true);
                            isActive = true;
                        }
                    }
                }
            }

            OurProposalsUI.active.DeActivate();
            TheirProposalsUI.active.DeActivate();
            OurAnswersToTheirProposalsUI.active.DeActivate();
            DiplomacyReportsUI.active.CloseAll();
            RefreshRelationIcons();
        }

        public void SetNewNation(int nationIconId, string nationName)
        {
            for (int i = 0; i < nationCells.Count; i++)
            {
                if (nationCells[i].nationName.text == nationName)
                {
                    return;
                }
            }

            GameObject go = Instantiate(nationListCellPrefab);
            go.transform.SetParent(nationList.transform);
            NationListCellUI nlcUI = go.GetComponent<NationListCellUI>();

            if ((nationIconId > -1) && (nationIconId < nationIcons.Count))
            {
                nlcUI.nationIcon.sprite = nationIcons[nationIconId];
            }

            nlcUI.nationName.text = nationName;
            nationCells.Add(nlcUI);
        }

        public void UpdateAsWizzardNation(string nationName)
        {
            for (int i = 0; i < nationCells.Count; i++)
            {
                if (nationCells[i].nationName.text == nationName)
                {
                    nationCells[i].nationIcon.sprite = wizzardIcon;
                }
            }
        }

        public void RemoveNation(string nationName)
        {
            int id = -1;

            for (int i = 0; i < nationCells.Count; i++)
            {
                if (nationCells[i].nationName.text == nationName)
                {
                    id = i;
                }
            }

            if (id > -1)
            {
                Destroy(nationCells[id].gameObject);
                nationCells.RemoveAt(id);
            }
        }

        public void RefreshRelationIcons()
        {
            for (int i = 0; i < nationCells.Count; i++)
            {
                nationCells[i].RefreshRelationIcon();
            }
        }
    }
}
