using UnityEngine;

namespace RTSToolkit
{
    public class LoadingPleaseWait : MonoBehaviour
    {
        public static LoadingPleaseWait active;

        public GameObject uiElement;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public static void Activate(bool activate)
        {
            if (LoadingPleaseWait.active != null)
            {
                if (LoadingPleaseWait.active.uiElement != null)
                {
                    LoadingPleaseWait.active.uiElement.SetActive(activate);
                }
            }
        }
    }
}
