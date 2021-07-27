using UnityEngine;

namespace RTSToolkit
{
    public class MinimapUI : MonoBehaviour
    {
        void Start()
        {

        }

        void Update()
        {

        }

        public void FlipActivity()
        {
            this.gameObject.SetActive(!this.gameObject.activeSelf);
        }
    }
}
