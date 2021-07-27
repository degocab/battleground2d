using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SelectionMarksUI : MonoBehaviour
    {
        public Toggle toggle;

        void Start()
        {

        }

        void Update()
        {

        }

        public void SwitchSelectionMarks()
        {
            if (toggle != null)
            {
                SelectionMarkOnGUI sm = SelectionMarkOnGUI.active;

                if (sm != null)
                {
                    if (sm.enabled)
                    {
                        if (sm.gameObject.activeSelf)
                        {
                            sm.useSelectionMarks = toggle.isOn;
                        }
                    }
                }

                UnitSelectionMark usm = UnitSelectionMark.active;

                if (usm != null)
                {
                    if (usm.enabled)
                    {
                        if (usm.gameObject.activeSelf)
                        {
                            if (toggle.isOn)
                            {
                                usm.ShowSelectionMarks();
                            }
                            else
                            {
                                usm.HideSelectionMarks();
                            }
                        }
                    }
                }

                SelectionMarkParticle smp = SelectionMarkParticle.active;

                if (smp != null)
                {
                    if (smp.enabled)
                    {
                        if (smp.gameObject.activeSelf)
                        {
                            smp.useSelectionMarks = toggle.isOn;
                        }
                    }
                }
            }
        }

        public void SwitchHealthBars()
        {
            if (toggle != null)
            {
                SelectionMarkOnGUI sm = SelectionMarkOnGUI.active;

                if (sm != null)
                {
                    if (sm.enabled)
                    {
                        if (sm.gameObject.activeSelf)
                        {
                            sm.useHealthBars = toggle.isOn;
                        }
                    }
                }

                UnitSelectionMark usm = UnitSelectionMark.active;

                if (usm != null)
                {
                    if (usm.enabled)
                    {
                        if (usm.gameObject.activeSelf)
                        {
                            if (toggle.isOn)
                            {
                                usm.ShowHealthBars();
                            }
                            else
                            {
                                usm.HideHealthBars();
                            }
                        }
                    }
                }

                HealthBarParticle hbp = HealthBarParticle.active;

                if (hbp != null)
                {
                    if (hbp.enabled)
                    {
                        if (hbp.gameObject.activeSelf)
                        {
                            hbp.useHealthBars = toggle.isOn;
                        }
                    }
                }
            }
        }
    }
}
