using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class FPSCountUI : MonoBehaviour
    {
        public static FPSCountUI active;
        public Text text;

        void Start()
        {
            active = this;
        }

        void Update()
        {
            if (FPSCount.active != null)
            {
                text.text = FPSCount.active.fps.ToString("#.0");
            }
        }
    }
}
