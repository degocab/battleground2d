using System.Collections.Generic;
using UnityEngine;

// 4th level of rendering
// contains animation frames for a given animation

namespace RTSToolkit
{
    public class RenderMesh
    {
        public GameObject model;
        public GameObject model2;

        public SkinnedMeshRenderer skinnedMesh;

        public RenderMeshAnimations renderMeshAnimations;

        public Mesh[] msh;
        public int[] mshVerticesCount;

        public List<RenderMeshFrame> renderMeshFrames = new List<RenderMeshFrame>();

        public Material[] mats = new Material[0];

        public List<UnitAnimation> renderMeshNodes = new List<UnitAnimation>();

        public List<UnitAnimation> lowFrequencyAdders = new List<UnitAnimation>();
        public List<UnitAnimation> renderMeshNodesLowFrequency = new List<UnitAnimation>();

        public List<UnitAnimation> rmnRemovals = new List<UnitAnimation>();
        public List<float> rmnRemovalsDist = new List<float>();

        public Material instanceMaterial;
        public Material[] instanceMaterials;

        public bool isReady = false;

        public int lodIndex;
        public Vector2 distances;
        public Vector3 offset;
        public int lodMode;

        public int animIndex;

        float totalTime = 0f;

        public int numFramesToBake = 25;
        public string clipToBake = "Idle";
        public bool isRepeatable = true;

        Transform camTransf;
        Camera cam;

        int lodUpdate = 0;
        int nLodUpdate = 4;

        public void Initialize()
        {
            camTransf = Camera.main.transform;
            cam = Camera.main;

            lodIndex = renderMeshAnimations.lodIndex;
            distances = renderMeshAnimations.GetRenderMeshLODs().renderAnimationsWrapper[lodIndex].distance;
            offset = renderMeshAnimations.GetRenderMeshLODs().renderAnimationsWrapper[lodIndex].offset;
            lodMode = renderMeshAnimations.GetRenderMeshLODs().renderAnimationsWrapper[lodIndex].lodMode;
            numFramesToBake = renderMeshAnimations.GetRenderMeshLODs().renderAnimationsWrapper[lodIndex].numberFramesToBake;

            useInstancingIndirect = RenderMeshModels.active.useMeshInstancingIndirect;

            if (lodMode == 0)
            {
                GetStaticMeshes();
            }

            isReady = true;
        }

        bool useInstancingIndirect = true;
        void GetStaticMeshes()
        {
            GameObject inst = model;

            foreach (Transform child in inst.transform)
            {
                if (child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    skinnedMesh = child.GetComponent<SkinnedMeshRenderer>();
                    skinnedMesh.updateWhenOffscreen = true;
                    mats = skinnedMesh.materials;
                }
            }

            GameObject inst2 = UnityEngine.Object.Instantiate(model2);
            SkinnedMeshRenderer skinnedMesh2 = null;

            foreach (Transform child in inst2.transform)
            {
                if (child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    skinnedMesh2 = child.GetComponent<SkinnedMeshRenderer>();
                }
            }

            Quaternion rot = skinnedMesh2.gameObject.transform.rotation;
            inst2.transform.rotation = Quaternion.identity;

            Animation animation = inst.GetComponent<Animation>();
            AnimationState state = animation[clipToBake];
            WrapMode wrapMode = state.wrapMode;

            if (wrapMode == WrapMode.Once)
            {
                isRepeatable = false;
            }

            animation.Play(clipToBake, PlayMode.StopAll);

            float deltaTime = state.length / (float)(numFramesToBake - 1);
            state.time = 0.0f;
            totalTime = state.length;

            if (useInstancingIndirect)
            {
                instanceMaterials = new Material[numFramesToBake];
                positionBuffer = new ComputeBuffer[numFramesToBake];
                rotationBuffer = new ComputeBuffer[numFramesToBake];
                argsBuffer = new ComputeBuffer[numFramesToBake];
            }

            msh = new Mesh[numFramesToBake];
            mshVerticesCount = new int[numFramesToBake];

            for (int i = 0; i < numFramesToBake; ++i)
            {
                string frameName = clipToBake + "_" + i;
                Mesh frameMesh = new Mesh();
                frameMesh.name = frameName;

                animation.Sample();
                skinnedMesh.BakeMesh(frameMesh);

                Vector3[] vertices = frameMesh.vertices;

                for (int j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = rot * vertices[j];
                }

                frameMesh.vertices = vertices;
                frameMesh.RecalculateBounds();
                frameMesh.RecalculateNormals();

                msh[i] = frameMesh;
                mshVerticesCount[i] = vertices.Length;

                RenderMeshFrame rmf = new RenderMeshFrame();
                rmf.msh = frameMesh;
                rmf.mats = mats;
                renderMeshFrames.Add(rmf);

                state.time += deltaTime;

                if (useInstancingIndirect)
                {
                    instanceMaterials[i] = new Material(Shader.Find("Instanced/InstancedShaderIndirect"));
                    instanceMaterials[i].mainTexture = mats[0].mainTexture;

                    argsBuffer[i] = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                }
            }

            animation.Stop();

            if (mats != null)
            {
                if (renderMeshAnimations.renderMeshLODs.nationColorOverwiteSubmeshIndex > -1)
                {
                    if (renderMeshAnimations.renderMeshLODs.mainNationColorMaterial == null)
                    {
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (i == renderMeshAnimations.renderMeshLODs.nationColorOverwiteSubmeshIndex)
                            {
                                if (mats[i] != null)
                                {
                                    renderMeshAnimations.renderMeshLODs.mainNationColorMaterial = mats[i];
                                }
                            }
                        }
                    }
                }
            }

