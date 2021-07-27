using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ASTAR	
using Pathfinding;
using Pathfinding.RVO;
#endif

namespace RTSToolkit
{
    public class Animals : MonoBehaviour
    {
        public static Animals active;

        RTSMaster rtsm;
        public List<GameObject> animalPrefabs = new List<GameObject>();
        public List<float> spawnTerrainDensity = new List<float>();

        List<Animal> animalInstances = new List<Animal>();
        List<Animal> animalInstancesHighPriority = new List<Animal>();

        [HideInInspector] public Transform cam;

        [HideInInspector] public List<Vector2i> filledTiles = new List<Vector2i>();

        public List<AnimalAttractionCenter> animalAttractionCenters = new List<AnimalAttractionCenter>();
        public int numberOfAttractionCentersPerTile = 10;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            cam = Camera.main.transform;
            isTriggerAnimalsInitializerRunning = true;
        }

        public void StartExternally()
        {
            Start();
        }

        public void CleanEverything()
        {
            isTriggerAnimalsInitializerRunning = false;
            RemoveAnimalsFromAllTiles();

            animalInstances = new List<Animal>();
            animalInstancesHighPriority = new List<Animal>();
            filledTiles = new List<Vector2i>();
            animalAttractionCenters = new List<AnimalAttractionCenter>();
        }

        bool isTriggerAnimalsInitializerRunning = false;
        float triggerAnimalsInitializerRunningTime = 0f;

        void TriggerAnimalsInitializer()
        {
            if (isTriggerAnimalsInitializerRunning)
            {
                triggerAnimalsInitializerRunningTime = triggerAnimalsInitializerRunningTime + deltaTime;

                if (triggerAnimalsInitializerRunningTime > 0.1f)
                {
                    triggerAnimalsInitializerRunningTime = 0f;

                    if (RenderMeshModels.active.isStarted)
                    {
                        isTriggerAnimalsInitializerRunning = false;
                        GenerateTerrain.active.TriggerAnimalSpawns();
                    }
                }
            }
        }

        public void FillTerrainTileWithAnimals(Vector2i tile)
        {
            bool isAdded = false;

            for (int i = 0; i < tilesBuffer.Count; i++)
            {
                if (tilesBuffer[i].x == tile.x)
                {
                    if (tilesBuffer[i].z == tile.z)
                    {
                        isAdded = true;
                    }
                }
            }

            if (isAdded == false)
            {
                tilesBuffer.Add(tile);

                if (isFillTerrainTilesWithAnimalsCor == false)
                {
                    isFillTerrainTilesWithAnimalsCor = true;
                }
            }
        }

        bool isFillTerrainTilesWithAnimalsCor = false;
        float fillTerrainTilesWithAnimalsCorTime = 0f;
        List<Vector2i> tilesBuffer = new List<Vector2i>();

        void FillTerrainTilesWithAnimalsCor()
        {
            if (isFillTerrainTilesWithAnimalsCor)
            {
                fillTerrainTilesWithAnimalsCorTime = fillTerrainTilesWithAnimalsCorTime + deltaTime;

                if (fillTerrainTilesWithAnimalsCorTime > 1f)
                {
                    fillTerrainTilesWithAnimalsCorTime = 0f;

                    if (IsNavigationRunning() == false)
                    {
                        List<Vector2i> removals = new List<Vector2i>();

                        for (int i = 0; i < tilesBuffer.Count; i++)
                        {
                            Vector2i tile = tilesBuffer[i];

                            if (IsTileFilled(tile) == false)
                            {
                                Terrain ter2 = GenerateTerrain.active.GetTerrainFromTile(tile);

                                if (ter2 != null)
                                {
                                    if (GenerateTerrain.active.HasNavigation(tile))
                                    {
                                        InstantiatePrefabsFamily(tile, 0);
                                        InstantiatePrefabsFamily(tile, 1);
                                        removals.Add(tile);
                                        filledTiles.Add(tile);
                                    }
                                }
                            }
                            else
                            {
                                removals.Add(tile);
                                filledTiles.Add(tile);
                            }
                        }

                        for (int i = 0; i < removals.Count; i++)
                        {
                            tilesBuffer.Remove(removals[i]);
                        }
                    }
                    if (tilesBuffer.Count == 0)
                    {
                        isFillTerrainTilesWithAnimalsCor = false;
                        int k2 = 0;

                        for (int i = 0; i < animalInstances.Count; i++)
                        {
                            if (animalInstances[i].family == 0)
                            {
                                k2++;
                            }
                        }
                    }
                }
            }
        }

