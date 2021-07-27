using UnityEngine;
using System.Collections.Generic;

// 1st level of rendering
// contains models

namespace RTSToolkit
{
    public class RenderMeshModels : MonoBehaviour
    {
        public static RenderMeshModels active;

        public List<RenderMeshLODs> renderModels = new List<RenderMeshLODs>();

        public bool isStarted = false;

        public float lodDistancesFactor = 1f;

        public bool adjustLODDistanceFactorRuntime = true;

        public float minLodDistancesFactor = 0.9f;
        public float maxLodDistancesFactor = 1.1f;

        public float minVertsCount = 0f;
        public float maxVertsCount = 5000000f;

        public float shadowCastDistance = 1000f;
        public float shadowReceiveDistance = 1000f;

        [HideInInspector] public bool modelsWrapperOpen = false;
        public bool useMeshInstancing = false;
        public bool useMeshInstancingIndirect = false;

        public static Vector3 cameraPosition;
        public static float deltaTime;
        public static float time;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            for (int i = 0; i < renderModels.Count; i++)
            {
                renderModels[i].Starter();
            }

            isStarted = true;
            camTransform = Camera.main.transform;
        }

        public int FindModelIndex(string name)
        {
            for (int i = 0; i < renderModels.Count; i++)
            {
                if (renderModels[i].modelName == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public void UpdateManually()
        {
            Update();
        }

        Transform camTransform;

        void Update()
        {
            cameraPosition = camTransform.position;
            deltaTime = Time.deltaTime;
            time = Time.time;

            UpdateInner();

            if (adjustLODDistanceFactorRuntime)
            {
                AdjustLODDistancesFactor();
            }
        }

        void UpdateInner()
        {
            int n = renderModels.Count;

            for (int i = 0; i < n; i++)
            {
                renderModels[i].RemoveEmptyNodes();
            }

            for (int i = 0; i < n; i++)
            {
                renderModels[i].UpdaterLODs();
            }

            for (int i = 0; i < n; i++)
            {
                renderModels[i].Updater_PosIndsDists();
            }

            for (int i = 0; i < n; i++)
            {
                renderModels[i].Updater_DrawMeshes();
            }
        }

        public void RemoveNode(RenderMesh rm, UnitAnimation node)
        {
            rm.renderMeshNodes.Remove(node);
            rm.renderMeshNodesLowFrequency.Remove(node);
            rm.lowFrequencyAdders.Remove(node);
        }

        float tAdjustLODDistancesFactor = 0f;
        void AdjustLODDistancesFactor()
        {
            tAdjustLODDistancesFactor = tAdjustLODDistancesFactor + deltaTime;

            if (tAdjustLODDistancesFactor > 0.5f)
            {
                tAdjustLODDistancesFactor = 0f;

                int vCount = GetTotalVerticesCountInScene();
                float newFactor = GenericMath.Interpolate(5f * vCount, maxVertsCount, minVertsCount, minLodDistancesFactor, maxLodDistancesFactor);
                float lodDistancesFactor1 = 0.5f * lodDistancesFactor + 0.5f * newFactor;

                if (lodDistancesFactor1 < minLodDistancesFactor)
                {
                    lodDistancesFactor1 = minLodDistancesFactor;
                }

                if (lodDistancesFactor1 > maxLodDistancesFactor)
                {
                    lodDistancesFactor1 = maxLodDistancesFactor;
                }

                if (Mathf.Abs(lodDistancesFactor1 - lodDistancesFactor) > 0.1f)
                {
                    lodDistancesFactor = lodDistancesFactor1;
                }
            }
        }

        public int GetTotalVerticesCountInScene()
        {
            int vcount = 0;
            RenderMeshLODs l1;
            RenderMeshAnimations l2;
            RenderMesh l3;

            for (int i1 = 0; i1 < renderModels.Count; i1++)
            {
                l1 = renderModels[i1];

                for (int i2 = 0; i2 < l1.renderAnimations.Count; i2++)
                {
                    l2 = l1.renderAnimations[i2];

                    if (l1.renderAnimationsWrapper[i2].lodMode == 0)
                    {
                        for (int i3 = 0; i3 < l2.renderMeshAnimations.Count; i3++)
                        {
                            l3 = l2.renderMeshAnimations[i3];
                            int nverts = 0;

                            if (l3.msh.Length > 0)
                            {
                                nverts = l3.mshVerticesCount[0];
                            }

                            for (int i4 = 0; i4 < l3.renderMeshNodes.Count; i4++)
                            {
                                if (l3.msh.Length > 0)
                                {
                                    vcount = vcount + nverts;
                                }
                            }
                        }
                    }
                }
            }

            return vcount;
        }

        public void RemoveTransform(Transform tr)
        {
            for (int i1 = 0; i1 < renderModels.Count; i1++)
            {
                RenderMeshLODs l1 = renderModels[i1];

                for (int i2 = 0; i2 < l1.renderAnimations.Count; i2++)
                {
                    RenderMeshAnimations l2 = l1.renderAnimations[i2];

                    if (l1.renderAnimationsWrapper[i2].lodMode == 0)
                    {
                        for (int i3 = 0; i3 < l2.renderMeshAnimations.Count; i3++)
                        {
                            RenderMesh l3 = l2.renderMeshAnimations[i3];

                            for (int i4 = 0; i4 < l3.renderMeshNodes.Count; i4++)
                            {
                                if (l3.renderMeshNodes[i4].transform == tr)
                                {
                                    l3.RemoveNode(l3.renderMeshNodes[i4]);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
