using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace RTSToolkit
{
    public class TerrainTextures : MonoBehaviour
    {
        public static TerrainTextures active;

        public List<TextureType> terrainTextures = new List<TextureType>();
        public List<TextureTypeThread> preparedThreads = new List<TextureTypeThread>();
        public List<TextureTypeThread> generatingThreads = new List<TextureTypeThread>();

        public bool useThreads = true;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public static TerrainTextures GetActive()
        {
            if (TerrainTextures.active != null)
            {
                if ((TerrainTextures.active.gameObject.activeSelf == false) || (TerrainTextures.active.enabled == false))
                {
                    TerrainTextures.active = null;
                }
            }

            if (TerrainTextures.active == null)
            {
                TerrainTextures[] obj = UnityEngine.Object.FindObjectsOfType<TerrainTextures>();

                for (int i = 0; i < obj.Length; i++)
                {
                    if (obj[i].gameObject.activeSelf)
                    {
                        if (obj[i].enabled)
                        {
                            TerrainTextures.active = obj[i];
                            return TerrainTextures.active;
                        }
                    }
                }
            }

            return TerrainTextures.active;
        }

        public void Clean()
        {
            preparedThreads.Clear();
            generatingThreads.Clear();
        }

        public void Update_E()
        {
            Update();
        }

        void Update()
        {
            if (generatingThreads.Count < 4)
            {
                if (preparedThreads.Count > 0)
                {
                    preparedThreads[0].ThreadStarter();
                    generatingThreads.Add(preparedThreads[0]);
                    preparedThreads.Remove(preparedThreads[0]);
                    return;
                }
            }

            for (int i = 0; i < generatingThreads.Count; i++)
            {
                if (generatingThreads[i].threadFinished)
                {
                    generatingThreads[i].RefreshPrototypes();

                    if (generatingThreads[i].terrainData != null)
                    {
                        generatingThreads[i].terrainData.terrainLayers = generatingThreads[i].splatPrototypes.ToArray();
                        generatingThreads[i].terrainData.RefreshPrototypes();


                        generatingThreads[i].terrainData.SetAlphamaps(0, 0, generatingThreads[i].splatMap);
                    }

                    generatingThreads[i].terrainChunk.texturesComplete = true;
                    generatingThreads[i].terrainChunk.CheckForCompletion();

                    generatingThreads.Remove(generatingThreads[i]);

                    return;
                }
            }
        }

        public void GenerateSplatMaps(TerrainChunk tc, Terrain ter)
        {
            TextureTypeThread ttt = new TextureTypeThread();
            ttt.terrainTextures = this;
            ttt.terrain = ter;
            ttt.terrainData = ter.terrainData;
            ttt.threadFinished = false;

            ttt.aRes = ter.terrainData.alphamapResolution;

            ttt.tsizex = ter.terrainData.size.x;
            ttt.tsizez = ter.terrainData.size.z;

            ttt.tposx = ter.transform.position.x;
            ttt.tposz = ter.transform.position.z;

            ttt.terrainChunk = tc;
            preparedThreads.Add(ttt);
        }

        [System.Serializable]
        public class TextureType
        {
            public Texture2D texture;
            public Texture2D normalMap;
            public AnimationCurve heightCurve;
            public float absoluteHeightMin = -1f;
            public float absoluteHeightMax = -1f;
            public AnimationCurve slopeCurve;
            public float weightMultiplier = 1f;
            public Vector2 tileSize = new Vector2(6f, 6f);
            public List<PerlinBiome> perlinBiomes = new List<PerlinBiome>();
            public List<RandomTextureType> randomTextures = new List<RandomTextureType>();
            public bool scaleTilesRandomly = false;

            public AnimationCurve heightMask;
            public AnimationCurve slopeMask;
            public PerlinBiome perlinMask;

            [System.Serializable]
            public class RandomTextureType
            {
                public Texture2D texture;
                public Texture2D normalMap;
            }
        }

        public class TextureTypeThread
        {
            public TerrainTextures terrainTextures;
            public Terrain terrain;
            public TerrainData terrainData;
            public bool threadFinished = false;

            public int aRes;

            public float tsizex;
            public float tsizez;

            public float tposx;
            public float tposz;

            public float[,] heightmap;

            public TerrainChunk terrainChunk;

            public List<TerrainLayer> splatPrototypes;
            public float[,,] splatMap;

            public float[,] steepnessMap;

            public void ThreadStarter()
            {
                GetSteepnessMapUnity();

#if UNITY_WEBGL && !UNITY_EDITOR
			    StartThread();
#else
                if (TerrainTextures.GetActive().useThreads)
                {
                    Thread thread = new Thread(StartThread);
                    thread.Start();
                }
                else
                {
                    StartThread();
                }
#endif
            }

            public void GetSteepnessMap()
            {
                steepnessMap = new float[aRes, aRes];

                for (int iz = 0; iz < aRes; iz++)
                {
                    for (int ix = 0; ix < aRes; ix++)
                    {
                        steepnessMap[ix, iz] = GetSteepness(ix, iz) / 90f;
                    }
                }
            }

            public void GetSteepnessMapUnity()
            {
                steepnessMap = new float[aRes, aRes];

                for (int iz = 0; iz < aRes; iz++)
                {
                    for (int ix = 0; ix < aRes; ix++)
                    {
                        float normalizedX = (float)ix / (aRes - 1);
                        float normalizedZ = (float)iz / (aRes - 1);
                        steepnessMap[ix, iz] = terrainData.GetSteepness(normalizedX, normalizedZ) / 90f;
                    }
                }
            }

            public void StartThread()
            {
                int tilex = (int)(tposx / tsizex);
                int tilez = (int)(tposz / tsizez);

                int n_splats = 0;
                int n_rand_max = 0;

                for (int i = 0; i < terrainTextures.terrainTextures.Count; i++)
                {
                    n_rand_max = Mathf.Max(n_rand_max, terrainTextures.terrainTextures[i].randomTextures.Count);
                }

                int[,] splatsRefs = new int[terrainTextures.terrainTextures.Count, n_rand_max + 1];

                for (int i = 0; i < terrainTextures.terrainTextures.Count; i++)
                {
                    if (terrainTextures.terrainTextures[i].randomTextures.Count == 0)
                    {
                        splatsRefs[i, 0] = n_splats;
                        n_splats = n_splats + 1;
                    }
                    else
                    {
                        for (int j = 0; j < terrainTextures.terrainTextures[i].randomTextures.Count; j++)
                        {
                            splatsRefs[i, j] = n_splats;
                            n_splats = n_splats + 1;
                        }
                    }
                }

                splatMap = new float[aRes, aRes, n_splats];

                for (int iz = 0; iz < aRes; iz++)
                {
                    for (int ix = 0; ix < aRes; ix++)
                    {
                        for (int i = 0; i < n_splats; i++)
                        {
                            splatMap[iz, ix, i] = 0f;
                        }
                    }
                }

                System.Random r = new System.Random();

                for (int iz = 0; iz < aRes; iz++)
                {
                    for (int ix = 0; ix < aRes; ix++)
                    {
                        float height_orig = terrainChunk.heightmap[iz, ix];
                        float steepness_orig = steepnessMap[ix, iz];

                        for (int j = 0; j < terrainTextures.terrainTextures.Count; j++)
                        {
                            int i = 0;
                            if (terrainTextures.terrainTextures[j].randomTextures.Count == 0)
                            {
                                i = splatsRefs[j, 0];
                            }
                            else
                            {
                                i = splatsRefs[j, r.Next(0, terrainTextures.terrainTextures[j].randomTextures.Count - 1)];
                            }

                            float normalizedX = (float)ix / (aRes - 1);
                            float normalizedZ = (float)iz / (aRes - 1);
                            float height = terrainTextures.terrainTextures[j].heightCurve.Evaluate(height_orig);

                            if (terrainTextures.terrainTextures[j].absoluteHeightMin >= 0 && terrainTextures.terrainTextures[j].absoluteHeightMax > 0)
                            {
                                if (terrainTextures.terrainTextures[j].absoluteHeightMin < terrainTextures.terrainTextures[j].absoluteHeightMax)
                                {
                                    float fract = terrainTextures.terrainTextures[j].absoluteHeightMax / terrainChunk.settings.height;
                                    float height_scaled = height_orig / fract;
                                    height = terrainTextures.terrainTextures[j].heightCurve.Evaluate(height_scaled);
                                }
                            }

                            float heightMask = 1f;
                            if (terrainTextures.terrainTextures[j].heightMask != null)
                            {
                                heightMask = 1f - terrainTextures.terrainTextures[j].heightMask.Evaluate(height_orig);
                            }
                            heightMask = Mathf.Clamp(heightMask, 0f, 1f);

                            float steepness = terrainTextures.terrainTextures[j].slopeCurve.Evaluate(steepness_orig);
                            float steepnessMask = 1f;

                            if (terrainTextures.terrainTextures[j].slopeMask != null)
                            {
                                steepnessMask = 1f - terrainTextures.terrainTextures[j].slopeMask.Evaluate(steepness_orig);
                            }

                            steepnessMask = Mathf.Clamp(steepnessMask, 0f, 1f);
                            float perlin = 0f;

                            if (terrainTextures.terrainTextures[j].perlinBiomes.Count > 0)
                            {
                                for (int ip = 0; ip < terrainTextures.terrainTextures[j].perlinBiomes.Count; ip++)
                                {
                                    perlin = perlin + terrainTextures.terrainTextures[j].perlinBiomes[ip].GetCurveAppliedValue((normalizedX + tilex) * tsizex, (normalizedZ + tilez) * tsizez);
                                }

                                perlin = perlin / terrainTextures.terrainTextures[j].perlinBiomes.Count;
                            }

                            float perlinMask = 1f;

                            if (terrainTextures.terrainTextures[j].perlinMask != null)
                            {
                                perlinMask = 1f - terrainTextures.terrainTextures[j].perlinMask.GetCurveAppliedValue((normalizedX + tilex) * tsizex, (normalizedZ + tilez) * tsizez);
                            }

                            perlinMask = Mathf.Clamp(perlinMask, 0f, 1f);
                            float totMask = heightMask * steepnessMask * perlinMask;

                            if ((terrainTextures.terrainTextures[j].randomTextures.Count > 0) && (terrainTextures.terrainTextures[j].scaleTilesRandomly))
                            {
                                for (int j1 = 0; j1 < terrainTextures.terrainTextures[j].randomTextures.Count; j1++)
                                {
                                    int i1 = splatsRefs[j, j1];
                                    splatMap[iz, ix, i1] = terrainTextures.terrainTextures[j].weightMultiplier * totMask * (height + steepness + perlin);
                                }
                            }
                            else
                            {
                                splatMap[iz, ix, i] = terrainTextures.terrainTextures[j].weightMultiplier * totMask * (height + steepness + perlin);
                            }
                        }

                        float splatSum = 0f;
                        float splatMin = 10000f;

                        for (int i = 0; i < n_splats; i++)
                        {
                            splatMin = Mathf.Min(splatMin, splatMap[iz, ix, i]);
                        }

                        for (int i = 0; i < n_splats; i++)
                        {
                            splatMap[iz, ix, i] = splatMap[iz, ix, i] - splatMin;
                        }

                        for (int i = 0; i < n_splats; i++)
                        {
                            splatSum = splatSum + splatMap[iz, ix, i];
                        }

                        for (int i = 0; i < n_splats; i++)
                        {
                            splatMap[iz, ix, i] = splatMap[iz, ix, i] / splatSum;
                        }

                        for (int i = 0; i < n_splats; i++)
                        {
                            if (splatMap[iz, ix, i] < 0f)
                            {
                                splatMap[iz, ix, i] = 0f;
                            }
                        }

                        for (int i = 0; i < n_splats; i++)
                        {
                            if (splatMap[iz, ix, i] > 1f)
                            {
                                splatMap[iz, ix, i] = 1f;
                            }
                        }
                    }
                }

                threadFinished = true;
            }

            public void RefreshPrototypes()
            {
                splatPrototypes = new List<TerrainLayer>();

                for (int i = 0; i < terrainTextures.terrainTextures.Count; i++)
                {
                    if (terrainTextures.terrainTextures[i].randomTextures.Count == 0)
                    {
                        TerrainLayer spl = new TerrainLayer();
                        spl.diffuseTexture = terrainTextures.terrainTextures[i].texture;
                        spl.normalMapTexture = terrainTextures.terrainTextures[i].normalMap;
                        spl.tileSize = terrainTextures.terrainTextures[i].tileSize;
                        splatPrototypes.Add(spl);
                    }
                    else
                    {
                        for (int j = 0; j < terrainTextures.terrainTextures[i].randomTextures.Count; j++)
                        {
                            TerrainLayer spl = new TerrainLayer();
                            spl.diffuseTexture = terrainTextures.terrainTextures[i].randomTextures[j].texture;
                            spl.normalMapTexture = terrainTextures.terrainTextures[i].randomTextures[j].normalMap;

                            if (terrainTextures.terrainTextures[i].scaleTilesRandomly)
                            {
                                float trand = 1f * j * UnityEngine.Random.Range(-1f, 1f);
                                spl.tileSize = new Vector2((3f * j + trand), (3f * j + trand));
                            }
                            else
                            {
                                spl.tileSize = terrainTextures.terrainTextures[i].tileSize;
                            }

                            splatPrototypes.Add(spl);
                        }
                    }
                }
            }

            float GetSteepness(int y, int x)
            {
                float slopeX = terrainChunk.heightmap[x < terrainChunk.settings.heightmapResolution - 1 ? x + 1 : x, y] - terrainChunk.heightmap[x > 0 ? x - 1 : x, y];
                float slopeZ = terrainChunk.heightmap[x, y < terrainChunk.settings.heightmapResolution - 1 ? y + 1 : y] - terrainChunk.heightmap[x, y > 0 ? y - 1 : y];

                if (x == 0 || x == terrainChunk.settings.heightmapResolution - 1)
                {
                    slopeX *= 2;
                }

                if (y == 0 || y == terrainChunk.settings.heightmapResolution - 1)
                {
                    slopeZ *= 2;
                }

                slopeX *= terrainChunk.settings.height;
                slopeZ *= terrainChunk.settings.height;

                Vector3 normal = new Vector3(
                    -slopeX * (terrainChunk.settings.heightmapResolution - 1) / terrainChunk.settings.length,
                    (terrainChunk.settings.heightmapResolution - 1) / terrainChunk.settings.height,
                    slopeZ * (terrainChunk.settings.heightmapResolution - 1) / terrainChunk.settings.length
                );

                normal.Normalize();
                float steepness = Mathf.Acos(Vector3.Dot(normal, Vector3.up));

                return steepness * Mathf.Rad2Deg;
            }

            float[,] SmoothMap(float[,] map)
            {
                int nx = map.GetLength(0);
                int nz = map.GetLength(1);

                int radius = 1;

                float[,] newMap = new float[nx, nz];
                float[,] newMapWeight = new float[nx, nz];

                for (int i = 0; i < nx; i++)
                {
                    for (int j = 0; j < nx; j++)
                    {
                        int minX = i - radius;
                        if (minX < 0)
                        {
                            minX = 0;
                        }

                        int maxX = i + radius;
                        if (maxX > (nx - 1))
                        {
                            maxX = (nx - 1);
                        }

                        int minY = j - radius;
                        if (minY < 0)
                        {
                            minY = 0;
                        }

                        int maxY = j + radius;
                        if (maxY > (nz - 1))
                        {
                            maxY = (nz - 1);
                        }

                        for (int i1 = minX; i1 <= maxX; i1++)
                        {
                            for (int j1 = minY; j1 <= maxY; j1++)
                            {
                                newMap[i, j] = newMap[i, j] + map[i1, j1];
                                newMapWeight[i, j] = newMapWeight[i, j] + 1f;
                            }
                        }
                    }
                }

                for (int i = 0; i < nx; i++)
                {
                    for (int j = 0; j < nx; j++)
                    {
                        newMap[i, j] = newMap[i, j] / newMapWeight[i, j];
                    }
                }

                return newMap;
            }
        }
    }
}
