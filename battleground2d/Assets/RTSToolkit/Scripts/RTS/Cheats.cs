using UnityEngine;

namespace RTSToolkit
{
    public class Cheats : MonoBehaviour
    {
        public static Cheats active;

        public bool useCheats = false;
        public int godMode = 0;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void Update()
        {
            if (useCheats == true)
            {
                if (Input.GetKey("g"))
                {
                    if (godMode == 0)
                    {
                        godMode = 1;
                    }
                    else
                    {
                        godMode = 0;
                    }
                }
            }
        }
    }
}
