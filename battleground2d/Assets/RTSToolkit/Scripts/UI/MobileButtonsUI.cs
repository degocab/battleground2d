using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class MobileButtonsUI : MonoBehaviour
    {
        public static MobileButtonsUI active;

        public GameObject grid;

        public GameObject moveButton;
        public GameObject rotateButton;
        public GameObject zoomButton;
        public GameObject selectionButton;

        public GameObject buildButton;
        public GameObject buildCancelButton;

        public Image moveButtonIcon;
        public Image rotateButtonIcon;
        public Image zoomButtonIcon;
        public Image selectionButtonIcon;

        Image moveButtonBg;
        Image rotateButtonBg;
        Image zoomButtonBg;
        Image selectionButtonBg;

        public Color activeBackground;
        public Color activeIcon;
        public Color inactiveBackground;
        public Color inactiveIcon;

        [HideInInspector] public bool isMobileMode = false;

        UnitPars model;
        int previousMovementMode = 0;

        void Awake()
        {
            active = this;
#if UNITY_IPHONE || UNITY_ANDROID
		isMobileMode = true;
#else
            isMobileMode = false;
#endif
        }

        void Start()
        {
            if (isMobileMode)
            {
                grid.SetActive(true);

                moveButtonBg = moveButton.GetComponent<Image>();
                rotateButtonBg = rotateButton.GetComponent<Image>();
                zoomButtonBg = zoomButton.GetComponent<Image>();
                selectionButtonBg = selectionButton.GetComponent<Image>();

                DeActivateBuildMode();
                ActivateMove();
            }
            else
            {
                DisableAllButtons();
            }
        }

        public void ActivateMove()
        {
            if (buildButton.activeSelf == false)
            {
                DeActivate();
                moveButtonBg.color = activeBackground;
                moveButtonIcon.color = activeIcon;
                RTSCamera.active.mobileCameraMode = 1;
            }
        }

        public void ActivateRotate()
        {
            if (buildButton.activeSelf == false)
            {
                DeActivate();
                rotateButtonBg.color = activeBackground;
                rotateButtonIcon.color = activeIcon;
                RTSCamera.active.mobileCameraMode = 2;
            }
        }

        public void ActivateZoom()
        {
            if (buildButton.activeSelf == false)
            {
                DeActivate();
                zoomButtonBg.color = activeBackground;
                zoomButtonIcon.color = activeIcon;
                RTSCamera.active.mobileCameraMode = 3;
            }
        }

        public void ActivateSelection()
        {
            if (buildButton.activeSelf == false)
            {
                DeActivate();
                selectionButtonBg.color = activeBackground;
                selectionButtonIcon.color = activeIcon;
                RTSCamera.active.mobileCameraMode = 4;
            }
        }

        public void ActivateBuildMode(UnitPars mdl)
        {
            DeActivate();
            model = mdl;
            buildButton.SetActive(true);
            buildCancelButton.SetActive(true);
            previousMovementMode = RTSCamera.active.mobileCameraMode;
            RTSCamera.active.mobileCameraMode = -1;
        }

        public void DeActivateBuildMode()
        {
            buildButton.SetActive(false);
            buildCancelButton.SetActive(false);

            DeActivate();

            if (previousMovementMode == 2)
            {
                ActivateRotate();
            }
            else if (previousMovementMode == 3)
            {
                ActivateZoom();
            }
            else if (previousMovementMode == 4)
            {
                ActivateSelection();
            }
            else
            {
                ActivateMove();
            }

            RTSCamera.active.mobileCameraMode = previousMovementMode;
            previousMovementMode = -1;
        }

        public void DeActivate()
        {
            moveButtonBg.color = inactiveBackground;
            rotateButtonBg.color = inactiveBackground;
            zoomButtonBg.color = inactiveBackground;
            selectionButtonBg.color = inactiveBackground;

            moveButtonIcon.color = inactiveIcon;
            rotateButtonIcon.color = inactiveIcon;
            zoomButtonIcon.color = inactiveIcon;
            selectionButtonIcon.color = inactiveIcon;
        }

        public void Build()
        {
            if (BuildMark.active.projector.activeSelf)
            {
                if (BuildMark.active.buildingAllowed)
                {
                    BuildMark.active.SetBuilding(BuildMark.active.buildPosition);
                    DeActivateBuildMode();
                }
            }
            else
            {
                SpawnElementUI.StartUnitSpawn(model);
                DeActivateBuildMode();
            }
        }

        public void CancelBuild()
        {
            DeActivateBuildMode();

            if (BuildMark.active.projector.activeSelf == true)
            {
                BuildMark.active.DisableProjector();
                DeActivateBuildMode();
            }
            else
            {
                SpawnGridUI.active.DisableAllGrids();
                SpawnNumberUI.active.DisableScrollMode();
                DeActivateBuildMode();
            }
        }

        public void DisableAllButtons()
        {
            moveButton.SetActive(false);
            rotateButton.SetActive(false);
            zoomButton.SetActive(false);
            selectionButton.SetActive(false);

            buildButton.SetActive(false);
            buildCancelButton.SetActive(false);

            grid.SetActive(false);
        }
    }
}
