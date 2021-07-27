using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class Journeys : MonoBehaviour
    {
        public static Journeys active;

        public List<Journey> journeys = new List<Journey>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        float deltaTime;
        void Update()
        {
            deltaTime = Time.deltaTime;
            UpdateJourneys();
        }

        float tUpdateJourneys = 0f;
        void UpdateJourneys()
        {
            tUpdateJourneys = tUpdateJourneys + deltaTime;

            if (tUpdateJourneys > 0.1f)
            {
                tUpdateJourneys = 0f;

                for (int i = 0; i < journeys.Count; i++)
                {
                    Journey jr = journeys[i];

                    if (jr.isStarted)
                    {
                        if ((Time.time - jr.prevTime) > jr.dt)
                        {
                            int ifailsBefore = jr.iFails;
                            jr.prevTime = Time.time;
                            jr.RefreshPath();

                            if (jr.iFails <= ifailsBefore)
                            {
                                jr.iFails = 0;
                            }
                        }
                    }
                }

                if (journeys.Count > 0)
                {
                    if (JourneysUI.active.closeMenu.activeSelf)
                    {
                        if (JourneysUI.active.openJourney != null)
                        {
                            JourneysUI.active.UpdateDistanceAndTimeLabels();
                        }
                    }
                }
            }
        }

        public void CreateNewJourney(List<UnitPars> members)
        {
            Journey jrn = new Journey();
            jrn.units = members;
            JourneysUI.active.OpenJourneyMenu(jrn);
            journeys.Add(jrn);
        }

        public void RemoveJourney(Journey jrn)
        {
            for (int i = 0; i < jrn.units.Count; i++)
            {
                UnitsMover.active.CompleteMovement(jrn.units[i]);
            }

            journeys.Remove(jrn);
        }

        public class Journey
        {
            public List<Vector3> positions = new List<Vector3>();
            public List<float> expectedArivalTimes = new List<float>();
            public List<UnitPars> units = new List<UnitPars>();
            public bool isStarted = false;
            public int nextPoint = 0;

            public float prevTime = 0f;
            public float dt = 4f;

            Vector3 prevMeanPos = Vector3.zero;

            public int iFails = 5;
            int nFails = 5;

            public void StartJourney()
            {
                isStarted = true;
                prevTime = Time.time;
                float minSpeed = MinimumSpeed();
                Vector3 meanPos = GetMeanPosition();
                float prevExpTime = 0f;
                expectedArivalTimes.Clear();

                for (int i = 0; i < positions.Count; i++)
                {
                    float expTime = 0f;

                    if (i == 0)
                    {
                        expTime = (positions[i] - meanPos).magnitude / minSpeed;
                    }
                    else
                    {
                        expTime = (positions[i] - positions[i - 1]).magnitude / minSpeed + prevExpTime;
                    }

                    prevExpTime = expTime;
                    expectedArivalTimes.Add(expTime + prevTime);
                }

                prevMeanPos = GetMeanPosition();
                iFails = nFails;
                RefreshPath();
            }

            public void RefreshPath()
            {
                int j = GetTimeStep();
                Vector3 meanPos = GetMeanPosition();

                if (j < 0)
                {
                    isStarted = false;
                    prevMeanPos = meanPos;
                    Debug.Log("j < 0 " + j);
                    return;
                }

                if ((meanPos - prevMeanPos).magnitude / dt > 0.3f * MinimumSpeed())
                {
                    prevMeanPos = meanPos;
                    return;
                }

                iFails++;
                if (iFails <= nFails)
                {
                    return;
                }
                else
                {
                    iFails = 0;
                }

                Vector3 nextPos = FindNextPosition(j);
                prevMeanPos = meanPos;

                for (int i = 0; i < units.Count; i++)
                {
                    UnitsMover.active.AddMilitaryAvoider(units[i], nextPos, 0);
                }
            }

            public float MinimumSpeed()
            {
                float minSpeed = float.MaxValue;

                for (int i = 0; i < units.Count; i++)
                {
                    float speed = units[i].GetUnitMaxSpeed();
                    if (speed > 0f)
                    {
                        if (speed < minSpeed)
                        {
                            minSpeed = speed;
                        }
                    }
                }

                return minSpeed;
            }

            public Vector3 GetMeanPosition()
            {
                Vector3 mean = Vector3.zero;

                for (int i = 0; i < units.Count; i++)
                {
                    mean = mean + units[i].transform.position;
                }

                return (mean / units.Count);
            }

            public float MeanDistanceAlongPath()
            {
                float mean = 0f;
                int n = 0;

                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i].agentPars != null)
                    {
                        mean = mean + units[i].agentPars.RemainingDistanceAlongPath();
                        n = n + 1;
                    }
                }

                if (n > 0)
                {
                    mean = mean / n;
                }

                return mean;
            }

            int GetTimeStep()
            {
                int ilargest = -1;

                for (int i = 0; i < expectedArivalTimes.Count; i++)
                {
                    if (expectedArivalTimes[i] > prevTime)
                    {
                        return i;
                    }

                    ilargest = i;
                }

                return ilargest;
            }

            Vector3 FindNextPosition(int i)
            {
                Vector3 mc = GetMeanPosition();

                if ((mc - positions[i]).magnitude < 1900f)
                {
                    return positions[i];
                }

                return TerrainProperties.GetFarestDestinationPoint(mc, positions[i], 150);
            }
        }
    }
}
