using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class DiplomacyReportsNodeUI : MonoBehaviour
    {
        public float timeToDisplay = 5f;
        [HideInInspector] public string nationName;

        public Text text;
        public NationListCellUI icon;

        public string actionMode = string.Empty;
        // 1 - alliance
        // 2 - mercy

        void Start()
        {
            StartCoroutine(Display());
        }

        IEnumerator Display()
        {
            yield return new WaitForSeconds(timeToDisplay);
            DiplomacyReportsUI.active.currentReports.Remove(this);
            Destroy(this.gameObject);
        }

        public void ProceedAction()
        {
            OurAnswersToTheirProposalsCallback();
            Close();
        }

        public void Close()
        {
            StopCoroutine("Display");
            DiplomacyReportsUI.active.currentReports.Remove(this);
            Destroy(this.gameObject);
        }

        void OurAnswersToTheirProposalsCallback()
        {
            if (actionMode != string.Empty)
            {
                OurAnswersToTheirProposalsUI.active.nationName = nationName;
                OurAnswersToTheirProposalsUI.active.Activate(actionMode);
            }
        }
    }
}
