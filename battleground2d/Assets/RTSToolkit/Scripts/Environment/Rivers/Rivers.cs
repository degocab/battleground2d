using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class Rivers : MonoBehaviour
    {
        public static Rivers active;

        // Input
        public Vector2 initialPosBarDirection = new Vector2(0f, 1f);
        public List<Vector2> origins = new List<Vector2>();
        public float randomSpeed = 0.1f;
        public float flowSpeed = 0.05f;
        public int maxMovementSteps = 5000;
        public int numberParticles = 400;
        public int numberSubSplits = 64;
        public Vector2 worldOffset = new Vector2(-5000f, 0f);
        public float worldRotation = -135f;
        public float worldScale = 1000f;
        public Vector2 randomBarInitializerX = new Vector2(-30f, 100f);
        public Vector2 randomBarInitializerY = new Vector2(-0.1f, 0.1f);
        public float minimumTerrainHeight = 45f;
        public int nShorePixels = 20;
        public float randomHeighAmount = 0.095f;
        public float randomHeighPosibility = 0.15f;
        public AnimationCurve shoreProfile;
        public int initialSeed = 48;

        // External global variables
        [HideInInspector] public DLASystem mainDLA;

        void Start()
        {
            Clean();
        }

        public void GenerateDLACheck()
        {
            CreateMainDLAIfDoesNotExist();
            mainDLA.GenerateDLACheck();
        }

        void CreateMainDLAIfDoesNotExist()
        {
            if (mainDLA == null)
            {
                mainDLA = new DLASystem();
                CopyToMainDLA();
            }
        }

        void CopyToMainDLA()
        {
            mainDLA.initialPosBarDirection = initialPosBarDirection;
            mainDLA.origins = new List<Vector3>();

            for (int i = 0; i < origins.Count; i++)
            {
                mainDLA.origins.Add(origins[i]);
            }

            mainDLA.randomSpeed = randomSpeed;
            mainDLA.flowSpeed = flowSpeed;
            mainDLA.maxMovementSteps = maxMovementSteps;
            mainDLA.numberParticles = numberParticles;
            mainDLA.numberSubSplits = numberSubSplits;
            mainDLA.worldOffset = worldOffset;
            mainDLA.worldRotation = worldRotation;
            mainDLA.worldScale = worldScale;
            mainDLA.initialSeed = initialSeed;
            mainDLA.randomBarInitializerX = randomBarInitializerX;
            mainDLA.randomBarInitializerY = randomBarInitializerY;
        }

        public void Clean()
        {
            CreateMainDLAIfDoesNotExist();
            mainDLA.Clean();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
            System.GC.Collect();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public static Rivers GetActive()
        {
            if (Rivers.active == null)
            {
                Rivers.active = UnityEngine.Object.FindObjectOfType<Rivers>();
            }

            return Rivers.active;
        }

        public void GenerateH(TerrainChunk terrainChunk)
        {
            Vector3 wPos = terrainChunk.GetChunkWorldPosition();
            int res = terrainChunk.settings.heightmapResolution;
            float[,] riverHeights = new float[res, res];
            float pixToPos = (1f * terrainChunk.settings.length) / res;

            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    riverHeights[i, j] = 1f;
                }
            }

            for (int k = 0; k < mainDLA.allPositionsBeg.Count; k++)
            {
                Vector2 allPositionsBegK = new Vector2(mainDLA.allPositionsBeg[k].x, mainDLA.allPositionsBeg[k].y);
                Vector2 allPositionsEndK = new Vector2(mainDLA.allPositionsEnd[k].x, mainDLA.allPositionsEnd[k].y);

                Vector2 wposBeg = allPositionsBegK - new Vector2(wPos.x, wPos.z) + worldOffset;
                Vector2 wposEnd = allPositionsEndK - new Vector2(wPos.x, wPos.z) + worldOffset;

                float wposx_min = wposBeg.x;
                if (wposEnd.x < wposBeg.x)
                {
                    wposx_min = wposEnd.x;
                }

                float wposy_min = wposBeg.y;
                if (wposEnd.y < wposBeg.y)
                {
                    wposy_min = wposEnd.y;
                }

                float wposx_max = wposBeg.x;
                if (wposEnd.x > wposBeg.x)
                {
                    wposx_max = wposEnd.x;
                }

                float wposy_max = wposBeg.y;
                if (wposEnd.y > wposBeg.y)
                {
                    wposy_max = wposEnd.y;
                }

                int i_min = (int)(wposx_min / pixToPos);
                int j_min = (int)(wposy_min / pixToPos);

                int i_max = (int)(wposx_max / pixToPos);
                int j_max = (int)(wposy_max / pixToPos);

                int dp = nShorePixels;

                if (i_max >= -dp && j_max >= -dp && i_min <= res + dp && j_min <= res + dp)
                {
                    for (int i1 = i_min - dp; i1 <= i_max + dp; i1++)
                    {
                        if (i1 >= 0 && i1 < res)
                        {
                            float i_sqr = (wposx_min - pixToPos * i1) * (wposx_min - pixToPos * i1);

                            for (int j1 = j_min - dp; j1 <= j_max + dp; j1++)
                            {
                                if (j1 >= 0 && j1 < res)
                                {
                                    float dposy = wposy_min - pixToPos * j1;
                                    float dist1 = Mathf.Sqrt(i_sqr + dposy * dposy) / pixToPos;

                                    dist1 = GenericMath.PointToLineSegmentDistance(pixToPos * i1, pixToPos * j1, wposBeg.x, wposBeg.y, wposEnd.x, wposEnd.y) / pixToPos;
                                    float interp = GenericMath.Interpolate(dist1, 0f, dp, 0f, 1f);

                                    if (i1 == 0)
                                    {
                                        float dist1_1 = GenericMath.PointToLineSegmentDistance(pixToPos * (i1 - 1), pixToPos * j1, wposBeg.x, wposBeg.y, wposEnd.x, wposEnd.y) / pixToPos;

                                        float interp_1 = GenericMath.Interpolate(dist1_1, 0f, dp, 0f, 1f);
                                        interp = 0.5f * (interp + interp_1);
                                    }

                                    if (i1 == res - 1)
                                    {
                                        float dist1_1 = GenericMath.PointToLineSegmentDistance(pixToPos * (i1 + 1), pixToPos * j1, wposBeg.x, wposBeg.y, wposEnd.x, wposEnd.y) / pixToPos;

                                        float interp_1 = GenericMath.Interpolate(dist1_1, 0f, dp, 0f, 1f);
                                        interp = 0.5f * (interp + interp_1);
                                    }

                                    if (j1 == 0)
                                    {
                                        float dist1_1 = GenericMath.PointToLineSegmentDistance(pixToPos * i1, pixToPos * (j1 - 1), wposBeg.x, wposBeg.y, wposEnd.x, wposEnd.y) / pixToPos;

                                        float interp_1 = GenericMath.Interpolate(dist1_1, 0f, dp, 0f, 1f);
                                        interp = 0.5f * (interp + interp_1);
                                    }

                                    if (j1 == res - 1)
                                    {
                                        float dist1_1 = GenericMath.PointToLineSegmentDistance(pixToPos * i1, pixToPos * (j1 + 1), wposBeg.x, wposBeg.y, wposEnd.x, wposEnd.y) / pixToPos;

                                        float interp_1 = GenericMath.Interpolate(dist1_1, 0f, dp, 0f, 1f);
                                        interp = 0.5f * (interp + interp_1);
                                    }

                                    if (mainDLA.posBegRandoms[k] < randomHeighPosibility)
                                    {
                                        float u = (1f - Mathf.Abs(terrainChunk.heightmap[j1, i1] - minimumTerrainHeight / terrainChunk.settings.height));
                                        if (interp < randomHeighAmount * u)
                                        {

                                            interp = randomHeighAmount * u;
                                        }
                                    }

                                    if (interp < riverHeights[j1, i1])
                                    {
                                        riverHeights[j1, i1] = interp;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (shoreProfile != null)
            {
                for (int i = 0; i < res; i++)
                {
                    for (int j = 0; j < res; j++)
                    {
                        riverHeights[i, j] = shoreProfile.Evaluate(riverHeights[i, j]);
                    }
                }
            }

            float min_t_height = minimumTerrainHeight / terrainChunk.settings.height;

            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    terrainChunk.heightmap[j, i] = GenericMath.Interpolate(riverHeights[j, i], 0f, 1f, min_t_height, terrainChunk.heightmap[j, i]);
                }
            }
        }
    }
}
