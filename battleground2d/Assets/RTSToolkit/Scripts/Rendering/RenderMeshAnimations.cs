using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 3rd level of rendering
// contains animations for a given LOD

namespace RTSToolkit
{
    public class RenderMeshAnimations
    {
        public GameObject model;
        public int lodIndex;
        public List<RenderMesh> renderMeshAnimations = new List<RenderMesh>();

        public RenderMeshLODs renderMeshLODs;

        public bool isLastLOD = false;

        public RenderMeshAnimations(RenderMeshLODs rml1)
        {
            renderMeshLODs = rml1;
        }

        public RenderMeshLODs GetRenderMeshLODs()
        {
            return renderMeshLODs;
        }

        public void Initialize()
        {
            GameObject instGo = UnityEngine.Object.Instantiate(model);

            Animation anim = instGo.GetComponent<Animation>();
            int n = 0;
            List<AnimationState> states = new List<AnimationState>(anim.Cast<AnimationState>());

            for (int i = 0; i < states.Count; i++)
            {
                AnimationState state = states[i];
                RenderMesh rm = new RenderMesh();
                rm.animIndex = n;
                rm.renderMeshAnimations = this;
                rm.model = instGo;
                rm.model2 = model;
                rm.clipToBake = state.name;
                rm.Initialize();
                renderMeshAnimations.Add(rm);
                n = n + 1;
            }

            UnityEngine.Object.Destroy(instGo);
        }

        public void Updater_DrawMeshes()
        {
            for (int i = 0; i < renderMeshAnimations.Count; i++)
            {
                renderMeshAnimations[i].Updater_DrawMeshes();
            }
        }

        public void RemoveEmptyNodes()
        {
            for (int i = 0; i < renderMeshAnimations.Count; i++)
            {
                renderMeshAnimations[i].RemoveEmptyNodes();
            }
        }

        public void Updater_PosIndsDists()
        {
            for (int i = 0; i < renderMeshAnimations.Count; i++)
            {
                renderMeshAnimations[i].Updater_PosIndsDists();
            }
        }

        public void UpdaterLODs()
        {
            for (int i = 0; i < renderMeshAnimations.Count; i++)
            {
                renderMeshAnimations[i].UpdaterLODs();
            }
        }

        public int FindAnimationIndex(string animationName)
        {
            int index = -1;

            for (int i = 0; i < renderMeshAnimations.Count; i++)
            {
                if (renderMeshAnimations[i].clipToBake == animationName)
                {
                    index = i;
                }
            }

            return index;
        }

        public void PlayAnimation(UnitAnimation node, string animationName)
        {
            int index1 = renderMeshAnimations.IndexOf(node.renderMesh);
            int index2 = FindAnimationIndex(animationName);

            if (index1 != index2)
            {
                if (index1 > -1 && index2 > -1)
                {
                    node.startTime = RenderMeshModels.time;
                    if (node.currentUnitAnim != null)
                    {
                        node.currentAnimationSpeed = node.currentUnitAnim.animationSpeed * node.walkSpeedMultiplier;
                    }
                    renderMeshAnimations[index1].RemoveNode(node);
                    renderMeshAnimations[index2].AddNode(node);
                }
            }
        }
    }
}
