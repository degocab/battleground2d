using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ASTAR
using Pathfinding;
using Pathfinding.RVO;
#endif

namespace RTSToolkit
{
    public class ManualRVOs : MonoBehaviour
    {
        public List<ManualAgent> manualAgents = new List<ManualAgent>();

#if ASTAR
	    Pathfinding.RVO.Simulator sim;
#endif
        RTSMaster rtsm;

        public static ManualRVOs active;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
#if ASTAR
            RVOSimulator rvoSim = FindObjectOfType(typeof(RVOSimulator)) as RVOSimulator;
            sim = rvoSim.GetSimulator();
#endif
        }

        void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < manualAgents.Count; i++)
            {
                manualAgents[i].UpdateAgent(dt);
            }
        }

        public ManualAgent AddAgent(GameObject instance)
        {
            AgentPars agentPars = instance.GetComponent<AgentPars>();

            for (int i = 0; i < manualAgents.Count; i++)
            {
                if (agentPars == manualAgents[i].agentPars)
                {
                    return manualAgents[i];
                }
            }

            ManualAgent ma = new ManualAgent();
            ma.rtsm = rtsm;
            ma.instance = instance;
#if ASTAR
		    ma.seeker = ma.instance.GetComponent<Seeker>();
#endif
            ma.agentPars = agentPars;
            instance.transform.position = TerrainProperties.TerrainVectorProc(instance.transform.position);
            ma.prevTerrainPos = instance.transform.position;
#if ASTAR
            ma.agent = sim.AddAgent(new Vector2(instance.transform.position.x, instance.transform.position.z), instance.transform.position.y);
            ma.targetPosition = instance.transform.position;
            ma.agent.Radius = 1f;
#endif
            manualAgents.Add(ma);
            return ma;
        }

        public void RemoveAgent(ManualAgent ma)
        {
            manualAgents.Remove(ma);
        }

        public class ManualAgent
        {
            public GameObject instance;
#if ASTAR
            public IAgent agent;
            public Path path;
            public Seeker seeker;
#endif
            public AgentPars agentPars;
            public Vector3 targetPosition;
            public bool isReady = false;
            public int currentWaypoint = 0;
            public float nextWaypointDistance = 3f;
            public RTSMaster rtsm;

            public Vector3 prevRotationVector = Vector3.forward;
            public float rotAngle = 3f;

#if ASTAR
            float updateTime = 0f;
            float totUpdateTime = 0;
            
            int i_failed = 0;
            int n_failed = 25;
            float r_targ_prev = 1000f;
#endif
            public Vector3 prevTerrainPos;

            public void SearchPath(Vector3 targetPos)
            {
#if ASTAR
                if (isReady)
                {
                    isReady = false;
                }

                targetPosition = targetPos;
                seeker.StartPath(instance.transform.position, targetPosition, OnPathComplete);
#endif
            }

            public void StopMoving()
            {
#if ASTAR
                isReady = false;
                path = null;
#endif
            }

#if ASTAR
            public void OnPathComplete(Path p)
            {
                if (p != null)
                {
                    path = p;
                    currentWaypoint = 0;
                    isReady = true;
                }
                else
                {
                    seeker.StartPath(instance.transform.position, targetPosition, OnPathComplete);
                }
            }
#endif

            public void UpdateAgent(float dt)
            {
#if ASTAR
                updateTime = updateTime + dt;

                if (updateTime > totUpdateTime)
                {
                    if (isReady)
                    {
                        if (path == null)
                        {
                            return;
                        }
                        if (currentWaypoint >= path.vectorPath.Count)
                        {
                            isReady = false;
                            return;
                        }

                        agent.Radius = agentPars.radius;
                        agent.AgentTimeHorizon = 10f;
                        agent.ObstacleTimeHorizon = 10f;
                        agent.MaxNeighbours = 20;

                        Vector2 currentPos = agent.Position;
                        Vector2 direction = Vector2.ClampMagnitude(agent.CalculatedTargetPoint - currentPos, agent.CalculatedSpeed * Time.deltaTime);
                        currentPos = currentPos + direction;
                        agent.Position = currentPos;
                        agent.ElevationCoordinate = 0;
                        Vector2 target2d = new Vector2(path.vectorPath[currentWaypoint].x, path.vectorPath[currentWaypoint].z);
                        float dist1 = (target2d - currentPos).magnitude;
                        agent.SetTarget(target2d, Mathf.Min(dist1, agentPars.maxSpeed), agentPars.maxSpeed * 1.1f);

                        Vector3 positionWorld = TerrainProperties.TerrainVectorProc(new Vector3(agent.Position.x, 0f, agent.Position.y));
                        instance.transform.position = positionWorld;

                        Vector3 dir1 = path.vectorPath[currentWaypoint] - positionWorld;
                        Vector3 dir1xz = new Vector3(dir1.x, 0f, dir1.z);

                        float r = (positionWorld - targetPosition).magnitude;

                        if (r > 3f)
                        {
                            float angle = GenericMath.SignedAngle(dir1xz, prevRotationVector, Vector3.up);

                            Vector3 dir1xzn = dir1xz;
                            if (Mathf.Abs(angle) > 3f)
                            {
                                dir1xzn = GenericMath.RotAround(Mathf.Sign(angle) * 2.9f, prevRotationVector, new Vector3(0f, 1f, 0f));
                                instance.transform.rotation = Quaternion.LookRotation(dir1xzn);
                            }

                            prevRotationVector = dir1xzn;

                            if (dir1xz.magnitude < nextWaypointDistance)
                            {
                                currentWaypoint++;
                                return;
                            }
                        }

                        if (r >= r_targ_prev)
                        {
                            i_failed++;

                            if (i_failed > n_failed)
                            {
                                i_failed = 0;
                                n_failed = Random.Range(20, 30);
                                SearchPath(targetPosition);
                            }
                        }

                        r_targ_prev = r;
                    }
                    else
                    {
                        Vector3 pos = GetPosition();
                        instance.transform.position = pos;
                    }

                    updateTime = 0f;
                }
#endif
            }

#if ASTAR
            public Vector3 GetPosition()
            {
                Vector3 pos = new Vector3(agent.Position.x, prevTerrainPos.y, agent.Position.y);

                if ((pos - prevTerrainPos).magnitude > 0.1f)
                {
                    pos = TerrainProperties.TerrainVectorProc(pos);
                    prevTerrainPos = pos;
                }

                return pos;
            }
#endif

#if ASTAR
            public float RemainingDistanceAlongPath()
            {
                float remDist = 0f;

                if (path == null)
                {
                    return remDist;
                }

                if (currentWaypoint >= path.vectorPath.Count)
                {
                    return remDist;
                }

                for (int i = currentWaypoint; i < path.vectorPath.Count; i++)
                {
                    if (i > currentWaypoint)
                    {
                        remDist = remDist + (path.vectorPath[i] - path.vectorPath[i - 1]).magnitude;
                    }
                    else
                    {
                        remDist = remDist + (path.vectorPath[i] - GetPosition()).magnitude;
                    }
                }

                return remDist;
            }
#endif
        }
    }
}
