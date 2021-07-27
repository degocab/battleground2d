using UnityEngine;
using Unity.Mathematics;

namespace RTSToolkit
{
    public class TerrainProperties : MonoBehaviour
    {
        public static TerrainProperties active;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public static Vector3 RandomTerrainVector(Terrain ter)
        {
            Vector3 randVect = new Vector3(
                UnityEngine.Random.Range(0f, ter.terrainData.size.x) + ter.gameObject.transform.position.x,
                0f,
                UnityEngine.Random.Range(0f, ter.terrainData.size.z) + ter.gameObject.transform.position.z
            );

            float y1 = ter.SampleHeight(randVect);
            y1 = y1 + ter.transform.position.y;

            randVect = new Vector3(randVect.x, y1, randVect.z);
            return randVect;
        }

        public static Vector3 RandomTerrainVectorProc(Vector2i tile)
        {
            Terrain ter = GenerateTerrain.active.GetTerrainFromTile(tile);

            if (ter != null)
            {
                return RandomTerrainVector(ter);
            }

            return Vector3.zero;
        }

        public static bool IsVectorOverTerrain(Vector3 origin, Terrain ter1)
        {
            if (origin.x <= ter1.gameObject.transform.position.x)
            {
                return false;
            }

            if (origin.x >= (ter1.terrainData.size.x + ter1.gameObject.transform.position.x))
            {
                return false;
            }

            if (origin.z <= ter1.gameObject.transform.position.z)
            {
                return false;
            }

            if (origin.z >= (ter1.terrainData.size.z + ter1.gameObject.transform.position.z))
            {
                return false;
            }

            return true;
        }

        public static Vector3 RandomTerrainVectorCircle(Vector3 origin, float circleSize, Terrain ter1, int nAttempts)
        {
            for (int i = 0; i < nAttempts; i++)
            {
                Vector2 cir2 = UnityEngine.Random.insideUnitCircle * circleSize;
                Vector3 cir3 = (new Vector3(origin.x, 0f, origin.z)) + (new Vector3(cir2.x, 0f, cir2.y));

                if (IsVectorOverTerrain(cir3, ter1))
                {
                    return TerrainVector(cir3, ter1);
                }
            }

            return origin;
        }

        public static Vector3 RandomTerrainVectorOnCircleProc(Vector3 origin, float circleSize)
        {
            Vector2 cir2 = (UnityEngine.Random.insideUnitCircle.normalized) * circleSize;
            Vector3 cir3 = (new Vector3(origin.x, 0f, origin.z)) + (new Vector3(cir2.x, 0f, cir2.y));
            return TerrainVectorProc(cir3);
        }

        public static Vector3 RandomTerrainVectorCircleProc(Vector3 origin, float circleSize)
        {
            Vector2 cir2 = UnityEngine.Random.insideUnitCircle * circleSize;
            Vector3 cir3 = (new Vector3(origin.x, 0f, origin.z)) + (new Vector3(cir2.x, 0f, cir2.y));
            return TerrainVectorProc(cir3);
        }

        public static Vector3 RandomTerrainVectorCirclePowProc(Vector3 origin, float circleSize, float power)
        {
            Vector2 randCircle = UnityEngine.Random.insideUnitCircle;
            Vector2 cir2 = randCircle * (Mathf.Pow(randCircle.magnitude, power) * circleSize);
            Vector3 cir3 = (new Vector3(origin.x, 0f, origin.z)) + (new Vector3(cir2.x, 0, cir2.y));
            return TerrainVectorProc(cir3);
        }

        public static Vector3 TerrainVector(Vector3 origin, Terrain ter1)
        {
            if (ter1 == null)
            {
                return origin;
            }

            Vector3 planeVect = new Vector3(origin.x, 0f, origin.z);
            float y1 = ter1.SampleHeight(planeVect);
            y1 = y1 + ter1.transform.position.y;

            Vector3 tv = new Vector3(origin.x, y1, origin.z);
            return tv;
        }

        public static Vector3 TerrainVector1(Vector3 origin, float[,] heights, float xbeg, float zbeg, float yoffset)
        {
            if (heights == null)
            {
                return origin;
            }

            float y1 = GetTerrainHeight(
                heights,
                xbeg,
                zbeg,
                GenerateTerrain.active.length,
                origin.x,
                origin.z
            ) * GenerateTerrain.active.height;

            y1 = y1 + yoffset;

            Vector3 tv = new Vector3(origin.x, y1, origin.z);
            return tv;
        }

