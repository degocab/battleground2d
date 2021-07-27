using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Unity.Mathematics;

namespace RTSToolkit
{
    public class TerrainHeightmap : MonoBehaviour
    {
        public static TerrainHeightmap active;
        public HeightBiome heightBiome;
        public HeightBiome cliffHeightBiome1;
        public HeightBiome cliffHeightBiome2;

        void Start()
        {

        }

        public static TerrainHeightmap GetActive()
        {
            if (TerrainHeightmap.active != null)
            {
                if ((TerrainHeightmap.active.gameObject.activeSelf == false) || (TerrainHeightmap.active.enabled == false))
                {
                    TerrainHeightmap.active = null;
                }
            }

            if (TerrainHeightmap.active == null)
            {
                TerrainHeightmap[] obj = UnityEngine.Object.FindObjectsOfType<TerrainHeightmap>();
                for (int i = 0; i < obj.Length; i++)
                {
                    if (obj[i].gameObject.activeSelf)
                    {
                        if (obj[i].enabled)
                        {
                            TerrainHeightmap.active = obj[i];
                            return TerrainHeightmap.active;
                        }
                    }
                }
            }

            return TerrainHeightmap.active;
        }

        public void GenerateHeightmap(TerrainChunk terrainChunk)
        {
            TerrainHeightmapObject obj = new TerrainHeightmapObject();
            obj.terrainChunk = terrainChunk;

            heightBiome.GetNoiseProvider();
            cliffHeightBiome1.GetNoiseProvider();
            cliffHeightBiome2.GetNoiseProvider();

            obj.heightBiome = heightBiome;
            obj.cliffHeightBiome1 = cliffHeightBiome1;
            obj.cliffHeightBiome2 = cliffHeightBiome2;

            if (Rivers.GetActive() != null)
            {
                if (Rivers.GetActive().gameObject.activeSelf)
                {
                    if (Rivers.GetActive().enabled)
                    {
                        obj.rivers = Rivers.GetActive();
                        obj.rivers.GenerateDLACheck();
                    }
                }
            }

            if (GenerateSea.GetActive() != null)
            {
                if (GenerateSea.GetActive().gameObject.activeSelf)
                {
                    if (GenerateSea.GetActive().enabled)
                    {
                        obj.generateSea = GenerateSea.GetActive();
                    }
                }
            }

#if UNITY_WEBGL
 		    obj.GenerateHeightmapSerial();
#else
            Thread thread = new Thread(obj.GenerateHeightmapThread);
            thread.Start();
#endif
        }

        public class TerrainHeightmapObject
        {
            public TerrainChunk terrainChunk;

            public HeightBiome heightBiome;
            public HeightBiome cliffHeightBiome1;
            public HeightBiome cliffHeightBiome2;

            public Rivers rivers;
            public GenerateSea generateSea;

            public void GenerateHeightmapSerial()
            {
                float[,] heightmap1 = new float[terrainChunk.settings.heightmapResolution, terrainChunk.settings.heightmapResolution];

                for (int zRes = 0; zRes < terrainChunk.settings.heightmapResolution; zRes++)
                {
                    for (int xRes = 0; xRes < terrainChunk.settings.heightmapResolution; xRes++)
                    {
                        float xCoordinate = terrainChunk.position.x + (float)xRes / (terrainChunk.settings.heightmapResolution - 1);
                        float zCoordinate = terrainChunk.position.z + (float)zRes / (terrainChunk.settings.heightmapResolution - 1);

                        heightmap1[zRes, xRes] = GetHeightJumpy(xCoordinate, zCoordinate);
                    }
                }

                terrainChunk.heightmap = heightmap1;

                if (rivers != null)
                {
                    rivers.GenerateH(terrainChunk);
                }
                if (generateSea != null)
                {
                    generateSea.GenerateH(terrainChunk);
                }

                terrainChunk.heightMapComplete = true;
            }

