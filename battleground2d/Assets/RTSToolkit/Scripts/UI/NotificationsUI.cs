using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class NotificationsUI : MonoBehaviour
    {
        public static NotificationsUI active;

        public Toggle taxes;
        public Toggle warWarning;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void SwitchTaxes()
        {
            Economy.active.taxesAndWagesReport = taxes.isOn;
        }

        public void SwitchWarWarning()
        {
            Diplomacy.active.useWarNoticeWarning = warWarning.isOn;
        }
    }
}
