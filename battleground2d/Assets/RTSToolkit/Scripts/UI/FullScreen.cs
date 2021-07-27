using UnityEngine;

namespace RTSToolkit
{
    public class FullScreen : MonoBehaviour
    {
        void Start()
        {

        }

        public void SwitchFullScreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
    }
}
