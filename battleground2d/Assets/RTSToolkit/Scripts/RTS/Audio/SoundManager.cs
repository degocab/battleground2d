using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager active;

        public List<AudioClipPars> audioClips;
        [HideInInspector] public List<AudioInstancePars> audioInstances = new List<AudioInstancePars>();
        public float zoneDistancesFactor = 1f;
        public float numberToPlaceScaler = 1f;

        bool isOnRuntime = false;
        Transform camTransform;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            isOnRuntime = true;
            camTransform = Camera.main.transform;
        }

        float tInstancesLoop = 0f;
        float tCullingLoop = 0.05f;

        void Update()
        {
            float dt = Time.deltaTime;

            tInstancesLoop = tInstancesLoop + dt;
            if (tInstancesLoop > 0.1f)
            {
                InstancesLoop();
                tInstancesLoop = 0f;
            }

            tCullingLoop = tCullingLoop + dt;
            if (tCullingLoop > 0.1f)
            {
                CullingLoop();
                tCullingLoop = 0f;
            }
        }

        public void InitializeOnTerrain(Terrain ter)
        {
            Random.InitState(TerrainProperties.GetTerrainSeed(ter));
            DistributeAudioSources(ter);
        }

        public static SoundManager GetActive()
        {
            if (SoundManager.active == null)
            {
                SoundManager.active = UnityEngine.Object.FindObjectOfType<SoundManager>();
            }

            return SoundManager.active;
        }

        void DistributeAudioSources(Terrain ter)
        {
            for (int i = 0; i < audioClips.Count; i++)
            {
                AudioClipPars acp = audioClips[i];
                int ntp = (int)(acp.placeDensity * ter.terrainData.size.x * ter.terrainData.size.z * numberToPlaceScaler);

                for (int j = 0; j < ntp; j++)
                {

                    Vector3 randomTerrVect = TerrainProperties.RandomTerrainVector(ter);

                    if (randomTerrVect.y > GenerateTerrain.GetActive().water.transform.position.y)
                    {
                        GameObject go = new GameObject();
                        go.transform.position = randomTerrVect;
                        go.name = "sound_" + acp.clip.name + "_" + i.ToString() + "_" + j.ToString();
                        go.transform.SetParent(ter.gameObject.transform);

                        if (isOnRuntime == false)
                        {
                            go.SetActive(false);
                        }

                        AudioSource as1 = go.AddComponent<AudioSource>();
                        as1.clip = acp.clip;
                        as1.volume = acp.volume;
                        as1.spatialBlend = 1f;
                        as1.minDistance = zoneDistancesFactor * acp.typeZoneDistanceFactor * 1f;
                        as1.maxDistance = zoneDistancesFactor * acp.typeZoneDistanceFactor * 100f;
                        as1.playOnAwake = false;

                        AudioInstancePars aip = new AudioInstancePars();
                        aip.aSource = as1;
                        aip.clipPars = acp;
                        aip.position = randomTerrVect;
                        aip.totalToDelay = Random.Range(acp.clip.length, acp.maxDelay * acp.clip.length);
                        aip.delayed = Random.Range(0f, aip.totalToDelay);
                        aip.terrain = ter;
                        aip.isActive = false;

                        audioInstances.Add(aip);
                    }
                }
            }
        }

        public void Clean()
        {
            for (int i = 0; i < audioInstances.Count; i++)
            {
                if (audioInstances[i].aSource != null)
                {
                    if (audioInstances[i].aSource.gameObject != null)
                    {
                        Destroy(audioInstances[i].aSource.gameObject);
                    }
                }
            }

            audioInstances.Clear();
        }

        public void UnsetFromTerrain(Terrain ter)
        {
            List<AudioInstancePars> removals = new List<AudioInstancePars>();
            for (int i = 0; i < audioInstances.Count; i++)
            {
                if (audioInstances[i].terrain == ter)
                {
                    if (audioInstances[i].aSource != null)
                    {
                        if (audioInstances[i].aSource.gameObject != null)
                        {
                            Destroy(audioInstances[i].aSource.gameObject);
                            removals.Add(audioInstances[i]);
                        }
                    }
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                audioInstances.Remove(removals[i]);
            }
        }

        public bool IsOnTerrain(Terrain ter)
        {
            for (int i = 0; i < audioInstances.Count; i++)
            {
                if (audioInstances[i].terrain == ter)
                {
                    return true;
                }
            }

            return false;
        }

        void InstancesLoop()
        {
            for (int i = 0; i < audioInstances.Count; i++)
            {
                AudioInstancePars inst = audioInstances[i];
                inst.delayed = inst.delayed + 0.1f;

                if (inst.delayed > inst.totalToDelay)
                {
                    inst.delayed = 0f;
                    inst.totalToDelay = Random.Range(inst.clipPars.clip.length, inst.clipPars.maxDelay * inst.clipPars.clip.length);

                    if (inst.isActive)
                    {
                        if (inst.terrain.gameObject.activeSelf)
                        {
                            inst.aSource.Play();
                        }
                    }
                }
            }
        }

        void CullingLoop()
        {
            for (int i = 0; i < audioInstances.Count; i++)
            {
                AudioInstancePars inst = audioInstances[i];
                float dist = (inst.position - camTransform.position).magnitude;

                if (inst.isActive)
                {
                    if (dist > 3f * inst.aSource.maxDistance)
                    {
                        inst.aSource.gameObject.SetActive(false);
                        inst.isActive = false;
                    }
                }
                else
                {
                    if (inst.aSource != null)
                    {
                        if (dist <= 3f * inst.aSource.maxDistance)
                        {
                            inst.aSource.gameObject.SetActive(true);
                            inst.isActive = true;
                        }
                    }
                }
            }
        }

        [System.Serializable]
        public class AudioClipPars
        {
            public AudioClip clip;
            public float placeDensity = 2.5e-7f;
            public float volume = 1f;
            public float maxDelay = 1.1f;
            public float typeZoneDistanceFactor = 1f;
        }

        [System.Serializable]
        public class AudioInstancePars
        {
            public AudioSource aSource;
            public Vector3 position;
            public AudioClipPars clipPars;
            public float totalToDelay = 0f;
            public float delayed = 0f;
            public Terrain terrain;
            public bool isActive;
        }
    }
}
