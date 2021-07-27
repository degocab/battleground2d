using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SelectedIconUI : MonoBehaviour
    {
        public static SelectedIconUI active;

        public Image image;
        public Text text;
        public Slider healthBar;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void Activate(int rtsId)
        {
            image.gameObject.SetActive(true);
            text.gameObject.SetActive(true);
            healthBar.gameObject.SetActive(true);


            if (SelectionManager.active.selectedGoPars.Count == 1)
            {
                text.text = SelectionManager.active.selectedGoPars[0].unitParsType.unitName;
                float health = SelectionManager.active.selectedGoPars[0].health / SelectionManager.active.selectedGoPars[0].maxHealth;

                if (health < 0)
                {
                    health = 0;
                }

                if (health > 1)
                {
                    health = 1;
                }

                healthBar.value = health;
                image.sprite = UnitIconsUI.active.unitIcons[rtsId];
            }
            else
            {
                text.text = "Troops (" + SelectionManager.active.selectedGoPars.Count + ")";
                image.sprite = UnitIconsUI.active.troopsIcon;
            }
        }

        public void SetHealth(float health1)
        {
            float health = health1;

            if (health < 0)
            {
                health = 0;
            }

            if (health > 1)
            {
                health = 1;
            }

            healthBar.value = health;
        }

        public void DeActivate()
        {
            image.gameObject.SetActive(false);
            text.gameObject.SetActive(false);
            healthBar.gameObject.SetActive(false);
        }

        public void GoToSelectedObject()
        {
            if (CameraSwitcher.active != null)
            {
                if (SelectionManager.active.selectedGoPars.Count == 1)
                {
                    UnitPars up = SelectionManager.active.selectedGoPars[0];
                    CameraSwitcher.active.LookRTSCameraToUnit(up);
                }
            }
        }
    }
}
