using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class NationListCellUI : MonoBehaviour
    {
        public Image relationIcon;
        public Image nationIcon;
        public Text nationName;

        NationPars nationPars;

        void Start()
        {

        }

        public void OpenOurProposals()
        {
            NationListUI.active.DeActivate();

            if (OurProposalsUI.active.isActive)
            {
                TheirProposalsUI.active.nationName = nationName.text;
                TheirProposalsUI.active.Activate();
                OurProposalsUI.active.DeActivate();
            }
            else
            {
                OurProposalsUI.active.nationName = nationName.text;
                OurProposalsUI.active.Activate();
                TheirProposalsUI.active.DeActivate();
            }
        }

        public void RefreshRelationIcon()
        {
            Diplomacy diplomacy = Diplomacy.active;

            int nation = diplomacy.GetNationIdFromName(nationName.text);
            int pNation = diplomacy.playerNation;

            if (diplomacy.relations[pNation][nation] == 0)
            {
                relationIcon.sprite = NationListUI.active.peaceIcon;
            }

            if (diplomacy.relations[pNation][nation] == 1)
            {
                relationIcon.sprite = NationListUI.active.warIcon;
            }

            if (diplomacy.relations[pNation][nation] == 2)
            {
                relationIcon.sprite = NationListUI.active.slavedIcon;
            }

            if (diplomacy.relations[pNation][nation] == 3)
            {
                relationIcon.sprite = NationListUI.active.masterIcon;
            }

            if (diplomacy.relations[pNation][nation] == 4)
            {
                relationIcon.sprite = NationListUI.active.allianceIcon;
            }

            nationPars = RTSMaster.active.GetNationPars(nationName.text);
            int nationIconId = nationPars.GetNationIconId();

            if ((nationIconId > -1) && (nationIconId < NationListUI.active.nationIcons.Count))
            {
                nationIcon.sprite = NationListUI.active.nationIcons[nationIconId];
            }

            if (nationPars.isWizzardNation)
            {
                nationIcon.sprite = NationListUI.active.wizzardIcon;
            }
        }
    }
}
