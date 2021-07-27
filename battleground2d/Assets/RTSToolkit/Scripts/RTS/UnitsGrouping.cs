using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class UnitsGrouping : MonoBehaviour
    {
        public static UnitsGrouping active;

        [HideInInspector] public List<UnitsGroup> unitsGroups = new List<UnitsGroup>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void GroupSelected()
        {
            UnitsGroup ug = new UnitsGroup();
            unitsGroups.Add(ug);

            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars goPars = RTSMaster.active.allUnits[i];

                if (goPars.isSelected)
                {
                    if (goPars.unitsGroup != null)
                    {
                        if (goPars.unitsGroup.members.Count > 1)
                        {
                            goPars.unitsGroup.members.Remove(goPars);
                        }
                        else
                        {
                            CollapseGroup(goPars.unitsGroup);
                        }
                    }

                    goPars.unitsGroup = ug;
                    ug.members.Add(goPars);
                }
            }

            CheckGroupFormation(ug);
        }

        public void CheckGroupFormation(UnitsGroup ug)
        {
            if (GroupingMenuUI.active != null)
            {
                if (GroupingMenuUI.active.GetFormationMode())
                {
                    Formation form = new Formation();
                    Formations.active.AddUnitsToFormations(ug.members, form, true);
                    ug.formationMode = 1;

                    form.GetDestinations(ug.members, form.CurrentMassCentre(ug.members));
                }

                if (GroupingMenuUI.active.GetJourneyMode())
                {
                    ug.journeyMode = 1;
                    Journeys.active.CreateNewJourney(ug.members);
                    ug.journey = Journeys.active.journeys[Journeys.active.journeys.Count - 1];
                }
            }
        }

        public void CollapseGroup(UnitsGroup ug)
        {
            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars goPars = RTSMaster.active.allUnits[i];

                if (goPars.unitsGroup == ug)
                {
                    goPars.unitsGroup.members.Remove(goPars);
                    goPars.unitsGroup = null;

                }
            }

            unitsGroups.Remove(ug);
        }

        public void CleanUpGroups()
        {
            for (int i = 0; i < unitsGroups.Count; i++)
            {
                if (unitsGroups[i].journeyMode == 1)
                {
                    Journeys.active.RemoveJourney(unitsGroups[i].journey);
                }
            }

            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars goPars = RTSMaster.active.allUnits[i];

                if (goPars.unitsGroup != null)
                {
                    goPars.unitsGroup.members.Remove(goPars);
                    goPars.unitsGroup = null;
                }
            }

            unitsGroups.Clear();
        }

        public void SelectGroup(int iGroup)
        {
            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars goPars = RTSMaster.active.allUnits[i];

                if (goPars.isSelected == true)
                {
                    SelectionManager.active.DeselectObject(goPars);
                }
            }

            UnitsGroup ug = null;

            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars goPars = RTSMaster.active.allUnits[i];

                if (goPars.unitsGroup != null)
                {
                    if ((unitsGroups.IndexOf(goPars.unitsGroup) + 1) == iGroup)
                    {
                        ug = goPars.unitsGroup;

                        if (goPars.isSelected == false)
                        {
                            SelectionManager.active.SelectObject(goPars);
                            SelectionManager.active.ActivateUnitsMenu();
                            SelectionManager.active.SelectedUnitsInfo();
                        }
                    }
                }
            }

            if (ug != null)
            {
                if (ug.journeyMode == 1)
                {
                    JourneysUI.active.OpenJourneyMenu(ug.journey);
                }
            }
        }

        public UnitsGroup GetUnitsGroup(int iGroup)
        {
            UnitsGroup ug = null;

            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars goPars = RTSMaster.active.allUnits[i];

                if (goPars.unitsGroup != null)
                {
                    if ((unitsGroups.IndexOf(goPars.unitsGroup) + 1) == iGroup)
                    {
                        ug = goPars.unitsGroup;
                    }
                }
            }

            return ug;
        }

        public void RemoveUnitFromGroup(UnitPars up)
        {
            UnitsGroup ug = up.unitsGroup;

            if (ug != null)
            {
                if (ug.members != null)
                {
                    ug.members.Remove(up);
                }
            }

            up.unitsGroup = null;
        }
    }

    [System.Serializable]
    public class UnitsGroup
    {
        public List<UnitPars> members = new List<UnitPars>();
        public int mode = 0;
        public int formationMode = 0;
        public int journeyMode = 0;
        public Journeys.Journey journey = null;
    }
}
