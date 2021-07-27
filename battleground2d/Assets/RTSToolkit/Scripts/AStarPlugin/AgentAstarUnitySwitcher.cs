using UnityEngine;
using System.Collections.Generic;

#if ASTAR
using Pathfinding;
using Pathfinding.RVO;
#endif

namespace RTSToolkit
{
    public class AgentAstarUnitySwitcher : MonoBehaviour
    {
        public List<GameObject> gameObjectsToSwitch;

        void Start()
        {

        }

        public void SwitchThisToUnityNavMesh()
        {
#if UNITY_EDITOR
            for (int i = 0; i < gameObjectsToSwitch.Count; i++)
            {
                SwitchPrefabToUnityNavMesh(gameObjectsToSwitch[i]);
            }

            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SwitchThisToAStar()
        {
#if UNITY_EDITOR
            for (int i = 0; i < gameObjectsToSwitch.Count; i++)
            {
                SwitchPrefabToUnityNavMesh(gameObjectsToSwitch[i]);
            }

            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public static void SwitchPrefabToUnityNavMesh(GameObject go)
        {
#if ASTAR
            UnitPars up = go.GetComponent<UnitPars>();
            Animal animal = go.GetComponent<Animal>();

            AgentPars ap = go.GetComponent<AgentPars>();
            Seeker seeker = go.GetComponent<Seeker>();
            FunnelModifier funnelModifier = go.GetComponent<FunnelModifier>();
            SimpleSmoothModifier ss = go.GetComponent<SimpleSmoothModifier>();
            NavmeshCut nmc = go.GetComponent<NavmeshCut>();

            UnityEngine.AI.NavMeshAgent nma = null;
            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
            {
                if (ap != null)
                {
                    nma = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
                    nma.radius = ap.radius;
                    nma.height = ap.height;
                    nma.speed = ap.maxSpeed;
                    nma.baseOffset = -0.75f;
                    if (up != null)
                    {
                        up.thisNMA = nma;
                        nma.enabled = false;
                    }
                }
            }

            UnityEngine.AI.NavMeshObstacle nmo = null;
            if (go.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                if (nmc != null)
                {
                    nmo = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    nmo.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                    nmo.carving = true;
                    if (up != null)
                    {
                        up.thisNMO = nmo;
                    }
                }
            }

            if (ap != null)
            {
                DestroyImmediate(ap, true);
            }
            if (funnelModifier != null)
            {
                DestroyImmediate(funnelModifier, true);
            }
            if (ss != null)
            {
                DestroyImmediate(ss, true);
            }
            if (nmc != null)
            {
                DestroyImmediate(nmc, true);
            }
            if (seeker != null)
            {
                DestroyImmediate(seeker, true);
            }

            if (animal != null)
            {
                animal.SetGo();
            }
#endif
        }

        public static void SwitchPrefabToAStar(GameObject go)
        {
#if ASTAR
            UnitPars up = go.GetComponent<UnitPars>();
            Animal animal = go.GetComponent<Animal>();

            UnityEngine.AI.NavMeshAgent nma = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            UnityEngine.AI.NavMeshObstacle nmo = go.GetComponent<UnityEngine.AI.NavMeshObstacle>();

            if (nma != null)
            {
                AgentPars ap = go.GetComponent<AgentPars>();
                Seeker seeker = go.GetComponent<Seeker>();
                FunnelModifier funnelModifier = go.GetComponent<FunnelModifier>();
                SimpleSmoothModifier ss = go.GetComponent<SimpleSmoothModifier>();

                if (ap == null)
                {
                    ap = go.AddComponent<AgentPars>();
                    ap.radius = nma.radius;
                    ap.height = nma.height;
                    ap.maxSpeed = nma.speed;
                }
                if (seeker == null)
                {
                    seeker = go.AddComponent<Seeker>();
                }
                if (funnelModifier == null)
                {
                    funnelModifier = go.AddComponent<FunnelModifier>();
                }
                if (ss == null)
                {
                    ss = go.AddComponent<SimpleSmoothModifier>();
                    ss.smoothType = SimpleSmoothModifier.SmoothType.OffsetSimple;
                }

                DestroyImmediate(nma, true);

                if (up != null)
                {
                    up.thisNMA = null;
                }
            }
            if (nmo != null)
            {
                NavmeshCut nmc = go.GetComponent<NavmeshCut>();
                if (nmc == null)
                {
                    nmc = go.AddComponent<NavmeshCut>();
                    Bounds bnd = go.GetComponent<MeshFilter>().sharedMesh.bounds;

                    nmc.mesh = go.GetComponent<MeshFilter>().sharedMesh;
                    nmc.height = 1f;

                    nmc.type = NavmeshCut.MeshType.Rectangle;
                    nmc.center = bnd.center;

                    nmc.rectangleSize.x = bnd.size.x;
                    nmc.rectangleSize.y = bnd.size.z;
                }

                DestroyImmediate(nmo, true);

                if (up != null)
                {
                    up.thisNMO = null;
                }
            }

            if (animal != null)
            {
                animal.SetGo();
            }
#endif
        }
    }
}
