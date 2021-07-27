using UnityEngine;

namespace RTSToolkit
{
    public class AgentPars : MonoBehaviour
    {
        public ManualRVOs.ManualAgent manualAgent;

        public float radius = 1f;
        public float height = 1f;
        public float maxSpeed = 1f;
        public float stopDistance = 1f;

        void Start()
        {
            AddAgent();
        }

        public void AddAgent()
        {
            manualAgent = ManualRVOs.active.AddAgent(this.gameObject);
#if ASTAR
		    manualAgent.agent.Radius = radius;
#endif
        }

        void OnDestroy()
        {
            RemoveFromManualRVOs();
        }

        public void RemoveFromManualRVOs()
        {
            ManualRVOs.active.RemoveAgent(manualAgent);
        }

        public Vector3 GetTargetPosition()
        {
            return manualAgent.targetPosition;
        }

        public float RemainingDistanceAlongPath()
        {
#if ASTAR
		    return manualAgent.RemainingDistanceAlongPath();
#else
            return 0f;
#endif
        }
    }
}