            public void GenerateHeightmapThread()
            {
                lock (terrainChunk.heightmapThreadLockObject)
                {
                    float[,] heightmap1 = new float[terrainChunk.settings.heightmapResolution, terrainChunk.settings.heightmapResolution];

                    for (int zRes = 0; zRes < terrainChunk.settings.heightmapResolution; zRes++)
                    {
                        for (int xRes = 0; xRes < terrainChunk.settings.heightmapResolution; xRes++)
                        {
                            float xCoordinate = (terrainChunk.position.x + (float)xRes / (terrainChunk.settings.heightmapResolution - 1)) * terrainChunk.settings.length;
                            float zCoordinate = (terrainChunk.position.z + (float)zRes / (terrainChunk.settings.heightmapResolution - 1)) * terrainChunk.settings.length;

                            heightmap1[zRes, xRes] = GetHeightJumpy(xCoordinate, zCoordinate);
                        }
                    }

                    terrainChunk.heightmap = heightmap1;

                    if (rivers != null)
                    {
                        rivers.GenerateH(terrainChunk);
                    }
                    if (generateSea != null)
                    {
                        generateSea.GenerateH(terrainChunk);
                    }

                    terrainChunk.heightMapComplete = true;
                }
            }

            float GetHeightJumpy(float xCoordinate, float zCoordinate)
            {
                float h = heightBiome.GetValue(xCoordinate, zCoordinate);
                float h1 = cliffHeightBiome1.GetValue(xCoordinate, zCoordinate);
                float h2 = cliffHeightBiome2.GetValue(xCoordinate, zCoordinate) - 0.5f;

                if (h2 < 0)
                {
                    h2 = 0;
                }

                if (h < h1)
                {
                    h = h - 0.3f * h2;
                }
                else
                {
                    h = h + 0.3f * h2;
                }

                if (h < 0)
                {
                    h = 0;
                }

                if (h > 1)
                {
                    h = 1;
                }

                return h;
            }
        }

        [System.Serializable]
        public class HeightBiome
        {
            public int seed;

            public float frequency = 1.0f;
            public float lacunarity = 2.0f;
            public int octaveCount = 6;
            public float persistence = 0.5f;

            public float scale = 2000f;

            public Vector2 offset = Vector2.zero;

            public AnimationCurve coverageCurve;
            List<float> animationCurveMapping = new List<float>();
            int curveMappingResolution = 20;

            [HideInInspector] public bool isCurveUsed;

            public void GetNoiseProvider()
            {
                isCurveUsed = true;

                if (coverageCurve == null)
                {
                    isCurveUsed = false;
                }
                else
                {
                    for (int i = 0; i <= curveMappingResolution; i++)
                    {
                        animationCurveMapping.Add(coverageCurve.Evaluate((1f * i) / curveMappingResolution));
                    }
                }
            }

            public float GetValue(float x, float y)
            {
                if (isCurveUsed == false)
                {
                    return NoiseExtensions.SNoise(
                        new float2(x - offset.x, y - offset.y) / scale,
                        frequency,
                        lacunarity,
                        persistence,
                        octaveCount,
                        1f,
                        offset,
                        seed
                    );
                }

                int imin = curveMappingResolution - 1;
                float xval = NoiseExtensions.SNoise(
                    new float2(x - offset.x, y - offset.y) / scale,
                    frequency,
                    lacunarity,
                    persistence,
                    octaveCount,
                    1f,
                    offset,
                    seed
                );

                for (int i = animationCurveMapping.Count - 1; i <= 0; i--)
                {
                    if (xval >= animationCurveMapping[i])
                    {
                        if (i < imin)
                        {
                            imin = i;
                        }
                    }
                }

                return GenericMath.Interpolate(
                    xval,
                    (1f * imin) / curveMappingResolution,
                    (1f * (imin + 1)) / curveMappingResolution,
                    animationCurveMapping[imin],
                    animationCurveMapping[imin + 1]
                );
            }
        }
    }
}
