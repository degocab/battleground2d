using UnityEngine;

namespace RTSToolkit
{
    public class Animal : MonoBehaviour
    {
        [HideInInspector] public Vector3 position;
        [HideInInspector] public Quaternion rotation;

        [HideInInspector] public GameObject prefabGo;
        [HideInInspector] public bool animalGoSet = true;
        [HideInInspector] public bool onHighPriority = true;
        public float cullingDistance = 1000f;

        [HideInInspector] public UnityEngine.AI.NavMeshAgent agent;

#if ASTAR
	    [HideInInspector] public AgentPars agentPars;
#endif
        [HideInInspector] public GameObject targetGo;

        public float timeSinceLastMovement = 1000f;

        public int family = 0;
        public float height = 0f;

        public float heightRandomness = 0f;
        [HideInInspector] public float currentHeight = 0f;

        [HideInInspector] public Vector3 movementDestination;

        // 0 - ground animal
        // 1 - air animal (birds, flies)

        public UnitAnimation spriteGameObjectModule;
        [HideInInspector] public RTSMaster rtsm;
        [HideInInspector] public Vector2i terrainTile;
        [HideInInspector] public Terrain terrain;

        Vector3 flyDirection = Vector3.zero;

        void Start()
        {

        }

        public void SetGo()
        {
            if (GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            }

            if (GetComponent<UnitAnimation>() != null)
            {
                spriteGameObjectModule = GetComponent<UnitAnimation>();
            }

#if ASTAR
            if(GetComponent<AgentPars>() != null)
            {
                agentPars = GetComponent<AgentPars>();
            }
#endif
        }

        public void UpdateAnimal(Vector3 camPos, float dt)
        {
            if (family == 1)
            {
                float mag = (camPos - position).magnitude;

                if (mag >= cullingDistance)
                {
                    if (animalGoSet)
                    {
                        gameObject.SetActive(false);
                        animalGoSet = false;
                    }
                }
                else if (mag < cullingDistance)
                {
                    if (animalGoSet == false)
                    {
                        gameObject.SetActive(true);
                        animalGoSet = true;
                        SetGo();
                    }
                }

                if (mag < 3 * cullingDistance)
                {
                    flyDirection = (20f * flyDirection + 2f * Animals.active.GetAttractionForceVector(position, terrainTile) * dt).normalized;

                    if (flyDirection == Vector3.zero)
                    {
                        Debug.Log("flyDirection == Vector3.zero");
                    }

                    position = position + 3f * flyDirection * dt;

                    if (animalGoSet)
                    {
                        currentHeight = currentHeight + dt * Random.Range(-0.1f, 0.1f);

                        if (currentHeight < height - heightRandomness)
                        {
                            currentHeight = height - heightRandomness;
                        }

                        if (currentHeight > height + heightRandomness)
                        {
                            currentHeight = height + heightRandomness;
                        }

                        Vector3 animPos = TerrainProperties.TerrainVectorProc(position) + new Vector3(0f, currentHeight, 0f);
                        transform.position = animPos;
                        Vector3 lookVect = position + flyDirection * dt;
                        lookVect.y = transform.position.y;
                        Vector3 rotationVector = new Vector3((lookVect - animPos).x, 0f, (lookVect - animPos).z);

                        if (rotationVector != Vector3.zero)
                        {
                            transform.rotation = Quaternion.LookRotation(rotationVector);
                        }
                    }
                    else
                    {
                        if (mag < cullingDistance)
                        {
                            Debug.Log("mag < cullingDistance " + mag);
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            RenderMeshModels.active.RemoveTransform(transform);
        }
    }
}
