using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class DLASystem
    {
        // Input
        public Vector3 initialPosBarDirection = new Vector3(0f, 1f, 0f);
        public List<Vector3> origins = new List<Vector3>();
        public float randomSpeed = 0.1f;
        public float flowSpeed = 0.05f;
        public int maxMovementSteps = 5000;
        public int numberParticles = 400;
        public int numberSubSplits = 64;
        public Vector3 worldOffset = new Vector3(-5000f, 0f, 0f);
        public float worldRotation = -135f;
        public float worldScale = 1000f;
        public int initialSeed = 48;
        public bool initializeSeed = true;
        public Vector2 randomBarInitializerX = new Vector2(-30f, 100f);
        public Vector2 randomBarInitializerY = new Vector2(-0.1f, 0.1f);
        public int dimension = 2;

        // Internal global variables
        List<Vector3> positions = new List<Vector3>();
        KDTree kd;
        Vector3 pt_this;
        int iAdded = 0;
        List<DLANode> mainNodes = new List<DLANode>();
        int kd_index = -1;
        int node_global = 0;
        int branch_global = 0;
        float maxDist = 3f;
        bool dlaSet = false;

        // External global variables
        [HideInInspector] public List<Vector3> allPositionsBeg = new List<Vector3>();
        [HideInInspector] public List<Vector3> allPositionsEnd = new List<Vector3>();
        [HideInInspector] public List<float> posBegRandoms = new List<float>();
        [HideInInspector] public List<int> branchIds = new List<int>();
        [HideInInspector] public List<DLANode> allNodes = new List<DLANode>();

        void GenerateDLA()
        {
            if (dlaSet == false)
            {
                if (initializeSeed)
                {
                    Random.InitState(initialSeed);
                }
            }

            Initialize();
            UpdateFrame();
        }

        public void GenerateDLACheck()
        {
            if (dlaSet == false)
            {
                GenerateDLA();
            }
        }

        public void Clean()
        {
            iAdded = 0;
            maxDist = 3f;
            pt_this = Vector3.zero;
            kd_index = -1;
            node_global = 0;
            branch_global = 0;
            positions.Clear();
            allNodes.Clear();
            mainNodes.Clear();

            allPositionsBeg.Clear();
            allPositionsEnd.Clear();
            branchIds.Clear();
            posBegRandoms.Clear();
            positions = new List<Vector3>();
            allNodes = new List<DLANode>();
            mainNodes = new List<DLANode>();
            allPositionsBeg = new List<Vector3>();
            allPositionsEnd = new List<Vector3>();
            branchIds = new List<int>();
            posBegRandoms = new List<float>();
            kd = null;
            dlaSet = false;
        }

        void Initialize()
        {
            if (dlaSet == false)
            {
                for (int i = 0; i < origins.Count; i++)
                {
                    positions.Add(origins[i]);

                    DLANode node = new DLANode(null, origins[i]);
                    node.node_global = node_global;
                    node_global = node_global + 1;
                    node.treeIndex = i;
                    allNodes.Add(node);
                    mainNodes.Add(node);
                }

                kd = KDTree.MakeFromPoints(positions.ToArray());

                Vector3 newPos = NewPosBar();
                pt_this = newPos;

                iAdded++;
            }
        }

        void MakeInstance()
        {
            Vector3 newPos = NewPosBar();
            positions.Add(pt_this);
            kd = KDTree.MakeFromPoints(positions.ToArray());

            if (pt_this.y > maxDist)
            {
                maxDist = pt_this.y;
            }

            pt_this = newPos;
            iAdded++;
        }

        Vector3 NewPosBar()
        {
            Vector3 initialPosBarDirectionNormalized = initialPosBarDirection.normalized;
            Vector3 newPos = maxDist * initialPosBarDirectionNormalized + 3.0f * initialPosBarDirectionNormalized;
            float randomX = Random.Range(randomBarInitializerX.x, randomBarInitializerX.y);
            float randomY = Random.Range(randomBarInitializerY.x, randomBarInitializerY.y);
            float randomZ = 0f;

            if (dimension == 3)
            {
                randomZ = Random.Range(randomBarInitializerX.x, randomBarInitializerX.y); ;
            }

            newPos = newPos + new Vector3(randomX, randomY, randomZ);
            return newPos;
        }

        Vector3 FlowPoints()
        {
            Vector3 flow = new Vector3(0f, 0f, 0f);

            for (int i = 0; i < origins.Count; i++)
            {
                float sqrMagnitude = 1f / ((pt_this - origins[i]).sqrMagnitude);
                flow = flow + sqrMagnitude * (pt_this - origins[i]);
            }

            return (flowSpeed * flow.normalized);
        }

        void UpdateFrame()
        {
            if (dlaSet == false)
            {
                while (iAdded < numberParticles)
                {
                    for (int i = 0; i < maxMovementSteps; i++)
                    {
                        if (iAdded < numberParticles)
                        {
                            UpdateSingle();
                        }
                    }
                }
            }

            EndScale();
        }

        void UpdateSingle()
        {
            float randx = Random.Range(-randomSpeed, randomSpeed);
            float randy = Random.Range(-randomSpeed, randomSpeed);
            float randz = 0f;

            if (dimension == 3)
            {
                randz = Random.Range(-randomSpeed, randomSpeed);
            }

            Vector3 flow = FlowPoints();
            pt_this = pt_this + new Vector3(randx, randy, randz) - flow;

            if (DistancePassKD())
            {
                if (iAdded < numberParticles - 1)
                {
                    MakeInstance();
                }
                else
                {
                    iAdded++;
                }
            }
        }

        bool DistancePassKD()
        {
            int id = kd.FindNearest(pt_this);
            float distSqr = (pt_this - positions[id]).sqrMagnitude;

            if (distSqr < 1f)
            {
                kd_index = id;

                if (kd_index > -1)
                {
                    DLANode nearestNode = allNodes[kd_index];

                    DLANode node = new DLANode(nearestNode, pt_this);
                    node.node_global = node_global;

                    if (nearestNode.nextNode == null)
                    {
                        nearestNode.nextNode = node;
                    }
                    else
                    {
                        branch_global = branch_global + 1;
                    }

                    node.branch_global = branch_global;
                    node_global = node_global + 1;
                    allNodes.Add(node);
                }

                return true;
            }

            return false;
        }

        void EndScale()
        {
            if (dlaSet == false)
            {
                for (int i = 0; i < mainNodes.Count; i++)
                {
                    mainNodes[i].ScalePositions(worldScale);
                }

                InsertNodes();
                dlaSet = true;
            }
        }

        void InsertNodes()
        {
            for (int i = 0; i < mainNodes.Count; i++)
            {
                mainNodes[i].InsertSubChilds(numberSubSplits, this);
            }
        }
    }
}
