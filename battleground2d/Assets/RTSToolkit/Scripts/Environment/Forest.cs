using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace RTSToolkit
{
    public class Forest : MonoBehaviour
    {
        public static Forest active;

        public List<VegetationType> vegetationTypes = new List<VegetationType>();
        public List<PerlinBiome> biomes = new List<PerlinBiome>();
        public PerlinBiome mainBiome;
        public int scannedTreesResourceType;
        public int scannedTreesResourceAmount = 500;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        [System.Serializable]
        public class VegetationType
        {
            public string treeName = "";
            public GameObject treePrefab;
            public float density = 5.0e-4f;
            public float minimumCriticalDistance = 3f;
            public int resourceType = 2;
            public int resourceAmount = 500;
            public Vector3 offset = Vector3.zero;
            public List<PerlinBiome> subBiomes = new List<PerlinBiome>();
            public int biomeId = 0;
            [HideInInspector] public int prefabMode = -1;
            public bool useThisModel = true;

            public void GetMode()
            {
                if ((treePrefab == null) && (treeName != null))
                {
                    prefabMode = 0;
                }
                else if (treePrefab != null)
                {
                    prefabMode = 1;
                }
            }
        }

        public static Forest GetActive()
        {
            if (Forest.active != null)
            {
                if ((Forest.active.gameObject.activeSelf == false) || (Forest.active.enabled == false))
                {
                    Forest.active = null;
                }
            }

            if (Forest.active == null)
            {
                Forest[] obj = UnityEngine.Object.FindObjectsOfType<Forest>();
                for (int i = 0; i < obj.Length; i++)
                {
                    if (obj[i].gameObject.activeSelf)
                    {
                        if (obj[i].enabled)
                        {
                            Forest.active = obj[i];
                            return Forest.active;
                        }
                    }
                }
            }

            return Forest.active;
        }

        public bool IsPointInsideForest(Vector3 point)
        {
            if (!
                (mainBiome.IsValueAccepted(point.x, point.z) &&
                 mainBiome.useBiome)
            )
            {
                return false;
            }

            return true;
        }
    }

    [System.Serializable]
    public class PerlinBiome
    {
        public int seed;
        public bool useBiome = true;
        public AnimationCurve coverageCurve;

        public float frequency = 1.0f;
        public float lacunarity = 2.0f;
        public int octaveCount = 6;
        public float persistence = 0.5f;

        public float size = 2000f;

        public Vector2 offset = Vector2.zero;

        public float GetCurveAppliedValue(float x, float z)
        {
            if (coverageCurve == null)
            {
                return 0f;
            }

            float pVal = NoiseExtensions.SNoise(
                new float2(x - offset.x, z - offset.y) / size,
                frequency,
                lacunarity,
                persistence,
                octaveCount,
                1f,
                offset,
                seed
            );

            return coverageCurve.Evaluate(pVal);
        }

        public bool IsValueAccepted(float x, float z)
        {
            if (coverageCurve == null)
            {
                return false;
            }

            float y1 = GetCurveAppliedValue(x, z);

            float rand = UnityEngine.Random.value;
            if (rand < y1)
            {
                return true;
            }

            return false;
        }

        public bool IsValueAccepted2(float x, float z)
        {
            if (coverageCurve == null)
            {
                return false;
            }

            float a = NoiseExtensions.SNoise(
                new float2(x - offset.x, z - offset.y) / size,
                frequency,
                lacunarity,
                persistence,
                octaveCount,
                1f,
                offset,
                seed
            );
            float b = 0f;

            Forest f = Forest.GetActive();
            int n = f.biomes.Count;
            for (int i = 0; i < n; i++)
            {
                PerlinBiome pb = f.biomes[i];

                b = Mathf.Max(
                    b,
                    NoiseExtensions.SNoise(
                        new float2(x, z) / size,
                        pb.frequency,
                        pb.lacunarity,
                        pb.persistence,
                        pb.octaveCount,
                        1f,
                        pb.offset,
                        pb.seed
                    )
                );
            }

            float c = a / b;

            if (c > 0.999f)
            {
                return true;
            }
            else if (c > 0.8f)
            {
                float rand = UnityEngine.Random.Range(0.8f, 0.999f);
                if (rand < c)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [System.Serializable]
    public class ForestPlacer
    {
        public float perlinSeed = 24f;

        [HideInInspector] public Terrain terrain = null;

        public List<Forest.VegetationType> vegetationTypes = new List<Forest.VegetationType>();

        List<List<TreePrototype>> tpsList = new List<List<TreePrototype>>();
        [HideInInspector] public List<int> treePartCount = new List<int>();
        [HideInInspector] public List<List<int>> treePart1dIndices = new List<List<int>>();

        [HideInInspector] public List<ResourcePointObject> terrainTrees = new List<ResourcePointObject>();

        [HideInInspector] public KDTree kd_treePositions = null;

        public int heightmapResolution = 512;

        int treeInd = 0;

        [HideInInspector] public List<int> removedTreeIndices = new List<int>();
        public Vector2i terrainTile;

        void Start()
        {
            Starter();
        }

        public void Starter()
        {
            Starter(true);
        }

        public void Starter(bool recalculatePositions)
        {
            GetVegetationTypes();

            tpsList.Clear();
            treePart1dIndices.Clear();
            treePartCount.Clear();

            for (int j = 0; j < vegetationTypes.Count; j++)
            {
                int j_mode = vegetationTypes[j].prefabMode;
                tpsList.Add(new List<TreePrototype>());
                treePart1dIndices.Add(new List<int>());

                if (j_mode == 0)
                {
                    treePartCount.Add(GetTreePartCount(j));
                }
                else
                {
                    treePartCount.Add(1);
                }
            }

            for (int j = 0; j < vegetationTypes.Count; j++)
            {
                SpawnTrees(j);
            }

            LoadTreePrototypes();

            if (recalculatePositions)
            {
                if (terrainTrees.Count == 0)
                {
                    CalculateAllTreePositions();
                }
            }

            for (int j = 0; j < vegetationTypes.Count; j++)
            {
                CalculateTrees(j);
            }
        }

        public void GetFromExistingTerrain(Terrain ter)
        {
            terrain = ter;
            TreeInstance[] ti = terrain.terrainData.treeInstances;

            for (int i = 0; i < ti.Length; i++)
            {
                ResourcePointObject rpo = new ResourcePointObject();
                rpo.position = new Vector3(
                    ti[i].position.x * terrain.terrainData.size.x,
                    ti[i].position.y * terrain.terrainData.size.y,
                    ti[i].position.z * terrain.terrainData.size.z
                ) + terrain.transform.position;

                rpo.terrain = terrain;
                rpo.resourceType = Forest.GetActive().scannedTreesResourceType;
                rpo.resourceAmount = Forest.GetActive().scannedTreesResourceAmount;
                rpo.indexOnTerrain = i;
                rpo.treeInstances = new List<TreeInstance>();
                rpo.treeInstances.Add(ti[i]);

                terrainTrees.Add(rpo);
            }

            RebuildKDTree();
        }

        void GetVegetationTypes()
        {
            Forest fr = Forest.GetActive();

            if ((fr != null) && (fr.enabled) && (fr.gameObject.activeSelf))
            {
                vegetationTypes = new List<Forest.VegetationType>();
                List<Forest.VegetationType> vTypes = fr.vegetationTypes;

                for (int i = 0; i < vTypes.Count; i++)
                {
                    vTypes[i].GetMode();

                    if (vTypes[i].prefabMode != -1)
                    {
                        if (vTypes[i].useThisModel)
                        {
                            vegetationTypes.Add(vTypes[i]);
                        }
                    }
                }
            }
        }

        int GetTreePartCount(int plantId)
        {
            string[] lines = new string[1];

            string filePath = "trees/" + vegetationTypes[plantId].treeName + "/config";
            TextAsset textRes = Resources.Load<TextAsset>(filePath);

            StringReader textReader = new StringReader(textRes.text);
            lines[0] = textReader.ReadLine();

            int ii = int.Parse(lines[0]);

            return ii;
        }

        void SpawnTrees(int plantId)
        {
            for (int i = 0; i < treePartCount[plantId]; i++)
            {
                tpsList[plantId].Add(new TreePrototype());

                if (vegetationTypes[plantId].prefabMode == 0)
                {
                    tpsList[plantId][i].prefab = Resources.Load<GameObject>("trees/" + vegetationTypes[plantId].treeName + "/" + vegetationTypes[plantId].treeName + " part " + i);
                }
                else
                {
                    tpsList[plantId][0].prefab = vegetationTypes[plantId].treePrefab;
                }

                tpsList[plantId][i].bendFactor = 0.3f;
            }
        }

        void LoadTreePrototypes()
        {
            int nPrototypes = 0;

            for (int i = 0; i < vegetationTypes.Count; i++)
            {
                for (int j = 0; j < treePartCount[i]; j++)
                {
                    nPrototypes++;
                }
            }

            TreePrototype[] tps = new TreePrototype[nPrototypes];
            nPrototypes = 0;

            for (int i = 0; i < vegetationTypes.Count; i++)
            {
                for (int j = 0; j < treePartCount[i]; j++)
                {
                    tps[nPrototypes] = tpsList[i][j];
                    treePart1dIndices[i].Add(nPrototypes);
                    nPrototypes++;
                }
            }

            terrain.terrainData.treePrototypes = tps;
        }

        public void CalculateAllTreePositions()
        {
            for (int i = 0; i < vegetationTypes.Count; i++)
            {
                CalculateTreePositions(i);
            }

            RebuildKDTree();
            RemoveCloseTrees();
        }

        void CalculateTreePositions(int plantId)
        {
            terrain.treeBillboardDistance = GenerateTerrain.GetActive().treeBilboardStart;
            terrain.treeCrossFadeLength = GenerateTerrain.GetActive().treeCrossFadeLength;
            terrain.treeDistance = 4000f;
            terrain.basemapDistance = 4000f;

            UnityEngine.Random.InitState(plantId);

            int kk = 0;

            float tsizex = terrain.terrainData.size.x;
            float tsizez = terrain.terrainData.size.z;

            float tposx = terrain.transform.position.x;
            float tposz = terrain.transform.position.z;

            int tilex = (int)(tposx / tsizex);
            int tilez = (int)(tposz / tsizez);

            Forest forest = Forest.GetActive();
            int nPlants = (int)(vegetationTypes[plantId].density * (tsizex * tsizez));

            for (int i = 0; i < nPlants; i++)
            {
                float rand1 = UnityEngine.Random.value;
                float rand2 = UnityEngine.Random.value;

                bool p_pass = true;

                if (forest.mainBiome != null)
                {
                    if (!
                        (
                            forest.mainBiome.IsValueAccepted((rand1 + tilex) * tsizex, (rand2 + tilez) * tsizez) &&
                            forest.mainBiome.useBiome
                        )
                    )
                    {
                        p_pass = false;
                    }
                }

                if ((vegetationTypes[plantId].biomeId >= 0) && (vegetationTypes[plantId].biomeId < forest.biomes.Count))
                {
                    if (!
                        (
                            forest.biomes[vegetationTypes[plantId].biomeId].IsValueAccepted2((rand1 + tilex) * tsizex, (rand2 + tilez) * tsizez) &&
                            forest.biomes[vegetationTypes[plantId].biomeId].useBiome
                        )
                    )
                    {
                        p_pass = false;
                    }
                }

                for (int i1 = 0; i1 < vegetationTypes[plantId].subBiomes.Count; i1++)
                {
                    if (!
                        (
                            vegetationTypes[plantId].subBiomes[i1].IsValueAccepted((rand1 + tilex) * tsizex, (rand2 + tilez) * tsizez) &&
                            vegetationTypes[plantId].subBiomes[i1].useBiome
                        )
                    )
                    {
                        p_pass = false;
                    }
                }

                if (p_pass)
                {
                    kk = kk + 1;
                    float height = terrain.SampleHeight(new Vector3(rand1 * tsizex + tposx, 0f, rand2 * tsizez + tposz));

                    bool w_pass = false;
                    GameObject w_go = GenerateTerrain.GetActive().water;

                    if (w_go == null)
                    {
                        w_pass = true;
                    }
                    else
                    {
                        if (height - 1f > w_go.transform.position.y)
                        {
                            w_pass = true;
                        }
                    }

                    if (w_pass)
                    {
                        Vector3 treePos = new Vector3(rand1 * tsizex, (height - 1f), rand2 * tsizez) + terrain.transform.position + vegetationTypes[plantId].offset;

                        float steepness = terrain.terrainData.GetSteepness(rand1, rand2);
                        if (steepness < 45f)
                        {

                            ResourcePointObject rpo = new ResourcePointObject();
                            rpo.position = treePos;
                            rpo.treeType = plantId;
                            rpo.terrain = terrain;
                            rpo.resourceType = vegetationTypes[plantId].resourceType;
                            rpo.resourceAmount = vegetationTypes[plantId].resourceAmount;
                            rpo.minNeighbourDist = vegetationTypes[plantId].minimumCriticalDistance;

                            terrainTrees.Add(rpo);
                            treeInd = treeInd + 1;
                        }
                    }
                }
            }
        }

        public void RemoveCloseTrees()
        {
            if (kd_treePositions != null)
            {
                if (terrainTrees.Count > 1)
                {
                    bool[] passes = new bool[terrainTrees.Count];
                    for (int i = 0; i < terrainTrees.Count; i++)
                    {
                        passes[i] = true;
                    }

                    for (int i = 0; i < terrainTrees.Count; i++)
                    {
                        int j = kd_treePositions.FindNearestK(terrainTrees[i].position, 2);
                        if (passes[i])
                        {
                            if ((j >= 0) && (j < passes.Length))
                            {
                                if (passes[j])
                                {
                                    if ((terrainTrees[i].position - terrainTrees[j].position).magnitude < terrainTrees[i].minNeighbourDist)
                                    {
                                        passes[i] = false;
                                    }
                                }
                            }
                        }
                    }

                    ResourcePointObject[] tta = terrainTrees.ToArray();
                    terrainTrees.Clear();

                    for (int i = 0; i < tta.Length; i++)
                    {
                        if (passes[i])
                        {
                            terrainTrees.Add(tta[i]);
                        }
                    }
                }

                RebuildKDTree();
            }
        }

        public void CalculateTrees(int plantId)
        {
            Color cl1 = new Color(UnityEngine.Random.Range(0.93f, 1f), UnityEngine.Random.Range(0.93f, 1f), UnityEngine.Random.Range(0.93f, 1f), 1f);
            Color cl2 = new Color(UnityEngine.Random.Range(0.93f, 1f), UnityEngine.Random.Range(0.93f, 1f), UnityEngine.Random.Range(0.93f, 1f), 1f);

            RebuildKDTree();
            Vector3 terPos = terrain.transform.position;
            Vector3 tsize = terrain.terrainData.size;

            for (int i = 0; i < terrainTrees.Count; i++)
            {
                if (terrainTrees[i].treeType == plantId)
                {
                    float rand3 = UnityEngine.Random.Range(-1f, 1f);

                    Vector3 treePos = terrainTrees[i].position - terPos;
                    treePos = new Vector3(treePos.x / tsize.x, treePos.y / tsize.y, treePos.z / tsize.z);

                    for (int j = 0; j < treePartCount[plantId]; j++)
                    {
                        TreeInstance tree = new TreeInstance();
                        tree.color = cl1;
                        tree.heightScale = 1f + 0.05f * rand3;
                        tree.lightmapColor = cl2;
                        tree.position = treePos;
                        tree.prototypeIndex = treePart1dIndices[plantId][j];
                        tree.widthScale = 1f + 0.05f * rand3;
                        terrainTrees[i].treeInstances.Add(tree);
                    }
                }
            }

            RefreshTrees(false);
        }

        public void CleanTrees()
        {
            terrain.terrainData.treeInstances = new TreeInstance[0];
        }

        public void RefreshTrees(bool updateKDTree)
        {
            int n = 0;

            for (int i = 0; i < terrainTrees.Count; i++)
            {
                for (int j = 0; j < terrainTrees[i].treeInstances.Count; j++)
                {
                    n++;
                }
            }

            TreeInstance[] inst = new TreeInstance[n];
            n = 0;

            for (int i = 0; i < terrainTrees.Count; i++)
            {
                for (int j = 0; j < terrainTrees[i].treeInstances.Count; j++)
                {
                    inst[n] = terrainTrees[i].treeInstances[j];
                    n++;
                }
            }

            terrain.terrainData.treeInstances = inst;

            if (updateKDTree)
            {
                RebuildKDTree();
            }
        }

        public void RemoveTrees(List<int> indices)
        {
            List<ResourcePointObject> treesToRemove = new List<ResourcePointObject>();

            for (int i = 0; i < indices.Count; i++)
            {
                int j = indices[i];
                treesToRemove.Add(terrainTrees[j]);
            }

            for (int i = 0; i < treesToRemove.Count; i++)
            {
                RemoveTree(treesToRemove[i]);
            }
        }

        public void RemoveTree(ResourcePointObject chopTree)
        {
            removedTreeIndices.Add(chopTree.indexOnTerrain);
            terrainTrees.Remove(chopTree);
            chopTree.isAlive = false;

            if (terrainTrees.Count > 0)
            {
                RebuildKDTree();
            }

            RefreshTrees(true);
        }

        public void SetDefaultSettings()
        {
            vegetationTypes = new List<Forest.VegetationType>();

            Forest.VegetationType vt = new Forest.VegetationType();
            vt.treeName = "sprc_1";
            vt.density = 1e-4f;
            vegetationTypes.Add(vt);

            vt = new Forest.VegetationType();
            vt.treeName = "sprc_11";
            vt.density = 1e-4f;
            vegetationTypes.Add(vt);


            vt = new Forest.VegetationType();
            vt.treeName = "sprc_46";
            vt.density = 1e-4f;
            vegetationTypes.Add(vt);

            vt = new Forest.VegetationType();
            vt.treeName = "sprc_47";
            vt.density = 1e-4f;
            vegetationTypes.Add(vt);

            vt = new Forest.VegetationType();
            vt.treeName = "sprc_54";
            vt.density = 1e-4f;
            vegetationTypes.Add(vt);

            vt = new Forest.VegetationType();
            vt.treeName = "sprc_55";
            vt.density = 1e-4f;
            vegetationTypes.Add(vt);
        }

        public void RebuildKDTree()
        {
            int n = terrainTrees.Count;
            Vector3[] positions = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                positions[i] = terrainTrees[i].position;
            }

            if (positions.Length > 0)
            {
                kd_treePositions = KDTree.MakeFromPoints(positions);
            }
            else
            {
                kd_treePositions = null;
            }
        }

        public ResourcePointObject FindNearestTerrainTree(Vector3 position)
        {
            if (kd_treePositions == null)
            {
                RebuildKDTree();
            }
            if (kd_treePositions == null)
            {
                return null;
            }

            ResourcePointObject tt = null;
            int id = kd_treePositions.FindNearest(position);

            if (id > -1)
            {
                tt = terrainTrees[id];
            }

            return tt;
        }

        public ResourcePointObject[] FindNearestsKTerrainTree(Vector3 position, int k)
        {
            if (kd_treePositions == null)
            {
                RebuildKDTree();
            }
            if (kd_treePositions == null)
            {
                return null;
            }

            int[] ids = kd_treePositions.FindNearestsK(position, k);
            List<ResourcePointObject> rpoList = new List<ResourcePointObject>();
            ResourcePointObject[] rpo = new ResourcePointObject[0];

            if (ids != null)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    int j = ids[i];
                    if (j > -1)
                    {
                        if (j < terrainTrees.Count)
                        {
                            rpoList.Add(terrainTrees[j]);
                        }
                    }
                }
            }

            if (rpoList.Count > 0)
            {
                rpo = rpoList.ToArray();
            }

            return rpo;
        }
    }

    public class ForestPlacerSaver
    {
        public List<int> removedTreeIndices = new List<int>();
        public Vector2i terrainTile;

        public static ForestPlacerSaver CreateForestPlacerSaver(ForestPlacer fp)
        {
            ForestPlacerSaver fps = new ForestPlacerSaver();
            fps.removedTreeIndices = fp.removedTreeIndices;
            fps.terrainTile = fp.terrainTile;
            return fps;
        }
    }
}