            if (useInstancingIndirect)
            {
                instanceMaterial = new Material(Shader.Find("Instanced/InstancedShaderIndirect"));
                instanceMaterial.mainTexture = mats[0].mainTexture;
            }

            UnityEngine.Object.Destroy(inst2);
        }

        public void RemoveEmptyNodes()
        {
            List<UnitAnimation> removals = new List<UnitAnimation>();

            for (int i = 0; i < renderMeshNodes.Count; i++)
            {
                if (renderMeshNodes[i] == null)
                {
                    removals.Add(renderMeshNodes[i]);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                renderMeshNodes.Remove(removals[i]);
                Debug.Log("Empty node " + i);
            }
        }

        Material currentMat;
        public void UpdatePosAnimationIndices()
        {
            if (RenderMeshModels.active.useMeshInstancing)
            {
                for (int i = 0; i < renderMeshFrames.Count; i++)
                {
                    renderMeshFrames[i].n = 0;
                    renderMeshFrames[i].n1 = 0;
                }
            }

            lodUpdate++;
            Vector3 camPos = RenderMeshModels.cameraPosition;
            float dt0 = RenderMeshModels.deltaTime;

            float shadowCastDistanceSq = RenderMeshModels.active.shadowCastDistance;
            shadowCastDistanceSq = shadowCastDistanceSq * shadowCastDistanceSq;

            float shadowReceiveDistanceSq = RenderMeshModels.active.shadowReceiveDistance;
            shadowReceiveDistanceSq = shadowReceiveDistanceSq * shadowReceiveDistanceSq;

            currentMat = null;

            for (int i = 0; i < renderMeshNodes.Count; i++)
            {
                UnitAnimation node = renderMeshNodes[i];
                node.currentAnimationIndex = -1;

                Vector3 pos = node.transform.position + offset;
                float cameraDistanceSq = (camPos - pos).sqrMagnitude;

                float dt = dt0;

                if (node.currentUnitAnim != null)
                {
                    if (node.currentUnitAnim.scaleWithSpeed)
                    {
                        float v = (pos - node.prevPos).sqrMagnitude / (dt0 * dt0);

                        if (v < 0f)
                        {
                            v = 0f;
                        }

                        float maxMovementSpeed = node.unitAnimationType.maxMovementSpeed;
                        float maxMovementSpeedSquared = maxMovementSpeed * maxMovementSpeed;

                        if (v > maxMovementSpeedSquared)
                        {
                            v = maxMovementSpeedSquared;
                        }

                        dt = dt0 * v / maxMovementSpeedSquared;

                        node.prevPos = pos;
                    }
                }

                float animationPhase = node.a_phase + dt / (totalTime * node.currentAnimationSpeed);

                if (isRepeatable)
                {
                    if (animationPhase > 1f)
                    {
                        animationPhase = animationPhase - 1f;
                    }
                }
                else
                {
                    if (animationPhase > 0.99f)
                    {
                        animationPhase = 0.99f;
                    }
                }

                node.a_phase = animationPhase;

                int aind = AnimationTimeIndex(numFramesToBake, 1.0f, animationPhase);

                if (aind >= 0)
                {
                    node.currentAnimationIndex = aind;
                    node.m_castShadows = node.unitAnimationType.castShadows;

                    if (cameraDistanceSq > shadowCastDistanceSq)
                    {
                        node.m_castShadows = false;
                    }

                    node.m_receiveShadows = true;

                    if (cameraDistanceSq > shadowReceiveDistanceSq)
                    {
                        node.m_receiveShadows = false;
                    }

                    if (RenderMeshModels.active.useMeshInstancing)
                    {
                        renderMeshFrames[aind].n = renderMeshFrames[aind].n + 1;

                        if (node.unitAnimationType.castShadows)
                        {
                            renderMeshFrames[aind].castShadows = UnityEngine.Rendering.ShadowCastingMode.On;
                        }
                        else
                        {
                            renderMeshFrames[aind].castShadows = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
                    }
                }
            }
        }

        void UpdateLODs_inner()
        {
            Vector3 camPos = RenderMeshModels.cameraPosition;
            float factor = RenderMeshModels.active.lodDistancesFactor * distances.y;
            float factorSq = factor * factor;

            for (int i = 0; i < renderMeshNodes.Count; i++)
            {
                UnitAnimation node = renderMeshNodes[i];
                Vector3 pos = node.transform.position + offset;
                float distSq = (camPos - pos).sqrMagnitude;
                int distMod = DistanceMode(distSq);

                bool lastLODpass = true;

                if (renderMeshAnimations.isLastLOD)
                {
                    if (distSq > factorSq)
                    {
                        lastLODpass = false;
                        lowFrequencyAdders.Add(node);
                    }
                }

                if (lastLODpass)
                {
                    if (distMod != 0)
                    {
                        if (lodUpdate > nLodUpdate)
                        {
                            CheckLOD(distSq, node);
                        }
                    }
                }
            }

            if (renderMeshAnimations.isLastLOD)
            {
                for (int i = 0; i < lowFrequencyAdders.Count; i++)
                {
                    renderMeshNodesLowFrequency.Add(lowFrequencyAdders[i]);
                    renderMeshNodes.Remove(lowFrequencyAdders[i]);
                }

                lowFrequencyAdders.Clear();
            }

            if (lodUpdate > nLodUpdate)
            {
                lodUpdate = 0;

                if (renderMeshAnimations.isLastLOD)
                {
                    float factorLowFreq = 0.99f * factor;
                    float factorLowFreqSq = factorLowFreq * factorLowFreq;

                    for (int i = 0; i < renderMeshNodesLowFrequency.Count; i++)
                    {
                        if (renderMeshNodesLowFrequency[i] == null)
                        {
                            RemoveNode(renderMeshNodesLowFrequency[i]);
                            return;
                        }

                        float distSq = (camPos - renderMeshNodesLowFrequency[i].transform.position).sqrMagnitude;

                        if (distSq <= factorLowFreqSq)
                        {
                            lowFrequencyAdders.Add(renderMeshNodesLowFrequency[i]);
                        }
                    }

                    for (int i = 0; i < lowFrequencyAdders.Count; i++)
                    {
                        renderMeshNodes.Add(lowFrequencyAdders[i]);
                        renderMeshNodesLowFrequency.Remove(lowFrequencyAdders[i]);
                    }

                    lowFrequencyAdders.Clear();
                }
            }
        }

        void CheckForDistOnly()
        {
            Vector3 camPos = RenderMeshModels.cameraPosition;

            for (int i = 0; i < renderMeshNodes.Count; i++)
            {
                UnitAnimation node = renderMeshNodes[i];
                Vector3 pos = node.transform.position;
                float distSq = (camPos - pos).sqrMagnitude;

                int distMod = DistanceMode(distSq);
                if (distMod != 0)
                {
                    CheckLOD(distSq, node);
                }
            }
        }

        int DistanceMode(float distSq)
        {
            int distanceMode = 0;
            float factor = RenderMeshModels.active.lodDistancesFactor;

            float x = factor * distances.x;
            float y = factor * distances.y;

            if (distSq >= y * y)
            {
                distanceMode = 1;
            }
            else if (distSq < x * x)
            {
                distanceMode = -1;
            }

            return distanceMode;
        }

        ComputeBuffer[] positionBuffer;
        ComputeBuffer[] rotationBuffer;
        ComputeBuffer[] argsBuffer;
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        public void Updater_DrawMeshes()
        {
            if (lodMode == 0)
            {
                if (RenderMeshModels.active.useMeshInstancing)
                {
                    if (useInstancingIndirect)
                    {
                        ShowInstancedMeshesIndirect();
                    }
                    else
                    {
                        ShowInstancedMeshes();
                    }
                }
                else
                {
                    ShowMeshes();
                }
            }
        }

        public void ShowMeshes()
        {
            int matsLength = mats.Length;
            int mshCount = msh.Length;

            RenderMeshLODs rmlods = renderMeshAnimations.renderMeshLODs;

            int nationColorOverwiteSubmeshIndex = rmlods.nationColorOverwiteSubmeshIndex;
            int coloredNationMaterialCount = rmlods.coloredNationMaterialCount;

            for (int i = 0; i < renderMeshNodes.Count; i++)
            {
                UnitAnimation node = renderMeshNodes[i];

                if (node != null)
                {
                    int currentAInd = node.currentAnimationIndex;

                    if ((currentAInd >= 0) && (currentAInd < mshCount))
                    {
                        for (int k = 0; k < matsLength; k++)
                        {
                            bool matPass = true;
                            int natid = -1;

                            if (k == nationColorOverwiteSubmeshIndex)
                            {
                                natid = node.nation;

                                if (natid > -1)
                                {
                                    if (natid < coloredNationMaterialCount)
                                    {
                                        matPass = false;
                                    }
                                }
                            }

                            if (matPass)
                            {
                                currentMat = mats[k];
                            }
                            else
                            {
                                currentMat = rmlods.coloredNationMaterial[natid];
                            }

                            Graphics.DrawMesh(
                                msh[currentAInd],                   // mesh
                                node.transform.position + offset,   // position
                                node.transform.rotation,            // rotation
                                currentMat,                         // material
                                0,                                  // layer
                                cam,                                // camera
                                k,                                  // submeshIndex
                                null,                               // MaterialPropertyBlock
                                node.m_castShadows,                 // cast shadows
                                node.m_receiveShadows               // receive shadows
                            );
                        }
                    }
                }
            }
        }

        public void ShowInstancedMeshes()
        {
            for (int i = 0; i < renderMeshFrames.Count; i++)
            {
                RenderMeshFrame rmf = renderMeshFrames[i];
                int cLength = rmf.GetCurrentLength();

                if (
                    ((rmf.n > 0) && (cLength == -1)) ||
                    ((rmf.n > 0) && (rmf.n >= cLength)) ||
                    ((rmf.n > 0) && (rmf.n < (cLength + 20)))
                )
                {
                    rmf.matrixList = new List<RMFMatrixList>();
                    int ml_size = (int)((rmf.n + 10) / rmf.blockSize) + 1;

                    int cLength1 = 0;

                    for (int j = 0; j < ml_size; j++)
                    {
                        cLength1 = cLength1 + rmf.blockSize;
                        int cLength2 = rmf.blockSize;

                        if (cLength1 > rmf.n)
                        {
                            cLength1 = cLength1 - (cLength1 - rmf.n);
                            cLength2 = cLength1;
                        }

                        if (cLength1 > 0)
                        {
                            RMFMatrixList ml1 = new RMFMatrixList();
                            ml1.matrices = new Matrix4x4[cLength2];
                            rmf.matrixList.Add(ml1);
                        }
                    }
                }
            }

            for (int i = 0; i < renderMeshNodes.Count; i++)
            {
                UnitAnimation node = renderMeshNodes[i];

                if ((node.currentAnimationIndex >= 0) && (node.currentAnimationIndex < renderMeshFrames.Count))
                {
                    RenderMeshFrame rmfCur = renderMeshFrames[node.currentAnimationIndex];

                    if (rmfCur.n > 0)
                    {
                        int ntot = rmfCur.n1;
                        int ni = (int)(ntot / rmfCur.blockSize);
                        int nj = ntot - ni * rmfCur.blockSize;

                        rmfCur.matrixList[ni].matrices[nj] = node.transform.localToWorldMatrix;
                        rmfCur.n1 = ntot + 1;
                    }
                }
            }

            for (int i = 0; i < renderMeshFrames.Count; i++)
            {
                renderMeshFrames[i].ShowMeshInstanced();
            }
        }

        public void ShowInstancedMeshesIndirect()
        {
            int n = renderMeshNodes.Count;

            if (n > 0)
            {
                int nMsh = msh.Length;
                int[] nInst = new int[nMsh];

                for (int i = 0; i < nMsh; i++)
                {
                    nInst[i] = 0;
                }

                for (int i = 0; i < n; i++)
                {
                    UnitAnimation node = renderMeshNodes[i];

                    if (node != null)
                    {

                        int aind = node.currentAnimationIndex;
                        if (aind >= 0 && aind < nMsh)
                        {
                            nInst[aind] = nInst[aind] + 1;
                        }
                    }
                }

                for (int i = 0; i < nMsh; i++)
                {
                    int nn = nInst[i];

                    if (nn > 0)
                    {
                        if (positionBuffer[i] != null)
                        {
                            positionBuffer[i].Release();
                        }
                        positionBuffer[i] = new ComputeBuffer(nn, 16);

                        if (rotationBuffer[i] != null)
                        {
                            rotationBuffer[i].Release();
                        }
                        rotationBuffer[i] = new ComputeBuffer(nn, 16);

                        Vector4[] positions = new Vector4[nn];
                        Vector4[] rotations = new Vector4[nn];

                        Mesh msh1 = msh[i];

                        if (msh1 != null)
                        {
                            int j1 = 0;

                            for (int j = 0; j < n; j++)
                            {
                                UnitAnimation node = renderMeshNodes[j];

                                if (node != null)
                                {
                                    if (node.currentAnimationIndex == i)
                                    {
                                        Vector3 pos = node.transform.position;
                                        Quaternion rot = node.transform.rotation;

                                        positions[j1] = new Vector4(pos.x, pos.y, pos.z, 1f);
                                        rotations[j1] = new Vector4(rot.x, rot.y, rot.z, rot.w);
                                        j1++;
                                    }
                                }
                            }
                        }

                        positionBuffer[i].SetData(positions);
                        rotationBuffer[i].SetData(rotations);

                        instanceMaterials[i].SetBuffer("positionBuffer", positionBuffer[i]);
                        instanceMaterials[i].SetBuffer("rotationBuffer", rotationBuffer[i]);

                        uint numIndices = (msh1 != null) ? (uint)msh1.GetIndexCount(0) : 0;
                        args[0] = numIndices;
                        args[1] = (uint)nn;
                        argsBuffer[i].SetData(args);
                    }
                }

                for (int i = 0; i < nMsh; i++)
                {
                    int nn = nInst[i];
                    if (nn > 0)
                    {
                        Mesh msh1 = msh[i];

                        if (msh1 != null)
                        {
                            Graphics.DrawMeshInstancedIndirect(
                                msh1,
                                0,
                                instanceMaterials[i],
                                new Bounds(Vector3.zero, new Vector3(6000.0f, 6000.0f, 6000.0f)),
                                argsBuffer[i],
                                0,
                                new MaterialPropertyBlock(),
                                UnityEngine.Rendering.ShadowCastingMode.Off,
                                true
                            );
                        }
                    }
                }
            }
        }

        public void Updater_PosIndsDists()
        {
            if (isReady)
            {
                if (lodMode == 0)
                {
                    UpdatePosAnimationIndices();
                }
                else if (lodMode == 1)
                {
                    CheckForDistOnly();
                }
                else if (lodMode == 2)
                {
                    CheckForDistOnly();
                }
            }
        }

        public void UpdaterLODs()
        {
            if (isReady)
            {
                if (lodMode == 0)
                {
                    UpdateLODs_inner();
                }
            }
        }

        void CheckLOD(float distSq, UnitAnimation node)
        {
            RenderMeshLODs rmlds = renderMeshAnimations.renderMeshLODs;
            int newLODindex = rmlds.GetLODIndex(distSq);

            if (newLODindex > -1)
            {
                if (newLODindex != lodIndex)
                {
                    RenderMesh newRm = rmlds.renderAnimations[newLODindex].renderMeshAnimations[animIndex];
                    RemoveNode(node);
                    newRm.AddNode(node);
                }
            }
        }

        public void AddTransform(UnitAnimation node)
        {
            node.renderMesh = this;
            node.startTime = Time.time;
            node.currentAnimationSpeed = node.GetAnimationSpeed();

            if (node.currentUnitAnim != null)
            {
                node.currentAnimationSpeed = node.currentUnitAnim.animationSpeed * node.walkSpeedMultiplier;
            }

            RenderMeshLODs rmlds = renderMeshAnimations.GetRenderMeshLODs();
            float distSq = (camTransf.position - node.transform.position).sqrMagnitude;

            for (int i = 0; i < rmlds.renderAnimationsWrapper.Count; i++)
            {
                if (rmlds.renderAnimationsWrapper[i].lodMode == 1)
                {
                    node.bilboardDistance = rmlds.renderAnimationsWrapper[i].distance.x;
                }
            }

            int newLODindex = rmlds.GetLODIndex(distSq);

            if (rmlds.renderAnimationsWrapper[newLODindex].lodMode == 1)
            {

                PSpriteLoader.active.ForceSpriteModeLoad(node);
                PSpriteLoader.active.clientSpritesGo.Add(node);
                node.EnableSprite();

                node.particleNode.nonRepeatableStartTime = node.startTime;
                PSpriteLoader.active.PlayAnimation(node, clipToBake);
            }

            renderMeshNodes.Add(node);
        }

        public void AddNode(UnitAnimation node)
        {
            if (renderMeshNodes.Contains(node))
            {
                return;
            }

            renderMeshNodes.Add(node);

            node.renderMesh = this;

            if (lodMode == 1)
            {
                PSpriteLoader.active.ForceSpriteModeLoad(node);
                PSpriteLoader.active.clientSpritesGo.Add(node);
                node.EnableSprite();

                node.particleNode.nonRepeatableStartTime = node.startTime;
                PSpriteLoader.active.PlayAnimation(node, clipToBake);
            }
            else
            {
                node.animName = clipToBake;
            }
        }

        public void RemoveNode(UnitAnimation node)
        {
            if (RenderMeshModels.active != null)
            {
                RenderMeshModels.active.RemoveNode(this, node);
            }
            else
            {
                renderMeshNodes.Remove(node);
                renderMeshNodesLowFrequency.Remove(node);
                lowFrequencyAdders.Remove(node);
            }

            if (lodMode == 1)
            {
                node.UnsetSprite();
            }
        }

        int AnimationTimeIndex(int n, float length, float t)
        {
            return ((int)(t * n / length) % (n));
        }

        public class RenderMeshFrame
        {
            public Mesh msh;
            public Material[] mats;

            public int n = 0;
            public int n1 = 0;

            public List<RMFMatrixList> matrixList;

            public int blockSize = 1000;
            public UnityEngine.Rendering.ShadowCastingMode castShadows = UnityEngine.Rendering.ShadowCastingMode.On;

            public void ShowMeshInstanced()
            {
                Camera cam = Camera.main;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (n > 0)
                    {
                        for (int j = 0; j < matrixList.Count; j++)
                        {
                            int nj = blockSize;
                            int terminalBlockId = n / blockSize;
                            if (j <= terminalBlockId)
                            {
                                if (j == terminalBlockId)
                                {
                                    nj = blockSize - ((terminalBlockId + 1) * blockSize - n);
                                }

                                Graphics.DrawMeshInstanced(
                                    msh,
                                    0,
                                    mats[i],
                                    matrixList[j].matrices,
                                    nj,
                                    null,
                                    castShadows,
                                    true,
                                    0,
                                    cam
                                );
                            }
                        }
                    }
                }
            }

            public int GetCurrentLength()
            {
                int cLength = 0;

                if (matrixList == null)
                {
                    return -1;
                }

                for (int i = 0; i < matrixList.Count; i++)
                {
                    if (matrixList[i].matrices == null)
                    {
                        return -1;
                    }
                    cLength = cLength + matrixList[i].matrices.Length;
                }

                return cLength;
            }

        }

        public struct RMFMatrixList
        {
            public Matrix4x4[] matrices;
        }
    }
}
