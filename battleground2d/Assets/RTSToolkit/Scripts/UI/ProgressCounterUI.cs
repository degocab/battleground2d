using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class ProgressCounterUI : MonoBehaviour
    {
        public static ProgressCounterUI active;

        public Text text;
        public Slider localProgress;
        public Slider globalProgress;

        [HideInInspector] public bool isActive = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void Activate()
        {
            text.gameObject.SetActive(true);
            localProgress.gameObject.SetActive(true);
            globalProgress.gameObject.SetActive(true);
            isActive = true;
        }

        public void DeActivate()
        {
            text.gameObject.SetActive(false);
            localProgress.gameObject.SetActive(false);
            globalProgress.gameObject.SetActive(false);
            isActive = false;
        }

        public void UpdateLocalValue(float value)
        {
            localProgress.value = value;
        }

        public void UpdateGlobalValue(float value)
        {
            globalProgress.value = value;
        }

        public void UpdateText(string txt)
        {
            text.text = txt;
        }

        public void UpdateText(int local, int global)
        {
            text.text = local.ToString() + "/" + global.ToString();
        }
    }
}
