using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class OurAnswersToTheirProposalsUI : MonoBehaviour
    {
        public static OurAnswersToTheirProposalsUI active;

        public GameObject grid;

        public GameObject answerTitle;
        public NationListCellUI nationCell;

        [HideInInspector] public string nationName;

        public ProposalRegister currentProposal;
        [HideInInspector] public bool isActive = false;

        public GameObject choicePrefab;
        public GameObject choiceParent;

        List<GameObject> choiceInstances = new List<GameObject>();

        Dictionary<string, ProposalNodeGroup> proposalGroupsByKey = new Dictionary<string, ProposalNodeGroup>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void Activate(string mode)
        {
            DeActivate();
            grid.SetActive(true);
            answerTitle.SetActive(true);

            NationPars nationPars = RTSMaster.active.GetNationPars(nationName);

            proposalGroupsByKey = nationPars.dialogGroup.ourAnswersToTheirProposalsByKey;
            DiplomacyTexts.active.diplomacyTextsByKey = nationPars.dialogGroup.diplomacyTextsByKey;

            ProposalNodeGroup proposalGroup;

            if (proposalGroupsByKey.TryGetValue(mode, out proposalGroup))
            {
                for (int i = 0; i < proposalGroup.proposals.Count; i++)
                {
                    ProposalNode proposal = proposalGroup.proposals[i];
                    CreateChoice(proposal);
                }
            }

            isActive = true;
            nationCell.nationName.text = nationName;
            nationCell.RefreshRelationIcon();
        }

        void CreateChoice(ProposalNode prop)
        {
            GameObject choice = (GameObject)Instantiate(choicePrefab);
            choice.name = "choice_" + prop.actionKey;

            choice.transform.SetParent(choiceParent.transform);
            choice.SetActive(true);

            choice.GetComponent<Text>().text = DiplomacyTexts.active.GetText(prop.diplomacyTextRefKey, nationName);
            choice.GetComponent<OurAnswersToTheirProposalsDialogAction>().proposal = prop;

            choiceInstances.Add(choice);
        }

        public void DeActivate()
        {
            answerTitle.SetActive(false);
            grid.SetActive(false);
            isActive = false;

            for (int i = 0; i < choiceInstances.Count; i++)
            {
                Destroy(choiceInstances[i]);
            }

            choiceInstances.Clear();
        }

        public void SwitchToTheirProposals()
        {
            DeActivate();
            TheirProposalsUI.active.nationName = nationName;
            TheirProposalsUI.active.Activate();
        }

        public void ActionResponse(ProposalNode prop)
        {
            DeActivate();
            TheirProposalsUI.active.receivedProposals.Remove(currentProposal);

            bool isMultiplayer = RTSMaster.active.isMultiplayer;
            bool isRelationDifferent = (prop.relationFrom != prop.relationTo);

            if (isMultiplayer || isRelationDifferent)
            {
                string pName = Diplomacy.active.GetPlayerNationName();

                if (isRelationDifferent)
                {
                    Diplomacy.active.SetRelation(pName, nationName, prop.relationTo);
                }

                if (isMultiplayer)
                {
                    RTSMaster.active.rtsCameraNetwork.Cmd_SendNationMessage(pName, nationName, prop.actionKey);
                }
            }
        }
    }
}
