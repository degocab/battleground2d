using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class NationScoresUI : MonoBehaviour
    {
        public static NationScoresUI active;

        public int nation = 0;

        public Text mainTitle;

        public Text numberBuildings;
        public Text numberUnits;

        public Text lostBuildings;
        public Text lostUnits;

        public Text damageMade;
        public Text damageGot;

        public GameObject nationScoresGo;

        public GameObject collectedResGo;
        public GameObject collectedResPrefab;
        List<GameObject> collectedResInstances = new List<GameObject>();

        public GameObject currentResGo;
        public GameObject currentResPrefab;
        List<GameObject> currentResInstances = new List<GameObject>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        float deltaTime;
        void Update()
        {
            deltaTime = Time.deltaTime;
            UpdateScores();
        }

        float tUpdateScores = 0f;
        void UpdateScores()
        {
            tUpdateScores = tUpdateScores + deltaTime;
            if (tUpdateScores > 0.5f)
            {
                tUpdateScores = 0f;

                if ((nationScoresGo != null) && (nationScoresGo.activeSelf))
                {
                    Scores sc = Scores.active;
                    RTSMaster rtsm = RTSMaster.active;

                    if (rtsm != null)
                    {
                        if (sc != null)
                        {
                            if (nation < rtsm.nationPars.Count)
                            {
                                if (nation < sc.masterScores.Count)
                                {
                                    mainTitle.text = rtsm.nationPars[nation].GetNationName() + " scores (" + ((int)(sc.masterScores[nation])).ToString() + ")";
                                }
                            }
                        }
                    }

                    if (sc != null)
                    {
                        if (nation < sc.nBuildings.Count)
                        {
                            numberBuildings.text = "Number of buildings: " + sc.nBuildings[nation];
                            numberUnits.text = "Number of units: " + sc.nUnits[nation];

                            lostBuildings.text = "Lost buildings: " + sc.buildingsLost[nation];
                            lostUnits.text = "Lost units: " + sc.unitsLost[nation];

                            damageMade.text = "Damage made: " + sc.damageMade[nation].ToString("#.0");
                            damageGot.text = "Damage got: " + sc.damageObtained[nation].ToString("#.0");
                        }
                    }

                    FillResourceInstances();
                }
            }
        }

        public void Activate()
        {
            if (nationScoresGo != null)
            {
                nationScoresGo.SetActive(true);
            }
        }

        void FillResourceInstances()
        {
            CleanResourceInstances();
            Economy ec = Economy.active;

            for (int i = 0; i < ec.resources.Count; i++)
            {
                if (ec.resources[i].deliveryRtsUnitId > -1)
                {
                    GameObject go = Instantiate(collectedResPrefab, collectedResGo.transform);
                    ResourceSlotUI rsui = go.GetComponent<ResourceSlotUI>();
                    rsui.image.sprite = ec.resources[i].icon;

                    if (nation < ec.nationResources.Count)
                    {
                        rsui.text.text = ec.nationResources[nation][i].collected.ToString();
                    }

                    go.SetActive(true);
                    collectedResInstances.Add(go);
                }
            }

            for (int i = 0; i < ec.resources.Count; i++)
            {
                GameObject go = Instantiate(currentResPrefab, currentResGo.transform);
                ResourceSlotUI rsui = go.GetComponent<ResourceSlotUI>();
                rsui.image.sprite = ec.resources[i].icon;

                if (nation < ec.nationResources.Count)
                {
                    rsui.text.text = ec.nationResources[nation][i].amount.ToString();
                }

                go.SetActive(true);
                currentResInstances.Add(go);
            }
        }

        void CleanResourceInstances()
        {
            for (int i = 0; i < collectedResInstances.Count; i++)
            {
                Destroy(collectedResInstances[i]);
            }

            collectedResInstances.Clear();

            for (int i = 0; i < currentResInstances.Count; i++)
            {
                Destroy(currentResInstances[i]);
            }

            currentResInstances.Clear();
        }
    }
}
