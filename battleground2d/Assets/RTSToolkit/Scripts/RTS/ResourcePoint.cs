using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class ResourcePoint : MonoBehaviour
    {
        public static ResourcePoint active;
        public int seed = 0;

        public List<ResourcePointType> resourcePointTypes = new List<ResourcePointType>();

        [HideInInspector] public Terrain terrain = null;

        [HideInInspector] public List<ResourcePointObject> resourcePoints = new List<ResourcePointObject>();
        public KDTree kd_allResLocations = null;

        [HideInInspector] public List<ResourcePointObjectData> savedResourcePoints = new List<ResourcePointObjectData>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            RefreshKDTrees();
        }

        public void Clean()
        {
            resourcePoints.Clear();

            for (int i = 0; i < resourcePointTypes.Count; i++)
            {
                resourcePointTypes[i].categorizedResourcePoints.Clear();
                resourcePointTypes[i].kd_catLocations = null;
            }
        }

        public static ResourcePoint GetActive()
        {
            if (ResourcePoint.active == null)
            {
                ResourcePoint.active = UnityEngine.Object.FindObjectOfType<ResourcePoint>();
            }

            return ResourcePoint.active;
        }

        public void PopulateResourcesOnTerrain(Terrain ter1)
        {
            terrain = ter1;
            Random.InitState(TerrainProperties.GetTerrainSeed(ter1) + seed);

            SpawnResources();
            RefreshKDTrees();
        }

        void SpawnResources()
        {
            Vector2i tile = GenerateTerrain.GetActive().GetTileFromTerrain(terrain);

            int nBefore = resourcePoints.Count;
            int n = 0;

            for (int j = 0; j < resourcePointTypes.Count; j++)
            {
                int nToSpawn = (int)(resourcePointTypes[j].densityOnTerrain * terrain.terrainData.size.x * terrain.terrainData.size.z);

                for (int k = 0; k < nToSpawn; k++)
                {
                    int resType = resourcePointTypes[j].resourceType;
                    GameObject res = resourcePointTypes[j].prefab;

                    float rand1 = Random.Range(0f, 1f);
                    float rand2 = Random.Range(0f, 1f);

                    int randResource = Random.Range(resourcePointTypes[j].minAmount, resourcePointTypes[j].maxAmount + 1);

                    Vector3 planeVect = new Vector3(rand1 * terrain.terrainData.size.x, 0f, rand2 * terrain.terrainData.size.x) + terrain.transform.position;
                    Vector3 mainPos = TerrainProperties.TerrainVector(planeVect, terrain);

                    GameObject goMain = new GameObject("resource" + n.ToString());
                    goMain.transform.SetParent(terrain.gameObject.transform);

                    ResourcePointObject rpo = new ResourcePointObject();
                    rpo.go = goMain;
                    rpo.position = mainPos;
                    rpo.resourceType = resType;
                    rpo.resourceAmount = randResource;
                    rpo.maxResourceAmount = randResource;
                    rpo.terrain = terrain;
                    rpo.indexOnTerrain = n;
                    rpo.tile = tile;
                    rpo.collectionRtsUnitId = Economy.GetActive().resources[resType].collectionRtsUnitId;
                    resourcePoints.Add(rpo);

                    resourcePointTypes[j].categorizedResourcePoints.Add(rpo);

                    int min_maxj = resourcePointTypes[j].numberPrefabsMin;
                    int max_maxj = resourcePointTypes[j].numberPrefabsMax;

                    float av_maxj = (1f * min_maxj + 1f * max_maxj) / 2f;

                    float averageRes = (1f * resourcePointTypes[j].minAmount + 1f * resourcePointTypes[j].maxAmount) / 2f;
                    float averageShift = 1f + (randResource - averageRes) / averageRes;
                    int maxj = (int)(av_maxj * averageShift);

                    if (maxj < min_maxj)
                    {
                        maxj = min_maxj;
                    }
                    else if (maxj > max_maxj)
                    {
                        maxj = max_maxj;
                    }

                    Material mat = null;

                    for (int j1 = 0; j1 < maxj; j1++)
                    {
                        Vector3 spawnPos = TerrainProperties.RandomTerrainVectorCircle(planeVect, resourcePointTypes[j].spreadRadius, terrain, 2);

                        float randAngle1 = Random.Range(0f, 360f);
                        float randAngle2 = Random.Range(0f, 360f);
                        float randAngle3 = Random.Range(0f, 360f);

                        Quaternion rot = Quaternion.Euler(randAngle1, randAngle2, randAngle3);

                        GameObject go = Instantiate(res, spawnPos, rot);
                        go.transform.SetParent(goMain.transform);

                        float scaleFactor = Random.Range(resourcePointTypes[j].prefabScaleMin, resourcePointTypes[j].prefabScaleMax);

                        go.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

                        if (Application.isPlaying)
                        {
                            mat = go.GetComponent<MeshRenderer>().material;
                        }
                        else
                        {
                            mat = go.GetComponent<MeshRenderer>().sharedMaterial;
                        }
                    }

                    MergeMeshesToParent(goMain, mat);
                    n++;
                }
            }

            List<ResourcePointObjectData> resourcesToRestore = new List<ResourcePointObjectData>();

            for (int i = 0; i < savedResourcePoints.Count; i++)
            {
                if (savedResourcePoints[i].tile.x == tile.x)
                {
                    if (savedResourcePoints[i].tile.z == tile.z)
                    {
                        int j = savedResourcePoints[i].indexOnTerrain + nBefore;

                        if (savedResourcePoints[i].resourceType == resourcePoints[j].resourceType)
                        {
                            resourcePoints[j].resourceAmount = savedResourcePoints[i].resourceAmount;
                            resourcesToRestore.Add(savedResourcePoints[i]);
                        }
                    }
                }
            }

            for (int i = 0; i < resourcesToRestore.Count; i++)
            {
                savedResourcePoints.Remove(resourcesToRestore[i]);
            }
        }

        void RefreshKDTrees()
        {
            for (int i = 0; i < resourcePointTypes.Count; i++)
            {
                RefreshCategoryKDTree(i);
            }

            RefreshFullKDTree();
        }

        void RefreshFullKDTree()
        {
            Vector3[] allPos = new Vector3[resourcePoints.Count];

            for (int i = 0; i < resourcePoints.Count; i++)
            {
                if (resourcePoints[i] != null)
                {
                    allPos[i] = resourcePoints[i].position;
                }
            }

            if (resourcePoints.Count > 0)
            {
                kd_allResLocations = KDTree.MakeFromPoints(allPos);
            }
            else
            {
                kd_allResLocations = null;
            }
        }

        void RefreshCategoryKDTree(int type)
        {
            Vector3[] allPos = new Vector3[resourcePointTypes[type].categorizedResourcePoints.Count];

            for (int i = 0; i < resourcePointTypes[type].categorizedResourcePoints.Count; i++)
            {
                if (resourcePointTypes[type].categorizedResourcePoints[i] != null)
                {
                    allPos[i] = resourcePointTypes[type].categorizedResourcePoints[i].position;
                }
            }

            if (resourcePointTypes[type].categorizedResourcePoints.Count > 0)
            {
                resourcePointTypes[type].kd_catLocations = KDTree.MakeFromPoints(allPos);
            }
            else
            {
                resourcePointTypes[type].kd_catLocations = null;
            }
        }

        void MergeMeshesToParent(GameObject go, Material mat)
        {
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
            }

            if (Application.isPlaying)
            {
                go.AddComponent<MeshFilter>();
                go.GetComponent<MeshFilter>().mesh = new Mesh();
                go.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
                go.AddComponent<MeshRenderer>();
                go.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                go.AddComponent<MeshFilter>();
                go.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                go.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
                go.AddComponent<MeshRenderer>();
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(true);

            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(meshFilters[i].gameObject);
                }
                else
                {
                    DestroyImmediate(meshFilters[i].gameObject);
                }
            }
        }

        public void UnsetFromTerrain(Terrain ter)
        {
            List<ResourcePointObject> removals = new List<ResourcePointObject>();

            for (int i = 0; i < resourcePoints.Count; i++)
            {
                if (resourcePoints[i].terrain == ter)
                {
                    removals.Add(resourcePoints[i]);

                    if (resourcePoints[i].resourceAmount < resourcePoints[i].maxResourceAmount)
                    {
                        savedResourcePoints.Add(new ResourcePointObjectData(resourcePoints[i]));
                    }
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                UnsetResourcePoint(removals[i], false);
            }

            RefreshKDTrees();
        }

        public bool IsOnTerrain(Terrain ter)
        {
            for (int i = 0; i < resourcePoints.Count; i++)
            {
                if (resourcePoints[i] != null)
                {
                    if (resourcePoints[i].terrain == ter)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void UnsetResourcePoint(ResourcePointObject rpo)
        {
            UnsetResourcePoint(rpo, true);
        }

        public ResourcePointObject GetResourcePointObject(int indexOnTerrain, Vector2i tile)
        {
            for (int i = 0; i < resourcePoints.Count; i++)
            {
                if (resourcePoints[i] != null)
                {
                    if (resourcePoints[i].indexOnTerrain == indexOnTerrain)
                    {
                        if (resourcePoints[i].tile != null && tile != null)
                        {
                            if (resourcePoints[i].tile.x == tile.x)
                            {
                                if (resourcePoints[i].tile.z == tile.z)
                                {
                                    return resourcePoints[i];
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public void UnsetResourcePoint(ResourcePointObject rpo, bool rebuildKD)
        {
            if (rebuildKD)
            {
                RefreshCategoryKDTree(rpo.resourceType);
            }

            GameObject go = rpo.go;

            resourcePoints.Remove(rpo);
            resourcePointTypes[rpo.resourceType].categorizedResourcePoints.Remove(rpo);

            Destroy(go);

            if (rebuildKD)
            {
                RefreshFullKDTree();
            }

            rpo.isAlive = false;
        }
    }

    [System.Serializable]
    public class ResourcePointType
    {
        public string name;
        public int minAmount;
        public int maxAmount;

        public int resourceType = -1;

        public float densityOnTerrain = 1.25e-5f;

        public GameObject prefab;

        public float prefabScaleMin = 0.01f;
        public float prefabScaleMax = 0.1f;

        public int numberPrefabsMin = 1;
        public int numberPrefabsMax = 10;

        public float spreadRadius = 1f;

        [HideInInspector] public List<ResourcePointObject> categorizedResourcePoints = new List<ResourcePointObject>();
        public KDTree kd_catLocations;
    }
}
