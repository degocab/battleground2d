using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class NationsScoresListUI : MonoBehaviour
    {
        public static NationsScoresListUI active;

        public GameObject nationScoresListGo;

        public GameObject gridGo;
        public GameObject listNodePrefab;

        List<GameObject> instances = new List<GameObject>();
        List<NationsScoresListNodeUI> instancesScripts = new List<NationsScoresListNodeUI>();

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
            UpdateList();
        }

        float tUpdateList = 0f;
        void UpdateList()
        {
            tUpdateList = tUpdateList + deltaTime;

            if (tUpdateList > 0.5f)
            {
                tUpdateList = 0f;

                if (nationScoresListGo.activeSelf)
                {
                    RTSMaster rtsm = RTSMaster.active;

                    if (rtsm != null)
                    {
                        if (instances.Count < Diplomacy.active.numberNations)
                        {
                            for (int i = 0; i < Diplomacy.active.numberNations; i++)
                            {
                                if (instances.Count < Diplomacy.active.numberNations)
                                {
                                    GameObject go = Instantiate(listNodePrefab);
                                    go.transform.SetParent(gridGo.transform);
                                    go.SetActive(true);
                                    instances.Add(go);
                                    NationsScoresListNodeUI nationsScoresListNodeUI = go.GetComponent<NationsScoresListNodeUI>();
                                    nationsScoresListNodeUI.nation = instancesScripts.Count;

                                    instancesScripts.Add(nationsScoresListNodeUI);
                                }
                            }
                        }
                        else if (instances.Count > Diplomacy.active.numberNations)
                        {
                            Destroy(instances[0]);
                            instances.RemoveAt(0);
                            instancesScripts.RemoveAt(0);
                        }

                        SortInstances();
                        SetTexts();
                    }
                }
            }
        }

        void SortInstances()
        {
            List<float> scrs = new List<float>();

            for (int i = 0; i < instancesScripts.Count; i++)
            {
                int nat = instancesScripts[i].nation;

                if (nat < Scores.active.masterScores.Count)
                {
                    scrs.Add(Scores.active.masterScores[nat]);
                }
            }

            List<float> sorted_scrs = new List<float>();

            for (int i = 0; i < scrs.Count; i++)
            {
                sorted_scrs.Add(scrs[i]);
            }

            sorted_scrs.Sort();
            bool isTheSame = true;

            for (int i = 0; i < sorted_scrs.Count; i++)
            {
                if (sorted_scrs[i] != scrs[i])
                {
                    isTheSame = false;
                }
            }

            if (isTheSame == false)
            {
                int[] indices = HeapSortFloat(scrs.ToArray());

                Transform gridParent = gridGo.transform.parent;

                for (int i = 0; i < instances.Count; i++)
                {
                    instances[i].transform.SetParent(gridParent);
                }

                for (int i = instances.Count - 1; i >= 0; i--)
                {
                    int j = indices[i];
                    instances[j].transform.SetParent(gridGo.transform);
                }
            }
        }

        void SetTexts()
        {
            RTSMaster rtsm = RTSMaster.active;

            for (int i = 0; i < instancesScripts.Count; i++)
            {
                int nat = instancesScripts[i].nation;

                if (nat < rtsm.nationPars.Count)
                {
                    int iscore = 0;

                    if (nat < Scores.active.masterScores.Count)
                    {
                        iscore = (int)(Scores.active.masterScores[nat]);
                    }

                    string youStatement = "";

                    if (nat == Diplomacy.active.playerNation)
                    {
                        youStatement = " - You";
                    }

                    instancesScripts[i].text.text = rtsm.nationPars[nat].GetNationName() + youStatement + " (" + iscore.ToString() + ")";
                }
            }
        }

        public void Activate()
        {
            if (nationScoresListGo != null)
            {
                nationScoresListGo.SetActive(true);
            }
        }

        public static int[] HeapSortFloat(float[] input)
        {
            //Build-Max-Heap
            int heapSize = input.Length;

            int[] iorig = new int[heapSize];

            for (int i = 0; i < heapSize; i++)
            {
                iorig[i] = i;
            }

            for (int p = (heapSize - 1) / 2; p >= 0; p--)
            {
                MaxHeapifyFloat(input, iorig, heapSize, p);
            }

            for (int i = input.Length - 1; i > 0; i--)
            {
                //Swap
                float temp = input[i];
                input[i] = input[0];
                input[0] = temp;

                int itemp = iorig[i];
                iorig[i] = iorig[0];
                iorig[0] = itemp;

                heapSize--;
                MaxHeapifyFloat(input, iorig, heapSize, 0);
            }

            return iorig;
        }

        private static void MaxHeapifyFloat(float[] input, int[] iorig, int heapSize, int index)
        {
            int left = (index + 1) * 2 - 1;
            int right = (index + 1) * 2;
            int largest = 0;

            if (left < heapSize && input[left] > input[index])
            {
                largest = left;
            }
            else
            {
                largest = index;
            }

            if (right < heapSize && input[right] > input[largest])
            {
                largest = right;
            }

            if (largest != index)
            {
                float temp = input[index];
                input[index] = input[largest];
                input[largest] = temp;

                int itemp = iorig[index];
                iorig[index] = iorig[largest];
                iorig[largest] = itemp;

                MaxHeapifyFloat(input, iorig, heapSize, largest);
            }
        }
    }
}
