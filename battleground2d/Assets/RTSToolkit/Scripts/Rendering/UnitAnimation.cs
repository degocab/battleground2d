using UnityEngine;

namespace RTSToolkit
{
    public class UnitAnimation : MonoBehaviour
    {
        [HideInInspector] public UnitAnimationType unitAnimationType;
        public int unitAnimationTypeId = 0;

        [HideInInspector] public int repeatableAnimations;

        public string animName;

        [HideInInspector] public float bilboardDistance = 20f;

        [HideInInspector] public ParticleNode particleNode;

        public RenderMeshAnimations renderMeshAnimations;
        public RenderMesh renderMesh;

        bool isReady = false;

        [HideInInspector] public bool m_castShadows = true;
        [HideInInspector] public bool m_receiveShadows = true;

        [HideInInspector] public float a_phase = 0f;

        [HideInInspector] public UnitAnim currentUnitAnim;

        [HideInInspector] public Vector3 prevPos;
        [HideInInspector] public float walkSpeedMultiplier = 1f;

        [HideInInspector] public int nation = -1;

        [HideInInspector] public bool isSpriteEnabled = true;

        [HideInInspector] public float startTime;
        [HideInInspector] public float currentAnimationSpeed = 1f;

        [HideInInspector] public int currentAnimationIndex = -1;

        void Start()
        {
            UnitAnimationType uat = GetComponent<UnitAnimationType>();

            if (uat != null)
            {
                Destroy(uat);
            }

            unitAnimationType = UnitAnimationTypesHolder.active.unitAnimationTypes[unitAnimationTypeId];

            int mind = RenderMeshModels.active.FindModelIndex(unitAnimationType.modelName);
            int lodIndex = RenderMeshModels.active.renderModels[mind].GetLODIndex((transform.position - Camera.main.transform.position).sqrMagnitude);

            if (lodIndex < 0)
            {
                lodIndex = 0;
            }

            renderMeshAnimations = RenderMeshModels.active.renderModels[mind].renderAnimations[lodIndex];
            int aind = renderMeshAnimations.FindAnimationIndex(animName);

            if (aind > -1)
            {
                renderMeshAnimations.renderMeshAnimations[aind].AddTransform(this);
            }

            isReady = true;
        }

        public void UnsetSprite()
        {
            if (PSpriteLoader.active != null)
            {
                PSpriteLoader.active.clientSpritesGo.Remove(this);
                PSpriteLoader.active.RemoveAnimation(this);
            }

            isSpriteEnabled = false;
        }

        public void DisableSprite()
        {
            isSpriteEnabled = false;
        }

        public void EnableSprite()
        {
            isSpriteEnabled = true;
        }

        public void PlayAnimationCheck(string animationName)
        {
            string oldAnimName = animName;

            if (string.IsNullOrEmpty(animationName) == false)
            {
                if (animationName != oldAnimName)
                {
                    PlayAnimationCheckInner(animationName);
                }
            }
        }

        public void PlayAnimationCheckInner(string animationName)
        {
            if (animationName != animName)
            {
                PlayAnimation(animationName);
            }
        }

        public void PlayAnimation(string animationName)
        {
            if (isReady)
            {
                a_phase = 0f;
                walkSpeedMultiplier = 1f;

                if (string.IsNullOrEmpty(animationName) == false)
                {
                    if (animationName == unitAnimationType.idleAnimation.animationName)
                    {
                        currentUnitAnim = unitAnimationType.idleAnimation;
                    }
                    else if (animationName == unitAnimationType.walkAnimation.animationName)
                    {
                        currentUnitAnim = unitAnimationType.walkAnimation;
                        if (WalkSpeedUI.active != null)
                        {
                            walkSpeedMultiplier = 1f / WalkSpeedUI.active.walkSpeed;
                        }
                    }
                    else if (animationName == unitAnimationType.runAnimation.animationName)
                    {
                        currentUnitAnim = unitAnimationType.runAnimation;

                        if (WalkSpeedUI.active != null)
                        {
                            walkSpeedMultiplier = 1f / WalkSpeedUI.active.walkSpeed;
                        }
                    }
                    else if (animationName == unitAnimationType.attackAnimation.animationName)
                    {
                        currentUnitAnim = unitAnimationType.attackAnimation;
                    }
                    else if (animationName == unitAnimationType.deathAnimation.animationName)
                    {
                        currentUnitAnim = unitAnimationType.deathAnimation;
                    }
                    else
                    {
                        for (int i = 0; i < unitAnimationType.otherAnimations.Length; i++)
                        {
                            if (animationName == unitAnimationType.otherAnimations[i].animationName)
                            {
                                currentUnitAnim = unitAnimationType.otherAnimations[i];
                            }
                        }
                    }
                }

                renderMesh.renderMeshAnimations.PlayAnimation(this, animationName);
            }
        }

        public string GetIdleAnimation()
        {
            if (unitAnimationType.idleAnimation == null)
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Idle animation");
                return "";
            }

            if (string.IsNullOrEmpty(unitAnimationType.idleAnimation.animationName))
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Idle animation");
            }

            return unitAnimationType.idleAnimation.animationName;
        }

        public string GetWalkAnimation()
        {
            if (string.IsNullOrEmpty(unitAnimationType.walkAnimation.animationName))
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Walk animation");
                return "";
            }

            if (string.IsNullOrEmpty(unitAnimationType.walkAnimation.animationName))
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Walk animation");
            }

            return unitAnimationType.walkAnimation.animationName;
        }

        public string GetRunAnimation()
        {
            if (unitAnimationType.runAnimation == null)
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Run animation");
                return "";
            }

            if (string.IsNullOrEmpty(unitAnimationType.runAnimation.animationName))
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Run animation");
            }

            return unitAnimationType.runAnimation.animationName;
        }

        public string GetAttackAnimation()
        {
            if (unitAnimationType.attackAnimation == null)
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Attack animation");
                return "";
            }

            if (string.IsNullOrEmpty(unitAnimationType.attackAnimation.animationName))
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Attack animation");
            }

            return unitAnimationType.attackAnimation.animationName;
        }

        public string GetDeathAnimation()
        {
            if (unitAnimationType.deathAnimation == null)
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Death animation");
                return "";
            }

            if (string.IsNullOrEmpty(unitAnimationType.deathAnimation.animationName))
            {
                Debug.Log("Warning: unit " + unitAnimationType.modelName + " has not assigned Death animation");
            }

            return unitAnimationType.deathAnimation.animationName;
        }

        public float GetAnimationSpeed()
        {
            if (WalkSpeedUI.active != null)
            {
                return walkSpeedMultiplier;
            }

            return 1f;
        }

        void SetNonRepeatableStartTime(string newAnimationName)
        {
            string mName1 = unitAnimationType.modelName;
            int newIndex = PSpriteLoader.active.GetAnimationIndexByModelAndName(mName1, newAnimationName);

            if (newIndex >= 0)
            {
                if (PSpriteLoader.active.repeatableAnimations[newIndex] == 0)
                {
                    if (particleNode != null)
                    {
                        particleNode.nonRepeatableStartTime = Time.time;
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (renderMesh != null)
            {
                renderMesh.RemoveNode(this);
            }

            UnsetSprite();
        }
    }

    [System.Serializable]
    public class UnitAnim
    {
        public string animationName = string.Empty;
        public float animationSpeed = 1f;
        public bool scaleWithSpeed = false;
    }
}
