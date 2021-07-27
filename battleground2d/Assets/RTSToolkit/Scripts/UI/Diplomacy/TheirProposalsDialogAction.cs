using UnityEngine;

namespace RTSToolkit
{
    public class TheirProposalsDialogAction : MonoBehaviour
    {
        public ProposalNode proposal;

        // Their proposals

        // 6 - Alliance respond
        // 7 - Mercy respond

        void Start()
        {

        }

        public void PerformAction()
        {
            TheirProposalsUI.active.ActionResponse(proposal);
        }
    }
}
