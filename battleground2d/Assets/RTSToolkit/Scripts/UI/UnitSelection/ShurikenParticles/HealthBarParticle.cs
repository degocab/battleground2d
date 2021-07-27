using UnityEngine;

namespace RTSToolkit
{
    public class HealthBarParticle : MonoBehaviour
    {
        public static HealthBarParticle active;

        public Shader healthBarShader;

        ParticleSystem ptsHBarRed;
        ParticleSystem ptsHBarGreen;

        public Texture2D healthBarTexture;

        Transform camTransform;

        public Color healthy;
        public Color damaged;

        public float minParticleSize = 0.01f;
        public bool useHealthBars = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            camTransform = Camera.main.transform;
            CreateParticleSystem(0);
            CreateParticleSystem(1);
        }

        void CreateParticleSystem(int i_mod)
        {
            GameObject ptSystemGo = new GameObject();

            if (i_mod == 0)
            {
                ptSystemGo.name = "HealthBarRed";
            }

            if (i_mod == 1)
            {
                ptSystemGo.name = "HealthBarGreen";
            }

            ParticleSystem ptSystem = ptSystemGo.AddComponent<ParticleSystem>();

            ParticleSystem.EmissionModule em = ptSystem.emission;
            em.enabled = false;

            ParticleSystemRenderer psr = ptSystem.GetComponent<ParticleSystemRenderer>();
            psr.sortMode = ParticleSystemSortMode.Distance;
            psr.minParticleSize = minParticleSize;
            psr.maxParticleSize = 10f;

            ParticleSystem.MainModule pmain = ptSystem.main;
            pmain.maxParticles = 100000;

            ptSystem.GetComponent<Renderer>().material = new Material(healthBarShader);

            if (i_mod == 0)
            {
                ptSystem.GetComponent<Renderer>().material.mainTexture = healthBarTexture;
                ptsHBarRed = ptSystem;
            }

            if (i_mod == 1)
            {
                ptSystem.GetComponent<Renderer>().material.mainTexture = healthBarTexture;
                ptsHBarGreen = ptSystem;
            }
        }

        void Update()
        {
            SelectionManager sm = SelectionManager.active;
            int n = sm.selectedGoPars.Count;

            if (useHealthBars && (n > 0))
            {
                if (ptsHBarRed.gameObject.activeSelf == false)
                {
                    ptsHBarRed.gameObject.SetActive(true);
                }

                if (ptsHBarGreen.gameObject.activeSelf == false)
                {
                    ptsHBarGreen.gameObject.SetActive(true);
                }

                ParticleSystem.Particle[] pool0 = new ParticleSystem.Particle[n];
                ParticleSystem.Particle[] pool1 = new ParticleSystem.Particle[n];

                for (int i = 0; i < n; i++)
                {
                    UnitPars up = sm.selectedGoPars[i];
                    SetParticle(ref pool0, i, 0, up);
                    SetParticle(ref pool1, i, 1, up);
                }

                ptsHBarRed.SetParticles(pool0, n);
                ptsHBarGreen.SetParticles(pool1, n);
            }
            else
            {
                if (ptsHBarRed.gameObject.activeSelf)
                {
                    ptsHBarRed.SetParticles(null, 0);
                    ptsHBarRed.gameObject.SetActive(false);
                }

                if (ptsHBarGreen.gameObject.activeSelf)
                {
                    ptsHBarGreen.SetParticles(null, 0);
                    ptsHBarGreen.gameObject.SetActive(false);
                }
            }
        }

        void SetParticle(ref ParticleSystem.Particle[] pool, int i, int mod, UnitPars up)
        {
            float healthFrac = up.health / up.maxHealth;
            float size = up.unitParsType.unitSize;

            pool[i].rotation = 0f;
            pool[i].angularVelocity = 0f;
            pool[i].velocity = Vector3.zero;
            pool[i].startLifetime = 10f;
            pool[i].remainingLifetime = 9f;

            if (mod == 0)
            {
                pool[i].position =
                    up.transform.position +
                    up.unitParsType.unitCenter +
                    new Vector3(0, 0.5f * size, 0) +
                    (healthFrac) * 1f * size * (GenericMath.GetLOSPerpendicular(camTransform, 270f));

                pool[i].startColor = damaged;
                pool[i].startSize3D = 2f * (new Vector3((1f - healthFrac) * size, size, size));
            }
            if (mod == 1)
            {
                pool[i].position =
                    up.transform.position +
                    up.unitParsType.unitCenter +
                    new Vector3(0, 0.5f * size, 0) +
                    (1f - healthFrac) * 1f * size * (GenericMath.GetLOSPerpendicular(camTransform, 90f));

                pool[i].startColor = healthy;
                pool[i].startSize3D = 2f * (new Vector3(healthFrac * size, size, size));
            }
        }
    }
}
