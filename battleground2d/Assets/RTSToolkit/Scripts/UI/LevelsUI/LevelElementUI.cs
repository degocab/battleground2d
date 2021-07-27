using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class LevelElementUI : MonoBehaviour
    {
        public Text text;

        void Start()
        {

        }

        public void PointerEnter()
        {
            LevelElementsManager.active.EnableLevelInfo(this);
        }

        public void PointerExit()
        {
            LevelElementsManager.active.DisableLevelInfo();
        }
    }
}
