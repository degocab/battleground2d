using UnityEngine;
using System.Collections;

namespace RTSToolkit
{
    public class RandomPositionShots : MonoBehaviour
    {
        public int n = 10;
        public float vshift = 3f;
        public float vangle = 0f;

        public int width = 512;
        public int height = 512;

        public float dt = 0.01f;

        public float radius = 400f;
        public bool takeMainCamera = false;

        void Start()
        {
            StartCoroutine(Starter());
        }

        IEnumerator Starter()
        {
            GameObject go = new GameObject();
            RenderTextureController rtc = go.AddComponent<RenderTextureController>();
            rtc.filePath = "CinematicCamera/shots/";
            rtc.width = width;
            rtc.height = height;

            Vector3 origin = new Vector3(500f, 0f, 500f);
            Vector3 voffset = new Vector3(0f, vshift, 0f);

            yield return new WaitForEndOfFrame();

            for (int i = 0; i < n; i++)
            {
                rtc.filename = i.ToString();
                rtc.position = TerrainProperties.RandomTerrainVectorCircleProc(origin, radius) + voffset;
                rtc.rotation = Quaternion.Euler(vangle, Random.Range(0f, 360f), 0f);

                if (takeMainCamera)
                {
                    RTSCamera.active.transform.position = rtc.position;
                    RTSCamera.active.transform.rotation = rtc.rotation;
                }

                yield return new WaitForSeconds(dt);
                rtc.TakeImage();
                Debug.Log("Taking shot " + (i + 1).ToString() + "/" + n);
            }

            Destroy(go);
            yield return new WaitForEndOfFrame();
        }

        void Update()
        {

        }
    }
}
