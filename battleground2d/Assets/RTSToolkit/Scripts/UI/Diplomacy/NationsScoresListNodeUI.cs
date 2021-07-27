using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class NationsScoresListNodeUI : MonoBehaviour
    {
        public Text text;
        public int nation = 0;

        void Start()
        {

        }

        public void ChangeNationScoresUI()
        {
            if (NationScoresUI.active != null)
            {
                NationScoresUI.active.nation = nation;
            }
        }
    }
}
