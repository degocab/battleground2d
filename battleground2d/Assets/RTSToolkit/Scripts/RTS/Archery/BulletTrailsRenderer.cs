using UnityEngine;

namespace RTSToolkit
{
    public class BulletTrailsRenderer : MonoBehaviour
    {
        public static BulletTrailsRenderer active;
        public ParticleSystem pSystem;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void Update()
        {

        }

        public void EmitBetween(Vector3 begTrail, Vector3 endTrail, int n)
        {
            Vector3 vel = 0f * (endTrail - begTrail).normalized;

            for (int i = 0; i < n; i++)
            {
                float rand = Random.value;

                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = rand * begTrail + (1f - rand) * endTrail;
                emitParams.velocity = vel;
                pSystem.Emit(emitParams, 1);
            }
        }
    }
}
