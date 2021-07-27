using Unity.Mathematics;

namespace RTSToolkit
{
    public static class NoiseExtensions
    {
        public static float SNoise(
            float2 v,
            float frequency,
            float lacunarity,
            float persistence,
            int octaveCount,
            float scale,
            float2 offset,
            int seed
        )
        {
            v *= scale;
            v += offset;

            float total = 0f;
            float amplitude = 1f;
            float totalAmplitude = 0f;

            for (float i = 0; i < octaveCount; i++)
            {
                float n = noise.snoise(new float3(v.x * frequency, v.y * frequency, 10f * seed));
                total += n * amplitude;
                totalAmplitude += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return (total / totalAmplitude) * 0.5f + 0.5f;
        }
    }
}
