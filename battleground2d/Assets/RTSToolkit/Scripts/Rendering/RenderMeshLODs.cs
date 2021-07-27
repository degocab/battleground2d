using System.Collections.Generic;
using UnityEngine;

// 2nd level of rendering
// contains LODs for a given model

namespace RTSToolkit
{
    [System.Serializable]
    public class RenderMeshLODs
    {
        public string modelName;

        // 0 - mesh
        // 1 - sprite
        // 2 - culled
        public List<RenderMeshAnimations> renderAnimations = new List<RenderMeshAnimations>();
        public bool wrapperOpen = false;

        public List<RenderMeshLODsWrapper> renderAnimationsWrapper = new List<RenderMeshLODsWrapper>();
        [HideInInspector] public List<Material> coloredNationMaterial = new List<Material>();
        public int coloredNationMaterialCount = 0;

        public bool useNationColor = false;
        public int nationColorOverwiteSubmeshIndex = -1;
        public Material mainNationColorMaterial = null;

        public void Initialize()
        {

        }

        public void Starter()
        {
            for (int i = 0; i < renderAnimationsWrapper.Count; i++)
            {
                RenderMeshAnimations renderAnimation = new RenderMeshAnimations(this);
                renderAnimation.model = renderAnimationsWrapper[i].model;
                renderAnimation.lodIndex = i;
                if (i == renderAnimationsWrapper.Count - 1)
                {
                    renderAnimation.isLastLOD = true;
                }
                renderAnimations.Add(renderAnimation);
            }

            for (int i = 0; i < renderAnimations.Count; i++)
            {
                renderAnimations[i].Initialize();
            }

            if (nationColorOverwiteSubmeshIndex > -1)
            {
                if (mainNationColorMaterial != null)
                {
                    if (NationSpawner.active != null)
                    {
                        if (NationSpawner.active.nations != null)
                        {
                            for (int i = 0; i < NationSpawner.active.nations.Count; i++)
                            {
                                Material mat1 = Object.Instantiate(mainNationColorMaterial);
                                mat1.color = NationSpawner.active.nations[i].nationColor;
                                coloredNationMaterial.Add(mat1);
                                coloredNationMaterialCount++;
                            }
                        }
                    }
                }
            }
        }

        public void Updater_DrawMeshes()
        {
            for (int i = 0; i < renderAnimations.Count; i++)
            {
                renderAnimations[i].Updater_DrawMeshes();
            }
        }

        public void RemoveEmptyNodes()
        {
            for (int i = 0; i < renderAnimations.Count; i++)
            {
                renderAnimations[i].RemoveEmptyNodes();
            }
        }

        public void Updater_PosIndsDists()
        {
            for (int i = 0; i < renderAnimations.Count; i++)
            {
                renderAnimations[i].Updater_PosIndsDists();
            }
        }

        public void UpdaterLODs()
        {
            for (int i = 0; i < renderAnimations.Count; i++)
            {
                renderAnimations[i].UpdaterLODs();
            }
        }

        public int GetLODIndex(float distSq)
        {
            int index = -1;
            float factor = RenderMeshModels.active.lodDistancesFactor;

            for (int i = 0; i < renderAnimationsWrapper.Count; i++)
            {
                Vector2 wrapperDistance = renderAnimationsWrapper[i].distance;
                float xi = factor * wrapperDistance.x;

                if (distSq >= xi * xi)
                {
                    float yi = factor * wrapperDistance.y;

                    if (distSq < yi * yi)
                    {
                        index = i;
                        return index;
                    }
                }
            }

            int lastElement = renderAnimationsWrapper.Count - 1;
            float yLast = factor * renderAnimationsWrapper[lastElement].distance.y;

            if (distSq >= yLast * yLast)
            {
                return lastElement;
            }

            return index;
        }

        [System.Serializable]
        public class RenderMeshLODsWrapper
        {
            public GameObject model;
            public Vector2 distance;
            public Vector3 offset = Vector3.zero;
            public int lodMode;
            public bool lodWrapperOpen;
            public int numberFramesToBake = 25;
        }
    }
}
