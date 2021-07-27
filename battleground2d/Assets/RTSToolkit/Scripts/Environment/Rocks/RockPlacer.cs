using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class RockPlacer : MonoBehaviour
    {
        public static RockPlacer active;

        public List<RockPlacerElement> rocks = new List<RockPlacerElement>();
        [HideInInspector] public List<FallingRockSpawner> fallingRocks = new List<FallingRockSpawner>();
        [HideInInspector] public List<RockInstanceElement> rockInstanceElements = new List<RockInstanceElement>();
        [HideInInspector] public Terrain ter;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void Clean()
        {
            rockInstanceElements.Clear();
        }

        public static RockPlacer GetActive()
        {
            if (RockPlacer.active == null)
            {
                RockPlacer.active = UnityEngine.Object.FindObjectOfType<RockPlacer>();
            }

            return RockPlacer.active;
        }

        public void Initialize(Terrain ter1)
        {
            ter = ter1;
            PlaceRocks();
        }

        public void UnsetFromTerrain(Terrain ter1)
        {
            List<RockInstanceElement> removals = new List<RockInstanceElement>();

            for (int i = 0; i < rockInstanceElements.Count; i++)
            {
                if (rockInstanceElements[i].ter == ter1)
                {
                    removals.Add(rockInstanceElements[i]);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                rockInstanceElements.Remove(removals[i]);
            }
        }

        void PlaceRocks()
        {
            for (int k = 0; k < rocks.Count; k++)
            {
                RockPlacerElement rock = rocks[k];
                RockInstanceElement rie = null;

                if (rock.useThis)
                {
                    int n1 = (int)(rock.density * ter.terrainData.size.x * ter.terrainData.size.z);

                    for (int i = 0; i < n1; i++)
                    {
                        MeshFilter mf = rock.prefab.GetComponent<MeshFilter>();
                        MeshRenderer mr = rock.prefab.GetComponent<MeshRenderer>();

                        Mesh msh = null;
                        Material mat = null;

                        float objSize = 0f;

                        if (mf != null)
                        {
                            msh = mf.sharedMesh;
                            objSize = msh.bounds.size.magnitude;
                        }

                        if (mr != null)
                        {
                            mat = mr.sharedMaterial;
                        }

                        Vector3 vCenter = TerrainProperties.RandomTerrainVector(ter);
                        int nCl = Random.Range(rock.nClusterMin, rock.nClusterMax);

                        for (int iclust = 0; iclust < nCl; iclust++)
                        {
                            float clSize = Random.Range(rock.clusterSizeMin, rock.clusterSizeMax);
                            Vector3 v3 = TerrainProperties.RandomTerrainVectorCircle(vCenter, clSize, ter, 10);

                            float steep = TerrainProperties.TerrainSteepness(ter, v3);

                            if ((steep > rock.steepnessMin) && (steep < rock.steepnessMax))
                            {
                                int slideIterations = Random.Range(rock.slideIterationsMin, rock.slideIterationsMax);

                                for (int j = 0; j < slideIterations; j++)
                                {
                                    Vector3 v31 = Random.Range(rock.slideVelocityMin, rock.slideVelocityMax) * TerrainProperties.GetFlowVector(ter, v3);
                                    v3 = TerrainProperties.TerrainVector(v3 + v31, ter);
                                }

                                float randScale = Mathf.Pow(Random.value, rock.sizeExponent);

                                Quaternion rot = Random.rotation;
                                Vector3 scale = (rock.sizeMultiplier * randScale + rock.sizeShift) * rock.prefab.transform.localScale;

                                if (rock.useDrawMesh == false)
                                {
                                    GameObject go = (GameObject)Instantiate(rock.prefab, v3, rot);
                                    go.transform.localScale = scale;
                                    go.transform.parent = ter.gameObject.transform;
                                }
                                else
                                {
                                    if ((rie == null) || (rie.matrices.Count > 1000))
                                    {
                                        rie = new RockInstanceElement();
                                        rie.matrices = new List<Matrix4x4>();
                                        rockInstanceElements.Add(rie);
                                    }

                                    Matrix4x4 m = Matrix4x4.identity;
                                    m.SetTRS(v3, rot, scale);

                                    rie.matrices.Add(m);

                                    rie.cameraTransform = Camera.main.transform;
                                    rie.msh = msh;
                                    rie.mat = mat;
                                    rie.viewAngle = rock.drawMeshViewAngle;
                                    rie.ter = ter;

                                    rie.positions.Add(v3);
                                    rie.sizes.Add(objSize * scale.magnitude);

                                    rie.distances.Add(0f);
                                    rie.passes.Add(false);
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < rockInstanceElements.Count; i++)
            {
                rockInstanceElements[i].RecalculateMatrices();
            }
        }

        void Update()
        {
            for (int i = 0; i < fallingRocks.Count; i++)
            {
                if (i < fallingRocks.Count && i >= 0)
                {
                    fallingRocks[i].RunSpawner(Time.deltaTime);
                }
            }

            ForceDrawElementMeshes();
            UpdateMatrices();
        }

        public void ForceDrawElementMeshes()
        {
            for (int i = 0; i < rockInstanceElements.Count; i++)
            {
                rockInstanceElements[i].DrawElementMesh();
            }
        }

        int iUpdateMatrices = 0;
        void UpdateMatrices()
        {
            int n = rockInstanceElements.Count;

            if (n > 0)
            {
                if (iUpdateMatrices >= n)
                {
                    iUpdateMatrices = 0;
                }

                rockInstanceElements[iUpdateMatrices].RecalculateMatrices();
                iUpdateMatrices++;
            }
        }

        [System.Serializable]
        public class RockPlacerElement
        {
            public GameObject prefab;
            public float density = 2.5e-4f;
            public int slideIterationsMin = 20;
            public int slideIterationsMax = 30;
            public float slideVelocityMin = 3f;
            public float slideVelocityMax = 5f;
            public float steepnessMin = 10f;
            public float steepnessMax = 90f;
            public int nClusterMin = 10;
            public int nClusterMax = 15;
            public float clusterSizeMin = 20f;
            public float clusterSizeMax = 30f;

            public float sizeExponent = 5f;
            public float sizeMultiplier = 10f;
            public float sizeShift = 2f;

            public bool useDrawMesh = false;
            public bool useThis = true;

            public float drawMeshViewAngle = 0.5f;
        }

        [System.Serializable]
        public class RockInstanceElement
        {
            public List<Matrix4x4> matrices = new List<Matrix4x4>();
            public List<Vector3> positions = new List<Vector3>();
            public List<float> sizes = new List<float>();
            public List<float> distances = new List<float>();
            public List<bool> passes = new List<bool>();
            public Matrix4x4[] matrices1;

            public Transform cameraTransform;

            public Mesh msh;
            public Material mat;

            public float viewAngle;

            public Terrain ter;

            public void RecalculateMatrices()
            {
                Vector3 camPos = cameraTransform.position;

                for (int i = 0; i < distances.Count; i++)
                {
                    distances[i] = GetParalax((positions[i] - camPos).magnitude, sizes[i]);
                }

                int n = 0;

                for (int i = 0; i < distances.Count; i++)
                {
                    passes[i] = false;

                    if (distances[i] > viewAngle)
                    {
                        n = n + 1;
                        passes[i] = true;
                    }
                }

                if ((matrices1 == null) || (n != matrices1.Length))
                {
                    matrices1 = new Matrix4x4[n];
                }

                n = 0;

                for (int i = 0; i < passes.Count; i++)
                {
                    if (passes[i])
                    {
                        matrices1[n] = matrices[i];
                        n = n + 1;
                    }
                }
            }

            public float GetParalax(float dist, float size)
            {
                return Mathf.Rad2Deg * Mathf.Atan(size / dist);
            }

            public void DrawElementMesh()
            {
                if (matrices1.Length > 0)
                {
                    bool instancingSupported = false;
#if !UNITY_WEBGL
                    instancingSupported = SystemInfo.supportsInstancing;
#endif
                    if (instancingSupported)
                    {
                        Graphics.DrawMeshInstanced(
                            msh,
                            0,
                            mat,
                            matrices1,
                            matrices1.Length,
                            null,
                            UnityEngine.Rendering.ShadowCastingMode.On,
                            true,
                            0,
                            Camera.main
                        );
                    }
                    else
                    {
                        for (int i = 0; i < matrices1.Length; i++)
                        {
                            Graphics.DrawMesh(
                                msh,                    // mesh
                                matrices1[i],           // position
                                mat,                    // material
                                0,                      // layer
                                Camera.main,            // camera
                                0,                      // submeshIndex
                                null,                   // MaterialPropertyBlock
                                true,                   // cast shadows
                                true                    // receive shadows
                            );
                        }
                    }
                }
            }
        }
    }
}
