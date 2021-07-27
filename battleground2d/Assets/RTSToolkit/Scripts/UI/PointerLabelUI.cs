using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class PointerLabelUI : MonoBehaviour
    {
        public string textToDisplay;
        public Text text;

        void Start()
        {

        }

        public void PointerEnter()
        {
            SetActivity(true);
            text.text = textToDisplay;
        }

        public void PointerExit()
        {
            SetActivity(false);
        }

        public void SetActivity(bool activity)
        {
            text.gameObject.SetActive(activity);
        }
    }
}
