using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class FontSwitcher : MonoBehaviour
    {
        public Font font;

        void Start()
        {

        }

        public void GetAllSceneGameObjects()
        {
            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            int i1 = 0;
            int i2 = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                GameObject go = texts[i].gameObject;
                if (go.activeSelf == false)
                {
                    i1++;
                }

                bool isOnScene = true;
                if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                {
                    isOnScene = false;
                }

#if UNITY_EDITOR
                if (!UnityEditor.EditorUtility.IsPersistent(go.transform.root.gameObject))
                {
                    isOnScene = false;
                }
#endif

                if (isOnScene)
                {
                    i2++;
                }
                if (font != null)
                {
                    texts[i].font = font;
                }
            }
        }
    }
}
