using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class MiningPointLabelUI : MonoBehaviour
    {
        public static MiningPointLabelUI active;
        public Text mineLabelText;
        public Image icon;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void Activate(int type, int value)
        {
            DeActivate();
            Economy ec = Economy.active;

            if (ec != null)
            {
                if (type > -1)
                {
                    if (type < ec.resources.Count)
                    {
                        icon.sprite = ec.resources[type].icon;
                    }
                }
            }

            mineLabelText.text = value.ToString();
            mineLabelText.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
        }

        public void UpdateAmount(int value)
        {
            mineLabelText.text = value.ToString();
        }

        public void DeActivate()
        {
            mineLabelText.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
        }
    }
}