        bool IsNavigationRunning()
        {
            bool isNavigationRunning = true;
#if ASTAR
		    isNavigationRunning = false;
#else
            UnityNavigation un = UnityNavigation.active;

            if (un == null)
            {
                isNavigationRunning = false;
            }
            else
            {
                if (un.asyncOperation == null)
                {
                    isNavigationRunning = false;
                }
                else
                {
                    if (un.asyncOperation.isDone)
                    {
                        isNavigationRunning = false;
                    }
                }
            }
#endif
            return isNavigationRunning;
        }

        public bool IsTileFilled(Vector2i tile)
        {
            for (int i = 0; i < filledTiles.Count; i++)
            {
                if (filledTiles[i].x == tile.x)
                {
                    if (filledTiles[i].z == tile.z)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void InstantiatePrefabsFamily(Vector2i tile, int fam)
        {
            Terrain ter2 = GenerateTerrain.active.GetTerrainFromTile(tile);
            Random.InitState(TerrainProperties.GetTerrainSeed(ter2));

            for (int i = 0; i < animalPrefabs.Count; i++)
            {
                Animal animalPrefab1 = animalPrefabs[i].GetComponent<Animal>();

                if (animalPrefab1.family == fam)
                {
                    int n1 = GenericMath.FloatToIntRandScaled(spawnTerrainDensity[i] * ter2.terrainData.size.x * ter2.terrainData.size.z);
                    for (int j = 0; j < n1; j++)
                    {
                        Vector3 randPos = TerrainProperties.RandomTerrainVectorProc(tile);
                        bool posPass = true;

                        if (animalPrefab1.family != 1)
                        {
                            UnityNavigation un = UnityNavigation.active;
                            if (un != null)
                            {
                                if (un.useMinY)
                                {
                                    if (randPos.y <= un.minY)
                                    {
                                        posPass = false;
                                    }
                                }
                            }
                        }

                        if (posPass)
                        {
                            if (animalPrefab1.family == 1)
                            {
                                randPos.y = randPos.y + animalPrefab1.height;
                            }

                            Quaternion randRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                            GameObject go = Instantiate(animalPrefabs[i], randPos, randRot);

                            Animal animl = go.GetComponent<Animal>();
                            animl.rtsm = rtsm;

                            animl.family = animalPrefab1.family;
                            animl.height = animalPrefab1.height;
                            animl.heightRandomness = animalPrefab1.heightRandomness;
                            animl.currentHeight = Random.Range(animl.height - animl.heightRandomness, animl.height + animl.heightRandomness);

                            animl.position = randPos;
                            animl.rotation = randRot;
                            animl.prefabGo = animalPrefabs[i];

                            if (go.GetComponent<UnitAnimation>() != null)
                            {
                                animl.spriteGameObjectModule = go.GetComponent<UnitAnimation>();
                            }

                            if (animalPrefab1.family == 0)
                            {
                                animl.cullingDistance = animalPrefab1.cullingDistance;
                                animl.movementDestination = TerrainProperties.RandomTerrainVectorProc(tile);
                            }

                            if (animalPrefab1.family == 1)
                            {
                                animl.timeSinceLastMovement = 1000f;
                            }

                            animalInstances.Add(animl);
                            animalInstancesHighPriority.Add(animl);

                            animl.terrainTile = GenerateTerrain.active.GetChunkPosition(randPos);
                            animl.terrain = GenerateTerrain.active.GetTerrainFromTile(animl.terrainTile);

                            UnityEngine.AI.NavMeshAgent nma = go.GetComponent<UnityEngine.AI.NavMeshAgent>();

                            if (nma != null)
                            {
                                agentsToEnable.Add(nma);
                                if (isNavMeshEnablerRunning == false)
                                {
                                    StartCoroutine(NavMeshEnabler());
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < numberOfAttractionCentersPerTile; i++)
            {
                AnimalAttractionCenter aac = new AnimalAttractionCenter();
                aac.position = TerrainProperties.RandomTerrainVectorProc(tile);
                aac.tile = tile;
                animalAttractionCenters.Add(aac);
            }
        }

        List<UnityEngine.AI.NavMeshAgent> agentsToEnable = new List<UnityEngine.AI.NavMeshAgent>();
        bool isNavMeshEnablerRunning = false;

        IEnumerator NavMeshEnabler()
        {
            isNavMeshEnablerRunning = true;
            yield return new WaitForEndOfFrame();

            for (int i = 0; i < agentsToEnable.Count; i++)
            {
                if (agentsToEnable[i] != null)
                {
                    if (agentsToEnable[i].enabled == false)
                    {
                        agentsToEnable[i].enabled = true;
                        if (agentsToEnable[i].isOnNavMesh == false)
                        {
                            Animal animl = agentsToEnable[i].GetComponent<Animal>();
                            if (animl != null)
                            {
                                RemoveAnimal(animl);
                            }
                        }
                    }
                }
            }

            agentsToEnable.Clear();
            isNavMeshEnablerRunning = false;
        }

        public void RemoveAnimalsFromAllTiles()
        {
            for (int i = 0; i < filledTiles.Count; i++)
            {
                Vector2i tile = filledTiles[i];
                RemoveTileAnimalInstances(tile);
                RemoveFilledTileMark(tile);
                RemoveAttractionPoints(tile);
            }

            List<Animal> removals = new List<Animal>();

            for (int i = 0; i < animalInstances.Count; i++)
            {
                removals.Add(animalInstances[i]);
            }

            for (int i = 0; i < removals.Count; i++)
            {
                RemoveAnimal(removals[i]);
            }
        }

        public void RemoveAnimalsFromTile(Vector2i tile)
        {
            RemoveTileAnimalInstances(tile);
            RemoveFilledTileMark(tile);
            RemoveAttractionPoints(tile);
        }

        void RemoveTileAnimalInstances(Vector2i tile)
        {
            List<Animal> removals = new List<Animal>();

            for (int i = 0; i < animalInstances.Count; i++)
            {
                if (Vector2i.IsEqual(animalInstances[i].terrainTile, tile))
                {
                    removals.Add(animalInstances[i]);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                RemoveAnimal(removals[i]);
            }
        }

        void RemoveFilledTileMark(Vector2i tile)
        {
            List<Vector2i> removals = new List<Vector2i>();

            for (int i = 0; i < filledTiles.Count; i++)
            {
                if (Vector2i.IsEqual(filledTiles[i], tile))
                {
                    removals.Add(filledTiles[i]);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                filledTiles.Remove(removals[i]);
            }

            removals = new List<Vector2i>();

            for (int i = 0; i < tilesBuffer.Count; i++)
            {
                if (Vector2i.IsEqual(tilesBuffer[i], tile))
                {
                    removals.Add(tilesBuffer[i]);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                tilesBuffer.Remove(removals[i]);
            }
        }

        void RemoveAttractionPoints(Vector2i tile)
        {
            List<AnimalAttractionCenter> removals = new List<AnimalAttractionCenter>();

            for (int i = 0; i < animalAttractionCenters.Count; i++)
            {
                if (Vector2i.IsEqual(animalAttractionCenters[i].tile, tile))
                {
                    removals.Add(animalAttractionCenters[i]);
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                animalAttractionCenters.Remove(removals[i]);
            }
        }

        void RemoveAnimal(Animal anm)
        {
            animalInstances.Remove(anm);
            animalInstancesHighPriority.Remove(anm);
            RenderMeshModels.active.RemoveTransform(anm.transform);
            Destroy(anm.gameObject);
        }

        float tRandomWalk = 0f;

        void RandomWalk()
        {
            tRandomWalk = tRandomWalk + deltaTime;

            if (tRandomWalk > 0.1f)
            {
                tRandomWalk = 0f;

                for (int i = 0; i < animalInstances.Count; i++)
                {
                    Animal anm = animalInstances[i];

                    if (anm.family == 0)
                    {
                        string curAnimNm = FindAnimationName(anm);

                        if (curAnimNm != anm.spriteGameObjectModule.GetIdleAnimation())
                        {
                            if (curAnimNm != anm.spriteGameObjectModule.GetWalkAnimation())
                            {
                                PlayAnim(anm, anm.spriteGameObjectModule.GetIdleAnimation());
                            }
                        }

                        float curVel = 0f;
                        float maxVel = 0f;

                        if (rtsm.useAStar)
                        {
#if ASTAR
                            curVel = anm.agentPars.manualAgent.agent.CalculatedSpeed;
                            maxVel = anm.agentPars.maxSpeed;
#endif
                        }
                        else
                        {
                            curVel = anm.agent.velocity.magnitude;
                            maxVel = anm.agent.speed;
                        }

                        if (curVel < 0.2f * maxVel)
                        {
                            AnimationSwitch(anm, anm.spriteGameObjectModule.GetWalkAnimation(), anm.spriteGameObjectModule.GetIdleAnimation());
                        }
                        else if (curVel > 0.2f * maxVel)
                        {
                            AnimationSwitch(anm, anm.spriteGameObjectModule.GetIdleAnimation(), anm.spriteGameObjectModule.GetWalkAnimation());
                        }

                        anm.timeSinceLastMovement = anm.timeSinceLastMovement + 0.1f;

                        if (anm.timeSinceLastMovement > Random.Range(100f, 300f))
                        {
                            anm.timeSinceLastMovement = 0f;

                            if (rtsm.useAStar)
                            {
#if ASTAR
							anm.agentPars.manualAgent.SearchPath(TerrainProperties.active.RandomTerrainVectorProc(anm.terrainTile));
#endif
                            }
                            else
                            {
                                if (anm.agent.isOnNavMesh)
                                {
                                    anm.agent.SetDestination(TerrainProperties.RandomTerrainVectorProc(anm.terrainTile));
                                }
                            }
                        }
                    }
                    else if (anm.family == 1)
                    {
                        anm.timeSinceLastMovement = anm.timeSinceLastMovement + 0.1f;

                        if (
                            (anm.timeSinceLastMovement > Random.Range(100f, 300f)) ||
                            ((anm.spriteGameObjectModule.transform.position - anm.movementDestination).magnitude < 30f)
                        )
                        {
                            anm.timeSinceLastMovement = 0f;
                            anm.movementDestination = TerrainProperties.RandomTerrainVectorProc(anm.terrainTile);
                            anm.movementDestination.y = anm.movementDestination.y + anm.height;

                            PlayAnim(anm, anm.spriteGameObjectModule.GetWalkAnimation());
                        }
                    }
                }
            }
        }

        public Vector3 GetAttractionForceVector(Vector3 pos, Vector2i tl)
        {
            Vector3 force = Vector3.zero;

            for (int i = 0; i < animalAttractionCenters.Count; i++)
            {
                if (Vector2i.IsEqual(animalAttractionCenters[i].tile, tl))
                {
                    Vector3 rad = animalAttractionCenters[i].position - pos;
                    force = force + rad.normalized / rad.magnitude;
                }
            }

            return force.normalized;
        }

        void AnimationSwitch(Animal anm, string isPlaying, string toPlay)
        {
            anm.spriteGameObjectModule.PlayAnimationCheck(toPlay);
        }

        string FindAnimationName(Animal anm)
        {
            return (anm.spriteGameObjectModule.animName);
        }

        void PlayAnim(Animal anm, string toPlay)
        {
            anm.spriteGameObjectModule.PlayAnimation(toPlay);
        }

        float fLowFreqUpdate = 0f;
        void LowFreqUpdate()
        {
            fLowFreqUpdate = fLowFreqUpdate + deltaTime;

            if (fLowFreqUpdate > 1f)
            {
                fLowFreqUpdate = 0f;
                Vector3 camPos = cam.position;

                for (int i = 0; i < animalInstances.Count; i++)
                {
                    Animal anm = animalInstances[i];
                    Vector3 dv3 = camPos - anm.position;

                    float mag = (new Vector3(dv3.x, 0f, dv3.z)).sqrMagnitude;
                    float cullingDistance = anm.cullingDistance;
                    float sqrCullingDistance = cullingDistance * cullingDistance;

                    if (mag > 9f * sqrCullingDistance)
                    {
                        if (anm.onHighPriority == true)
                        {
                            anm.onHighPriority = false;
                            animalInstancesHighPriority.Remove(anm);
                        }
                    }
                    else if (mag < 8.41f * sqrCullingDistance)
                    {
                        if (anm.onHighPriority == false)
                        {
                            anm.onHighPriority = true;
                            animalInstancesHighPriority.Add(anm);
                        }
                    }

                    if (anm.onHighPriority == false)
                    {
                        animalInstances[i].UpdateAnimal(camPos, 1f);
                    }
                }
            }
        }

        float deltaTime;
        void Update()
        {
            Vector3 camPos = cam.position;
            deltaTime = Time.deltaTime;

            for (int i = 0; i < animalInstancesHighPriority.Count; i++)
            {
                animalInstancesHighPriority[i].UpdateAnimal(camPos, deltaTime);
            }

            for (int i = 0; i < animalAttractionCenters.Count; i++)
            {
                AnimalAttractionCenter aac = animalAttractionCenters[i];
                aac.timeTillSwitchDirection = aac.timeTillSwitchDirection - deltaTime;

                if (aac.timeTillSwitchDirection < 0f)
                {
                    aac.moveDestination = TerrainProperties.RandomTerrainVectorProc(aac.tile);

                    float rand1 = Random.Range(0f, 1f);
                    float rand2 = 0f;

                    if (rand1 < 0.5f)
                    {
                        rand2 = Random.Range(1.5f, 2.4f);
                    }
                    else
                    {
                        rand2 = Random.Range(4f, 6f);
                    }

                    aac.moveVelocity = rand2;
                    aac.timeTillSwitchDirection = (aac.moveDestination - aac.position).magnitude / aac.moveVelocity + Random.Range(0f, 20f);
                }

                Vector3 moveDir = (aac.moveDestination - aac.position).normalized;
                aac.position = aac.position + aac.moveVelocity * deltaTime * moveDir;
            }

            RandomWalk();
            LowFreqUpdate();
            TriggerAnimalsInitializer();
            FillTerrainTilesWithAnimalsCor();
        }

        public void SwitchPrefabsToUnityNavMesh()
        {
#if UNITY_EDITOR
            for (int i = 0; i < animalPrefabs.Count; i++)
            {
                AgentAstarUnitySwitcher.SwitchPrefabToUnityNavMesh(animalPrefabs[i]);
            }

            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SwitchPrefabsToAStar()
        {
#if UNITY_EDITOR
            for (int i = 0; i < animalPrefabs.Count; i++)
            {
                AgentAstarUnitySwitcher.SwitchPrefabToAStar(animalPrefabs[i]);
            }

            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public static Animals GetActive()
        {
            if (Animals.active == null)
            {
                Animals.active = UnityEngine.Object.FindObjectOfType<Animals>();
            }

            return Animals.active;
        }

        public class AnimalAttractionCenter
        {
            public Vector3 position;
            public Vector3 moveDestination;
            public float moveVelocity = 3f;
            public float timeTillSwitchDirection = -1f;
            public Vector2i tile;
        }
    }
}
