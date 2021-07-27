using UnityEngine;

namespace RTSToolkit
{
    public class OurAnswersToTheirProposalsDialogAction : MonoBehaviour
    {
        public ProposalNode proposal;

        // Our answers to their proposals
        // 8 - Alliance accept
        // 9 - Alliance decline
        // 10 - Mercy accept
        // 11 - mercy decline

        void Start()
        {

        }

        public void PerformAction()
        {
            OurAnswersToTheirProposalsUI.active.ActionResponse(proposal);
        }
    }
}
