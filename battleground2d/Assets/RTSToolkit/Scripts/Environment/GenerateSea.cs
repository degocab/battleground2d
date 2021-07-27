using UnityEngine;

namespace RTSToolkit
{
    public class GenerateSea : MonoBehaviour
    {
        public static GenerateSea active;

        public Vector2 seaCenter = new Vector2(-180000f, -35000f);
        public float seaRadius = 185000f;
        public float outerRadius = 185500f;

        public float dropTerrainAmount = 100f;

        [HideInInspector] public Terrain terr;
        public float[,] origHeights;

        public float worldRotation = -45f;
        public Vector2 worldOffset = new Vector2(-5000f, 0f);

        void Start()
        {

        }

        public static GenerateSea GetActive()
        {
            if (GenerateSea.active == null)
            {
                GenerateSea.active = UnityEngine.Object.FindObjectOfType<GenerateSea>();
            }
            return GenerateSea.active;
        }

        public void GenerateH(TerrainChunk terrainChunk)
        {
            Vector3 offset = terrainChunk.GetChunkWorldPosition();

            int res = terrainChunk.settings.heightmapResolution;
            float pixToPos = (1f * terrainChunk.settings.length) / res;

            float tsizey = terrainChunk.settings.height;
            Vector2 seaCenterRot = seaCenter.Rotate(worldRotation) + worldOffset;

            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    float wposx = pixToPos * i + offset.x;
                    float wposy = pixToPos * j + offset.z;

                    float dx = wposx - seaCenterRot.x;
                    float dy = wposy - seaCenterRot.y;

                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float rHeight = terrainChunk.heightmap[j, i];
                    float oHeight = rHeight;

                    if (r < outerRadius)
                    {
                        if (r < seaRadius)
                        {
                            oHeight = rHeight - dropTerrainAmount / tsizey;
                        }
                        else
                        {
                            oHeight = GenericMath.Interpolate(r, seaRadius, outerRadius, rHeight - dropTerrainAmount / tsizey, rHeight);

                            if (i == 0)
                            {
                                float dx1 = pixToPos * (i - 1) + offset.x - seaCenterRot.x;
                                float dy1 = pixToPos * j + offset.z - seaCenterRot.y;

                                float r1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);

                                float oHeight1 = GenericMath.Interpolate(r1, seaRadius, outerRadius, rHeight - dropTerrainAmount / tsizey, rHeight);
                                oHeight = 0.5f * (oHeight + oHeight1);
                            }

                            if (i == res - 1)
                            {
                                float dx1 = pixToPos * (i + 1) + offset.x - seaCenterRot.x;
                                float dy1 = pixToPos * j + offset.z - seaCenterRot.y;

                                float r1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);

                                float oHeight1 = GenericMath.Interpolate(r1, seaRadius, outerRadius, rHeight - dropTerrainAmount / tsizey, rHeight);
                                oHeight = 0.5f * (oHeight + oHeight1);
                            }

                            if (j == 0)
                            {
                                float dx1 = pixToPos * i + offset.x - seaCenterRot.x;
                                float dy1 = pixToPos * (j - 1) + offset.z - seaCenterRot.y;

                                float r1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);

                                float oHeight1 = GenericMath.Interpolate(r1, seaRadius, outerRadius, rHeight - dropTerrainAmount / tsizey, rHeight);
                                oHeight = 0.5f * (oHeight + oHeight1);
                            }

                            if (j == res - 1)
                            {
                                float dx1 = pixToPos * i + offset.x - seaCenterRot.x;
                                float dy1 = pixToPos * (j + 1) + offset.z - seaCenterRot.y;

                                float r1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);

                                float oHeight1 = GenericMath.Interpolate(r1, seaRadius, outerRadius, rHeight - dropTerrainAmount / tsizey, rHeight);
                                oHeight = 0.5f * (oHeight + oHeight1);
                            }
                        }
                    }

                    terrainChunk.heightmap[j, i] = oHeight;
                }
            }
        }
    }
}
