using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class TheirProposalsUI : MonoBehaviour
    {
        public static TheirProposalsUI active;

        public GameObject theirProposalsGrid;

        public GameObject theirProposalsTitle;
        public NationListCellUI nationCell;

        [HideInInspector] public string nationName;
        RTSMaster rtsm;

        [HideInInspector] public List<ProposalRegister> receivedProposals = new List<ProposalRegister>();

        [HideInInspector] public bool isActive = false;

        public float proposalExpireTime = 30f;

        public GameObject choicePrefab;
        public GameObject choiceParent;

        List<GameObject> choiceInstances = new List<GameObject>();
        Dictionary<string, ProposalNode> proposalsByActionKey = new Dictionary<string, ProposalNode>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        public void Activate()
        {
            DeActivate();

            theirProposalsGrid.SetActive(true);
            theirProposalsTitle.SetActive(true);

            isActive = true;
            nationCell.nationName.text = nationName;
            nationCell.RefreshRelationIcon();

            NationPars nationPars = RTSMaster.active.GetNationPars(nationName);

            proposalsByActionKey = nationPars.dialogGroup.theirProposalsByActionKey;
            DiplomacyTexts.active.diplomacyTextsByKey = nationPars.dialogGroup.diplomacyTextsByKey;

            if (receivedProposals.Count > 0)
            {
                string pName = rtsm.nationPars[Diplomacy.active.playerNation].GetNationName();

                for (int i = 0; i < receivedProposals.Count; i++)
                {
                    if (receivedProposals[i].nationName == nationName)
                    {
                        ProposalNode proposal;

                        if (proposalsByActionKey.TryGetValue(receivedProposals[i].proposalKey, out proposal))
                        {
                            CreateChoice(proposal, pName);
                        }
                    }
                }
            }
        }

        void CreateChoice(ProposalNode prop, string pName)
        {
            GameObject choice = Instantiate(choicePrefab);
            choice.name = "choice_" + prop.actionKey;

            choice.transform.SetParent(choiceParent.transform);
            choice.SetActive(true);

            choice.GetComponent<Text>().text = DiplomacyTexts.active.GetText(prop.diplomacyTextRefKey, pName);
            choice.GetComponent<TheirProposalsDialogAction>().proposal = prop;

            choiceInstances.Add(choice);
        }

        public void DeActivate()
        {
            theirProposalsTitle.SetActive(false);

            theirProposalsGrid.SetActive(false);
            isActive = false;

            for (int i = 0; i < choiceInstances.Count; i++)
            {
                Destroy(choiceInstances[i]);
            }

            choiceInstances.Clear();
        }

        public void MakeProposal(string nat, string key)
        {
            if (IsProposalAlreadyReceived(nat, key) == false)
            {
                StartCoroutine(MakeProposalCor(nat, key));
            }
        }

        IEnumerator MakeProposalCor(string nat, string key)
        {
            ProposalRegister prop = new ProposalRegister();
            prop.nationName = nat;
            prop.proposalKey = key;
            receivedProposals.Add(prop);

            yield return new WaitForSeconds(proposalExpireTime);

            receivedProposals.Remove(prop);
        }

        bool IsProposalAlreadyReceived(string nat, string key)
        {
            for (int i = 0; i < receivedProposals.Count; i++)
            {
                ProposalRegister prop = receivedProposals[i];

                if ((prop.nationName == nat) && (prop.proposalKey == key))
                {
                    return true;
                }
            }

            return false;
        }

        public void SwitchToOurProposals()
        {
            DeActivate();
            OurProposalsUI.active.nationName = nationName;
            OurProposalsUI.active.Activate();
        }

        public void ActionResponse(ProposalNode prop)
        {
            DeActivate();
            OurAnswersToTheirProposalsUI.active.nationName = nationName;

            for (int i = 0; i < receivedProposals.Count; i++)
            {
                if (receivedProposals[i].nationName == nationName)
                {
                    if (receivedProposals[i].proposalKey == prop.actionKey)
                    {
                        OurAnswersToTheirProposalsUI.active.currentProposal = receivedProposals[i];
                    }
                }
            }

            OurAnswersToTheirProposalsUI.active.Activate(prop.actionKey);
        }
    }

    public class ProposalRegister
    {
        public string nationName = string.Empty;
        public string proposalKey = string.Empty;
    }
}
