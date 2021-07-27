using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class OurProposalsUI : MonoBehaviour
    {
        public static OurProposalsUI active;

        public GameObject ourProposalsGrid;
        public GameObject ourProposalsTitle;
        public GameObject choicePrefab;
        public GameObject choiceParent;

        List<GameObject> choiceInstances = new List<GameObject>();

        public NationListCellUI nationCell;

        [HideInInspector] public string nationName;
        List<OurProposalsNode> proposals = new List<OurProposalsNode>();

        RTSMaster rtsm;
        [HideInInspector] public bool isActive = false;

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

            int nation = Diplomacy.active.GetNationIdFromName(nationName);
            int relation = Diplomacy.active.relations[Diplomacy.active.playerNation][nation];

            if (relation > -1)
            {
                ourProposalsGrid.SetActive(true);
                ourProposalsTitle.SetActive(true);
                nationCell.nationName.text = nationName;
                nationCell.RefreshRelationIcon();

                isActive = true;
            }

            NationPars nationPars = RTSMaster.active.GetNationPars(nationName);
            proposals = nationPars.dialogGroup.ourProposals;
            DiplomacyTexts.active.diplomacyTextsByKey = nationPars.dialogGroup.diplomacyTextsByKey;

            for (int i = 0; i < proposals.Count; i++)
            {
                OurProposalsNode prop = proposals[i];

                if (relation == prop.relationFrom)
                {
                    if (nationPars.isWizzardNation == prop.applyToWizzardNation)
                    {
                        CreateChoice(prop);
                    }
                }
            }
        }

        void CreateChoice(OurProposalsNode prop)
        {
            GameObject choice = (GameObject)Instantiate(choicePrefab);
            choice.name = "choice_" + prop.actionKey;

            choice.transform.SetParent(choiceParent.transform);
            choice.SetActive(true);

            choice.GetComponent<Text>().text = DiplomacyTexts.active.GetText(prop.diplomacyTextRefKey, nationName);
            choice.GetComponent<OurProposalsDialogAction>().proposal = prop;

            choiceInstances.Add(choice);
        }

        public void DeActivate()
        {
            ourProposalsTitle.SetActive(false);

            ourProposalsGrid.SetActive(false);
            isActive = false;

            for (int i = 0; i < choiceInstances.Count; i++)
            {
                Destroy(choiceInstances[i]);
            }

            choiceInstances.Clear();
        }

        public void ActionResponse(OurProposalsNode prop)
        {
            if (prop.type == OurProposalsNode.OurProposalsNodeType.ProposalWithDelayedResponse)
            {
                ProposalWithDelayedResponse(prop);
            }
            else if (prop.type == OurProposalsNode.OurProposalsNodeType.AnnouncementWithRelationChange)
            {
                AnnouncementWithRelationChange(prop.relationTo, prop.actionKey, prop.responseKey);
            }
        }

        void AnnouncementWithRelationChange(int newRelation, string multiplayerMessageKey, string wizzardResponse)
        {
            // Peace, War
            // WizardPeaceResponse, WizardWarResponse
            // AllianceLeave, MercyLeave

            DeActivate();
            string pName = Diplomacy.active.GetPlayerNationName();
            Diplomacy.active.SetRelation(pName, nationName, newRelation);
            NationListUI.active.RefreshRelationIcons();

            if (wizzardResponse != string.Empty)
            {
                CheckForWizardResponse(wizzardResponse);
            }

            if (RTSMaster.active.isMultiplayer)
            {
                RTSMaster.active.rtsCameraNetwork.Cmd_SendNationMessage(pName, nationName, multiplayerMessageKey);
            }
        }

        void CheckForWizardResponse(string wizardResponseKey)
        {
            NationPars np = RTSMaster.active.GetNationPars(nationName);

            if (np.isWizzardNation)
            {
                StartCoroutine(CheckWizzardResponseCor(nationName, wizardResponseKey));
            }
        }

        void ProposalWithDelayedResponse(OurProposalsNode prop)
        {
            DeActivate();

            if (RTSMaster.active.isMultiplayer)
            {
                if (RTSMaster.active.GetNationPars(nationName).gameObject.GetComponent<NationCentreNetworkNode>().isPlayer)
                {
                    string pName = Diplomacy.active.GetPlayerNationName();
                    RTSMaster.active.rtsCameraNetwork.Cmd_SendNationMessage(pName, nationName, prop.actionKey);
                }
                else
                {
                    HandleDelayedResponse(prop);
                }
            }
            else
            {
                HandleDelayedResponse(prop);
            }
        }

        void HandleDelayedResponse(OurProposalsNode prop)
        {
            StartCoroutine(CheckDecisionCor(prop, nationName));
        }

        public void SwitchToTheirProposals()
        {
            DeActivate();
            TheirProposalsUI.active.nationName = nationName;
            TheirProposalsUI.active.Activate();
        }

        IEnumerator CheckDecisionCor(OurProposalsNode prop, string natName)
        {
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            ProceedDecision(prop, natName);
        }

        void ProceedDecision(OurProposalsNode prop, string natName)
        {
            if (prop.actionKey == "AskAlliance")
            {
                CheckSlaveryDecision(prop, natName);
            }
            else if (prop.actionKey == "BegMercy")
            {
                CheckAllianceDecision(prop, natName);
            }
        }

        void CheckSlaveryDecision(OurProposalsNode prop, string natName)
        {
            string pName = Diplomacy.active.GetPlayerNationName();

            Diplomacy.active.SetRelation(pName, natName, prop.relationTo);
            NationListUI.active.RefreshRelationIcons();

            // Mercy accept
            DiplomacyReportsUI.active.MakeProposal(natName, prop.delayedDecisionKeys.acceptKey);
        }

        void CheckAllianceDecision(OurProposalsNode prop, string natName)
        {
            int nation = Diplomacy.active.GetNationIdFromName(natName);
            int pNation = Diplomacy.active.playerNation;
            string pName = Diplomacy.active.GetPlayerNationName();

            bool decisionAccepted = false;

            if (
                (rtsm.nationPars[nation].nationAI.beatenUnits[pNation] > 20) &&
                   (Scores.active.nUnits[nation] > 20)
            )
            {
                decisionAccepted = true;
            }
            else if (Scores.active.nUnits[nation] <= 20)
            {
                decisionAccepted = true;
            }

            if (decisionAccepted)
            {
                Diplomacy.active.SetRelation(pName, natName, prop.relationTo);
                NationListUI.active.RefreshRelationIcons();

                // Alliance accept
                DiplomacyReportsUI.active.MakeProposal(natName, prop.delayedDecisionKeys.acceptKey);
            }
            else
            {
                // Alliance decline
                DiplomacyReportsUI.active.MakeProposal(natName, prop.delayedDecisionKeys.declineKey);
            }
        }

        IEnumerator CheckWizzardResponseCor(string natName, string wizardResponse)
        {
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();

            DiplomacyReportsUI.active.MakeProposal(natName, wizardResponse);
        }
    }

    [System.Serializable]
    public class ProposalNode
    {
        public string diplomacyTextRefKey = string.Empty;
        public string actionKey = string.Empty;
        public int relationFrom = 0;
        public int relationTo = 0;
        public bool applyToWizzardNation = false;
    }

    [System.Serializable]
    public class OurProposalsNode
    {
        public string diplomacyTextRefKey = string.Empty;
        public string actionKey = string.Empty;
        public string responseKey = string.Empty;
        public int relationFrom = 0;
        public int relationTo = 0;
        public bool applyToWizzardNation = false;
        public OurProposalsNodeType type;
        public DelayedDecitionKeys delayedDecisionKeys;

        public enum OurProposalsNodeType
        {
            ProposalWithDelayedResponse,
            AnnouncementWithRelationChange
        }

        [System.Serializable]
        public class DelayedDecitionKeys
        {
            public string acceptKey = string.Empty;
            public string declineKey = string.Empty;
        }
    }

    [System.Serializable]
    public class ProposalNodeGroup
    {
        public string groupKey = string.Empty;
        public List<ProposalNode> proposals = new List<ProposalNode>();
    }
}
