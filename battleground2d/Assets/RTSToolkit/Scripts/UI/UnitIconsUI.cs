using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class UnitIconsUI : MonoBehaviour
    {
        public static UnitIconsUI active;
        public List<Sprite> unitIcons = new List<Sprite>();
        public Sprite troopsIcon;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }
    }
}