        public static float GetTerrainHeight(float[,] hmap, float xbeg, float ybeg, float world_size, float world_point_x, float world_point_y)
        {
            float h = 0;

            int res = hmap.GetLength(0);
            float rel_x = world_point_x - xbeg;
            float rel_y = world_point_y - ybeg;

            int ix = (int)((rel_x / world_size) * res);
            int iy = (int)((rel_y / world_size) * res);

            if ((ix >= 0) && (iy >= 0))
            {
                if ((ix < res) && (iy < res))
                {
                    if (((ix + 1) < res) && ((iy + 1) < res))
                    {
                        float h00 = hmap[iy, ix];
                        float h01 = hmap[iy, ix + 1];
                        float h10 = hmap[iy + 1, ix];
                        float h11 = hmap[iy + 1, ix + 1];

                        rel_x = (rel_x - world_size * ix / res) * res / world_size; // / world_size;
                        rel_y = (rel_y - world_size * iy / res) * res / world_size; // / world_size;

                        float xlow_h = GenericMath.Interpolate(rel_x, 0, 1, h00, h01);
                        float xup_h = GenericMath.Interpolate(rel_x, 0, 1, h10, h11);

                        h = GenericMath.Interpolate(rel_y, 0, 1, xlow_h, xup_h);
                    }
                }
            }

            return h;
        }

        public static Terrain GetTerrainBellow(Vector3 origin)
        {
            return GenerateTerrain.active.GetTerrainBellow(origin);
        }

        public static int GetTerrainIndexBellow(Vector3 origin)
        {
            return GenerateTerrain.active.GetTerrainIndexBellow(origin);
        }

        public static Vector3 TerrainVectorProc(Vector3 origin)
        {
            return TerrainVector(origin, GetTerrainBellow(origin));
        }

        public static Vector3 TerrainVectorProc1(Vector3 origin)
        {
            int i = GetTerrainIndexBellow(origin);

            if (i >= 0)
            {
                return TerrainVector1(
                    origin,
                    GenerateTerrain.active.loadedTerrainsHeightmaps[i],
                    GenerateTerrain.active.loadedTileIndices[i].x,
                    GenerateTerrain.active.loadedTileIndices[i].z,
                    0
                );
            }

            return origin;
        }

        public static float HeightFromTerrain(Vector3 origin)
        {
            Vector3 tv = TerrainVectorProc(origin);
            return (origin.y - tv.y);
        }

        public float HeightFromTerrain(Vector3 origin, Terrain ter1)
        {
            Vector3 tv = TerrainVector(origin, ter1);
            return (origin.y - tv.y);
        }

        public static bool HasNavigation(Vector3 pos)
        {
            return GenerateTerrain.active.HasNavigation(pos);
        }

        public float TerrainSteepness(Vector3 point1, float markRadius)
        {
            Terrain ter = GetTerrainBellow(point1);

            if (ter == null)
            {
                return 0f;
            }

            return TerrainProperties.TerrainSteepness(ter, point1, markRadius);
        }

