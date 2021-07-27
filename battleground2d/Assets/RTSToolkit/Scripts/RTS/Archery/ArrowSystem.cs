using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class ArrowSystem : MonoBehaviour
    {
        public static ArrowSystem active;
        [HideInInspector] public List<ArrowPars> arrowPars = new List<ArrowPars>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void Update()
        {
            UpdateSearcheableTargets();

            for (int i = 0; i < arrowPars.Count; i++)
            {
                arrowPars[i].Updater();
            }
        }

        int iUpdateSearcheableTargets = 0;
        int nUpdateSearcheableTargets = 4;

        void UpdateSearcheableTargets()
        {
            iUpdateSearcheableTargets++;

            if (iUpdateSearcheableTargets >= nUpdateSearcheableTargets)
            {
                iUpdateSearcheableTargets = 0;
            }

            for (int i = 0; i < arrowPars.Count; i++)
            {
                if ((i + iUpdateSearcheableTargets) % nUpdateSearcheableTargets == 0)
                {
                    arrowPars[i].UpdateSearcheableTargets();
                }
            }
        }

        public void AddArrowPars(ArrowPars ap)
        {
            arrowPars.Add(ap);
        }

        public void RemoveArrowPars(ArrowPars ap)
        {
            arrowPars.Remove(ap);
        }
    }
}
