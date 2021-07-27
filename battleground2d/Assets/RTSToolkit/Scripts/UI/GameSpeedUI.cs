using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class GameSpeedUI : MonoBehaviour
    {
        public Slider slider;
        public bool usePowerLaw = false;
        public float powerLawIndex = 4f;

        void Start()
        {

        }

        public void ChangeGameSpeed()
        {
            if (usePowerLaw == true)
            {
                Time.timeScale = Mathf.Pow(slider.value, powerLawIndex);
            }
            else
            {
                Time.timeScale = slider.value;
            }

            if (slider.value == slider.minValue)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
