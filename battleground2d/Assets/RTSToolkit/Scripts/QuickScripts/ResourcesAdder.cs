using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class ResourcesAdder : MonoBehaviour
    {
        public List<int> resourceToAdd = new List<int>();
        public int nation = 0;
        public bool allNations = false;
        public bool updateMode = false;
        public KeyCode key = KeyCode.R;

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                if (updateMode)
                {
                    if (allNations)
                    {
                        for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                        {
                            UpdateResources(i);
                        }
                    }
                    else
                    {
                        UpdateResources(nation);
                    }
                }
                else
                {
                    if (allNations)
                    {
                        for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                        {
                            AddResources(i);
                        }
                    }
                    else
                    {
                        AddResources(nation);
                    }
                }
            }
        }

        void AddResources(int nat)
        {
            if (nat > -1)
            {
                if (nat < RTSMaster.active.nationPars.Count)
                {
                    Economy eco = Economy.active;

                    if (eco != null)
                    {
                        for (int i = 0; i < resourceToAdd.Count; i++)
                        {
                            if (i < eco.nationResources[nat].Count)
                            {
                                eco.nationResources[nat][i].amount = eco.nationResources[nat][i].amount + resourceToAdd[i];
                            }
                        }

                        eco.RefreshResources();
                    }
                }
            }
        }

        void UpdateResources(int nat)
        {
            if (nat > -1)
            {
                if (nat < RTSMaster.active.nationPars.Count)
                {
                    Economy eco = Economy.active;

                    if (eco != null)
                    {
                        for (int i = 0; i < resourceToAdd.Count; i++)
                        {
                            if (i < eco.nationResources[nat].Count)
                            {
                                eco.nationResources[nat][i].amount = resourceToAdd[i];
                            }
                        }

                        eco.RefreshResources();
                    }
                }
            }
        }
    }
}
