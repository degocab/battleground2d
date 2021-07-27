using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class DiplomacyReportsUI : MonoBehaviour
    {
        public static DiplomacyReportsUI active;

        public GameObject grid;
        public GameObject functionlessText;
        public GameObject proposal;
        public GameObject proposalsParent;

        Dictionary<string, DiplomacyReportUI> diplomacyReportsByName = new Dictionary<string, DiplomacyReportUI>();
        // 0 - ask alliance
        // 1 - beg mercy

        Dictionary<string, ProposalRegister> receivedProposals = new Dictionary<string, ProposalRegister>();
        [HideInInspector] public List<DiplomacyReportsNodeUI> currentReports = new List<DiplomacyReportsNodeUI>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        // Types of proposals

        // Nation functionless with expiration
        // MakeGreetings, ProposeRetreating, ProposeWar

        // Nation functionless without expiration
        // ProposePeace, AllianceAccept, AllianceDecline, AllianceLeave, MercyAccept, MercyDecline, MercyLeave, WizzardWarResponse, WizzardPeaceResponse

        // Nation with request action
        // Alliance, Mercy

        // Non-nation text report
        // MakeTextReport

        public void CloseAll()
        {
            List<DiplomacyReportsNodeUI> reportsToClose = new List<DiplomacyReportsNodeUI>();

            for (int i = 0; i < currentReports.Count; i++)
            {
                reportsToClose.Add(currentReports[i]);
            }

            for (int i = 0; i < reportsToClose.Count; i++)
            {
                reportsToClose[i].Close();
            }
        }

        public void MakeTextReport(string repText)
        {
            if (IsActiveAny() == false)
            {
                GameObject go = Instantiate(functionlessText);
                go.transform.SetParent(functionlessText.transform.parent);
                DiplomacyReportsNodeUI drnUI = go.GetComponent<DiplomacyReportsNodeUI>();
                currentReports.Add(drnUI);
                drnUI.text.text = repText;
                go.SetActive(true);
            }
        }

        public void MakeProposal(string natName, string proposalName)
        {
            NationPars nationPars = RTSMaster.active.GetNationPars(natName);
            diplomacyReportsByName = nationPars.dialogGroup.diplomacyReportsByName;
            DiplomacyTexts.active.diplomacyTextsByKey = nationPars.dialogGroup.diplomacyTextsByKey;

            DiplomacyReportUI report;

            if (diplomacyReportsByName.TryGetValue(proposalName, out report))
            {
                MakeProposal(natName, report);
            }
        }

        void MakeProposal(string natName, DiplomacyReportUI report)
        {
            TheirProposalsUI.active.MakeProposal(natName, report.theirProposalsUIRefKey);

            if (IsActiveAny() == false)
            {
                if (IsProposalExpired(natName, report.key))
                {
                    DiplomacyReportsNodeUI drnUI = Proposal_Pre(natName);

                    drnUI.text.text = DiplomacyTexts.active.GetText(report.diplomacyTextRefKey, Diplomacy.active.GetPlayerNationName());
                    drnUI.actionMode = report.diplomacyReportsNodeUIActionMode;

                    Proposal_Post(drnUI, natName);
                    ScheduleProposalCooldown(natName, report);
                }
            }
        }

        DiplomacyReportsNodeUI Proposal_Pre(string natName)
        {
            GameObject go = (GameObject)Instantiate(proposal);
            go.transform.SetParent(proposalsParent.transform);
            DiplomacyReportsNodeUI drnUI = go.GetComponent<DiplomacyReportsNodeUI>();
            currentReports.Add(drnUI);
            drnUI.nationName = natName;
            return drnUI;
        }

        void Proposal_Post(DiplomacyReportsNodeUI drnUI, string natName)
        {
            drnUI.icon.nationName.text = natName;
            drnUI.icon.RefreshRelationIcon();
            drnUI.gameObject.SetActive(true);
        }

        bool IsActiveAny()
        {
            if (NationListUI.active.isActive)
            {
                return true;
            }

            if (OurProposalsUI.active.isActive)
            {
                return true;
            }

            if (TheirProposalsUI.active.isActive)
            {
                return true;
            }

            if (OurAnswersToTheirProposalsUI.active.isActive)
            {
                return true;
            }

            return false;
        }

        bool IsProposalExpired(string nat, string key)
        {
            ProposalRegister prop;

            if (receivedProposals.TryGetValue(GetNationAndProposalIdString(nat, key), out prop))
            {
                return false;
            }

            return true;
        }

        void ScheduleProposalCooldown(string nat, DiplomacyReportUI report)
        {
            if (report != null)
            {
                if (report.coolDownTime > 0)
                {
                    string nationAndProposalIdString = GetNationAndProposalIdString(nat, report.key);

                    if (!receivedProposals.ContainsKey(nationAndProposalIdString))
                    {
                        StartCoroutine(ScheduleProposalCooldownCor(nat, nationAndProposalIdString, report));
                    }
                }
            }
        }

        IEnumerator ScheduleProposalCooldownCor(string nat, string nationAndProposalIdString, DiplomacyReportUI report)
        {
            ProposalRegister prop = new ProposalRegister();
            prop.nationName = nat;
            receivedProposals.Add(nationAndProposalIdString, prop);

            yield return new WaitForSeconds(report.coolDownTime);
            yield return null;

            receivedProposals.Remove(nationAndProposalIdString);
        }

        string GetNationAndProposalIdString(string natName, string key)
        {
            return natName + key;
        }
    }

    [System.Serializable]
    public class DiplomacyReportUI
    {
        // Functional
        // 0 - Alliance  20f, 2, 4, 1
        // 1 - Mercy 20f, 3, 2, 2

        // Functionless with expiration
        // 2 - Greetings 3f, 6, -1, -1
        // 3 - Retreat 1f, 7, -1, -1
        // 4 - War 1f, 0, -1, -1

        // 5 - Wizard retreat in peace 1f, 14, -1, -1
        // 6 - Wizard retreat in war 1f, 13, -1, -1

        // Functionless without expiration
        // 7 - Mercy leave -1f, 4, -1, -1
        // 8 - Alliance leave -1f, 5, -1, -1
        // 9 - Mercy accept -1f, 10, -1, -1
        // 10 - Mercy decline -1f, 11, -1, -1
        // 11 - Alliance accept -1f, 8, -1, -1
        // 12 - Alliance decline -1f, 9, -1, -1
        // 13 - Peace -1f, 1, -1, -1
        // 14 - Wizard war response -1f, 12, -1, -1
        // 15 - Wizard peace response -1f, 8, -1, -1

        public string key = string.Empty;
        public bool hasNationCell = false;
        public float coolDownTime = 1f;
        public string diplomacyTextRefKey = string.Empty;
        public string theirProposalsUIRefKey = string.Empty;
        public string diplomacyReportsNodeUIActionMode = string.Empty;
    }
}
