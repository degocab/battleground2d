using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class UnitAnimationTypesHolder : MonoBehaviour
    {
        public static UnitAnimationTypesHolder active;

        public List<GameObject> unitAnimationTypePrefabs = new List<GameObject>();
        public List<GameObject> unitAnimationTypePrefabsNetwork = new List<GameObject>();

        public List<UnitAnimationType> unitAnimationTypes = new List<UnitAnimationType>();
        public List<UnitAnimationType> unitAnimationTypesNetwork = new List<UnitAnimationType>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            for (int i = 0; i < unitAnimationTypePrefabs.Count; i++)
            {
                UnitAnimationType uat = unitAnimationTypePrefabs[i].GetComponent<UnitAnimationType>();

                if (uat != null)
                {
                    unitAnimationTypes.Add(uat);
                }
            }

            for (int i = 0; i < unitAnimationTypePrefabsNetwork.Count; i++)
            {
                UnitAnimationType uat = unitAnimationTypePrefabsNetwork[i].GetComponent<UnitAnimationType>();

                if (uat != null)
                {
                    unitAnimationTypesNetwork.Add(uat);
                }
            }
        }
    }
}