        public static float TerrainSteepness(Terrain ter, Vector3 point1, float markRadius)
        {
            Vector3 point = point1 - ter.transform.position;

            float baseRes = ter.terrainData.baseMapResolution;
            float xTer = ter.terrainData.size.x;
            float zTer = ter.terrainData.size.z;

            float norm_markRadius = markRadius / xTer;

            float ttx = point.x / xTer;
            float ttz = point.z / zTer;

            float tileXbase = ttx * baseRes;
            float tileZbase = ttz * baseRes;
            float delta_tileRadius = norm_markRadius * baseRes;

            int tilexmin = (int)(tileXbase - delta_tileRadius);
            int tilexmax = (int)(tileXbase + delta_tileRadius);
            int tilezmin = (int)(tileZbase - delta_tileRadius);
            int tilezmax = (int)(tileZbase + delta_tileRadius);

            float highestSteepness = 0f;

            for (int i = tilexmin; i <= tilexmax; i++)
            {
                if (i >= 0)
                {
                    if (i < baseRes)
                    {
                        for (int j = tilezmin; j <= tilezmax; j++)
                        {
                            if (j >= 0)
                            {
                                if (j < baseRes)
                                {
                                    // if inside the circle		
                                    if (
                                        ((1f * i / baseRes - ttx) * (1f * i / baseRes - ttx)) +
                                        ((1f * j / baseRes - ttz) * (1f * j / baseRes - ttz))
                                        <
                                        norm_markRadius * norm_markRadius
                                    )
                                    {
                                        float steepness = ter.terrainData.GetSteepness(1f * i / baseRes, 1f * j / baseRes);
                                        if (highestSteepness < steepness)
                                        {
                                            highestSteepness = steepness;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return highestSteepness;
        }

        public static float TerrainSteepness(Terrain ter, Vector3 point1)
        {
            Vector3 point = point1 - ter.transform.position;

            float ttx = point.x / ter.terrainData.size.x;
            float ttz = point.z / ter.terrainData.size.z;

            return (ter.terrainData.GetSteepness(ttx, ttz));
        }

        public static Vector3 TerrainNormalVectorProc(Vector3 origin)
        {
            return TerrainNormalVector(origin, GetTerrainBellow(origin));
        }

        public static Vector3 TerrainNormalVector(Vector3 origin, Terrain ter)
        {
            Vector3 point = origin - ter.transform.position;

            float ttx = point.x / ter.terrainData.size.x;
            float ttz = point.z / ter.terrainData.size.z;

            return (ter.terrainData.GetInterpolatedNormal(ttx, ttz));
        }

        public static Vector3 GetFlowVector(Terrain ter, Vector3 origin)
        {
            Vector3 normal = TerrainNormalVector(origin, ter);
            float steepness = TerrainSteepness(ter, origin);

            if (steepness > 0.001f)
            {
                Vector3 normalXZ = new Vector3(normal.x, 0f, normal.z);
                Vector3 perp_normal = Vector3.Cross(normal, normalXZ);

                float velocity = GenericMath.InterpolateClamped(steepness, 0f, 90f, 0f, 1f);
                return GenericMath.RotAround(-90f, normal, perp_normal).normalized * velocity;
            }

            return Vector3.zero;
        }

        public static int GetTerrainSeed(Terrain ter)
        {
            float f_seed = NoiseExtensions.SNoise(
                new float2(ter.transform.position.x / Mathf.PI, ter.transform.position.z / Mathf.PI),
                1f,
                2f,
                0.5f,
                6,
                1f,
                float2.zero,
                0
            );

            return ((int)(f_seed * 2147483646f * 0.5f));
        }

        public static Vector3 GetFarestDestinationPoint(Vector3 orig, Vector3 target, int nIter)
        {
            Vector3[] terPositions = GetNeighbourTerrainMidPoints(Camera.main.transform.position);
            Vector3 nearestTerrainPos = Vector3.zero;
            float rSmallest = 0f;

            for (int i = 0; i < terPositions.Length; i++)
            {
                float r = (terPositions[i] - target).magnitude;
                if (r > rSmallest)
                {
                    rSmallest = r;
                }
            }

            for (int i = 0; i < terPositions.Length; i++)
            {
                float r = (terPositions[i] - target).magnitude;
                if (r < rSmallest)
                {
                    rSmallest = r;
                    nearestTerrainPos = terPositions[i];
                }
            }

            float halfLength = 0.5f * GenerateTerrain.active.length;
            Vector3 bestPos = nearestTerrainPos;

            for (int i = 0; i < nIter; i++)
            {
                Vector3 randPos = new Vector3(
                    UnityEngine.Random.Range((nearestTerrainPos.x - halfLength), (nearestTerrainPos.x + halfLength)),
                    0f,
                    UnityEngine.Random.Range((nearestTerrainPos.z - halfLength), (nearestTerrainPos.z + halfLength))
                );

                float r = (randPos - target).magnitude;
                if (r < rSmallest)
                {
                    rSmallest = r;
                    bestPos = randPos;
                }
            }

            if (HasNavigation(bestPos) == false)
            {
                return orig;
            }

            return TerrainVectorProc(bestPos);
        }

        public static Vector3[] GetNeighbourTerrainMidPoints(Vector3 pos)
        {
            Vector3[] mpts = new Vector3[9];
            Vector3 mpt = GetTerrainMidPoint(pos);
            float length = GenerateTerrain.active.length;

            int k = 0;

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    mpts[k] = mpt - new Vector3(length * i, 0, length * j);
                    k++;
                }
            }

            return mpts;
        }

        public static Vector3 GetTerrainMidPoint(Vector3 pos)
        {
            float length = GenerateTerrain.active.length;
            int x_neg = 0;
            if (pos.x < 0)
            {
                x_neg = -1;
            }
            int z_neg = 0;
            if (pos.z < 0)
            {
                z_neg = -1;
            }
            float x = length * (((int)(pos.x / length)) + x_neg) + 0.5f * length;
            float z = length * (((int)(pos.z / length)) + z_neg) + 0.5f * length;

            return (new Vector3(x, 0f, z));
        }
    }
}
