using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSToolkit
{
    public class ActiveScreenPEC : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static ActiveScreenPEC active;
        [HideInInspector] public bool isActive = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void OnPointerEnter(PointerEventData eventData)
        {

        }

        public void OnPointerExit(PointerEventData eventData)
        {

        }

        public void PointerEnter()
        {
            isActive = true;
            SelectionManager.active.ActiveScreenTrue();
        }

        public void PointerExit()
        {
            isActive = false;
            SelectionManager.active.ActiveScreenFalse();
        }
    }
}
