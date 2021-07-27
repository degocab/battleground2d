using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class Economy : MonoBehaviour
    {
        public static Economy active;

        public List<EconomyResource> resources;
        [HideInInspector] public List<List<EconomyResource>> nationResources = new List<List<EconomyResource>>();

        RTSMaster rtsm;
        Diplomacy diplomacy;

        public float taxesUpdateTime = 90f;
        public float productionUpdateTime = 3f;

        PlayerResourcesUI playerResourcesUI;

        public Color staticTextColor;
        public Color negativeTextColor;

        public bool taxesAndWagesReport = true;

        void Awake()
        {
            active = this;
        }

        public static Economy GetActive()
        {
            if (Economy.active == null)
            {
                Economy.active = UnityEngine.Object.FindObjectOfType<Economy>();
            }

            return Economy.active;
        }

        public void AddNewNationRes()
        {
            nationResources.Add(new List<EconomyResource>());
            Diplomacy dip = Diplomacy.active;

            for (int i = 0; i < resources.Count; i++)
            {
                EconomyResource er = new EconomyResource();
                er.name = resources[i].name;
                er.icon = resources[i].icon;
                er.amount = resources[i].amount;
                er.producers = resources[i].producers;
                er.consumersRtsIds = resources[i].consumersRtsIds;
                er.taxesAndWagesFactor = resources[i].taxesAndWagesFactor;
                nationResources[nationResources.Count - 1].Add(er);
            }

            if (dip != null)
            {
                if ((nationResources.Count - 1) == dip.playerNation)
                {
                    PlayerResourcesUI prui = PlayerResourcesUI.active;

                    if (prui != null)
                    {
                        prui.InitializeResources();
                    }
                }
            }
        }

        public void RemoveNation(int natId)
        {
            if ((natId > -1) && (natId < nationResources.Count))
            {
                nationResources.RemoveAt(natId);
            }
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            diplomacy = Diplomacy.active;
            lockUpdate = false;
        }

        float iUpdateTaxes = 0f;
        float iUpdatePopulation = 0f;

        public bool lockUpdate = false;

        void Update()
        {
            if (lockUpdate == false)
            {
                float dt = Time.deltaTime;

                iUpdateTaxes = iUpdateTaxes + dt;
                if (iUpdateTaxes > taxesUpdateTime)
                {
                    UpdateTaxes();
                    iUpdateTaxes = 0f;
                }

                iUpdatePopulation = iUpdatePopulation + dt;
                if (iUpdatePopulation > productionUpdateTime)
                {
                    IncreasePopulation();
                    iUpdatePopulation = 0f;
                }
            }
        }

        void UpdateTaxes()
        {
            for (int i = 0; i < nationResources.Count; i++)
            {
                int diff = GetExpectedProductionDifference(i);

                if (rtsm.nationPars[i].nationAI.masterNationId == -1)
                {
                    for (int j = 0; j < nationResources[i].Count; j++)
                    {
                        nationResources[i][j].amount = nationResources[i][j].amount + (int)(nationResources[i][j].taxesAndWagesFactor * diff);

                        if (nationResources[i][j].taxesAndWagesFactor > 0)
                        {
                            if (nationResources[i][j].amount < 100)
                            {
                                nationResources[i][j].amount = 100;
                            }
                        }
                    }
                }
                else
                {
                    int master = rtsm.nationPars[i].nationAI.masterNationId;

                    for (int j = 0; j < nationResources[i].Count; j++)
                    {
                        // master will never pay taxes for slave
                        if (diff <= 0)
                        {
                            nationResources[i][j].amount = nationResources[i][j].amount + (int)(nationResources[i][j].taxesAndWagesFactor * diff);
                            if (nationResources[i][j].taxesAndWagesFactor > 0)
                            {
                                if (nationResources[i][j].amount < 100)
                                {
                                    nationResources[i][j].amount = 100;
                                }
                            }
                        }

                        // slave will always give half of its profit to master	
                        else
                        {
                            nationResources[i][j].amount = nationResources[i][j].amount + (int)(0.5f * nationResources[i][j].taxesAndWagesFactor * diff);
                            if (nationResources[i][j].taxesAndWagesFactor > 0)
                            {
                                if (nationResources[i][j].amount < 50)
                                {
                                    nationResources[i][j].amount = 50;
                                }
                            }
                            nationResources[master][j].amount = nationResources[master][j].amount + (int)(0.5f * nationResources[i][j].taxesAndWagesFactor * diff);
                        }
                    }
                }

                for (int j = 0; j < nationResources[i].Count; j++)
                {
                    if (nationResources[i][j].amount < 0)
                    {
                        nationResources[i][j].amount = 0;
                    }
                }
            }

            if (playerResourcesUI == null)
            {
                playerResourcesUI = PlayerResourcesUI.active;
            }

            if (playerResourcesUI != null)
            {
                RefreshResources();
            }

            if (taxesAndWagesReport)
            {
                DiplomacyReportsUI.active.MakeTextReport("Taxes collected, wages paid");
            }
        }

        void IncreasePopulation()
        {
            for (int i = 0; i < nationResources.Count; i++)
            {
                for (int j = 0; j < nationResources[i].Count; j++)
                {
                    if (nationResources[i][j].producers.Count > 0)
                    {
                        if (nationResources[i][j].amount < GetExpectedProduction(i))
                        {
                            nationResources[i][j].amount = nationResources[i][j].amount + 1;

                            if (i == diplomacy.playerNation)
                            {
                                RefreshResources();
                            }
                        }
                    }

                    if (i == diplomacy.playerNation)
                    {
                        if (playerResourcesUI == null)
                        {
                            playerResourcesUI = PlayerResourcesUI.active;
                        }

                        if (playerResourcesUI != null)
                        {
                            if (nationResources[i][j].consumersRtsIds.Count > 0)
                            {
                                int consumption = GetExpectedConsumption(i);

                                if (consumption > nationResources[i][j].amount)
                                {
                                    playerResourcesUI.resourceSlotInstances[j].GetComponent<ResourceSlotUI>().text.color = negativeTextColor;
                                }
                                else
                                {
                                    playerResourcesUI.resourceSlotInstances[j].GetComponent<ResourceSlotUI>().text.color = staticTextColor;
                                }
                            }
                            else
                            {
                                playerResourcesUI.resourceSlotInstances[j].GetComponent<ResourceSlotUI>().text.color = staticTextColor;
                            }
                        }
                    }
                }
            }
        }

        public int GetExpectedProductionDifference(int nat)
        {
            int diff = 0;

            if (nat < nationResources.Count)
            {
                for (int j = 0; j < nationResources[nat].Count; j++)
                {
                    if (nationResources[nat][j].producers.Count > 0)
                    {
                        if (nationResources[nat][j].consumersRtsIds.Count > 0)
                        {
                            diff = diff + nationResources[nat][j].amount - GetExpectedConsumption(nat);
                        }
                    }
                }
            }

            return diff;
        }

        int GetExpectedProduction(int nat)
        {
            float pop = 0f;

            for (int i = 0; i < nationResources[nat].Count; i++)
            {
                for (int j = 0; j < nationResources[nat][i].producers.Count; j++)
                {
                    EconomyResource.Producer prod = nationResources[nat][i].producers[j];

                    int rtsId = prod.rtsId;
                    int n = rtsm.numberOfUnitTypes[nat][rtsId];
                    pop = pop + (prod.totalAmountPerUnit * n + prod.productionOffset);

                    for (int k = 0; k < rtsm.unitsListByType[rtsId].Count; k++)
                    {
                        if (rtsm.unitsListByType[rtsId][k].nation == nat)
                        {
                            if (3 < rtsm.unitsListByType[rtsId][k].levelValues.Length)
                            {
                                pop = pop + prod.additionPerUnitLevel * rtsm.unitsListByType[rtsId][k].levelValues[3];
                            }
                        }
                    }
                }
            }

            return (int)(pop);
        }

        int GetExpectedConsumption(int nat)
        {
            int pop = 0;

            for (int i = 0; i < nationResources[nat].Count; i++)
            {
                for (int j = 0; j < nationResources[nat][i].consumersRtsIds.Count; j++)
                {
                    int k = nationResources[nat][i].consumersRtsIds[j];
                    pop = pop + rtsm.numberOfUnitTypes[nat][k];
                }
            }

            return pop;
        }

        public void AddResource(int nationId, int resId, int amount)
        {
            if (resId > -1)
            {
                int finalAmount = amount;

                if (rtsm.nationPars[nationId].nationAI.masterNationId == -1)
                {
                    nationResources[nationId][resId].amount = nationResources[nationId][resId].amount + amount;
                    nationResources[nationId][resId].collected = nationResources[nationId][resId].collected + amount;
                }
                else
                {
                    int master = rtsm.nationPars[nationId].nationAI.masterNationId;

                    int half = (int)(0.5f * amount);
                    int half2 = amount - half;

                    nationResources[nationId][resId].amount = nationResources[nationId][resId].amount + half;
                    nationResources[nationId][resId].collected = nationResources[nationId][resId].collected + half;
                    nationResources[master][resId].amount = nationResources[master][resId].amount + half2;
                    finalAmount = half;

                }

                Scores.active.AddToMasterScoreDiff(0.005f * finalAmount, nationId);

                if (nationId == diplomacy.playerNation)
                {
                    if (playerResourcesUI == null)
                    {
                        playerResourcesUI = PlayerResourcesUI.active;
                    }
                    if (playerResourcesUI != null)
                    {
                        playerResourcesUI.RefreshResources();
                    }
                }
            }
        }

        public void HugeResources(int nationId)
        {
            nationResources[nationId][0].amount = 100000;
            nationResources[nationId][1].amount = 100000;
            nationResources[nationId][2].amount = 100000;
            nationResources[nationId][3].amount = 50000;
        }

        public void VeryLargeResources(int nationId)
        {
            nationResources[nationId][0].amount = 50000;
            nationResources[nationId][1].amount = 50000;
            nationResources[nationId][2].amount = 50000;
            nationResources[nationId][3].amount = 5000;
        }

        public void LargeResources(int nationId)
        {
            nationResources[nationId][0].amount = 30000;
            nationResources[nationId][1].amount = 30000;
            nationResources[nationId][2].amount = 30000;
            nationResources[nationId][3].amount = 1000;

            if (nationId == diplomacy.playerNation)
            {
                RefreshResources();
            }
        }

        public void MediumResources(int nationId)
        {
            nationResources[nationId][0].amount = 10000;
            nationResources[nationId][1].amount = 10000;
            nationResources[nationId][2].amount = 10000;
            nationResources[nationId][3].amount = 100;

            if (nationId == diplomacy.playerNation)
            {
                RefreshResources();
            }
        }

        public void RefreshResources()
        {
            if (playerResourcesUI == null)
            {
                playerResourcesUI = PlayerResourcesUI.active;
            }
            if (playerResourcesUI != null)
            {
                playerResourcesUI.RefreshResources();
            }
        }
    }

    [System.Serializable]
    public class EconomyResource
    {
        public string name;
        public Sprite icon;
        public int amount = 0;
        [HideInInspector] public int collected = 0;

        public int collectionRtsUnitId = -1;
        public int deliveryRtsUnitId = -1;

        public float collectionTime;
        public int collectionAmount;
        public AudioClip collectionSound;

        public string loadedIdleAnimation;
        public string loadedWalkAnimation;
        public string loadedDeathAnimation;
        public string collectionAnimation;

        public bool isTerrainTree = false;

        public List<Producer> producers = new List<Producer>();
        public List<int> consumersRtsIds = new List<int>();

        public float taxesAndWagesFactor = 0f;

        [System.Serializable]
        public class Producer
        {
            public int rtsId;
            public int totalAmountPerUnit;
            public float additionPerUnitLevel;
            public float productionOffset;
        }
    }

    [System.Serializable]
    public class EconomyResourceUnitPars
    {
        public string name;
        public int amount = 0;

        public EconomyResource GetCorrespondingResource()
        {
            Economy ec = Economy.GetActive();

            if (ec != null)
            {
                for (int i = 0; i < ec.resources.Count; i++)
                {
                    if (name == ec.resources[i].name)
                    {
                        return ec.resources[i];
                    }
                }
            }

            return null;
        }
    }
}
