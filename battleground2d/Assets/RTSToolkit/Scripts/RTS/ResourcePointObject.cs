using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    [System.Serializable]
    public class ResourcePointObject
    {
        public GameObject go;
        public Vector3 position;

        public int resourceType = 0;
        public int resourceAmount;
        public int maxResourceAmount;

        public Terrain terrain;
        public int indexOnTerrain = 0;
        public Vector2i tile;

        public int treeType;
        public List<TreeInstance> treeInstances = new List<TreeInstance>();
        public float minNeighbourDist = 0f;

        public int collectionRtsUnitId = -1;
        public bool isAlive = true;

        public FireScaler primaryFire;
        public List<FireScaler> fires = new List<FireScaler>();

        public List<UnitPars> linkedUnits = new List<UnitPars>();

        void Start()
        {

        }

        public ForestPlacer FindForestPlacer()
        {
            for (int i = 0; i < GenerateTerrain.active.forestPlacers.Count; i++)
            {
                if (terrain == GenerateTerrain.active.forestPlacers[i].terrain)
                {
                    return GenerateTerrain.active.forestPlacers[i];
                }
            }

            return null;
        }

        public static ResourcePointObject FindNearestTerrainTreeProc(Vector3 position)
        {
            ResourcePointObject rpo = null;
            ResourcePointObject rpo1 = null;
            float rmin = float.MaxValue;
            float rcurent = 0f;

            for (int i = 0; i < GenerateTerrain.active.forestPlacers.Count; i++)
            {
                ForestPlacer fp = GenerateTerrain.active.forestPlacers[i];

                if (fp != null)
                {
                    rpo1 = fp.FindNearestTerrainTree(position);

                    if (rpo1 != null)
                    {
                        rcurent = (rpo1.position - position).magnitude;

                        if (rcurent < rmin)
                        {
                            rmin = rcurent;
                            rpo = rpo1;
                        }
                    }
                }
            }

            return rpo;
        }

        public static ResourcePointObject[] FindNearestsKTerrainTreeProc(Vector3 position, int k)
        {
            ResourcePointObject[] rpo = new ResourcePointObject[0];
            List<ResourcePointObject> rpoMaster = new List<ResourcePointObject>();

            for (int i = 0; i < GenerateTerrain.active.forestPlacers.Count; i++)
            {
                ForestPlacer fp = GenerateTerrain.active.forestPlacers[i];

                if (fp != null)
                {
                    ResourcePointObject[] rpo1 = fp.FindNearestsKTerrainTree(position, k);

                    if (rpo1 != null)
                    {
                        for (int j = 0; j < rpo1.Length; j++)
                        {
                            if (rpo1[j] != null)
                            {
                                rpoMaster.Add(rpo1[j]);
                            }
                        }
                    }
                }
            }

            if (rpoMaster.Count > 0)
            {
                int[] masks = new int[rpoMaster.Count];

                for (int i = 0; i < masks.Length; i++)
                {
                    masks[i] = 0;
                }

                List<ResourcePointObject> rpoBests = new List<ResourcePointObject>();

                for (int ik = 0; ik < k; ik++)
                {
                    float rmin = float.MaxValue;
                    int ibest = -1;

                    for (int i = 0; i < rpoMaster.Count; i++)
                    {
                        float rcurent = (rpoMaster[i].position - position).magnitude;

                        if (rcurent < rmin)
                        {
                            if (masks[i] == 0)
                            {
                                rmin = rcurent;
                                ibest = i;
                            }
                        }
                    }

                    if (ibest > -1)
                    {
                        masks[ibest] = 1;
                        rpoBests.Add(rpoMaster[ibest]);
                    }
                }

                if (rpoBests.Count > 0)
                {
                    rpo = rpoBests.ToArray();
                }
            }

            return rpo;
        }

        public Vector3 GetPosition(UnitPars linkedUnit)
        {
            if (linkedUnit == null)
            {
                return position;
            }
            else
            {
                for (int i = 0; i < linkedUnits.Count; i++)
                {
                    if (linkedUnit == linkedUnits[i])
                    {
                        return linkedUnit.collectionOrDeliveryPoint.position;
                    }
                }
            }

            return position;
        }

        public float GetEffectiveDistance(UnitPars linkedUnit)
        {
            if (linkedUnit == null)
            {
                return 16f;
            }
            else
            {
                for (int i = 0; i < linkedUnits.Count; i++)
                {
                    if (linkedUnit == linkedUnits[i])
                    {
                        return (2f * linkedUnit.rEnclosed * linkedUnit.rEnclosed);
                    }
                }
            }

            return 16f;
        }
    }

    public class ResourcePointObjectData
    {
        public int resourceType;
        public int resourceAmount;
        public int indexOnTerrain = 0;
        public Vector2i tile;

        public ResourcePointObjectData(ResourcePointObject rpo)
        {
            resourceType = rpo.resourceType;
            resourceAmount = rpo.resourceAmount;
            indexOnTerrain = rpo.indexOnTerrain;
            tile = rpo.tile;
        }
    }
}
