using UnityEngine;

namespace RTSToolkit
{
    public class RenderTextureController : MonoBehaviour
    {
        private Camera cam;
        public int width = 512;
        public int height = 512;

        public Vector3 position = Vector3.zero;
        [HideInInspector] public Quaternion rotation = Quaternion.identity;

        public string filePath = "";
        public string filename = "p";

        [HideInInspector] public bool writeToFile = true;
        [HideInInspector] public Color[] pixels;

        void Start()
        {

        }

        void Update()
        {

        }

        public void TakeImage()
        {
            CreateCamera();
            MakeSquarePngFromOurVirtualThingy();
            RemoveCamera();
        }

        public void TakeImage(Camera cam1)
        {
            cam = cam1;
            MakeSquarePngFromOurVirtualThingy();
            cam = null;
        }

        public void CreateCamera()
        {
            GameObject cameraGo = new GameObject("Cam1");
            cameraGo.transform.position = position;
            cameraGo.transform.rotation = rotation;
            cam = cameraGo.AddComponent<Camera>();
        }

        public void RemoveCamera()
        {
            Destroy(cam.gameObject);
        }

        public void MakeSquarePngFromOurVirtualThingy()
        {
            // capture the virtuCam and save it as a square PNG.

            int w = width;
            int h = height;

            cam.aspect = 1.0f * w / h;
            // recall that the height is now the "actual" size from now on
            // the .aspect property is very tricky in Unity, and bizarrely is NOT shown in the editor
            // the editor will still incorrectly show the frustrum being screen-shaped

            RenderTexture tempRT = new RenderTexture(w, h, 24);
            // the "24" can be 0,16,24 or formats like RenderTextureFormat.Default, ARGB32 etc.

            cam.targetTexture = tempRT;
            cam.Render();

            RenderTexture.active = tempRT;
            Texture2D virtualPhoto = new Texture2D(w, h, TextureFormat.RGB24, false);
            // false, meaning no need for mipmaps

            virtualPhoto.ReadPixels(new Rect(0, 0, w, h), 0, 0); // you get the center section
            pixels = virtualPhoto.GetPixels(0, 0, w, h);

            RenderTexture.active = null; // "just in case" 
            cam.targetTexture = null;

            Destroy(tempRT);
            //////Destroy(tempRT); - tricky on android and other platforms, take care

            if (writeToFile)
            {
                WriteToFile(virtualPhoto);
            }
            // virtualCam.SetActive(false); ... not necesssary but take care

            // now use the image somehow...
            //      YourOngoingRoutine( OurTempSquareImageLocation() );
        }

        public void WriteToFile(Texture2D tex2d)
        {
#if !UNITY_WEBPLAYER
            byte[] bytes;
            bytes = tex2d.EncodeToPNG();


            System.IO.File.WriteAllBytes(GetFilePath(), bytes);
#endif

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        string GetFilePath()
        {
            string r = Application.dataPath + "/" + filePath + filename + ".png";
            return r;
        }

        public bool FileExist()
        {
            return System.IO.File.Exists(Application.dataPath + "/" + filePath + filename + ".png");
        }
    }
}
