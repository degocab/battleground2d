using UnityEngine;

namespace RTSToolkit
{
    public class FPSCount : MonoBehaviour
    {
        public static FPSCount active;

        [HideInInspector] public float fps = 0.0f;
        public float refreshTime = 1.0f;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        float totalDeltaTime = 0f;
        int nDeltaTime = 0;

        void Update()
        {
            totalDeltaTime = totalDeltaTime + Time.deltaTime;
            nDeltaTime = nDeltaTime + 1;

            if (totalDeltaTime > refreshTime)
            {
                fps = 1f / (totalDeltaTime / nDeltaTime);
                totalDeltaTime = 0f;
                nDeltaTime = 0;
            }
        }
    }
}
