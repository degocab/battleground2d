using UnityEngine;

namespace RTSToolkit
{
    public class BuildingLight : MonoBehaviour
    {
        TimeOfDay timeOfDay;
        Light light1;
        bool isApplicationQuiting = false;

        void Start()
        {
            timeOfDay = TimeOfDay.active;
            light1 = GetComponent<Light>();

            if (timeOfDay != null)
            {
                timeOfDay.AddNightPointLight(light1);
            }
        }

        void OnDestroy()
        {
            if (isApplicationQuiting == false)
            {
                if (this.enabled)
                {
                    TimeOfDay.active.RemoveNightPointLight(light1);
                }
            }
        }

        void OnApplicationQuit()
        {
            isApplicationQuiting = true;
        }
    }
}
