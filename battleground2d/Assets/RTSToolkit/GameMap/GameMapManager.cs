using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// This script allows to manually export images of the entire map of the game 
namespace RTSToolkit
{
    public class GameMapManager : MonoBehaviour
    {
        public Vector2 upperPoint;
        public RenderTexture renderTexture;

        public int numberOfTilesInX = 1;
        public int numberOfTilesInY = 1;

        public int tileResolution = 512;

        void Start()
        {
            StartCoroutine(Starter());
        }

        IEnumerator Starter()
        {
            Camera cam = GetComponent<Camera>();
            GameObject go = new GameObject();
            RenderTextureController rtc = go.AddComponent<RenderTextureController>();
            rtc.filePath = "RTSToolkit/GameMap/Resources/Map/";
            rtc.width = tileResolution;
            rtc.height = tileResolution;

            yield return new WaitForEndOfFrame();

            Texture2D mainTexture = new Texture2D(tileResolution * numberOfTilesInX, tileResolution * numberOfTilesInY, TextureFormat.RGB24, false);
            Color[,,] pixels = new Color[numberOfTilesInY, numberOfTilesInX, tileResolution * tileResolution];

            List<int2> missingTileIndices = new List<int2>();

            for (int i = 0; i < numberOfTilesInY; i++)
            {
                for (int j = 0; j < numberOfTilesInX; j++)
                {
                    rtc.filename = "Map_" + i + "_" + j;

                    if (!rtc.FileExist())
                    {
                        missingTileIndices.Add(new int2(i, j));
                    }
                }
            }

            for (int ij = 0; ij < missingTileIndices.Count; ij++)
            {
                Texture2D tileTexture = new Texture2D(tileResolution, tileResolution, TextureFormat.RGB24, false);

                int i = missingTileIndices[ij].x;
                int j = missingTileIndices[ij].y;

                rtc.filename = "Map_" + i + "_" + j;
                cam.transform.position = new Vector3(upperPoint.x, 0, upperPoint.y) + new Vector3(cam.orthographicSize * i * 2, 700, -cam.orthographicSize * j * 2);
                cam.transform.rotation = Quaternion.Euler(90, 90, 0);
                RTSCamera.active.transform.position = cam.transform.position;
                RTSCamera.active.transform.rotation = cam.transform.rotation;
                yield return new WaitForSeconds(1f);

                while (LoadingPleaseWait.active.uiElement.activeSelf)
                {
                    yield return new WaitForSeconds(1f);
                }

                yield return new WaitForSeconds(5f);
                rtc.TakeImage(cam);

                for (int k = 0; k < rtc.pixels.Length; k++)
                {
                    pixels[i, j, k] = rtc.pixels[k];
                }

                for (int i1 = 0; i1 < tileResolution; i1++)
                {
                    for (int j1 = 0; j1 < tileResolution; j1++)
                    {
                        int k = tileResolution * i1 + j1;
                        mainTexture.SetPixel(j * tileResolution + j1, i * tileResolution + i1, pixels[i, j, k]);
                        tileTexture.SetPixel(j1, i1, pixels[i, j, k]);
                    }
                }

                float progress = ((float)(i * numberOfTilesInX + j)) / ((float)(numberOfTilesInX * numberOfTilesInY)) * 100f;
                Debug.Log("Map " + (i + 1).ToString() + "/" + (j + 1).ToString() + " " + progress.ToString("#.0") + " %");

                tileTexture.Apply();
                rtc.WriteToFile(tileTexture);
            }

            rtc.filename = "Map_main";
            mainTexture.Apply();
            rtc.WriteToFile(mainTexture);

            Destroy(go);
            yield return new WaitForEndOfFrame();
        }
    }
}
