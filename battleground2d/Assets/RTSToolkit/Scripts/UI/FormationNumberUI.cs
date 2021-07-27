using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class FormationNumberUI : MonoBehaviour
    {
        public static FormationNumberUI active;

        public Text text;
        public Slider slider;

        [HideInInspector] public bool isActive = false;
        SpawnPoint spawner;

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
            slider.gameObject.SetActive(true);
            isActive = true;
        }

        public void DeActivate()
        {
            text.gameObject.SetActive(false);
            slider.gameObject.SetActive(false);
            isActive = false;
        }

        public void RefreshSliderText()
        {
            text.text = "Formation (" + slider.value.ToString() + ")";

            if (spawner == null)
            {
                SelectionManager selM = SelectionManager.active;

                if (selM.selectedGoPars.Count > 0)
                {
                    if (selM.selectedGoPars[0].gameObject.GetComponent<SpawnPoint>() != null)
                    {
                        spawner = selM.selectedGoPars[0].gameObject.GetComponent<SpawnPoint>();
                    }
                }
            }

            if (spawner != null)
            {
                spawner.formationSize = (int)(slider.value);
            }
        }
    }
}
