using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class PlayerResourcesUI : MonoBehaviour
    {
        public static PlayerResourcesUI active;

        public GameObject grid;
        public GameObject resourceSlotPrefab;

        [HideInInspector] public List<GameObject> resourceSlotInstances = new List<GameObject>();
        [HideInInspector] public List<Text> resourceSlotInstancesText = new List<Text>();
        List<int> prevResources = new List<int>();

        Color defaultColor;
        public Color resourceAddColor = Color.green;
        public Color resourceSubtractColor = Color.red;

        public float fractionToUpdatePerFrame = 0.05f;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void DisableAll()
        {
            if (refreshResourcesCorRunning)
            {
                StopCoroutine("RefreshResourcesCor");
                refreshResourcesCorRunning = false;
            }

            for (int i = 0; i < resourceSlotInstances.Count; i++)
            {
                Destroy(resourceSlotInstances[i]);
            }

            resourceSlotInstances.Clear();
            resourceSlotInstancesText.Clear();
            prevResources.Clear();
        }

        public void InitializeResources()
        {
            DisableAll();
            Economy eco = Economy.active;
            Diplomacy dip = Diplomacy.active;

            if (eco != null)
            {
                if (dip != null)
                {
                    for (int i = 0; i < eco.nationResources.Count; i++)
                    {
                        if (i == dip.playerNation)
                        {
                            for (int j = 0; j < eco.nationResources[i].Count; j++)
                            {
                                GameObject go = Instantiate(resourceSlotPrefab, grid.transform);
                                ResourceSlotUI rsui = go.GetComponent<ResourceSlotUI>();
                                rsui.image.sprite = eco.nationResources[i][j].icon;
                                rsui.text.text = eco.nationResources[i][j].amount.ToString();
                                resourceSlotInstances.Add(go);
                                resourceSlotInstancesText.Add(rsui.text);
                                prevResources.Add(0);
                                defaultColor = rsui.text.color;
                            }
                        }
                    }
                }
            }

            RefreshResources();
        }

        public void RefreshResources()
        {
            if (refreshResourcesCorRunning == false)
            {
                refreshResourcesCorRunning = true;
                StartCoroutine(RefreshResourcesCor());
            }
        }

        bool refreshResourcesCorRunning = false;
        IEnumerator RefreshResourcesCor()
        {
            refreshResourcesCorRunning = true;
            WaitForEndOfFrame weof = new WaitForEndOfFrame();
            Economy economy = Economy.GetActive();

            if (economy != null)
            {
                bool runFurther = true;

                while (runFurther)
                {
                    if (Diplomacy.active.playerNation < economy.nationResources.Count)
                    {
                        int nFinishedTypes = 0;

                        for (int i = 0; i < resourceSlotInstances.Count; i++)
                        {
                            if (resourceSlotInstances[i] != null)
                            {
                                if (i < economy.nationResources[Diplomacy.active.playerNation].Count)
                                {
                                    int curRes = economy.nationResources[Diplomacy.active.playerNation][i].amount;
                                    int prevRes = prevResources[i];

                                    if (curRes == prevRes)
                                    {
                                        resourceSlotInstancesText[i].color = defaultColor;
                                        prevResources[i] = curRes;
                                        nFinishedTypes++;
                                    }
                                    else if (curRes > prevRes)
                                    {
                                        int diff = (int)(fractionToUpdatePerFrame * (curRes - prevRes));

                                        if (diff < 1)
                                        {
                                            diff = 1;
                                        }

                                        prevRes = prevRes + diff;
                                        resourceSlotInstancesText[i].text = prevRes.ToString();
                                        resourceSlotInstancesText[i].color = resourceAddColor;
                                        prevResources[i] = prevRes;
                                    }
                                    else if (curRes < prevRes)
                                    {
                                        int diff = (int)(fractionToUpdatePerFrame * (prevRes - curRes));

                                        if (diff < 1)
                                        {
                                            diff = 1;
                                        }

                                        prevRes = prevRes - diff;
                                        resourceSlotInstancesText[i].text = prevRes.ToString();
                                        resourceSlotInstancesText[i].color = resourceSubtractColor;
                                        prevResources[i] = prevRes;
                                    }
                                }
                            }
                        }

                        if (nFinishedTypes == resourceSlotInstances.Count)
                        {
                            runFurther = false;
                        }
                    }

                    yield return weof;
                }
            }

            refreshResourcesCorRunning = false;
            yield return null;
        }
    }
}
