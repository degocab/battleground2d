using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class GameMapDisplayUI : MonoBehaviour
    {
        public GridLayoutGroup grid;
        public GameObject gridElement;

        public int nX = 2;
        public int nY = 2;

        List<GameObject> gridElementInstances = new List<GameObject>();

        Vector2 initialMin;
        Vector2 initialMax;

        public string folderInsideResources = "Map/";

        public int mapTileSize = 256;

        void Start()
        {
            grid.cellSize = new Vector2(mapTileSize, mapTileSize);

            grid.gameObject.GetComponent<RectTransform>().offsetMin = new Vector2(0, -mapTileSize * nX / 2);
            grid.gameObject.GetComponent<RectTransform>().offsetMax = new Vector2(mapTileSize * nY - nY, mapTileSize * nX / 2 - nX);

            grid.constraintCount = nY;
            initialMin = grid.gameObject.GetComponent<RectTransform>().offsetMin;
            initialMax = grid.gameObject.GetComponent<RectTransform>().offsetMax;

            for (int i = nX - 1; i >= 0; i--)
            {
                for (int j = 0; j < nY; j++)
                {
                    Texture2D tex = Resources.Load(folderInsideResources + "Map_" + i + "_" + j) as Texture2D;
                    GameObject go = Instantiate(gridElement, grid.gameObject.transform);
                    go.SetActive(true);
                    RawImage im = go.GetComponent<RawImage>();
                    im.texture = tex;
                    gridElementInstances.Add(go);
                }
            }
        }

        void Update()
        {

        }

        public void ResetRectTransform()
        {
            grid.gameObject.GetComponent<RectTransform>().offsetMin = initialMin;
            grid.gameObject.GetComponent<RectTransform>().offsetMax = initialMax;
        }
    }
}
