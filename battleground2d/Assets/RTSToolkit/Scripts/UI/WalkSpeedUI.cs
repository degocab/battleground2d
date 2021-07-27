using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class WalkSpeedUI : MonoBehaviour
    {
        public static WalkSpeedUI active;

        public Slider slider;
        public bool usePowerLaw = false;
        public float powerLawIndex = 4f;

        float previousValue = 1f;
        [HideInInspector] public float walkSpeed = 1f;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            previousValue = slider.value;
            walkSpeed = slider.value;
        }

        public void ChangeWalkSpeed()
        {
            float value = 0f;

            if (usePowerLaw == true)
            {
                value = Mathf.Pow(slider.value, powerLawIndex);
            }
            else
            {
                value = slider.value;
            }

            if (slider.value < 0.1f)
            {
                value = 0.1f;
            }

            walkSpeed = value;

            if (RTSMaster.active.useAStar)
            {
                AgentPars[] agentPars = UnityEngine.Object.FindObjectsOfType<AgentPars>();

                for (int i = 0; i < agentPars.Length; i++)
                {
                    agentPars[i].maxSpeed = (agentPars[i].maxSpeed / previousValue) * walkSpeed;
                }
            }
            else
            {
                UnityEngine.AI.NavMeshAgent[] navMeshAgents = UnityEngine.Object.FindObjectsOfType<UnityEngine.AI.NavMeshAgent>();

                for (int i = 0; i < navMeshAgents.Length; i++)
                {
                    navMeshAgents[i].speed = (navMeshAgents[i].speed / previousValue) * walkSpeed;
                    navMeshAgents[i].acceleration = (navMeshAgents[i].acceleration / previousValue) * walkSpeed;
                }
            }

            UnitAnimation[] unitAnimations = UnityEngine.Object.FindObjectsOfType<UnitAnimation>();

            for (int i = 0; i < unitAnimations.Length; i++)
            {
                if (unitAnimations[i] != null)
                {
                    unitAnimations[i].currentAnimationSpeed = unitAnimations[i].GetAnimationSpeed();
                }
            }

            previousValue = value;
        }
    }
}
