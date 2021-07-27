using UnityEngine;

namespace RTSToolkit
{
    public class TextureSwitcher : MonoBehaviour
    {
        public Texture2D oldTexture;
        public Texture2D newTexture;

        void Start()
        {

        }

        public void SwitchMaterialTextures()
        {
            if ((oldTexture != null) && (newTexture != null))
            {
                GameObject[] gos = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
                int i1 = 0;

                for (int i = 0; i < gos.Length; i++)
                {
                    MeshRenderer mr = gos[i].GetComponent<MeshRenderer>();

                    if (mr != null)
                    {
                        Material[] mats = mr.sharedMaterials;

                        if (mats != null)
                        {
                            for (int j = 0; j < mats.Length; j++)
                            {
                                if (mats[j] != null)
                                {
                                    if (mats[j].HasProperty("_MainTex"))
                                    {
                                        if (mats[j].mainTexture == oldTexture)
                                        {
                                            mats[j].mainTexture = newTexture;
                                            i1++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Debug.Log(i1 + " " + gos.Length);
            }
        }

        public void SwitchBetweenOldAndNew()
        {
            Texture2D oldTex = oldTexture;
            Texture2D newTex = newTexture;

            oldTexture = newTex;
            newTexture = oldTex;
        }
    }
}
