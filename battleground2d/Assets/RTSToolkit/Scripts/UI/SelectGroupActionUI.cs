using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SelectGroupActionUI : MonoBehaviour
    {
        public int groupId;
        public Text text;
        public Image image;

        public Color nonFormationColor;
        public Color formationColor;
        public Color journeyColor;

        void Start()
        {

        }

        public void SelectGroup()
        {
            UnitsGrouping.active.SelectGroup(groupId);
        }

        public void SetFormationColor(bool onFormation)
        {
            if (onFormation)
            {
                if (image != null)
                {
                    image.color = formationColor;
                }
            }
            else
            {
                if (image != null)
                {
                    image.color = nonFormationColor;
                }
            }
        }

        public void SetJourneyColor()
        {
            image.color = journeyColor;
        }
    }
}
