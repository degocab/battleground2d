using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class UnitPars : MonoBehaviour
    {
        [HideInInspector] public UnitParsType unitParsType;

        public int rtsUnitId = 0;

        [HideInInspector] public bool isBuildFinished = false;

        [HideInInspector] public bool isMovable = true;
        [HideInInspector] public bool onManualControl = false;

        [HideInInspector] public int militaryMode = 0;
        [HideInInspector] public int wanderingMode = 0;

        [HideInInspector] public float sqrSearchDistance = 900f;

        [HideInInspector] public bool strictApproachMode = false;

        [HideInInspector] public bool isAttackable = true;
        [HideInInspector] public bool isDying = false;
        [HideInInspector] public bool isSinking = false;

        [HideInInspector] public Vector3 velocityVector = Vector3.zero;
        [HideInInspector] public Vector3 lastPosition;

        [HideInInspector] public UnityEngine.AI.NavMeshAgent thisNMA = null;
        [HideInInspector] public UnityEngine.AI.NavMeshObstacle thisNMO = null;

        [HideInInspector] public UnitAnimation thisUA = null;
        [HideInInspector] public SpawnPoint thisSpawn = null;

        [HideInInspector] public AgentPars agentPars;

        [HideInInspector] public UnitPars targetUP = null;

        // storing closest building approach position
        [HideInInspector] public Vector3 targetPos = Vector3.zero;

        [HideInInspector] public List<UnitPars> attackers = new List<UnitPars>();

        [HideInInspector] public float prevR;
        [HideInInspector] public int failedR = 0;
        [HideInInspector] public float timeMark = -100f;

        [HideInInspector] public float health = 100.0f;
        [HideInInspector] public float maxHealth = 100.0f;
        [HideInInspector] public float selfHealFactor = 4.0f;

        [HideInInspector] public float lastDamageTakenTime = -1000f;

        [HideInInspector] public float strength = 10.0f;
        [HideInInspector] public float defence = 10.0f;

        [HideInInspector] public int deathCalls = 0;

        [HideInInspector] public int nation = 0;
        [HideInInspector] public string nationName = "";

        // MC options
        [HideInInspector] public bool isSelected = false;
        [HideInInspector] public bool isMovingMC = false;

        [HideInInspector] public float prevDist = 0.0f;
        [HideInInspector] public int failedDist = 0;

        [HideInInspector] public Vector3 manualDestination;
        [HideInInspector] public Vector3 guardingPosition;

        [HideInInspector] public int guardResetCount = -1;

        [HideInInspector] public float rEnclosed = 0.0f;

        [HideInInspector] public UnitPars collectionUnit;

        // -1 - no chopping
        //  1 - approaching
        //  2 - chopping
        //  3 - returning logs  
        //  4 - waiting for wood cutter (if none are build)
        [HideInInspector] public int chopTreePhase = -1;
        [HideInInspector] public float collectionTimeSpend = 0f;

        [HideInInspector] public ResourcePointObject resourcePointObject;
        [HideInInspector] public int deliveryPointId = -1;
        [HideInInspector] public int resourceType = -1;
        [HideInInspector] public int resourceAmount = 0;

        [HideInInspector] public Transform collectionOrDeliveryPoint;

        // !!! unitsGroup and formation are not saved (when saving the game) at the moment !!!
        [HideInInspector] public UnitsGroup unitsGroup = null;
        [HideInInspector] public Formation formation = null;

        [HideInInspector] public float[] levelExp;
        [HideInInspector] public int[] levelValues;

        [HideInInspector] public int totalLevel = 0;

        // 0 - life points    
        // 1 - attack 
        // 2 - defence 
        // 3 - building
        // 4 - wood cutting     
        // 5 - resource collection

        // path resetter
        [HideInInspector] public int failPath = 0;
        [HideInInspector] public float remainingPathDistance = 1000000000000f;

        [HideInInspector] public int fakePathMode = 0;
        [HideInInspector] public int fakePathCount = 0;
        [HideInInspector] public Vector3 restoreTruePath = Vector3.zero;

        // unitsMover    
        [HideInInspector] public Vector3 um_staticPosition;
        [HideInInspector] public Vector3 um_previousPosition;
        [HideInInspector] public int um_completionMark;
        [HideInInspector] public int um_complete;
        [HideInInspector] public float um_stopDistance;
        [HideInInspector] public string um_animationOnMove;
        [HideInInspector] public string um_animationOnComplete;
        [HideInInspector] public bool hasPath = false;
        [HideInInspector] public bool um_isOnMilitaryAvoiders = false;

        [HideInInspector] public int isWandering;
        [HideInInspector] public bool lockForestSpeedChanges = false;

        [HideInInspector] public NetworkUnique networkUnique;

        static bool isApplicationQuiting = false;

        // !!! fires are not saved (when saving the game) at the moment !!!
        [HideInInspector] public FireScaler primaryFire;
        [HideInInspector] public List<FireScaler> fires = new List<FireScaler>();

        [HideInInspector] public MeshFilter mf;

        bool adjustMultiplayerAttackAnimationCor = false;
        [HideInInspector] public bool isRestoring = false;
        [HideInInspector] public bool isBuildingGrowing = false;

        [HideInInspector] public List<ParticleSystem> smokes;

        [HideInInspector] public MeshRenderer meshRenderer;

        void Start()
        {
            UnitParsType upt = GetComponent<UnitParsType>();

            if (upt != null)
            {
                Destroy(upt);
            }

            unitParsType = RTSMaster.active.rtsUnitTypePrefabs[rtsUnitId].GetComponent<UnitParsType>();

            int nLevels = 6;
            levelExp = new float[nLevels];
            levelValues = new int[nLevels];

            for (int i = 0; i < nLevels; i++)
            {
                levelExp[i] = 0f;
                levelValues[i] = 0;
            }

            if (GetComponent<NetworkUnique>() != null)
            {
                networkUnique = GetComponent<NetworkUnique>();
                networkUnique.unitPars = this;
            }

            if (RTSMaster.active.isMultiplayer)
            {
                UpdateHealth(health);
            }

            if (thisUA == null)
            {
                thisUA = GetComponent<UnitAnimation>();
            }

            if (thisSpawn == null)
            {
                if (GetComponent<SpawnPoint>() != null)
                {
                    thisSpawn = GetComponent<SpawnPoint>();
                }
            }

            if (unitParsType.isBuilding)
            {
                mf = GetComponent<MeshFilter>();

                if (smokes != null && smokes.Count > 0)
                {
                    StartCoroutine(SmokeFluctuations());
                }
            }

            meshRenderer = GetComponent<MeshRenderer>();
            collectionOrDeliveryPoint = transform;

            int nChildren = transform.childCount;

            for (int i = 0; i < nChildren; i++)
            {
                Transform children = transform.GetChild(i);
                if (children.gameObject.name == "CollectionOrDeliveryPoint")
                {
                    collectionOrDeliveryPoint = children;
                }
            }
        }

        public void AddExp(int id, float xp)
        {
            if (isLevelCoolDownActive == false)
            {
                levelExp[id] = levelExp[id] + xp;
                UpdateLevel(id);
            }
        }

        public void UpdateLevel(int id)
        {
            int oldLevel = levelValues[id];
            int newLevel = (int)Mathf.Pow((levelExp[id] / 50f), 0.8f);

            if (newLevel > oldLevel)
            {
                levelValues[id] = newLevel;

                if (id == 0)
                {
                    float addHealth = 100f * 0.1f * (newLevel - oldLevel);
                    maxHealth = maxHealth + addHealth;

                    if (isSelected == true)
                    {
                        SelectionManager.active.totalSelectedHealth = SelectionManager.active.totalSelectedHealth + addHealth;
                    }

                    if (health > 0f)
                    {
                        UpdateHealth(health + addHealth);

                        if (isSelected == true)
                        {
                            SelectionManager.active.remainingSelectedHealth = SelectionManager.active.remainingSelectedHealth + addHealth;
                        }
                    }
                }

                if (id == 1)
                {
                    float addStrength = 10f * 0.2f * (newLevel - oldLevel);
                    strength = strength + addStrength;
                }

                if (id == 2)
                {
                    float addDefence = 10f * 0.2f * (newLevel - oldLevel);
                    defence = defence + addDefence;
                }

                float levelForScores = newLevel * newLevel;

                if (levelForScores < 200)
                {
                    Scores.active.AddToMasterScoreDiff(0.1f * newLevel * newLevel, nation);
                }
                else
                {
                    Scores.active.AddToMasterScoreDiff(0.1f * 200f, nation);
                }

                UpdateTotalLevel();

                if (this.gameObject.activeSelf)
                {
                    StartCoroutine(LevelAdvanceCoolDown());
                }
            }
        }

        bool isLevelCoolDownActive = false;
        IEnumerator LevelAdvanceCoolDown()
        {
            isLevelCoolDownActive = true;
            yield return new WaitForSeconds(20);
            isLevelCoolDownActive = false;
        }

        public float NextLevelExp(int id)
        {
            return (50f * Mathf.Pow((levelValues[id] + 1), (1f / 0.8f)));
        }

        public float RemainingExpTillNextLevel(int id)
        {
            return (NextLevelExp(id) - levelExp[id]);
        }

        public void UpdateTotalLevel()
        {
            totalLevel = 0;

            for (int i = 0; i < levelValues.Length; i++)
            {
                totalLevel = totalLevel + levelValues[i];
            }
        }

        public void MoveUnit(Vector3 dest)
        {
            if (RTSMaster.active.useAStar)
            {
                if (agentPars != null)
                {
                    if (agentPars.manualAgent != null)
                    {
                        agentPars.manualAgent.SearchPath(TerrainProperties.TerrainVectorProc(dest));
                    }
                }
            }
            else
            {
                if (thisNMA.enabled)
                {
                    thisNMA.SetDestination(TerrainProperties.TerrainVectorProc(dest));
                }
            }
        }

        public void MoveUnit(Vector3 dest, string animationName)
        {
            thisUA.PlayAnimationCheck(animationName);

            if (RTSMaster.active.useAStar)
            {
                bool pass = true;

                if (agentPars == null)
                {
                    pass = false;
                    Debug.Log("agentPars == null");
                }
                else if (agentPars.manualAgent == null)
                {
                    pass = false;
                }
                else if (TerrainProperties.active == null)
                {
                    pass = false;
                    Debug.Log("TerrainProperties.active == null");
                }

                if (pass)
                {
                    agentPars.manualAgent.SearchPath(TerrainProperties.TerrainVectorProc(dest));
                }
            }
            else
            {
                if (thisNMA.enabled)
                {
                    thisNMA.SetDestination(TerrainProperties.TerrainVectorProc(dest));
                }
            }
        }

        public float GetCurrentSpeed()
        {
            return velocityVector.magnitude;
        }

        public float GetUnitMaxSpeed()
        {
            if (RTSMaster.active.useAStar)
            {
                if (agentPars != null)
                {
                    return agentPars.maxSpeed;
                }
                else
                {
                    return 0f;
                }
            }

            return thisNMA.speed;
        }

        public void StartMultiplayerAttackAnimation()
        {
            if (militaryMode != 30)
            {
                if (adjustMultiplayerAttackAnimationCor == false)
                {
                    adjustMultiplayerAttackAnimationCor = true;
                    StartCoroutine(AdjustMultiplayerAttackAnimationCor());
                }
            }
        }

        public void StopMultiplayerAttackAnimation()
        {
            adjustMultiplayerAttackAnimationCor = false;
        }

        IEnumerator AdjustMultiplayerAttackAnimationCor()
        {
            while (adjustMultiplayerAttackAnimationCor)
            {
                if (Time.time - timeMark > unitParsType.attackWaiter)
                {
                    timeMark = Time.time;

                    if ((isDying == false) && (isSinking == false) && (health > 0))
                    {
                        if (string.IsNullOrEmpty(thisUA.GetAttackAnimation()) == false)
                        {
                            thisUA.PlayAnimation(thisUA.GetAttackAnimation());
                        }
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        public void StopUnit()
        {
            if (RTSMaster.active.useAStar)
            {
                if (agentPars != null)
                {
                    agentPars.manualAgent.StopMoving();
                }
            }
            else
            {
                if (thisNMA.enabled)
                {
                    thisNMA.ResetPath();
                }
            }
        }

        public void StopUnit(string animationName)
        {
            if (this != null && this.transform != null)
            {
                if (thisUA != null)
                {
                    if (isDying == false)
                    {
                        if (isSinking == false)
                        {
                            thisUA.PlayAnimationCheck(animationName);
                        }
                    }
                }

                StopUnit();
            }
        }

        public string GetIdleAnimation()
        {
            if (thisUA == null)
            {
                return "";
            }

            if (unitParsType.isWorker)
            {
                if (resourceType > -1)
                {
                    if (resourceAmount > 0)
                    {
                        if (resourceType < Economy.active.resources.Count)
                        {
                            if (string.IsNullOrEmpty(Economy.active.resources[resourceType].loadedIdleAnimation) == false)
                            {
                                return Economy.active.resources[resourceType].loadedIdleAnimation;
                            }
                        }
                    }
                }
            }

            return thisUA.GetIdleAnimation();
        }

        public string GetWalkAnimation()
        {
            if (thisUA == null)
            {
                return "";
            }

            if (unitParsType.isWorker)
            {
                if (resourceType > -1)
                {
                    if (resourceAmount > 0)
                    {
                        if (resourceType < Economy.active.resources.Count)
                        {
                            if (string.IsNullOrEmpty(Economy.active.resources[resourceType].loadedWalkAnimation) == false)
                            {
                                return Economy.active.resources[resourceType].loadedWalkAnimation;
                            }
                        }
                    }
                }
            }

            return thisUA.GetWalkAnimation();
        }

        public string GetDeathAnimation()
        {
            if (thisUA == null)
            {
                return "";
            }

            if (unitParsType.isWorker)
            {
                if (resourceType > -1)
                {
                    if (resourceAmount > 0)
                    {
                        if (resourceType < Economy.active.resources.Count)
                        {
                            if (string.IsNullOrEmpty(Economy.active.resources[resourceType].loadedDeathAnimation) == false)
                            {
                                return Economy.active.resources[resourceType].loadedDeathAnimation;
                            }
                        }
                    }
                }
            }

            return thisUA.GetDeathAnimation();
        }

        public void Spawn(string natName)
        {
            StartCoroutine(SpawnCor(natName));
        }

        IEnumerator SpawnCor(string natName)
        {
            bool isThisRunning = true;
            UnityNavigation un = UnityNavigation.active;

            if (un == null)
            {
                isThisRunning = false;
            }
            else
            {
                while (isThisRunning)
                {
                    if (UnityNavigation.IsAsyncRunning() == false)
                    {
                        isThisRunning = false;
                    }
                    else
                    {
                        if (nation == Diplomacy.active.playerNation)
                        {
                            LoadingPleaseWait.Activate(true);
                        }
                    }

                    yield return new WaitForSeconds(Random.Range(0.01f, 0.2f));
                }
            }

            isThisRunning = true;

            while (isThisRunning)
            {
                if (GenerateTerrain.active.HasNavigation(transform.position))
                {
                    isThisRunning = false;
                }
                else
                {
                    if (nation == Diplomacy.active.playerNation)
                    {
                        LoadingPleaseWait.Activate(true);
                    }
                }

                yield return new WaitForSeconds(Random.Range(0.2f, 0.3f));
            }

            if (nation == Diplomacy.active.playerNation)
            {
                LoadingPleaseWait.Activate(false);
            }

            nation = RTSMaster.active.GetNationIdByName(natName);

            if ((nation == -1) && (string.IsNullOrEmpty(natName) == false))
            {
                bool natFount = false;

                while (natFount == false)
                {
                    nation = RTSMaster.active.GetNationIdByName(natName);

                    if (nation != -1)
                    {
                        natFount = true;
                    }

                    yield return new WaitForSeconds(Random.Range(0.2f, 0.3f));
                }
            }

            PostSpawn(natName);
            yield return new WaitForEndOfFrame();
        }

        void PostSpawn(string natName)
        {
            nation = RTSMaster.active.GetNationIdByName(natName);
            nationName = natName;

            thisNMA = GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (RTSMaster.active.useAStar)
            {
                agentPars = GetComponent<AgentPars>();

                if (agentPars != null)
                {
                    if (WalkSpeedUI.active != null)
                    {
                        agentPars.maxSpeed = agentPars.maxSpeed * WalkSpeedUI.active.walkSpeed;
                    }
                }
            }
            else
            {
                if (GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
                {
                    if (unitParsType.isBuilding == false)
                    {
                        thisNMA.enabled = true;

                        if (WalkSpeedUI.active != null)
                        {
                            thisNMA.speed = thisNMA.speed * WalkSpeedUI.active.walkSpeed;
                            thisNMA.acceleration = thisNMA.acceleration * WalkSpeedUI.active.walkSpeed;
                        }

                        if (RTSMaster.active.isMultiplayer)
                        {
                            if ((nation > -1) && (nation < RTSMaster.active.nationPars.Count))
                            {
                                if (RTSMaster.active.nationPars[nation] != null)
                                {
                                    if (RTSMaster.active.nationPars[nation].gameObject.GetComponent<NationCentreNetworkNode>() != null)
                                    {
                                        if (RTSMaster.active.nationPars[nation].gameObject.GetComponent<NationCentreNetworkNode>().isPlayer == false)
                                        {
                                            if (RTSMaster.active.rtsCameraNetwork.isHost == 0)
                                            {
                                                thisNMA.enabled = false;
                                                NetworkSyncTransform nst = GetComponent<NetworkSyncTransform>();

                                                if (nst != null)
                                                {
                                                    if (nst._syncNavMeshAgent)
                                                    {
                                                        thisNMA.enabled = true;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (nation != Diplomacy.active.playerNation)
                                            {
                                                thisNMA.enabled = false;
                                                NetworkSyncTransform nst = GetComponent<NetworkSyncTransform>();

                                                if (nst != null)
                                                {
                                                    if (nst._syncNavMeshAgent)
                                                    {
                                                        thisNMA.enabled = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
            {
                thisNMO = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            }

            if (thisUA == null)
            {
                thisUA = GetComponent<UnitAnimation>();
            }

            if (thisUA != null)
            {
                transform.position = transform.position - new Vector3(0f, 0.5f * thisUA.unitAnimationType.spriteSize, 0f);

                // unit colors	
                if (string.IsNullOrEmpty(nationName) == false)
                {
                    if (NationSpawner.active != null)
                    {
                        if (NationSpawner.active.nations != null)
                        {
                            for (int i = 0; i < NationSpawner.active.nations.Count; i++)
                            {
                                if (nationName == NationSpawner.active.nations[i].name)
                                {
                                    thisUA.nation = i;
                                }
                            }
                        }
                    }
                }
            }

            thisSpawn = GetComponent<SpawnPoint>();

            if (thisSpawn != null)
            {
                thisSpawn.nation = nation;
                thisSpawn.enabled = true;
            }

            militaryMode = 10;

            if (unitParsType.isBuilding)
            {
                rEnclosed = 0.6f * rEnclosed;

                bool pass = true;

                if (RTSMaster.active.isMultiplayer)
                {
                    if (RTSMaster.active.rtsCameraNetwork.isHost != 1)
                    {
                        if (nation != Diplomacy.active.playerNation)
                        {
                            pass = false;
                        }
                    }
                    if (nation != Diplomacy.active.playerNation)
                    {
                        if (networkUnique.isPlayer)
                        {
                            pass = false;
                        }
                    }
                }

                if (pass)
                {
                    GrowBuilding();
                }
            }

            BattleSystem battleSystem = BattleSystem.active;
            battleSystem.AddSelfHealer(this);
            battleSystem.AddFromBuffer(this);

            nation = RTSMaster.active.GetNationIdByName(nationName);

            if (unitParsType.isBuilding == false)
            {
                FinishBuilding();

                if ((nation >= 0) && (nation < Scores.active.nUnits.Count))
                {
                    Scores.active.nUnits[nation] = Scores.active.nUnits[nation] + 1;
                    Scores.active.AddToMasterScoreDiff(0.1f, nation);
                }

                UpdateHealth(maxHealth);
            }
            else
            {
                if (nation >= Scores.active.nBuildings.Count)
                {
                    Debug.Log(nation + " nation >= Scores.active.nBuildings.Count " + Scores.active.nBuildings.Count + " " + RTSMaster.active.nationPars.Count);
                }

                if (nation < 0)
                {
                    Debug.Log(nation + " nation < 0 " + Scores.active.nBuildings.Count + " " + nationName + " " + RTSMaster.active.nationPars.Count);
                }

                if (nation >= 0)
                {
                    if (nation < Scores.active.nBuildings.Count)
                    {
                        Scores.active.nBuildings[nation] = Scores.active.nBuildings[nation] + 1;
                    }

                    Scores.active.AddToMasterScoreDiff(1f, nation);
                }

                if (RTSMaster.active.isMultiplayer)
                {
                    if (networkUnique == null)
                    {
                        networkUnique = GetComponent<NetworkUnique>();
                    }

                    if (networkUnique != null)
                    {
                        health = networkUnique.health;
                        UpdateBuildSequenceMesh(health);
                    }
                }
            }

            if (nation >= 0)
            {
                NationAI thisNationAI = RTSMaster.active.nationPars[nation].nationAI;

                if (rtsUnitId == 0)
                {
                    RTSMaster.active.nationPars[nation].transform.position = transform.position;
                }

                if (thisNationAI != null)
                {
                    thisNationAI.SetUnit(this);
                }

                RTSMaster.active.numberOfUnitTypes[nation][rtsUnitId] = RTSMaster.active.numberOfUnitTypes[nation][rtsUnitId] + 1;
                RTSMaster.active.nationPars[nation].resourcesCollection.AddToResourcesCollection(this);
                RTSMaster.active.unitsListByType[rtsUnitId].Add(this);

                if (unitParsType.isWorker)
                {
                    if (RTSMaster.active.useAStar == false)
                    {
                        thisNMA.radius = thisNMA.radius + 0.05f * thisNMA.radius * Random.Range(-1f, 1f);
                        thisNMA.speed = thisNMA.speed + 0.05f * thisNMA.speed * Random.Range(-1f, 1f);
                    }
                }

                if (unitParsType.isBuilding == false)
                {
                    if (unitParsType.isWorker == false)
                    {
                        WandererAI thisWandererAI = null;

                        if (nation != Diplomacy.active.playerNation)
                        {
                            thisWandererAI = RTSMaster.active.nationPars[nation].wandererAI;
                        }
                        else
                        {
                            thisWandererAI = RTSMaster.active.nationPars[nation].gameObject.GetComponent<WandererAI>();
                        }

                        if (thisWandererAI != null)
                        {
                            if (thisWandererAI.guardsPars == null)
                            {
                                thisWandererAI.guardsPars = new List<UnitPars>();
                            }

                            thisWandererAI.guardsPars.Add(this);
                            wanderingMode = 110;
                        }
                    }
                }
            }

            // Refresh buildings menu if hero spawned
            SelectionManager.active.RefreshCentralBuildingMenuOnHeroPresence(this);

            foreach(Transform child in transform)
            {
                if(child.name == "Flag")
                {
                    MeshRenderer flagMeshRenderer = child.GetComponent<MeshRenderer>();

                    if(flagMeshRenderer != null)
                    {
                        flagMeshRenderer.material.color = RTSMaster.active.nationPars[nation].nationColor;
                    }
                }
            }
        }

        public void GrowBuilding()
        {
            if (unitParsType.isBuilding)
            {
                BuildingGrowSystem.active.AddToSystem(this);
            }
        }

        public void RestoreBuilding()
        {
            if (unitParsType.isBuilding)
            {
                if (isRestoring == false)
                {
                    BuildingRestoreSystem.active.AddToSystem(this);
                }
            }
        }

        public bool HasNetworkAuthority()
        {
            bool hasAuth = false;
            int isHost = 100;

            if (networkUnique != null)
            {
                if (RTSMaster.active != null)
                {
                    if (RTSMaster.active.rtsCameraNetwork != null)
                    {
                        isHost = RTSMaster.active.rtsCameraNetwork.isHost;
                    }
                }

                if (isHost == 0)
                {
                    if (nation == Diplomacy.active.playerNation)
                    {
                        hasAuth = true;
                    }
                }

                if ((isHost == 1) && (hasAuth == false))
                {
                    hasAuth = true;
                }
            }

            return hasAuth;
        }

        public void UpdateHealth(float newHealth)
        {
            float dh = newHealth - health;

            if (unitParsType.isBuilding)
            {
                if (dh > 0)
                {
                    UpdateBuildSequenceMesh(newHealth);
                }
                else if (dh < 0)
                {
                    UpdateDestroySequenceMesh(newHealth);
                }
            }

            if (RTSMaster.active.isMultiplayer == false)
            {
                health = newHealth;
                CheckBuildingRestore();

                if (health < 0)
                {
                    if (isDying == false)
                    {
                        if (isSinking == false)
                        {
                            BattleSystem.active.MakeDead(this);
                        }
                    }
                }
            }
            else
            {
                if (RTSMaster.active.rtsCameraNetwork != null)
                {
                    cmd_UpdateUnitHealthDelayedHealth = newHealth;
                    if (isCmd_UpdateUnitHealthDelayedRunning == false)
                    {
                        if (this.gameObject.activeSelf)
                        {
                            StartCoroutine(Cmd_UpdateUnitHealthDelayed());
                        }
                    }
                }
            }
        }

        bool isCmd_UpdateUnitHealthDelayedRunning = false;
        float cmd_UpdateUnitHealthDelayedHealth = 0f;
        IEnumerator Cmd_UpdateUnitHealthDelayed()
        {
            isCmd_UpdateUnitHealthDelayedRunning = true;
            WaitForSeconds wfs = new WaitForSeconds(Time.fixedDeltaTime);

            if (MultiplayerUI.active != null)
            {
                while (
                    (NetworkSyncTransform.TrafficLimitFilter(true) == false) &&
                    (BSystemStatisticsUI.healthSpeed >= BSystemStatisticsUI.healthPassFraction * BSystemStatisticsUI.trafficSpeed)
                )
                {
                    yield return wfs;
                }
            }

            if (this != null)
            {
                if (this.gameObject != null)
                {
                    RTSMultiplayer.n_Cmd_BytesUpdateUnitHealth = RTSMultiplayer.n_Cmd_BytesUpdateUnitHealth + 4;
                    RTSMaster.active.rtsCameraNetwork.Cmd_UpdateUnitHealth(this.gameObject, cmd_UpdateUnitHealthDelayedHealth);
                }
            }

            cmd_UpdateUnitHealthDelayedHealth = health;
            isCmd_UpdateUnitHealthDelayedRunning = false;
            yield return null;
        }

        public void CheckBuildingRestore()
        {
            if (health < maxHealth)
            {
                if (SelectionManager.active.selectedGoPars.Count == 1)
                {
                    if (SelectionManager.active.selectedGoPars[0] == this)
                    {
                        if (unitParsType.isBuilding)
                        {
                            if (SpawnGridUI.active.destroyButton.activeSelf)
                            {
                                if (isRestoring == false)
                                {
                                    if (SpawnGridUI.active.restoreButton.activeSelf == false)
                                    {
                                        SpawnGridUI.active.restoreButton.SetActive(true);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (health >= maxHealth)
            {
                if (SelectionManager.active.selectedGoPars.Count == 1)
                {
                    if (SelectionManager.active.selectedGoPars[0] == this)
                    {
                        if (unitParsType.isBuilding)
                        {
                            if (SpawnGridUI.active.destroyButton.activeSelf)
                            {
                                if (SpawnGridUI.active.restoreButton.activeSelf)
                                {
                                    SpawnGridUI.active.restoreButton.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateBuildSequenceMesh(float newHealth)
        {
            if (unitParsType.buildSequenceMeshes.Count > 0)
            {
                int id1 = (int)((newHealth / maxHealth) * (unitParsType.buildSequenceMeshes.Count - 1));

                if (id1 >= 0)
                {
                    bool pass = false;

                    if (unitParsType.buildSequenceMeshMode)
                    {
                        if (id1 < unitParsType.buildSequenceMeshes.Count)
                        {
                            pass = true;
                        }
                    }
                    else
                    {
                        if ((id1 < unitParsType.buildSequenceMeshes.Count) && (id1 < unitParsType.buildSequenceMaterials.Count))
                        {
                            pass = true;
                        }
                    }

                    if (pass)
                    {
                        mf.mesh = unitParsType.buildSequenceMeshes[id1];

                        if (meshRenderer != null)
                        {
                            if (meshRenderer.enabled == false)
                            {
                                meshRenderer.enabled = true;
                            }
                        }

                        if (unitParsType.buildSequenceMeshMode == false)
                        {
                            meshRenderer.materials = unitParsType.buildSequenceMaterials[id1];
                        }

                        RotatingPart[] rotatingParts = GetComponents<RotatingPart>();

                        for (int i = 0; i < rotatingParts.Length; i++)
                        {
                            RotatingPart rotatingPart = rotatingParts[i];

                            if (rotatingPart != null)
                            {
                                if (id1 == (unitParsType.buildSequenceMeshes.Count - 1))
                                {
                                    if (rotatingPart.rotatingPart.activeSelf == false)
                                    {
                                        rotatingPart.rotatingPart.SetActive(true);
                                    }
                                }

                                if (id1 != (unitParsType.buildSequenceMeshes.Count - 1))
                                {
                                    if (rotatingPart.rotatingPart.activeSelf == true)
                                    {
                                        rotatingPart.rotatingPart.SetActive(false);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateDestroySequenceMesh(float newHealth)
        {
            if (unitParsType.destroySequenceMeshes.Count > 0)
            {
                int id1 = (int)(((maxHealth - newHealth) / maxHealth) * (unitParsType.destroySequenceMeshes.Count - 1));

                if (id1 >= 0 && id1 < unitParsType.destroySequenceMeshes.Count)
                {
                    mf.mesh = unitParsType.destroySequenceMeshes[id1];

                    if (meshRenderer != null)
                    {
                        if (meshRenderer.enabled == false)
                        {
                            meshRenderer.enabled = true;
                        }
                    }

                    if (unitParsType.destroySequenceMeshMode == false)
                    {
                        meshRenderer.materials = unitParsType.destroySequenceMaterials[id1];
                    }

                    RotatingPart[] rotatingParts = GetComponents<RotatingPart>();

                    for (int i = 0; i < rotatingParts.Length; i++)
                    {
                        RotatingPart rotatingPart = rotatingParts[i];

                        if (rotatingPart != null)
                        {
                            if (id1 == 0)
                            {
                                if (rotatingPart.rotatingPart.activeSelf == false)
                                {
                                    rotatingPart.rotatingPart.SetActive(true);
                                }
                            }

                            if (id1 != 0)
                            {
                                if (rotatingPart.rotatingPart.activeSelf == true)
                                {
                                    rotatingPart.rotatingPart.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AttackDelay()
        {
            AttackDelayCor();
        }

        [HideInInspector] public bool attackDelayCorPass = true;
        [HideInInspector] public bool isAttackDelayRunning = false;
        [HideInInspector] public float attackDelayStartTime = 0f;

        void AttackDelayCor()
        {
            if (unitParsType.isWizzard)
            {
                WizzardLightning wizzardLightning = GetComponent<WizzardLightning>();

                if (wizzardLightning != null)
                {
                    wizzardLightning.TriggerStrike();
                }
            }

            if (attackDelayCorPass)
            {
                Attack();
            }

            attackDelayCorPass = true;
            isAttackDelayRunning = false;
        }

        public void Attack()
        {
            Attack(Random.value);
        }

        public void Attack(float val)
        {
            if (targetUP != null)
            {
                if (val < (strength / (strength + targetUP.defence)))
                {
                    float damage = 2.0f * strength * Random.value;

                    float damageCoolDownTime = targetUP.unitParsType.damageCoolDownTime;
                    float damageCoolDownMin = targetUP.unitParsType.damageCoolDownMin * damage;
                    float damageCoolDownMax = targetUP.unitParsType.damageCoolDownMax * damage;

                    damage = GenericMath.InterpolateClamped((Time.time - targetUP.lastDamageTakenTime), 0, damageCoolDownTime, damageCoolDownMax, damageCoolDownMin);

                    targetUP.lastDamageTakenTime = Time.time;

                    float damageDefended = 2.0f * strength - damage;
                    float newHealth = targetUP.health - damage;

                    AddExp(0, Mathf.Clamp(0.5f * damage, 0f, 15f));
                    AddExp(1, Mathf.Clamp(damage, 0f, 15f));

                    AddExp(0, Mathf.Clamp(0.5f * damageDefended, 0f, 15f));
                    AddExp(2, Mathf.Clamp(damageDefended, 0f, 15f));

                    Scores.active.damageMade[nation] = Scores.active.damageMade[nation] + damage;
                    Scores.active.damageObtained[targetUP.nation] = Scores.active.damageObtained[targetUP.nation] + damage;

                    UnitPars targetUPCp = targetUP;

                    if (targetUP.health > 0)
                    {
                        if (targetUP.health - damage < 0)
                        {
                            if (targetUP.nation != nation)
                            {
                                RTSMaster.active.nationPars[targetUP.nation].nationAI.beatenUnits[nation] = RTSMaster.active.nationPars[targetUP.nation].nationAI.beatenUnits[nation] + 1;
                                BattleSystem.active.UpdateBeatenUnitScores(this, targetUP);

                                if (targetUP != null)
                                {
                                    if (targetUP.attackers != null)
                                    {
                                        targetUP.CleanAttackers();
                                    }
                                }
                            }
                        }
                    }

                    targetUPCp.UpdateHealth(newHealth);
                }

                PlayAttackSound();
            }
        }

        void PlayAttackSound()
        {
            if ((transform.position - RTSCamera.active.transform.position).magnitude < 200f)
            {
                if (unitParsType.attackSounds != null)
                {
                    if (unitParsType.attackSounds.Count > 0)
                    {
                        int randid = Random.Range(0, unitParsType.attackSounds.Count);
                        if (unitParsType.attackSounds[randid] != null)
                        {
                            AudioSource.PlayClipAtPoint(unitParsType.attackSounds[randid], transform.position, 1f);
                        }
                    }
                }
            }
        }

        public void PlayDeathSound()
        {
            if ((transform.position - RTSCamera.active.transform.position).magnitude < 200f)
            {
                if (unitParsType.deathSounds != null)
                {
                    if (unitParsType.deathSounds.Count > 0)
                    {
                        int randid = Random.Range(0, unitParsType.deathSounds.Count);
                        if (unitParsType.deathSounds[randid] != null)
                        {
                            AudioSource.PlayClipAtPoint(unitParsType.deathSounds[randid], transform.position, 1f);
                        }
                    }
                }
            }
        }

        public void AssignTarget(UnitPars targ)
        {
            AssignTarget(targ, false);
        }

        public void AssignTarget(UnitPars targ, bool strApprMode)
        {
            bool deathPass = true;

            if (targetUP == null)
            {
                strictApproachMode = false;
            }
            else
            {
                targetUP.attackers.Remove(this);
            }

            if (isDying)
            {
                deathPass = false;
            }
            if (isSinking)
            {
                deathPass = false;
            }

            if (targ != null)
            {
                if (targ.isDying)
                {
                    deathPass = false;
                }
                if (targ.isSinking)
                {
                    deathPass = false;
                }
            }

            if (deathPass)
            {
                UnitPars prevTargetPars = targetUP;

                if (prevTargetPars != null)
                {
                    targetUP = null;

                    if (prevTargetPars.isAttackable == false)
                    {
                        if (prevTargetPars.attackers.Count < prevTargetPars.unitParsType.maxAttackers)
                        {
                            if ((prevTargetPars.isDying == false) && (prevTargetPars.isSinking == false))
                            {
                                prevTargetPars.isAttackable = true;
                            }
                        }
                    }
                }

                if (targ != null)
                {
                    targetUP = targ;
                    targ.attackers.Add(this);
                    militaryMode = 20;
                    strictApproachMode = strApprMode;
                    UnitsMover.active.AddMilitaryAvoider(this, BattleSystem.GetClosestBuildingPoint(this, targetUP), 0);

                    if (targ.isAttackable)
                    {
                        if (targ.attackers.Count >= targ.unitParsType.maxAttackers)
                        {
                            targ.isAttackable = false;
                        }
                    }
                }
                else
                {
                    militaryMode = 10;
                    strictApproachMode = false;

                    if (thisUA != null)
                    {
                        if (thisUA.animName == thisUA.GetAttackAnimation())
                        {
                            thisUA.PlayAnimation(thisUA.GetIdleAnimation());
                        }
                    }
                }
            }
            else
            {
                targetUP = null;
                if (isDying || isSinking)
                {
                    militaryMode = -110;
                }
                else
                {
                    militaryMode = 10;
                    strictApproachMode = false;
                }
            }

            if (targ == null)
            {
                targetUP = null;
                if ((isDying == false) && (isSinking == false))
                {
                    militaryMode = 10;
                    strictApproachMode = false;
                }
            }
        }

        public void CleanAttackers()
        {
            if (RTSMaster.active.isMultiplayer)
            {
                if (RTSMaster.active.rtsCameraNetwork != null)
                {
                    RTSMaster.active.rtsCameraNetwork.Cmd_RemoveAttackers(this.gameObject);
                }
            }
            else
            {
                CleanAttackersInner();
            }
        }

        public void CleanAttackersInner()
        {
            List<UnitPars> attackersCp = new List<UnitPars>();

            for (int i = 0; i < attackers.Count; i++)
            {
                attackersCp.Add(attackers[i]);
            }

            for (int i = 0; i < attackersCp.Count; i++)
            {
                attackersCp[i].AssignTarget(null);
                UnitsMover.active.CompleteMovement(attackersCp[i]);

                if ((attackersCp[i].nation > -1) && (attackersCp[i].nation < RTSMaster.active.nationPars.Count))
                {
                    if (RTSMaster.active.nationPars[attackersCp[i].nation].battleAI != null)
                    {
                        RTSMaster.active.nationPars[attackersCp[i].nation].battleAI.RemoveTarget(targetUP);
                    }
                }
            }

            attackers.Clear();
        }

        public void LaunchArrowDelay(UnitPars targPars, Vector3 launchPoint)
        {
            StartCoroutine(LaunchArrowDelayCor(targPars, launchPoint));
        }

        IEnumerator LaunchArrowDelayCor(UnitPars targPars, Vector3 launchPoint)
        {
            yield return new WaitForSeconds(unitParsType.attackDelay);
            BattleSystem.active.LaunchArrow(this, targPars, launchPoint);
        }

        public void FinishBuilding()
        {
            if (RTSMaster.active.isMultiplayer == false)
            {
                isBuildFinished = true;
            }
            else
            {
                RTSMaster.active.rtsCameraNetwork.Cmd_FinishBuilding(this.gameObject);
            }
        }

        public bool IsEnoughResources(int nat)
        {
            Economy eco = Economy.active;

            if (eco != null)
            {
                UnitParsType upt = RTSMaster.active.rtsUnitTypePrefabsUpt[rtsUnitId];

                for (int i = 0; i < upt.costs.Count; i++)
                {
                    for (int j = 0; j < eco.resources.Count; j++)
                    {
                        if (upt.costs[i].name == eco.resources[j].name)
                        {
                            if (upt.costs[i].amount > eco.nationResources[nat][j].amount)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public void ResetFakePath()
        {
            fakePathMode = 0;
            fakePathCount = 0;
            failPath = 0;
        }

        public void ChangeSmokeColor(Color col)
        {
            for (int j = 0; j < smokes.Count; j++)
            {
                var col1 = smokes[j].colorOverLifetime;
                Gradient grad = new Gradient();

                GradientColorKey[] defaultGradientKeysArray = unitParsType.defaultSmokeGradientKeys.ToArray();
                for (int i = 0; i < defaultGradientKeysArray.Length; i++)
                {
                    defaultGradientKeysArray[i] = new GradientColorKey(defaultGradientKeysArray[i].color * col, defaultGradientKeysArray[i].time);
                }

                GradientAlphaKey[] defaultGradientAlphaKeysArray = unitParsType.defaultSmokeGradientAlphaKeys.ToArray();
                for (int i = 0; i < defaultGradientAlphaKeysArray.Length; i++)
                {
                    defaultGradientAlphaKeysArray[i] = new GradientAlphaKey(defaultGradientAlphaKeysArray[i].alpha, defaultGradientAlphaKeysArray[i].time);
                }

                grad.SetKeys(defaultGradientKeysArray, defaultGradientAlphaKeysArray);
                col1.color = grad;
            }
        }

        IEnumerator SmokeFluctuations()
        {
            while (true)
            {
                if (smokes != null && smokes.Count > 0)
                {
                    for (int i = 0; i < smokes.Count; i++)
                    {
                        if (i < smokes.Count && smokes[i] != null)
                        {
                            var em = smokes[i].emission;

                            if (health >= maxHealth)
                            {
                                if (Random.value < 0.5f)
                                {
                                    em.enabled = false;
                                }
                                else
                                {
                                    em.enabled = true;
                                }
                            }
                            else
                            {
                                em.enabled = false;
                            }

                            if (i != smokes.Count - 1)
                            {
                                yield return new WaitForSeconds(Random.Range(unitParsType.smokeMinUpdateTime, unitParsType.smokeMaxUpdateTime));
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(Random.Range(unitParsType.smokeMinUpdateTime, unitParsType.smokeMaxUpdateTime));
            }
        }

        void OnDestroy()
        {
            if (isApplicationQuiting == false)
            {
                RTSMaster.active.UnsetUnit(this);
            }

            StopCoroutine("AdjustMultiplayerAttackAnimationCor");
            StopCoroutine("SpawnCor");
            StopCoroutine("AttackDelayCor");
            StopCoroutine("LaunchArrowDelayCor");
            StopCoroutine("SmokeFluctuations");
        }

        void OnApplicationQuit()
        {
            isApplicationQuiting = true;
        }

        public bool IsResourceCollectionBuilding()
        {
            for (int i = 0; i < Economy.active.resources.Count; i++)
            {
                if (Economy.active.resources[i].collectionRtsUnitId == rtsUnitId)
                {
                    return true;
                }
            }

            return false;
        }

        public void TakeResourcesFromMiningPoint(int resToTake)
        {
            ResourcePointObject rpo = resourcePointObject;
            rpo.resourceAmount = rpo.resourceAmount - resToTake;

            if (rpo.resourceAmount < 0)
            {
                rpo.resourceAmount = 0;
            }

            float r = ResourcePoint.active.kd_allResLocations.FindNearest_R(transform.position);
            int resToAdd = (int)((1f - 0.6f * (r / 7f)) * rpo.resourceAmount);
            resourceAmount = resToAdd;

            if (isSelected == true)
            {
                MiningPointLabelUI.active.UpdateAmount(resourceAmount);
            }
        }
    }
}
