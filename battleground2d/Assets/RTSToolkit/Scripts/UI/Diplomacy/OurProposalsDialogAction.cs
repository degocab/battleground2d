using UnityEngine;

namespace RTSToolkit
{
    public class OurProposalsDialogAction : MonoBehaviour
    {
        public OurProposalsNode proposal;

        // Our proposals

        // 0 - Alliance leave
        // 1 - SetWar
        // 2 - Ask alliance
        // 3 - Beg mercy
        // 4 - Leave slavery
        // 5 - Set peace

        void Start()
        {

        }

        public void PerformAction()
        {
            OurProposalsUI.active.ActionResponse(proposal);
        }
    }
}
