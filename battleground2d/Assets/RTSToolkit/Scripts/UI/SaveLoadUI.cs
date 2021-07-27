using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace RTSToolkit
{
    public class SaveLoadUI : MonoBehaviour
    {
        public static SaveLoadUI active;

        public GameObject saveLoadPrefab;
        public GameObject grid;

        public int numberOfSaves = 8;
        List<GameObject> instances = new List<GameObject>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            Clean();

            for (int i = 0; i < numberOfSaves; i++)
            {
                GameObject go = Instantiate(saveLoadPrefab);
                go.transform.SetParent(grid.transform);
                go.SetActive(true);
                SaveLoadCellUI slcui = go.GetComponent<SaveLoadCellUI>();
                slcui.rowNumberText.text = (i + 1).ToString();

                if (!File.Exists(Application.persistentDataPath + "/" + (i + 1).ToString() + ".sav"))
                {
                    slcui.loadButtonGameObject.SetActive(false);
                    slcui.deleteButtonGameObject.SetActive(false);
                }

                instances.Add(go);
            }
        }

        void Clean()
        {
            for (int i = 0; i < instances.Count; i++)
            {
                Destroy(instances[i]);
            }

            instances.Clear();
        }
    }
}
