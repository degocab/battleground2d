using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class UnitSelectionNode : MonoBehaviour
    {
        [HideInInspector] public UnitPars unit;

        public GameObject selectionMarkGo;
        [HideInInspector] public RectTransform selectionRect;
        [HideInInspector] public Image selectionMarkImage;


        public GameObject healthBarGo;
        [HideInInspector] public RectTransform healthBarRect;
        [HideInInspector] public Slider healthBarSlider;

        [HideInInspector] public int markType;

        void Start()
        {

        }

        public void GetCompon()
        {
            selectionRect = selectionMarkGo.GetComponent<RectTransform>();
            selectionMarkImage = selectionMarkGo.GetComponent<Image>();

            healthBarRect = healthBarGo.GetComponent<RectTransform>();
            healthBarSlider = healthBarGo.GetComponent<Slider>();
        }

        public void UpdateSelectionMarkPosition()
        {
            Rect rect = SelectionMarkBounds(unit);
            selectionRect.position = new Vector2(rect.xMin + 0.5f * rect.width, rect.yMin + 0.5f * rect.height);
            selectionRect.sizeDelta = new Vector2(rect.width, rect.height);
        }

        public void UpdateHealthBarPosition()
        {
            Rect rect = HealthBarBounds(unit);
            healthBarRect.position = new Vector2(rect.xMin + 0.5f * rect.width, rect.yMin + 0.5f * rect.height);
            healthBarRect.sizeDelta = new Vector2(rect.width, rect.height);
        }

        static Rect SelectionMarkBounds(UnitPars up)
        {
            Vector3 screenPos = UnitSelectionMark.cam.WorldToScreenPoint(up.transform.position + up.unitParsType.unitCenter);
            float scale = 0;

            if (screenPos.z < 52 * up.unitParsType.unitSize)
            {
                scale = 1000f * up.unitParsType.unitSize / screenPos.z;
            }
            else
            {
                scale = 1000f * up.unitParsType.unitSize / (52f * up.unitParsType.unitSize);
            }

            return Rect.MinMaxRect(screenPos.x - 0.5f * scale, screenPos.y - 0.5f * scale, screenPos.x + 0.5f * scale, screenPos.y + 0.5f * scale);
        }

        static Rect HealthBarBounds(UnitPars up)
        {
            Vector3 screenPos = UnitSelectionMark.cam.WorldToScreenPoint(up.transform.position + up.unitParsType.unitCenter);
            float scale = 0;

            if (screenPos.z < 52 * up.unitParsType.unitSize)
            {
                scale = 1000f * up.unitParsType.unitSize / screenPos.z;
            }
            else
            {
                scale = 1000f * up.unitParsType.unitSize / (52f * up.unitParsType.unitSize);
            }

            return Rect.MinMaxRect(screenPos.x - 0.5f * scale, screenPos.y + 0.55f * scale, screenPos.x + 0.5f * scale, screenPos.y + 0.62f * scale);
        }
    }
}
