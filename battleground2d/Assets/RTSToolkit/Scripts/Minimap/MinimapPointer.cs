using UnityEngine;

namespace RTSToolkit
{
    public class MinimapPointer : MonoBehaviour
    {
        public static MinimapPointer active;
        [HideInInspector] public bool isPointerOnMinimap = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void OnPointerEnter()
        {
            isPointerOnMinimap = true;
        }

        public void OnPointerExit()
        {
            isPointerOnMinimap = false;
        }
    }
}
