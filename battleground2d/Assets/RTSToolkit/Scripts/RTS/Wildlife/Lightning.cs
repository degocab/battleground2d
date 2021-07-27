using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class Lightning : MonoBehaviour
    {
        public static Lightning active;

        float velocity = 0.05f;
        float startingPointForce = 0f;
        float randomVelocity = 0.15f;

        List<Lightning.LightingStrike> strikes = new List<Lightning.LightingStrike>();

        public GameObject lightningPrefab;
        public GameObject lightningLightPrefab;

        public List<AudioClip> thunderSounds;
        float lastSoundTime = 0f;

        List<AreaDecayLightning> areaDecayLightnings = new List<AreaDecayLightning>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        int numberOfBranches = 0;

        public void CalculatePoints(Vector3 stPt, Vector3 dir, int num, float velocityIn, float startingPointForceIn, float randomVelocityIn, Vector3 branchDecVect, float strikeDelay)
        {
            numberOfBranches = 0;
            velocity = velocityIn;
            startingPointForce = startingPointForceIn;
            randomVelocity = randomVelocityIn;
            CalculatePointsRecursive(stPt, dir, num, branchDecVect, 0, strikeDelay);
        }

        public void CalculatePointsRecursive(Vector3 stPt, Vector3 dir, int num, Vector3 branchDecVect, int branchOrder, float strikeDelay)
        {
            Lightning.LightingStrike strike = new Lightning.LightingStrike();

            int branchOrder2 = branchOrder + 1;
            numberOfBranches = numberOfBranches + 1;

            strike.n = num;
            strike.branchOrder = branchOrder;
            strike.points = new List<Vector3>();
            strike.points.Add(stPt);

            for (int i = 1; i < strike.n; i++)
            {
                Vector3 directionForceOffset = (dir + branchDecVect).normalized * velocity;
                Vector3 startingPointForceOffset = (strike.points[i - 1] - stPt).normalized * startingPointForce;
                Vector3 randomForceOffset = Random.insideUnitSphere * randomVelocity;

                Vector3 nextPosition = strike.points[i - 1] + directionForceOffset + startingPointForceOffset + randomForceOffset;

                strike.points.Add(nextPosition);
                float rand1 = Random.value;

                if (rand1 < 3f / (strike.n * Mathf.Pow(1f * numberOfBranches, 0.5f)))
                {
                    if (i < strike.n - 1)
                    {
                        if (branchOrder2 < 3)
                        {
                            CalculatePointsRecursive(strike.points[i - 1], dir, (int)(0.5f * (strike.n - i)), Random.insideUnitSphere * 500f * velocity, branchOrder2, strikeDelay);
                        }
                    }
                }

                branchDecVect = 0.9f * branchDecVect;
            }

            strikes.Add(strike);

            strike.startTime = Time.time + strikeDelay;
            strike.strikeDuration = 0.5f;

            AddPositons(strike);
        }

        void AddPositons(Lightning.LightingStrike strike)
        {
            for (int i = 0; i < strike.points.Count; i++)
            {
                strike.pointsRendered.Add(strike.points[i]);
                strike.pointTimes.Add((1f * i) / strike.strikeDuration);
            }
        }

        public void AddAreaDecayLightning(AreaDecayLightning areaDecayLightning)
        {
            areaDecayLightnings.Add(areaDecayLightning);
        }

        void RenderStrikes()
        {
            for (int i = 0; i < strikes.Count; i++)
            {
                if (Time.time > strikes[i].startTime)
                {
                    RenderStrike(strikes[i]);
                }
            }
        }

        void RenderStrike(Lightning.LightingStrike strike)
        {
            if (strike.isRendered == false)
            {
                strike.isRendered = true;
                strike.go = (GameObject)Instantiate(lightningPrefab);
                strike.lr = strike.go.GetComponent<LineRenderer>();

                if (strike.branchOrder == 0)
                {
                    strike.lightGo = (GameObject)Instantiate(lightningLightPrefab);
                    strike.lght = strike.lightGo.GetComponent<Light>();

                    if (strike.pointsRendered.Count > 0)
                    {
                        if (Time.time - lastSoundTime > 0.1f)
                        {
                            PlayRandomLightingSound(strike.pointsRendered[strike.pointsRendered.Count / 2]);
                            lastSoundTime = Time.time;
                        }
                    }
                }

                Vector3[] pointsArray = strike.pointsRendered.ToArray();
                strike.lr.positionCount = pointsArray.Length;
                strike.lr.SetPositions(pointsArray);

                strike.lr.material.SetColor("_EmissionColor", Color.blue);
                strike.lr.material.renderQueue = 2800;
            }

            float shortestTime = Mathf.Min(Time.time - strike.startTime, strike.startTime + strike.strikeDuration - Time.time);
            float lineWidth = Mathf.Pow(shortestTime / strike.strikeDuration, 3);

            strike.lr.startWidth = 5f * lineWidth;
            strike.lr.endWidth = 5f * lineWidth;

            if (strike.branchOrder == 0)
            {
                int imed = (int)((Time.time - strike.startTime) * strike.pointsRendered.Count / strike.strikeDuration);

                if (imed < 0)
                {
                    imed = 0;
                }
                else if (imed >= strike.pointsRendered.Count)
                {
                    imed = 0;
                }

                if (strike.pointsRendered.Count > 0)
                {
                    strike.lightGo.transform.position = strike.pointsRendered[imed];
                    float intensity = Mathf.Pow(2f * shortestTime / strike.strikeDuration, 3);

                    if (intensity < 0)
                    {
                        intensity = 0;
                    }

                    strike.lght.intensity = intensity;
                    SearchAndIgniteFires(strike.pointsRendered[imed]);
                }
            }
        }

        void RemoveOldStrikes()
        {
            List<Lightning.LightingStrike> removals = new List<Lightning.LightingStrike>();
            float time = Time.time;

            for (int i = 0; i < strikes.Count; i++)
            {
                Lightning.LightingStrike strike = strikes[i];

                if (time > (strike.startTime + strike.strikeDuration))
                {
                    removals.Add(strike);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                Lightning.LightingStrike strike = removals[i];

                strike.points.Clear();
                strike.pointsRendered.Clear();
                strike.pointTimes.Clear();

                strikes.Remove(strike);
                Destroy(strike.go);

                if (strike.branchOrder == 0)
                {
                    Destroy(strike.lightGo);
                }
            }
        }

        void UpdateAreaDecayLightnings()
        {
            for (int i = 0; i < areaDecayLightnings.Count; i++)
            {
                AreaDecayLightning adl = areaDecayLightnings[i];

                float probability = GenericMath.Interpolate(adl.timePassed, 0, adl.duration, 1, 0);
                float powerProbability = Mathf.Pow(probability, 8);
                float progressToAdd = powerProbability * adl.strikesPerSecond * deltaTime;

                adl.updateProgress = adl.updateProgress + progressToAdd;
                int intUpdateProgress = (int)adl.updateProgress;

                for (int j = 0; j < intUpdateProgress; j++)
                {
                    Vector3 pos = TerrainProperties.RandomTerrainVectorCirclePowProc(adl.areaCenter, 500, 1.5f) + new Vector3(0f, Random.Range(0.5f, 40f), 0f);
                    Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    Vector3 lookingVector = randomRotation * Vector3.forward;
                    CalculatePoints(pos, lookingVector, 500, 0.02f, 0.03f, 0.15f, Vector3.zero, 0f);
                }

                adl.updateProgress = adl.updateProgress - intUpdateProgress;
                adl.timePassed = adl.timePassed + deltaTime;
            }

            if (areaDecayLightnings.Count > 0)
            {
                List<AreaDecayLightning> removals = new List<AreaDecayLightning>();

                for (int i = 0; i < areaDecayLightnings.Count; i++)
                {
                    AreaDecayLightning adl = areaDecayLightnings[i];

                    if (adl.timePassed > adl.duration)
                    {
                        removals.Add(adl);
                    }
                }

                for (int i = 0; i < removals.Count; i++)
                {
                    areaDecayLightnings.Remove(removals[i]);
                }
            }
        }

        float deltaTime = 0f;
        void Update()
        {
            deltaTime = Time.deltaTime;
            RenderStrikes();
            RemoveOldStrikes();
            UpdateAreaDecayLightnings();
        }

        void PlayRandomLightingSound(Vector3 pos)
        {
            if (thunderSounds != null)
            {
                if (thunderSounds.Count > 0)
                {
                    int isound = Random.Range(0, thunderSounds.Count);

                    if (thunderSounds[isound] != null)
                    {
                        Vector3 camPos = RTSCamera.active.transform.position;
                        float distMax = 150f;
                        float curDist = (pos - camPos).magnitude;

                        if (curDist < distMax)
                        {
                            Vector3 pos1 = 0.03f * pos + 0.97f * camPos;
                            float intensity = GenericMath.Interpolate(curDist, 0f, distMax, 1f, 0f);
                            intensity = Mathf.Pow(intensity, 0.8f);
                            AudioSource.PlayClipAtPoint(thunderSounds[isound], pos1, intensity);
                        }
                    }
                }
            }
        }

        void SearchAndIgniteFires(Vector3 ignitionSourcePosition)
        {
            FireScaler.SearchAndIgniteFires(
                ignitionSourcePosition,
                true,
                0.5f,
                0.01f,
                deltaTime,
                10f
            );
        }

        public class LightingStrike
        {
            public List<Vector3> points = new List<Vector3>();
            public List<Vector3> pointsRendered = new List<Vector3>();
            public List<float> pointTimes = new List<float>();
            public int n = 10;
            public int branchOrder = 0;
            public bool isRendered = false;
            public GameObject go;
            public LineRenderer lr;
            public GameObject lightGo;
            public Light lght;
            public float startTime = 0f;
            public float strikeDuration = 1f;
            public float lastRandom = 1f;
        }

        public class AreaDecayLightning
        {
            public float strikesPerSecond = 75f;
            public float timePassed = 0f;
            public float duration = 1800f;
            public Vector3 areaCenter = Vector3.zero;
            public float areaRadius = 100f;
            public float updateProgress = 0f;
        }
    }
}
