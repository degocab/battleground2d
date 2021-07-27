using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class UnitSelectionMark : MonoBehaviour
    {
        public static UnitSelectionMark active;

        public GameObject playerUnit;
        public GameObject peacefulNpc;
        public GameObject hostileNpc;

        List<UnitSelectionNode> instances = new List<UnitSelectionNode>();

        public Gradient colorGradient;
        public bool showSelection = true;
        public bool showHealthBars = true;

        public static Camera cam;

        void Awake()
        {
            active = this;
            cam = Camera.main;
        }

        void Start()
        {

        }

        void Update()
        {
            if (showSelection)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    UnitSelectionNode usn = instances[i];
                    usn.UpdateSelectionMarkPosition();
                    usn.selectionMarkImage.color = colorGradient.Evaluate(usn.unit.health / usn.unit.maxHealth);
                }

                CheckMarkTypesUpdate();
            }

            if (showHealthBars)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    UnitSelectionNode usn = instances[i];
                    usn.UpdateHealthBarPosition();
                    usn.healthBarSlider.value = usn.unit.health / usn.unit.maxHealth;
                }
            }
        }

        public void AddUnit(UnitPars up)
        {
            bool isSet = false;

            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].unit == up)
                {
                    isSet = true;
                }
            }

            if (isSet == false)
            {
                int type = MarkType(up);
                GameObject pref = playerUnit;

                if (type == 1)
                {
                    pref = peacefulNpc;
                }
                else if (type == 2)
                {
                    pref = hostileNpc;
                }

                GameObject inst = Instantiate(pref, transform);
                UnitSelectionNode usn = inst.GetComponent<UnitSelectionNode>();

                usn.GetCompon();
                usn.unit = up;

                usn.UpdateSelectionMarkPosition();
                usn.UpdateHealthBarPosition();

                usn.selectionMarkGo.SetActive(showSelection);
                usn.healthBarGo.SetActive(showHealthBars);

                usn.selectionMarkImage.color = colorGradient.Evaluate(up.health / up.maxHealth);
                usn.healthBarSlider.value = usn.unit.health / usn.unit.maxHealth;

                usn.markType = type;

                instances.Add(usn);
            }
        }

        public void RemoveUnit(UnitPars up)
        {
            UnitSelectionNode usn = null;

            for (int i = 0; i < instances.Count; i++)
            {
                if (up == instances[i].unit)
                {
                    usn = instances[i];
                }
            }

            if (usn != null)
            {
                instances.Remove(usn);
                Destroy(usn.gameObject);
            }
        }

        public void ShowSelectionMarks()
        {
            showSelection = true;

            for (int i = 0; i < instances.Count; i++)
            {
                UnitSelectionNode usn = instances[i];
                usn.UpdateSelectionMarkPosition();
                usn.selectionMarkImage.color = colorGradient.Evaluate(usn.unit.health / usn.unit.maxHealth);
                usn.selectionMarkGo.SetActive(showSelection);
            }
        }

        public void HideSelectionMarks()
        {
            showSelection = false;

            for (int i = 0; i < instances.Count; i++)
            {
                UnitSelectionNode usn = instances[i];
                usn.selectionMarkGo.SetActive(showSelection);
            }
        }

        public void ShowHealthBars()
        {
            showHealthBars = true;

            for (int i = 0; i < instances.Count; i++)
            {
                UnitSelectionNode usn = instances[i];
                usn.UpdateHealthBarPosition();
                usn.healthBarSlider.value = usn.unit.health / usn.unit.maxHealth;
                usn.healthBarGo.SetActive(showHealthBars);
            }
        }

        public void HideHealthBars()
        {
            showHealthBars = false;

            for (int i = 0; i < instances.Count; i++)
            {
                UnitSelectionNode usn = instances[i];
                usn.healthBarGo.SetActive(showHealthBars);
            }
        }

        void CheckMarkTypesUpdate()
        {
            for (int i = 0; i < instances.Count; i++)
            {
                UnitSelectionNode usn = instances[i];
                int newMarkType = MarkType(usn.unit);

                if (newMarkType != usn.markType)
                {
                    GameObject instOld = usn.gameObject;
                    GameObject pref = playerUnit;

                    if (newMarkType == 1)
                    {
                        pref = peacefulNpc;
                    }
                    else if (newMarkType == 2)
                    {
                        pref = hostileNpc;
                    }

                    UnitPars up = usn.unit;

                    GameObject instGo = Instantiate(pref, transform);
                    Destroy(instOld);

                    usn = instGo.GetComponent<UnitSelectionNode>();
                    instances[i] = usn;

                    usn.GetCompon();
                    usn.unit = up;
                    usn.UpdateSelectionMarkPosition();
                    usn.UpdateHealthBarPosition();

                    usn.selectionMarkGo.SetActive(showSelection);

                    usn.selectionMarkImage.color = colorGradient.Evaluate(up.health / up.maxHealth);
                    usn.healthBarSlider.value = up.health / up.maxHealth;

                    usn.markType = newMarkType;
                }
            }
        }

        int MarkType(UnitPars up)
        {
            if (up.nation != Diplomacy.active.playerNation)
            {
                if (Diplomacy.active.relations[Diplomacy.active.playerNation][up.nation] != 1)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }

            return 0;
        }
    }
}
