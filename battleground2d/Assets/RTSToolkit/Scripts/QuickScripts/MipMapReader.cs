using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RTSToolkit
{
    public class MipMapReader : MonoBehaviour
    {
        public Texture2D texture;
        public float newValue = -1f;

        void Start()
        {

        }

        public void Reader()
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter tImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            Debug.Log(tImporter.mipMapBias + " " + tImporter.mipmapFadeDistanceStart + " " + tImporter.mipmapFadeDistanceEnd);
#endif
        }

        public void Writer()
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter tImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (tImporter.textureType != TextureImporterType.Default)
            {
                tImporter.textureType = TextureImporterType.Default;
            }

            tImporter.mipMapBias = newValue;
            tImporter.SaveAndReimport();
            AssetDatabase.Refresh();

            tImporter.SaveAndReimport();
            AssetDatabase.Refresh();

            tImporter.SaveAndReimport();
            AssetDatabase.Refresh();
#endif
        }
    }
}
