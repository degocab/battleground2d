using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Networking;
using System.IO.Compression;

namespace RTSToolkit
{
    public class SaveLoad : MonoBehaviour
    {
        public static SaveLoad active;

        RTSMaster rtsm;

        [HideInInspector] public List<byte[]> memBytes = new List<byte[]>();
        [HideInInspector] public List<Vector2i> savedNationTileIndices = new List<Vector2i>();

        [HideInInspector] public List<UnitPars> tiledSaveUnits = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> loadedUnits = new List<UnitPars>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        public void Save(string fname)
        {
            SaveManager saver = new SaveManager();

            saver.SaveGeneralPars(rtsm);
            saver.SaveNationPars(rtsm.nationPars);
            saver.SaveUnitPars(rtsm.allUnits);

            string path = Path.Combine(Application.persistentDataPath, fname);

            FileStream fileStream = File.Create(path);
            GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress);

            BinaryFormatter binary = new BinaryFormatter();
            binary.Serialize(gzipStream, saver);

            gzipStream.Close();
            fileStream.Close();
        }

        public void Load(string fname)
        {
            string path = Path.Combine(Application.persistentDataPath, fname);

            if (File.Exists(path))
            {
                UnloadEverything();

                FileStream fileStream = File.Open(path, FileMode.Open);
                GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

                BinaryFormatter binary = new BinaryFormatter();
                SaveManager saver = (SaveManager)binary.Deserialize(gzipStream);

                gzipStream.Close();
                fileStream.Close();

                LoadNations(saver);
                Spawn(saver);
                LoadNationUnits(saver);
                saver.LoadGeneralPars(rtsm);
            }
        }

        public void SaveTerrainTileNations(Vector2i tile)
        {

            List<NationPars> nationsToSave = new List<NationPars>();

            for (int i = 0; i < rtsm.nationPars.Count; i++)
            {
                if (Vector2i.IsEqual(rtsm.nationPars[i].terrainTile, tile))
                {
                    if (i != Diplomacy.active.playerNation)
                    {
                        nationsToSave.Add(rtsm.nationPars[i]);
                    }
                }
            }

            List<UnitPars> unitsToSave = new List<UnitPars>();
            tiledSaveUnits = new List<UnitPars>();

            for (int i = 0; i < rtsm.allUnits.Count; i++)
            {
                int nid = rtsm.allUnits[i].nation;

                if (nid == Diplomacy.active.playerNation)
                {
                    if (Vector2i.IsEqual(GenerateTerrain.active.GetChunkPosition(rtsm.allUnits[i].transform.position), tile))
                    {
                        unitsToSave.Add(rtsm.allUnits[i]);
                        tiledSaveUnits.Add(rtsm.allUnits[i]);
                    }
                }
                else
                {
                    if (nid >= rtsm.nationPars.Count)
                    {
                        Debug.Log("nid >= rtsm.nationPars.Count " + nid);
                    }

                    if ((nid > -1) && (nid < rtsm.nationPars.Count))
                    {
                        if (Vector2i.IsEqual(rtsm.nationPars[nid].terrainTile, tile))
                        {
                            unitsToSave.Add(rtsm.allUnits[i]);
                            tiledSaveUnits.Add(rtsm.allUnits[i]);
                        }
                    }
                }
            }

            SaveManager saver = new SaveManager();
            saver.SaveNationPars(nationsToSave);
            saver.SaveUnitPars(unitsToSave);

            byte[] bytes = SaveLoad.SerializeObject(saver);
            memBytes.Add(bytes);

            savedNationTileIndices.Add(tile);
        }

        public byte[] SavePlayerNationUnitsToBytes()
        {
            List<UnitPars> unitsToSave = new List<UnitPars>();

            for (int i = 0; i < rtsm.allUnits.Count; i++)
            {
                int nid = rtsm.allUnits[i].nation;
                if (nid == Diplomacy.active.playerNation)
                {
                    unitsToSave.Add(rtsm.allUnits[i]);
                }
            }

            SaveManager saver = new SaveManager();
            saver.SaveUnitPars(unitsToSave);
            byte[] bytes = SaveLoad.SerializeObject(saver);

            return bytes;
        }

        public void LoadNationFromBytes(byte[] bytes)
        {
            SaveManager saver = SaveLoad.DeserializeObject(bytes);
            LoadNations(saver);
            Spawn(saver);
            LoadNationUnits(saver);
        }

        public void LoadTerrainTileNations(Vector2i tile)
        {
            StartCoroutine(LoadTerrainTileNationsCor(tile));
        }

        IEnumerator LoadTerrainTileNationsCor(Vector2i tile)
        {
            bool isNavigationReady = false;
#if ASTAR
		isNavigationReady = true;
#else
            UnityNavigation un = UnityNavigation.active;

            while (isNavigationReady == false)
            {
                if (un == null)
                {
                    isNavigationReady = true;
                }
                else
                {
                    if (un.asyncOperation == null)
                    {
                        isNavigationReady = true;
                    }
                    else
                    {
                        if (un.asyncOperation.isDone)
                        {
                            isNavigationReady = true;
                        }
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
#endif
            isNavigationReady = false;

            while (isNavigationReady == false)
            {
                if (GenerateTerrain.active.HasNavigation(tile))
                {
                    isNavigationReady = true;
                }

                yield return new WaitForSeconds(0.5f);
            }

            int ind = savedNationTileIndices.IndexOf(tile);

            if (ind > -1)
            {
                byte[] bytes = memBytes[ind];
                SaveManager saver = SaveLoad.DeserializeObject(bytes);
                LoadNations(saver);
                Spawn(saver);
                LoadNationUnits(saver);

                memBytes.RemoveAt(ind);
                savedNationTileIndices.RemoveAt(ind);
            }

            yield return null;
        }

        public static byte[] SerializeObject<T>(T serializableObject)
        {
            T obj = serializableObject;

            using (MemoryStream stream = new MemoryStream())
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(T));
                x.Serialize(stream, obj);

                return stream.ToArray();
            }
        }

        public static SaveManager DeserializeObject(byte[] serilizedBytes)
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(SaveManager));

            using (MemoryStream stream = new MemoryStream(serilizedBytes))
            {
                return (SaveManager)x.Deserialize(stream);
            }
        }

        public void DeleteSavedGame(string fname)
        {
            if (File.Exists(Application.persistentDataPath + "/" + fname))
            {
                File.Delete(Application.persistentDataPath + "/" + fname);
            }
        }

        public void UnloadEverything()
        {
            UnloadEverything(true);
        }

        public void UnloadEverything(bool unloadMultiplayer)
        {
            List<UnitPars> removals = new List<UnitPars>();

            for (int i = 0; i < rtsm.allUnits.Count; i++)
            {
                if (rtsm.allUnits[i] != null)
                {
                    if (rtsm.allUnits[i].gameObject != null)
                    {
                        if (unloadMultiplayer)
                        {
                            removals.Add(rtsm.allUnits[i]);
                        }
                        else
                        {
#if URTS_UNET
						if(rtsm.allUnits[i].gameObject.GetComponent<NetworkIdentity>() == null)
						{
							removals.Add(rtsm.allUnits[i]);
						}
#else
                            removals.Add(rtsm.allUnits[i]);
#endif
                        }
                    }
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    rtsm.DestroyUnit(removals[i]);
                }
            }

            removals.Clear();
            InstantInnerNationUnload(unloadMultiplayer);
        }

        public void InstantInnerNationUnload()
        {
            InstantInnerNationUnload(true);
        }

        public void InstantInnerNationUnload(bool unloadMultiplayer)
        {
            int n = Diplomacy.active.numberNations;

            for (int i = 0; i < n; i++)
            {
                int i1 = 0;

                if (i > Diplomacy.active.playerNation)
                {
                    i1 = i1 + 1;
                }

                if (i != Diplomacy.active.playerNation)
                {
                    if (i1 < Diplomacy.active.numberNations)
                    {
                        if (unloadMultiplayer)
                        {
                            Diplomacy.active.RemoveNation(Diplomacy.active.GetNationNameFromId(i1));
                        }
                        else
                        {
                            if (i1 > -1 && i1 < RTSMaster.active.nationPars.Count)
                            {
                                if (RTSMaster.active.nationPars[i1] != null)
                                {
                                    if (RTSMaster.active.nationPars[i1].gameObject != null)
                                    {
#if URTS_UNET
									if(RTSMaster.active.nationPars[i1].gameObject.GetComponent<NetworkIdentity>() == null)
									{
										Diplomacy.active.RemoveNation(Diplomacy.active.GetNationNameFromId(i1));
									}
#else
                                        Diplomacy.active.RemoveNation(Diplomacy.active.GetNationNameFromId(i1));
#endif
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Diplomacy.active.RemoveNation(Diplomacy.active.GetPlayerNationName());
        }

        void LoadNations(SaveManager saver)
        {
            for (int i = 0; i < saver.nationParsS.Count; i++)
            {
                saver.LoadNationPars(rtsm, i);
            }
        }

        void LoadNationUnits(SaveManager saver)
        {
            int nPresentUnits = rtsm.allUnits.Count;

            for (int i = 0; i < saver.nationParsS.Count; i++)
            {
                int j = Diplomacy.active.GetNationIdFromName(saver.nationParsS[i].nationName);
                saver.LoadNationUnits(rtsm.nationPars[j], i, nPresentUnits);
            }
        }

        void Spawn(SaveManager saver)
        {
            List<UnitPars> unitPars = new List<UnitPars>();
            List<int> targetUPs = new List<int>();

            loadedUnits = new List<UnitPars>();

            for (int i = 0; i < saver.ups.Count; i++)
            {
                UnitParsS ups = saver.ups[i];

                if (rtsm.isMultiplayer)
                {
                    rtsm.rtsCameraNetwork.AddNetworkComponent(ups.rtsUnitId, ups.VectorFromArray(ups.position), ups.QuaternionFromArray(ups.rotation), ups.nationName, 1);
                }
                else
                {
                    GameObject go = Instantiate(
                        rtsm.rtsUnitTypePrefabs[ups.rtsUnitId],
                        ups.VectorFromArray(ups.position),
                        ups.QuaternionFromArray(ups.rotation)
                    );

                    UnitPars up = go.GetComponent<UnitPars>();
                    loadedUnits.Add(up);

                    if (rtsm.useAStar)
                    {
                        AgentPars agentPars = go.GetComponent<AgentPars>();

                        if (agentPars != null)
                        {
                            up.agentPars = agentPars;
                        }
                    }

                    saver.LoadUnitPars(up, i);
                    targetUPs.Add(ups.targetUP);

                    rtsm.allUnits.Add(up);
                    unitPars.Add(up);

                    MeshRenderer mr = go.GetComponent<MeshRenderer>();

                    if (mr != null)
                    {
                        if (mr.enabled == false)
                        {
                            mr.enabled = true;
                        }
                    }
                }
            }

            rtsm.UpdateAllNumberOfUnitTypes();

            for (int i = 0; i < unitPars.Count; i++)
            {
                UnitParsS ups = saver.ups[i];
                UnitPars up = unitPars[i];
                int iTarg = ups.targetUP;

                if ((iTarg > -1) && (iTarg < unitPars.Count))
                {
                    up.targetUP = unitPars[iTarg];
                }

                if (ups.attackers != null)
                {
                    int attackersCount = ups.attackers.Length;

                    if (attackersCount > 0)
                    {
                        up.attackers = new List<UnitPars>();

                        for (int j = 0; j < attackersCount; j++)
                        {
                            int k = ups.attackers[j];

                            if (k > -1)
                            {
                                if (k < unitPars.Count)
                                {
                                    up.attackers.Add(unitPars[k]);
                                }
                            }
                        }
                    }
                }

                // loading collection unit
                if ((ups.collectionUnit > -1) && (ups.collectionUnit < unitPars.Count))
                {
                    up.collectionUnit = unitPars[ups.collectionUnit];
                }
            }
        }
    }

    [Serializable]
    public class SaveManager
    {
        public List<NationParsS> nationParsS;
        public List<UnitParsS> ups;
        public GeneralParsS generalParsS;

        public SaveManager()
        {
            nationParsS = new List<NationParsS>();
            ups = new List<UnitParsS>();
            generalParsS = new GeneralParsS();
        }

        public void SaveNationPars(List<NationPars> nationList)
        {
            nationParsS = new List<NationParsS>();

            for (int i = 0; i < nationList.Count; i++)
            {
                NationParsS nat1 = new NationParsS();
                nat1.SaveNationPars(nationList[i]);
                nationParsS.Add(nat1);
            }
        }

        public void SaveUnitPars(List<UnitPars> upList)
        {
            ups = new List<UnitParsS>();

            for (int i = 0; i < upList.Count; i++)
            {
                UnitParsS ups1 = new UnitParsS();
                ups1.SaveUnitPars(upList[i]);
                ups.Add(ups1);
            }
        }

        public void SaveGeneralPars(RTSMaster rtsm)
        {
            generalParsS.SavePars(rtsm);
        }

        public void LoadUnitPars(UnitPars up, int i)
        {
            ups[i].LoadUnitPars(up);
        }

        public void LoadNationPars(RTSMaster rtsm, int i)
        {
            nationParsS[i].LoadNationPars(rtsm);
        }

        public void LoadNationUnits(NationPars nationPars, int i, int nPresentUnits)
        {
            nationParsS[i].LoadNationUnits(nationPars, nPresentUnits);
        }

        public void LoadGeneralPars(RTSMaster rtsm)
        {
            generalParsS.LoadPars(rtsm);
        }
    }

    [Serializable]
    public class UnitParsS
    {
        public float[] position;
        public float[] rotation;

        public int rtsUnitId;
        public bool isBuildFinished;
        public bool isMovable;
        public bool onManualControl;

        public int militaryMode;
        public int wanderingMode;

        public float sqrSearchDistance;
        public bool strictApproachMode;

        public bool isAttackable;
        public bool isDying;
        public bool isSinking;

        public float[] velocityVector;
        public float[] lastPosition;

        public int targetUP;

        public float[] targetPos;

        public int[] attackers;

        public float prevR;
        public int failedR;

        public float timeMark;
        public float health;
        public float maxHealth;
        public float selfHealFactor;

        public float lastDamageTakenTime;

        public float strength;
        public float defence;
        public int deathCalls;

        public int nation;
        public string nationName;
        public bool isSelected;

        public bool isMovingMC;
        public float prevDist;
        public int failedDist;

        public float[] manualDestination;
        public float[] guardingPosition;

        public int guardResetCount;
        public float rEnclosed;

        public int collectionUnit;

        public int chopTreePhase;
        public float collectionTimeSpend;

        public int resourcePointObject;
        public int deliveryPointId;

        public Vector2i miningPointResourceTile;

        public int resourceType;
        public int resourceAmount;

        public float[] levelExp;
        public int[] levelValues;
        public int totalLevel;

        public int failPath;

        public float remainingPathDistance;
        public int fakePathMode;
        public int fakePathCount;

        public float[] restoreTruePath;

        public float[] um_staticPosition;
        public float[] um_previousPosition;
        public int um_completionMark;
        public int um_complete;
        public float um_stopDistance;
        public string um_animationOnMove;
        public string um_animationOnComplete;
        public bool hasPath;
        public bool um_isOnMilitaryAvoiders;

        public int isWandering;
        public bool lockForestSpeedChanges;

        public bool isRestoring;
        public bool isBuildingGrowing;


        // SpriteLoader parameters
        public string animName;
        public float bilboardDistance;

        // UnitsMover save
        public bool onMilitaryAvoiders;

        public bool thisNMAEnabled;

        public float[] navMeshDestination;

        public void SaveUnitPars(UnitPars up)
        {
            position = ArrayFromVector(up.transform.position);
            rotation = ArrayFromQuaternion(up.transform.rotation);

            rtsUnitId = up.rtsUnitId;
            isBuildFinished = up.isBuildFinished;
            isMovable = up.isMovable;
            onManualControl = up.onManualControl;

            militaryMode = up.militaryMode;
            wanderingMode = up.wanderingMode;

            sqrSearchDistance = up.sqrSearchDistance;
            strictApproachMode = up.strictApproachMode;

            isAttackable = up.isAttackable;
            isDying = up.isDying;
            isSinking = up.isSinking;

            velocityVector = ArrayFromVector(up.velocityVector);
            lastPosition = ArrayFromVector(up.velocityVector);

            targetUP = RTSMaster.active.allUnits.IndexOf(up.targetUP);

            targetPos = ArrayFromVector(up.targetPos);

            int count1 = up.attackers.Count;

            if (count1 > 0)
            {
                attackers = new int[count1];

                for (int i = 0; i < count1; i++)
                {
                    attackers[i] = RTSMaster.active.allUnits.IndexOf(up.attackers[i]);
                }
            }

            prevR = up.prevR;
            failedR = up.failedR;

            timeMark = up.timeMark;
            health = up.health;
            maxHealth = up.maxHealth;
            selfHealFactor = up.selfHealFactor;

            lastDamageTakenTime = up.lastDamageTakenTime;

            strength = up.strength;
            defence = up.defence;
            deathCalls = up.deathCalls;

            nation = up.nation;
            nationName = up.nationName;
            isSelected = up.isSelected;

            isMovingMC = up.isMovingMC;
            prevDist = up.prevDist;
            failedDist = up.failedDist;

            manualDestination = ArrayFromVector(up.manualDestination);
            guardingPosition = ArrayFromVector(up.guardingPosition);

            guardResetCount = up.guardResetCount;
            rEnclosed = up.rEnclosed;

            collectionUnit = RTSMaster.active.allUnits.IndexOf(up.collectionUnit);

            chopTreePhase = up.chopTreePhase;
            collectionTimeSpend = up.collectionTimeSpend;

            deliveryPointId = up.deliveryPointId;

            if (up.resourcePointObject == null)
            {
                resourcePointObject = -1;
                miningPointResourceTile = null;
            }
            else
            {
                resourcePointObject = up.resourcePointObject.indexOnTerrain;
                miningPointResourceTile = up.resourcePointObject.tile;
            }

            resourceType = up.resourceType;
            resourceAmount = up.resourceAmount;

            levelExp = up.levelExp;
            levelValues = up.levelValues;
            totalLevel = up.totalLevel;

            failPath = up.failPath;
            remainingPathDistance = up.remainingPathDistance;
            fakePathMode = up.fakePathMode;
            fakePathCount = up.fakePathCount;

            restoreTruePath = ArrayFromVector(up.restoreTruePath);

            um_staticPosition = ArrayFromVector(up.um_staticPosition);
            um_previousPosition = ArrayFromVector(up.um_previousPosition);
            um_completionMark = up.um_completionMark;
            um_complete = up.um_complete;
            um_stopDistance = up.um_stopDistance;
            um_animationOnMove = up.um_animationOnMove;
            um_animationOnComplete = up.um_animationOnComplete;
            hasPath = up.hasPath;
            um_isOnMilitaryAvoiders = up.um_isOnMilitaryAvoiders;

            isWandering = up.isWandering;
            lockForestSpeedChanges = up.lockForestSpeedChanges;

            if (up.thisUA != null)
            {
                UnitAnimation sl = up.thisUA;
                animName = sl.animName;
                bilboardDistance = sl.bilboardDistance;
            }

            if (up.thisNMA != null)
            {
                thisNMAEnabled = up.thisNMA.enabled;
            }

            onMilitaryAvoiders = false;

            if (UnitsMover.active.militaryAvoiders.Contains(up))
            {
                onMilitaryAvoiders = true;
            }

            navMeshDestination = ArrayFromVector(up.transform.position);

            if (up.thisNMA != null)
            {
                if (up.unitParsType.isBuilding == false)
                {
                    navMeshDestination = ArrayFromVector(up.thisNMA.destination);
                }
            }

            if (RTSMaster.active.useAStar)
            {
                if (up.agentPars != null)
                {
                    navMeshDestination = ArrayFromVector(up.agentPars.manualAgent.targetPosition);
                }
            }

            isRestoring = up.isRestoring;
            isBuildingGrowing = up.isBuildingGrowing;
        }

        public void LoadUnitPars(UnitPars up)
        {
            up.unitParsType = RTSMaster.active.rtsUnitTypePrefabsUpt[up.rtsUnitId];
            up.mf = up.GetComponent<MeshFilter>();
            up.meshRenderer = up.GetComponent<MeshRenderer>();
            up.isBuildFinished = isBuildFinished;
            up.isMovable = isMovable;
            up.onManualControl = onManualControl;

            up.militaryMode = militaryMode;
            up.wanderingMode = wanderingMode;
            up.sqrSearchDistance = sqrSearchDistance;
            up.strictApproachMode = strictApproachMode;
            up.isAttackable = isAttackable;

            up.isDying = isDying;
            up.isSinking = isSinking;

            up.velocityVector = VectorFromArray(velocityVector);
            up.lastPosition = VectorFromArray(lastPosition);

            GameObject go = up.gameObject;

            if (go.GetComponent<UnitAnimation>() != null)
            {
                UnitAnimation sl = go.GetComponent<UnitAnimation>();
                up.thisUA = sl;

                sl.animName = animName;
                sl.bilboardDistance = bilboardDistance;

                UnitAnimationType uat = sl.GetComponent<UnitAnimationType>();
                if (uat != null)
                {
                    UnityEngine.Object.Destroy(uat);
                }

                sl.unitAnimationType = UnitAnimationTypesHolder.active.unitAnimationTypes[sl.unitAnimationTypeId];
                sl.nation = Diplomacy.active.GetNationIdFromName(nationName);
            }

            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                up.thisNMA = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                up.thisNMA.enabled = thisNMAEnabled;

                if (up.unitParsType.isBuilding == false)
                {
                    if (up.thisNMA.enabled)
                    {
                        up.thisNMA.SetDestination(VectorFromArray(navMeshDestination));
                    }
                }
            }

            if (go.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
            {
                up.thisNMO = go.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            }

            up.agentPars = go.GetComponent<AgentPars>();
            if (up.agentPars != null)
            {
                up.agentPars.AddAgent();
                up.MoveUnit(VectorFromArray(navMeshDestination));
            }

            if (go.GetComponent<SpawnPoint>() != null)
            {
                SpawnPoint spawn = go.GetComponent<SpawnPoint>();
                up.thisSpawn = spawn;
                spawn.nation = Diplomacy.active.GetNationIdFromName(nationName);
                spawn.nationName = nationName;
            }

            up.targetPos = VectorFromArray(targetPos);

            up.prevR = prevR;
            up.failedR = failedR;

            up.timeMark = timeMark;
            up.health = health;
            up.maxHealth = maxHealth;
            up.selfHealFactor = selfHealFactor;

            up.lastDamageTakenTime = lastDamageTakenTime;

            up.strength = strength;
            up.defence = defence;
            up.deathCalls = deathCalls;

            up.nation = Diplomacy.active.GetNationIdFromName(nationName);
            up.nationName = nationName;
            up.isSelected = isSelected;
            up.isMovingMC = isMovingMC;
            up.prevDist = prevDist;
            up.failedDist = failedDist;

            up.manualDestination = VectorFromArray(manualDestination);
            up.guardingPosition = VectorFromArray(guardingPosition);

            up.guardResetCount = guardResetCount;
            up.rEnclosed = rEnclosed;

            up.chopTreePhase = chopTreePhase;

            if (chopTreePhase == 1 || chopTreePhase == 2 || chopTreePhase == 3 || chopTreePhase == 4)
            {
                up.chopTreePhase = 6;
            }

            up.collectionTimeSpend = collectionTimeSpend;
            up.deliveryPointId = deliveryPointId;

            if (resourcePointObject < 0)
            {
                up.resourcePointObject = null;
            }
            else
            {
                up.resourcePointObject = ResourcePoint.active.GetResourcePointObject(resourcePointObject, miningPointResourceTile);
            }

            up.resourceType = resourceType;
            up.resourceAmount = resourceAmount;

            up.levelExp = levelExp;
            up.levelValues = levelValues;
            up.totalLevel = totalLevel;

            up.failPath = failPath;
            up.remainingPathDistance = remainingPathDistance;
            up.fakePathMode = fakePathMode;
            up.fakePathCount = fakePathCount;

            up.restoreTruePath = VectorFromArray(restoreTruePath);

            up.um_staticPosition = VectorFromArray(um_staticPosition);
            up.um_previousPosition = VectorFromArray(um_previousPosition);
            up.um_completionMark = um_completionMark;
            up.um_complete = um_complete;
            up.um_stopDistance = um_stopDistance;
            up.um_animationOnMove = um_animationOnMove;
            up.um_animationOnComplete = um_animationOnComplete;
            up.hasPath = hasPath;
            up.um_isOnMilitaryAvoiders = um_isOnMilitaryAvoiders;

            up.isWandering = isWandering;
            up.lockForestSpeedChanges = lockForestSpeedChanges;

            if (onMilitaryAvoiders)
            {
                UnitsMover.active.militaryAvoiders.Add(up);
            }

            if (up.nation == Diplomacy.active.playerNation)
            {
                RTSMaster.active.nationPars[up.nation].resourcesCollection.AddToResourcesCollection(up);
            }

            RTSMaster.active.nationPars[up.nation].nationAI.SetUnit(up);

            up.isRestoring = isRestoring;
            if (isRestoring)
            {
                up.RestoreBuilding();
            }

            up.isBuildingGrowing = isBuildingGrowing;
            if (isBuildingGrowing)
            {
                up.GrowBuilding();
            }

            if (up.unitParsType.isBuilding)
            {
                if (up.unitParsType.buildSequenceMeshes.Count > 0)
                {
                    int meshId = (int)((up.health / up.maxHealth) * (up.unitParsType.buildSequenceMeshes.Count - 1));

                    RotatingPart[] rotatingParts = up.GetComponents<RotatingPart>();

                    for (int i = 0; i < rotatingParts.Length; i++)
                    {
                        RotatingPart rotatingPart = rotatingParts[i];

                        if (rotatingPart != null)
                        {
                            if (meshId == (up.unitParsType.buildSequenceMeshes.Count - 1))
                            {
                                if (rotatingPart.rotatingPart.activeSelf == false)
                                {
                                    rotatingPart.rotatingPart.SetActive(true);
                                }
                            }
                            if (meshId != (up.unitParsType.buildSequenceMeshes.Count - 1))
                            {
                                if (rotatingPart.rotatingPart.activeSelf == true)
                                {
                                    rotatingPart.rotatingPart.SetActive(false);
                                }
                            }
                        }
                    }
                }

                foreach(Transform child in up.transform)
                {
                    if(child.name == "Flag")
                    {
                        MeshRenderer flagMeshRenderer = child.GetComponent<MeshRenderer>();

                        if(flagMeshRenderer != null && up.nation >= 0 && up.nation < RTSMaster.active.nationPars.Count)
                        {
                            flagMeshRenderer.material.color = RTSMaster.active.nationPars[up.nation].nationColor;
                        }
                    }
                }
            }
        }

        public float[] ArrayFromVector(Vector3 vec)
        {
            float[] array = new float[3];
            array[0] = vec.x;
            array[1] = vec.y;
            array[2] = vec.z;

            return array;
        }

        public float[] ArrayFromQuaternion(Quaternion qt)
        {
            float[] array = new float[4];
            array[0] = qt.x;
            array[1] = qt.y;
            array[2] = qt.z;
            array[3] = qt.w;

            return array;
        }

        public Vector3 VectorFromArray(float[] array)
        {
            return (new Vector3(array[0], array[1], array[2]));
        }

        public Quaternion QuaternionFromArray(float[] array)
        {
            return (new Quaternion(array[0], array[1], array[2], array[3]));
        }
    }

    [Serializable]
    public class NationParsS
    {
        public int nation;

        public float nationSize;
        public float sumOfAllNationsDistances;
        public float rSafe;

        public bool isWizzardNation;
        public bool isWizzardSpawned;

        public string nationName;
        public int nationIcon;

        public float[] position;


        // NationAI
        public bool nationAIEnabled;

        public int maxNumBuildings;

        public List<int> numb;
        public List<int> maxNumb;

        public List<int> minMaxNumb;
        public List<int> maxMaxNumb;

        public float size;
        public List<float> buildingRadii;

        public List<int> spawnedBuildings;

        public List<int> barracks;
        public List<int> factories;
        public List<int> stables;

        public List<int> centralBuildings;

        public List<int> workers;
        public List<List<int>> workersRes;

        public List<int> countAllianceWarning;

        public List<List<int>> resMiningPoints;

        public List<int> nResMiningPoints;
        public List<int> nMaxResMiningPoints;

        public List<int> militaryUnits;

        public List<int> beatenUnits;
        public List<int> allianceAcceptanceTimes;

        public int masterNationId;

        public int nEntitiesUnderAttack;

        public bool isFirstTime;

        public bool runTownBuilder;
        public bool runMiningPointsControl;
        public bool runWorkersSpawner;
        public bool runSetWorkers;
        public bool runRestoreDamagedBuildings;
        public bool runTroopsControl;

        // WandererAI
        public bool wandererAIEnabled;

        public List<int> guardsPars;

        public int wanderingNation;

        public float sumOfAllDistances;
        public float nationDistanceRatio;

        public List<float> nationDistanceRatios;
        public List<int> maxWanderersCounts;

        public List<int> nOponentsInside;

        public List<WandererS> wanderers;
        public List<WandererS> returners;

        public bool areListsReady;

        public bool runReturnGuardsToTheirPositions;
        public bool runWandererPhase;

        // ResourcesCollection   
        public List<int> up_workers;

        public List<List<int>> up_collection;
        public List<List<float[]>> pos_collection;

        public List<List<int>> up_delivery;
        public List<List<float[]>> pos_delivery;

        public void SaveNationPars(NationPars nationPars)
        {
            nation = nationPars.GetNationId();

            nationSize = nationPars.nationSize;
            sumOfAllNationsDistances = nationPars.sumOfAllNationsDistances;
            rSafe = nationPars.rSafe;

            isWizzardNation = nationPars.isWizzardNation;
            isWizzardSpawned = nationPars.isWizzardSpawned;

            ResourcesCollection resourcesCollection = nationPars.gameObject.GetComponent<ResourcesCollection>();
            NationAI nationAI = nationPars.gameObject.GetComponent<NationAI>();
            WandererAI wandererAI = nationPars.gameObject.GetComponent<WandererAI>();

            nationName = nationAI.nationName;

            // NationAI
            nationAIEnabled = nationAI.enabled;

            maxNumBuildings = nationAI.maxNumBuildings;

            numb = nationAI.numb;
            maxNumb = nationAI.maxNumb;

            minMaxNumb = nationAI.minMaxNumb;
            maxMaxNumb = nationAI.maxMaxNumb;

            size = nationAI.size;
            buildingRadii = nationAI.buildingRadii;

            List<UnitPars> upl = SaveLoad.active.tiledSaveUnits;

            spawnedBuildings = UnitParsToIntList(nationAI.spawnedBuildings, upl);
            barracks = UnitParsToIntList(nationAI.barracks, upl);
            factories = UnitParsToIntList(nationAI.factories, upl);
            stables = UnitParsToIntList(nationAI.stables, upl);
            centralBuildings = UnitParsToIntList(nationAI.centralBuildings, upl);

            workers = UnitParsToIntList(nationAI.workers, upl);

            workersRes = UnitParsToIntListList(nationAI.workersRes, upl);

            countAllianceWarning = nationAI.countAllianceWarning;

            resMiningPoints = UnitParsToIntListList(nationAI.resMiningPoints, upl);

            nResMiningPoints = nationAI.nResMiningPoints;
            nMaxResMiningPoints = nationAI.nMaxResMiningPoints;

            militaryUnits = UnitParsToIntList(nationAI.militaryUnits, upl);

            beatenUnits = nationAI.beatenUnits;
            allianceAcceptanceTimes = nationAI.allianceAcceptanceTimes;

            masterNationId = nationAI.masterNationId;

            nEntitiesUnderAttack = nationAI.nEntitiesUnderAttack;

            isFirstTime = nationAI.isFirstTime;

            runTownBuilder = nationAI.runTownBuilder;
            runMiningPointsControl = nationAI.runMiningPointsControl;
            runWorkersSpawner = nationAI.runWorkersSpawner;
            runSetWorkers = nationAI.runSetWorkers;
            runRestoreDamagedBuildings = nationAI.runRestoreDamagedBuildings;
            runTroopsControl = nationAI.runTroopsControl;

            nationIcon = nationPars.nationIcon;
            position = ArrayFromVector(nationPars.gameObject.transform.position);

            // wandererAI
            wandererAIEnabled = wandererAI.enabled;

            guardsPars = UnitParsToIntList(wandererAI.guardsPars, upl);

            wanderingNation = wandererAI.wanderingNation;

            sumOfAllDistances = wandererAI.sumOfAllDistances;
            nationDistanceRatio = wandererAI.nationDistanceRatio;

            nationDistanceRatios = wandererAI.nationDistanceRatios;
            maxWanderersCounts = wandererAI.maxWanderersCounts;

            nOponentsInside = wandererAI.nOponentsInside;

            wanderers = WD_to_WDS_List(wandererAI.wanderers);
            returners = WD_to_WDS_List(wandererAI.returners);

            areListsReady = wandererAI.areListsReady;

            runReturnGuardsToTheirPositions = wandererAI.runReturnGuardsToTheirPositions;
            runWandererPhase = wandererAI.runWandererPhase;

            // ResourcesCollection
            up_workers = UnitParsToIntList(resourcesCollection.up_workers, upl);

            up_collection = new List<List<int>>();
            pos_collection = new List<List<float[]>>();

            for (int i = 0; i < resourcesCollection.collectionPoints.Count; i++)
            {
                up_collection.Add(UnitParsToIntList(resourcesCollection.collectionPoints[i].ups, upl));
                pos_collection.Add(Vector3ToArrayList(resourcesCollection.collectionPoints[i].pos));
            }

            up_delivery = new List<List<int>>();
            pos_delivery = new List<List<float[]>>();

            for (int i = 0; i < resourcesCollection.deliveryPoints.Count; i++)
            {
                up_delivery.Add(UnitParsToIntList(resourcesCollection.deliveryPoints[i].ups, upl));
                pos_delivery.Add(Vector3ToArrayList(resourcesCollection.deliveryPoints[i].pos));
            }
        }

        public void LoadNationPars(RTSMaster rtsm)
        {
            Diplomacy.active.AddNewNationCheckName(VectorFromArray(position), nationIcon, nationName, nationIcon);
            int natId = Diplomacy.active.numberNations - 1;
            rtsm.nationPars[natId].nationAI.isFirstTime = isFirstTime;

            NationPars nationPars = rtsm.nationPars[natId];
            nationPars.nationSize = nationSize;
            nationPars.sumOfAllNationsDistances = sumOfAllNationsDistances;
            nationPars.rSafe = rSafe;

            nationPars.isWizzardNation = isWizzardNation;
            nationPars.isWizzardSpawned = isWizzardSpawned;

            if(NationSpawner.active != null && NationSpawner.active.nations != null)
            {
                for(int i=0; i<NationSpawner.active.nations.Count; i++)
                {
                    if(nationPars.GetNationName() == NationSpawner.active.nations[i].name)
                    {
                        nationPars.nationColor = NationSpawner.active.nations[i].nationColor;
                    }
                }
            }
        }

        public void LoadNationUnits(NationPars nationPars, int nPresentUnits1)
        {
            NationAI nationAI = nationPars.nationAI;
            WandererAI wandererAI = nationPars.wandererAI;
            ResourcesCollection resourcesCollection = nationPars.resourcesCollection;

            nationAI.enabled = nationAIEnabled;

            nationAI.maxNumBuildings = maxNumBuildings;

            nationAI.numb = numb;
            nationAI.maxNumb = maxNumb;

            nationAI.minMaxNumb = minMaxNumb;
            nationAI.maxMaxNumb = maxMaxNumb;

            nationAI.size = size;
            nationAI.buildingRadii = buildingRadii;

            List<UnitPars> upl = SaveLoad.active.loadedUnits;

            nationAI.spawnedBuildings = new List<UnitPars>();
            nationAI.barracks = new List<UnitPars>();
            nationAI.factories = new List<UnitPars>();
            nationAI.stables = new List<UnitPars>();
            nationAI.centralBuildings = new List<UnitPars>();

            nationAI.workers = new List<UnitPars>();

            nationAI.workersRes = new List<List<UnitPars>>();
            for (int i = 0; i < Economy.active.resources.Count; i++)
            {
                nationAI.workersRes.Add(new List<UnitPars>());
            }

            nationAI.resMiningPoints = new List<List<UnitPars>>();
            for (int i = 0; i < ResourcePoint.active.resourcePointTypes.Count; i++)
            {
                nationAI.resMiningPoints.Add(new List<UnitPars>());
            }

            for (int i = 0; i < upl.Count; i++)
            {
                UnitPars up = upl[i];

                if (up.nationName == nationName)
                {
                    if (up.unitParsType.isBuilding)
                    {
                        nationAI.spawnedBuildings.Add(up);
                    }
                    if (up.rtsUnitId == 1)
                    {
                        nationAI.barracks.Add(up);
                    }
                    if (up.rtsUnitId == 6)
                    {
                        nationAI.factories.Add(up);
                    }
                    if (up.rtsUnitId == 7)
                    {
                        nationAI.stables.Add(up);
                    }
                    if (up.rtsUnitId == 0)
                    {
                        nationAI.centralBuildings.Add(up);
                    }

                    if (RTSMaster.active.rtsUnitTypePrefabsUpt[up.rtsUnitId].isWorker)
                    {
                        nationAI.workers.Add(up);
                        if ((up.resourceType > -1) && (up.resourceType < nationAI.workersRes.Count))
                        {
                            nationAI.workersRes[up.resourceType].Add(up);
                        }
                    }

                    if (up.rtsUnitId == 5)
                    {
                        if (up.resourceType == 0)
                        {
                            nationAI.resMiningPoints[up.resourceType].Add(up);
                        }
                        else if (up.resourceType == 1)
                        {
                            nationAI.resMiningPoints[up.resourceType].Add(up);
                        }
                    }

                    if ((up.rtsUnitId >= 11) && (up.rtsUnitId <= 14))
                    {
                        nationAI.militaryUnits.Add(up);
                    }
                    if ((up.rtsUnitId == 16) || (up.rtsUnitId == 17) || (up.rtsUnitId == 18))
                    {
                        nationAI.militaryUnits.Add(up);
                    }
                }
            }

            // NationAI    
            nationAI.countAllianceWarning = countAllianceWarning;

            nationAI.nResMiningPoints = nResMiningPoints;
            nationAI.nMaxResMiningPoints = nMaxResMiningPoints;

            nationAI.beatenUnits = beatenUnits;
            nationAI.allianceAcceptanceTimes = allianceAcceptanceTimes;

            nationAI.masterNationId = masterNationId;

            nationAI.nEntitiesUnderAttack = nEntitiesUnderAttack;

            nationAI.runTownBuilder = runTownBuilder;
            nationAI.runMiningPointsControl = runMiningPointsControl;
            nationAI.runWorkersSpawner = runWorkersSpawner;
            nationAI.runSetWorkers = runSetWorkers;
            nationAI.runRestoreDamagedBuildings = runRestoreDamagedBuildings;
            nationAI.runTroopsControl = runTroopsControl;

            // WandererAI	
            wandererAI.enabled = wandererAIEnabled;

            wandererAI.wanderingNation = wanderingNation;

            wandererAI.sumOfAllDistances = sumOfAllDistances;
            wandererAI.nationDistanceRatio = nationDistanceRatio;

            wandererAI.nationDistanceRatios = nationDistanceRatios;
            wandererAI.maxWanderersCounts = maxWanderersCounts;

            wandererAI.allUnits = RTSMaster.active.allUnits;

            wandererAI.nOponentsInside = nOponentsInside;

            WDS_to_WD_List(wanderers, wandererAI);
            WDS_to_WD_List(returners, wandererAI);

            wandererAI.areListsReady = areListsReady;

            wandererAI.runReturnGuardsToTheirPositions = runReturnGuardsToTheirPositions;
            wandererAI.runWandererPhase = runWandererPhase;

            // ResourcesCollection
            resourcesCollection.SetCollectionAndDeliveryPoints();

            resourcesCollection.up_workers = new List<UnitPars>();

            for (int i = 0; i < resourcesCollection.collectionPoints.Count; i++)
            {
                resourcesCollection.collectionPoints[i].ups = new List<UnitPars>();
                resourcesCollection.collectionPoints[i].pos = new List<Vector3>();
            }

            for (int i = 0; i < resourcesCollection.deliveryPoints.Count; i++)
            {
                resourcesCollection.deliveryPoints[i].ups = new List<UnitPars>();
                resourcesCollection.deliveryPoints[i].pos = new List<Vector3>();
            }

            for (int i = 0; i < upl.Count; i++)
            {
                UnitPars up = upl[i];

                if (up.nation == nation)
                {
                    if (RTSMaster.active.rtsUnitTypePrefabsUpt[up.rtsUnitId].isWorker)
                    {
                        resourcesCollection.up_workers.Add(up);
                    }

                    for (int i1 = 0; i1 < resourcesCollection.collectionPoints.Count; i1++)
                    {
                        if (up.rtsUnitId == resourcesCollection.collectionPoints[i1].rtsUnitId)
                        {
                            resourcesCollection.collectionPoints[i1].ups.Add(up);

                            Vector3 pos = up.transform.position;

                            int nChildren = up.transform.childCount;
                            for (int i2 = 0; i2 < nChildren; i2++)
                            {
                                Transform children = up.transform.GetChild(i2);
                                if (children.gameObject.name == "CollectionOrDeliveryPoint")
                                {
                                    pos = children.position;
                                }
                            }

                            resourcesCollection.collectionPoints[i1].pos.Add(pos);
                        }
                    }

                    for (int i1 = 0; i1 < resourcesCollection.deliveryPoints.Count; i1++)
                    {
                        if (up.rtsUnitId == resourcesCollection.deliveryPoints[i1].rtsUnitId)
                        {
                            resourcesCollection.deliveryPoints[i1].ups.Add(up);

                            Vector3 pos = up.transform.position;

                            int nChildren = up.transform.childCount;
                            for (int i2 = 0; i2 < nChildren; i2++)
                            {
                                Transform children = up.transform.GetChild(i2);
                                if (children.gameObject.name == "CollectionOrDeliveryPoint")
                                {
                                    pos = children.position;
                                }
                            }

                            resourcesCollection.deliveryPoints[i1].pos.Add(pos);
                        }
                    }
                }
            }

            for (int i = 0; i < resourcesCollection.collectionPoints.Count; i++)
            {
                resourcesCollection.collectionPoints[i].RefreshKDTree();
            }

            for (int i = 0; i < resourcesCollection.deliveryPoints.Count; i++)
            {
                resourcesCollection.deliveryPoints[i].RefreshKDTree();
            }
        }

        public float[] ArrayFromVector(Vector3 vec)
        {
            float[] array = new float[3];
            array[0] = vec.x;
            array[1] = vec.y;
            array[2] = vec.z;

            return array;
        }
        public float[] ArrayFromQuaternion(Quaternion qt)
        {
            float[] array = new float[4];
            array[0] = qt.x;
            array[1] = qt.y;
            array[2] = qt.z;
            array[3] = qt.w;

            return array;
        }

        public Vector3 VectorFromArray(float[] array)
        {
            return (new Vector3(array[0], array[1], array[2]));
        }

        public Quaternion QuaternionFromArray(float[] array)
        {
            return (new Quaternion(array[0], array[1], array[2], array[3]));
        }

        public List<float[]> Vector3ToArrayList(List<Vector3> queries)
        {
            List<float[]> list = new List<float[]>();

            for (int i = 0; i < queries.Count; i++)
            {
                list.Add(ArrayFromVector(queries[i]));
            }

            return list;
        }

        public List<Vector3> ArrayToVector3List(List<float[]> queries)
        {
            List<Vector3> list = new List<Vector3>();
            for (int i = 0; i < queries.Count; i++)
            {
                list.Add(VectorFromArray(queries[i]));
            }
            return list;
        }

        public List<int> UnitParsToIntList(List<UnitPars> upQueries, List<UnitPars> upAll)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < upQueries.Count; i++)
            {
                list.Add(upAll.IndexOf(upQueries[i]));
            }

            return list;
        }

        public List<UnitPars> IntToUnitParsList(List<int> intQueries, List<UnitPars> upAll)
        {
            List<UnitPars> list = new List<UnitPars>();

            for (int i = 0; i < intQueries.Count; i++)
            {
                int j = intQueries[i];

                if (j >= 0)
                {
                    list.Add(upAll[j]);
                }
            }

            return list;
        }

        public List<List<int>> UnitParsToIntListList(List<List<UnitPars>> upQueries, List<UnitPars> upAll)
        {
            List<List<int>> list = new List<List<int>>();

            for (int i = 0; i < upQueries.Count; i++)
            {
                list.Add(new List<int>());
            }

            for (int i = 0; i < upQueries.Count; i++)
            {
                for (int j = 0; j < upQueries[i].Count; j++)
                {
                    list[i].Add(upAll.IndexOf(upQueries[i][j]));
                }
            }

            return list;
        }

        public List<List<UnitPars>> IntToUnitParsListList(List<List<int>> intQueries, List<UnitPars> upAll)
        {
            List<List<UnitPars>> list = new List<List<UnitPars>>();

            for (int i = 0; i < intQueries.Count; i++)
            {
                list.Add(new List<UnitPars>());
            }

            for (int i = 0; i < intQueries.Count; i++)
            {
                for (int j = 0; j < intQueries[i].Count; j++)
                {
                    int k = intQueries[i][j];

                    if (k >= 0)
                    {
                        list[i].Add(upAll[k]);
                    }
                }
            }

            return list;
        }

        public List<WandererS> WD_to_WDS_List(List<Wanderer> query)
        {
            List<WandererS> list = new List<WandererS>();

            for (int i = 0; i < query.Count; i++)
            {
                list.Add(new WandererS());
                list[i].SaveWanderer(query[i]);
            }

            return list;
        }

        public void WDS_to_WD_List(List<WandererS> query, WandererAI wAI)
        {
            for (int i = 0; i < query.Count; i++)
            {
                query[i].LoadWanderer(wAI);
            }
        }
    }

    [Serializable]
    public class WandererS
    {
        public List<int> wanderersPars;
        public int nationToWander;
        public float zeroTime;

        public void SaveWanderer(Wanderer wd)
        {
            wanderersPars = UnitParsToIntList(wd.wanderersPars, RTSMaster.active.allUnits);
            nationToWander = wd.nationToWander;
            zeroTime = wd.zeroTime;
        }

        public void LoadWanderer(WandererAI wAI)
        {
            Wanderer wd = new Wanderer(wAI);
            wd.wanderersPars = IntToUnitParsList(wanderersPars, RTSMaster.active.allUnits);
            wd.nationToWander = nationToWander;
            wd.zeroTime = zeroTime;
        }

        public List<int> UnitParsToIntList(List<UnitPars> upQueries, List<UnitPars> upAll)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < upQueries.Count; i++)
            {
                list.Add(upAll.IndexOf(upQueries[i]));
            }

            return list;
        }

        public List<UnitPars> IntToUnitParsList(List<int> intQueries, List<UnitPars> upAll)
        {
            List<UnitPars> list = new List<UnitPars>();

            for (int i = 0; i < intQueries.Count; i++)
            {
                int j = intQueries[i];
                if (j >= 0)
                {
                    list.Add(upAll[j]);
                }
            }

            return list;
        }
    }

    [Serializable]
    public class GeneralParsS
    {
        // Economy
        public List<List<int>> nationResourceAmounts;

        // Diplomacy
        public List<List<int>> relations;

        // Scores
        public List<int> nUnits;
        public List<int> nBuildings;

        public List<int> unitsLost;
        public List<int> buildingsLost;

        public List<float> damageMade;
        public List<float> damageObtained;

        public List<float> masterScores;
        public List<float> masterScoresDiff;

        public List<Scores.TechTreeLocker> techTreeLockers;

        // UnitsMover
        public List<int> staticMovers;
        public List<int> followers;
        public List<int> militaryAvoiders;

        // RTSMaster
        public List<List<int>> numberOfUnitTypes;
        public List<List<int>> numberOfUnitTypesPrev;
        public List<List<int>> unitTypeLocking;
        public List<List<float>> unitTypeLockingProgress;

        public List<List<int>> unitsListByType;

        // Camera    
        public float[] camPos;
        public float[] camRot;

        // TimeOfDay
        public float[] sunRotation;

        // RTSCameraInGameControls
        public bool isRTSCameraInGameControlsSaved;

        public string rtsc_moveForwardKey;
        public string rtsc_moveBackwardKey;
        public string rtsc_moveLeftKey;
        public string rtsc_moveRightKey;

        public string rtsc_rotateUpKey;
        public string rtsc_rotateDownKey;
        public string rtsc_rotateLeftKey;
        public string rtsc_rotateRightKey;

        public string rtsc_zoomInKey;
        public string rtsc_zoomOutKey;

        public bool rtsc_edgeMovement;
        public bool rtsc_rotateWithMouse;
        public bool rtsc_zoomWithMouse;
        public bool rtsc_flipHorizontalRotation;
        public bool rtsc_flipVerticalRotation;
        public bool rtsc_flipZoom;

        public float rtsc_moveSpeed;
        public float rtsc_moveAcceleration;
        public float rtsc_rotationSpeed;
        public float rtsc_zoomSpeed;
        public float rtsc_minimumTerrainHeight;
        public float rtsc_maximumTerrainHeight;
        public float rtsc_minimumAbsoluteHeight;

        // RPGCameraInGameControls
        public bool isRPGCameraInGameControlsSaved;

        public string rpgc_rotateRightKey;
        public string rpgc_rotateLeftKey;
        public string rpgc_rotateUpKey;
        public string rpgc_rotateDownKey;

        public string rpgc_zoomInKey;
        public string rpgc_zoomOutKey;

        public bool rpgc_zoomWithMouse;
        public bool rpgc_flipZoom;

        public float rpgc_rotationSpeed;
        public float rpgc_zoomSpeed;
        public float rpgc_maxZoomOut;

        // CameraSwitcher	
        public bool isCameraSwitcherSaved;

        public int cs_followPars;
        public float[] cs_lastRTSposition;
        public float[] cs_lastRTSrotation;
        public int cs_mode;

        // GraphicsSettings
        public bool isGraphicsSettingsSaved;

        public bool gs_materialInstancing;
        public bool gs_meshInstancing;

        public float gs_shadowCastDistance;
        public float gs_shadowReceiveDistance;
        public float gs_cameraFarClippingPlane;

        public int gs_waterDetails;
        public int gs_qualityPreset;
        public int gs_terrainResolution;

        public bool gs_fireLights;
        public bool gs_fireArrowLights;
        public bool gs_buildingLights;

        // Notifications
        public bool isNotificationsSaved;

        public bool nt_taxes;
        public bool nt_warWarning;

        public void SavePars(RTSMaster rtsm)
        {
            // Economy
            nationResourceAmounts = new List<List<int>>();

            for (int i = 0; i < Economy.active.nationResources.Count; i++)
            {
                nationResourceAmounts.Add(new List<int>());

                for (int j = 0; j < Economy.active.nationResources[i].Count; j++)
                {
                    nationResourceAmounts[i].Add(Economy.active.nationResources[i][j].amount);
                }
            }

            // Diplomacy	
            relations = Diplomacy.active.relations;

            // Scores	
            nUnits = Scores.active.nUnits;
            nBuildings = Scores.active.nBuildings;

            unitsLost = Scores.active.unitsLost;
            buildingsLost = Scores.active.buildingsLost;

            damageMade = Scores.active.damageMade;
            damageObtained = Scores.active.damageObtained;

            masterScores = Scores.active.masterScores;
            masterScoresDiff = Scores.active.masterScoresDiff;

            techTreeLockers = Scores.active.techTreeLockers;

            // UnitsMover	
            militaryAvoiders = UnitParsToIntList(UnitsMover.active.militaryAvoiders, rtsm.allUnits);

            // RTSMaster	
            numberOfUnitTypes = rtsm.numberOfUnitTypes;
            numberOfUnitTypesPrev = rtsm.numberOfUnitTypesPrev;
            unitTypeLocking = rtsm.unitTypeLocking;
            unitTypeLockingProgress = rtsm.unitTypeLockingProgress;
            unitsListByType = UnitParsToIntListList(rtsm.unitsListByType, rtsm.allUnits);

            // Camera	
            camPos = ArrayFromVector(RTSCamera.active.transform.position);
            camRot = ArrayFromQuaternion(RTSCamera.active.transform.rotation);

            // Time of day
            sunRotation = ArrayFromQuaternion(TimeOfDay.active.transform.rotation);

            // RTSCameraInGameControls
            if (RTSCameraInGameControls.active != null)
            {
                isRTSCameraInGameControlsSaved = true;

                rtsc_moveForwardKey = RTSCameraInGameControls.active.keys["MoveForward"].ToString();
                rtsc_moveBackwardKey = RTSCameraInGameControls.active.keys["MoveBackward"].ToString();
                rtsc_moveLeftKey = RTSCameraInGameControls.active.keys["MoveLeft"].ToString();
                rtsc_moveRightKey = RTSCameraInGameControls.active.keys["MoveRight"].ToString();

                rtsc_rotateUpKey = RTSCameraInGameControls.active.keys["RotateUp"].ToString();
                rtsc_rotateDownKey = RTSCameraInGameControls.active.keys["RotateDown"].ToString();
                rtsc_rotateLeftKey = RTSCameraInGameControls.active.keys["RotateLeft"].ToString();
                rtsc_rotateRightKey = RTSCameraInGameControls.active.keys["RotateRight"].ToString();

                rtsc_zoomInKey = RTSCameraInGameControls.active.keys["ZoomIn"].ToString();
                rtsc_zoomOutKey = RTSCameraInGameControls.active.keys["ZoomOut"].ToString();


                rtsc_edgeMovement = RTSCamera.active.edgeMovement;
                rtsc_rotateWithMouse = RTSCamera.active.rotateWithMouse;
                rtsc_zoomWithMouse = RTSCamera.active.zoomWithMouse;
                rtsc_flipHorizontalRotation = RTSCamera.active.flipHorizontalRotation;
                rtsc_flipVerticalRotation = RTSCamera.active.flipVerticalRotation;
                rtsc_flipZoom = RTSCamera.active.flipZoom;


                rtsc_moveSpeed = RTSCamera.active.moveSpeed;
                rtsc_moveAcceleration = RTSCamera.active.moveAcceleration;
                rtsc_rotationSpeed = RTSCamera.active.rotationSpeed;
                rtsc_zoomSpeed = RTSCamera.active.zoomSpeed;
                rtsc_minimumTerrainHeight = RTSCamera.active.minZoomHeight;
                rtsc_maximumTerrainHeight = RTSCamera.active.maxZoomHeight;
                rtsc_minimumAbsoluteHeight = RTSCamera.active.minCameraHeight;
            }
            else
            {
                isRTSCameraInGameControlsSaved = false;
            }

            // RPGCameraInGameControls
            if (RPGCameraInGameControls.active != null)
            {
                isRPGCameraInGameControlsSaved = true;

                rpgc_rotateRightKey = RPGCameraInGameControls.active.keys["RotateRight"].ToString();
                rpgc_rotateLeftKey = RPGCameraInGameControls.active.keys["RotateLeft"].ToString();
                rpgc_rotateUpKey = RPGCameraInGameControls.active.keys["RotateUp"].ToString();
                rpgc_rotateDownKey = RPGCameraInGameControls.active.keys["RotateDown"].ToString();

                rpgc_zoomInKey = RPGCameraInGameControls.active.keys["ZoomIn"].ToString();
                rpgc_zoomOutKey = RPGCameraInGameControls.active.keys["ZoomOut"].ToString();

                rpgc_zoomWithMouse = RPGCamera.active.zoomWithMouse;
                rpgc_flipZoom = RPGCamera.active.flipZoom;

                rpgc_rotationSpeed = RPGCamera.active.rotationSpeed;
                rpgc_zoomSpeed = RPGCamera.active.zoomSpeed;
                rpgc_maxZoomOut = RPGCamera.active.maxZoomOut;
            }
            else
            {
                isRPGCameraInGameControlsSaved = false;
            }

            // CameraSwitcher
            if (CameraSwitcher.active != null)
            {
                isCameraSwitcherSaved = true;

                cs_followPars = UnitParsToInt(CameraSwitcher.active.followPars, rtsm.allUnits);
                cs_lastRTSposition = ArrayFromVector(CameraSwitcher.active.lastRTSposition);
                cs_lastRTSrotation = ArrayFromQuaternion(CameraSwitcher.active.lastRTSrotation);
                cs_mode = CameraSwitcher.active.mode;

                if (cs_mode == 1)
                {
                    cs_lastRTSposition = ArrayFromVector(RTSCamera.active.transform.position);
                    cs_lastRTSrotation = ArrayFromQuaternion(RTSCamera.active.transform.rotation);
                }
            }
            else
            {
                isCameraSwitcherSaved = false;
            }

            // GraphicsSettings
            if (GraphicsSettings.active != null)
            {
                isGraphicsSettingsSaved = true;

                gs_materialInstancing = GraphicsSettings.active.materialInstancing.isOn;
                gs_meshInstancing = GraphicsSettings.active.meshInstancing.isOn;

                if (RenderMeshModels.active != null)
                {
                    gs_shadowCastDistance = RenderMeshModels.active.shadowCastDistance;
                    gs_shadowReceiveDistance = RenderMeshModels.active.shadowReceiveDistance;
                }

                gs_cameraFarClippingPlane = Camera.main.farClipPlane;
                gs_waterDetails = GraphicsSettings.active.waterDetails.value;
                gs_qualityPreset = GraphicsSettings.active.qualityPreset.value;
                gs_terrainResolution = GraphicsSettings.active.terrainResolution.value;

                gs_fireLights = GraphicsSettings.active.fireLights.isOn;
                gs_fireArrowLights = GraphicsSettings.active.fireArrowLights.isOn;
                gs_buildingLights = GraphicsSettings.active.buildingLights.isOn;
            }
            else
            {
                isGraphicsSettingsSaved = false;
            }

            // Notifications
            if (NotificationsUI.active != null)
            {
                isNotificationsSaved = true;

                nt_taxes = NotificationsUI.active.taxes.isOn;
                nt_warWarning = NotificationsUI.active.warWarning.isOn;
            }
        }

        public void LoadPars(RTSMaster rtsm)
        {
            // Economy
            Economy.active.nationResources.Clear();

            for (int i = 0; i < nationResourceAmounts.Count; i++)
            {
                Economy.active.nationResources.Add(new List<EconomyResource>());

                for (int j = 0; j < nationResourceAmounts[i].Count; j++)
                {
                    EconomyResource er = new EconomyResource();
                    er.name = Economy.active.resources[j].name;
                    er.icon = Economy.active.resources[j].icon;
                    er.amount = nationResourceAmounts[i][j];
                    er.producers = Economy.active.resources[j].producers;
                    er.consumersRtsIds = Economy.active.resources[j].consumersRtsIds;
                    er.taxesAndWagesFactor = Economy.active.resources[j].taxesAndWagesFactor;
                    Economy.active.nationResources[i].Add(er);
                }
            }

            PlayerResourcesUI prui = PlayerResourcesUI.active;

            if (prui != null)
            {
                prui.InitializeResources();
            }

            Economy.active.RefreshResources();

            // Scores	
            Scores.active.nUnits = nUnits;
            Scores.active.nBuildings = nBuildings;

            Scores.active.unitsLost = unitsLost;
            Scores.active.buildingsLost = buildingsLost;

            Scores.active.damageMade = damageMade;
            Scores.active.damageObtained = damageObtained;

            Scores.active.masterScores = masterScores;
            Scores.active.masterScoresDiff = masterScoresDiff;

            Scores.active.techTreeLockers = techTreeLockers;

            // Diplomacy	
            Diplomacy.active.relations = relations;

            // UnitsMover	
            UnitsMover.active.militaryAvoiders = IntToUnitParsList(militaryAvoiders, rtsm.allUnits);

            // RTSMaster	
            rtsm.numberOfUnitTypes = numberOfUnitTypes;
            rtsm.numberOfUnitTypesPrev = numberOfUnitTypesPrev;
            rtsm.unitTypeLocking = unitTypeLocking;

            for (int i = 0; i < unitTypeLocking.Count; i++)
            {
                if (i == Diplomacy.active.playerNation)
                {
                    for (int j = 0; j < unitTypeLocking[i].Count; j++)
                    {
                        if (unitTypeLocking[i][j] == 0)
                        {
                            SpawnGridUI.active.DisableElement(j);
                        }
                        else if (unitTypeLocking[i][j] == 1)
                        {
                            SpawnGridUI.active.EnableElement(j);
                        }
                    }
                }
            }

            rtsm.unitTypeLockingProgress = unitTypeLockingProgress;
            rtsm.unitsListByType = IntToUnitParsListList(unitsListByType, rtsm.allUnits);

            RTSCamera.active.transform.position = VectorFromArray(camPos);
            RTSCamera.active.transform.rotation = QuaternionFromArray(camRot);

            // TimeOfDay
            TimeOfDay.active.transform.rotation = QuaternionFromArray(sunRotation);

            // RTSCameraInGameControls
            if (isRTSCameraInGameControlsSaved)
            {
                RTSCameraInGameControls rtsc = RTSCameraInGameControls.active;

                if (rtsc != null)
                {
                    KeyCode key = StringToKeyCode(rtsc_moveForwardKey);
                    rtsc.keys["MoveForward"] = key;
                    rtsc.moveForward.text = rtsc_moveForwardKey;
                    RTSCamera.active.moveForward = key;

                    key = StringToKeyCode(rtsc_moveBackwardKey);
                    rtsc.keys["MoveBackward"] = key;
                    rtsc.moveBackward.text = rtsc_moveBackwardKey;
                    RTSCamera.active.moveBackward = key;

                    key = StringToKeyCode(rtsc_moveLeftKey);
                    rtsc.keys["MoveLeft"] = key;
                    rtsc.moveLeft.text = rtsc_moveLeftKey;
                    RTSCamera.active.moveLeft = key;

                    key = StringToKeyCode(rtsc_moveRightKey);
                    rtsc.keys["MoveRight"] = key;
                    rtsc.moveRight.text = rtsc_moveRightKey;
                    RTSCamera.active.moveRight = key;

                    key = StringToKeyCode(rtsc_rotateUpKey);
                    rtsc.keys["RotateUp"] = key;
                    rtsc.rotateUp.text = rtsc_rotateUpKey;
                    RTSCamera.active.rotateUp = key;

                    key = StringToKeyCode(rtsc_rotateDownKey);
                    rtsc.keys["RotateDown"] = key;
                    rtsc.rotateDown.text = rtsc_rotateDownKey;
                    RTSCamera.active.rotateDown = key;

                    key = StringToKeyCode(rtsc_rotateLeftKey);
                    rtsc.keys["RotateLeft"] = key;
                    rtsc.rotateLeft.text = rtsc_rotateLeftKey;
                    RTSCamera.active.rotateLeft = key;

                    key = StringToKeyCode(rtsc_rotateRightKey);
                    rtsc.keys["RotateRight"] = key;
                    rtsc.rotateRight.text = rtsc_rotateRightKey;
                    RTSCamera.active.rotateRight = key;

                    key = StringToKeyCode(rtsc_zoomInKey);
                    rtsc.keys["ZoomIn"] = key;
                    rtsc.zoomIn.text = rtsc_zoomInKey;
                    RTSCamera.active.zoomInKey = key;

                    key = StringToKeyCode(rtsc_zoomOutKey);
                    rtsc.keys["ZoomOut"] = key;
                    rtsc.zoomOut.text = rtsc_zoomOutKey;
                    RTSCamera.active.zoomOutKey = key;

                    RTSCamera.active.edgeMovement = rtsc_edgeMovement;
                    rtsc.edgeMovement.isOn = rtsc_edgeMovement;

                    RTSCamera.active.rotateWithMouse = rtsc_rotateWithMouse;
                    rtsc.rotateWithMouse.isOn = rtsc_rotateWithMouse;

                    RTSCamera.active.zoomWithMouse = rtsc_zoomWithMouse;
                    rtsc.zoomWithMouse.isOn = rtsc_zoomWithMouse;

                    RTSCamera.active.flipHorizontalRotation = rtsc_flipHorizontalRotation;
                    rtsc.flipHorizontalRotation.isOn = rtsc_flipHorizontalRotation;

                    RTSCamera.active.flipVerticalRotation = rtsc_flipVerticalRotation;
                    rtsc.flipVerticalRotation.isOn = rtsc_flipVerticalRotation;

                    RTSCamera.active.flipZoom = rtsc_flipZoom;
                    rtsc.flipZoom.isOn = rtsc_flipZoom;

                    RTSCamera.active.moveSpeed = rtsc_moveSpeed;
                    rtsc.moveSpeedInputField.text = rtsc_moveSpeed.ToString();

                    RTSCamera.active.moveAcceleration = rtsc_moveAcceleration;
                    rtsc.moveAccelerationInputField.text = rtsc_moveAcceleration.ToString();

                    RTSCamera.active.rotationSpeed = rtsc_rotationSpeed;
                    rtsc.rotationSpeedInputField.text = rtsc_rotationSpeed.ToString();

                    RTSCamera.active.zoomSpeed = rtsc_zoomSpeed;
                    rtsc.zoomSpeedInputField.text = rtsc_zoomSpeed.ToString();

                    RTSCamera.active.minZoomHeight = rtsc_minimumTerrainHeight;
                    rtsc.minimumTerrainHeightInputField.text = rtsc_minimumTerrainHeight.ToString();

                    RTSCamera.active.maxZoomHeight = rtsc_maximumTerrainHeight;
                    rtsc.maximumTerrainHeightInputField.text = rtsc_maximumTerrainHeight.ToString();

                    RTSCamera.active.minCameraHeight = rtsc_minimumAbsoluteHeight;
                    rtsc.minimumAbsoluteHeightInputField.text = rtsc_minimumAbsoluteHeight.ToString();
                }
            }

            // RPGCameraInGameControls
            if (isRPGCameraInGameControlsSaved)
            {
                RPGCameraInGameControls rpgc = RPGCameraInGameControls.active;

                if (rpgc != null)
                {
                    KeyCode key = StringToKeyCode(rpgc_rotateRightKey);
                    rpgc.keys["RotateRight"] = key;
                    rpgc.rotateRight.text = rpgc_rotateRightKey;
                    RPGCamera.active.rotateRight = key;

                    key = StringToKeyCode(rpgc_rotateLeftKey);
                    rpgc.keys["RotateLeft"] = key;
                    rpgc.rotateLeft.text = rpgc_rotateLeftKey;
                    RPGCamera.active.rotateLeft = key;

                    key = StringToKeyCode(rpgc_rotateUpKey);
                    rpgc.keys["RotateUp"] = key;
                    rpgc.rotateUp.text = rpgc_rotateUpKey;
                    RPGCamera.active.rotateUp = key;

                    key = StringToKeyCode(rpgc_rotateDownKey);
                    rpgc.keys["RotateDown"] = key;
                    rpgc.rotateDown.text = rpgc_rotateDownKey;
                    RPGCamera.active.rotateDown = key;

                    key = StringToKeyCode(rpgc_zoomInKey);
                    rpgc.keys["ZoomIn"] = key;
                    rpgc.zoomIn.text = rpgc_zoomInKey;
                    RPGCamera.active.zoomInKey = key;

                    key = StringToKeyCode(rpgc_zoomOutKey);
                    rpgc.keys["ZoomOut"] = key;
                    rpgc.zoomOut.text = rpgc_zoomOutKey;
                    RPGCamera.active.zoomOutKey = key;

                    RPGCamera.active.zoomWithMouse = rpgc_zoomWithMouse;
                    rpgc.zoomWithMouse.isOn = rpgc_zoomWithMouse;

                    RPGCamera.active.flipZoom = rpgc_flipZoom;
                    rpgc.flipZoom.isOn = rpgc_flipZoom;

                    RPGCamera.active.rotationSpeed = rpgc_rotationSpeed;
                    rpgc.rotationSpeedInputField.text = rpgc_rotationSpeed.ToString();

                    RPGCamera.active.zoomSpeed = rpgc_zoomSpeed;
                    rpgc.zoomSpeedInputField.text = rpgc_zoomSpeed.ToString();

                    RPGCamera.active.maxZoomOut = rpgc_maxZoomOut;
                    rpgc.maxZoomOutInputField.text = rpgc_maxZoomOut.ToString();
                }
            }

            // CameraSwitcher
            if (isCameraSwitcherSaved)
            {
                CameraSwitcher.active.lastRTSposition = VectorFromArray(cs_lastRTSposition);
                CameraSwitcher.active.lastRTSrotation = QuaternionFromArray(cs_lastRTSrotation);

                if (cs_mode == 1)
                {
                    CameraSwitcher.active.SwitchToRTS();
                }
                else if (cs_mode == 2)
                {
                    UnitPars followPars = IntToUnitPars(cs_followPars, rtsm.allUnits);

                    if (followPars != null)
                    {
                        CameraSwitcher.active.SwitchToRPG(followPars, false);
                    }
                    else
                    {
                        CameraSwitcher.active.SwitchToRTS();
                    }
                }
            }

            // GraphicsSettings	
            if (isGraphicsSettingsSaved)
            {
                if (gs_materialInstancing != GraphicsSettings.active.materialInstancing.isOn)
                {
                    GraphicsSettings.active.materialInstancing.isOn = gs_materialInstancing;
                    GraphicsSettings.active.SwitchUnitsIntsancedShader();
                }

                if (gs_meshInstancing != GraphicsSettings.active.meshInstancing.isOn)
                {
                    GraphicsSettings.active.meshInstancing.isOn = gs_meshInstancing;
                    GraphicsSettings.active.SwitchMeshInstancing();
                }

                GraphicsSettings.active.shadowCastDistance.text = gs_shadowCastDistance.ToString();
                GraphicsSettings.active.shadowReceiveDistance.text = gs_shadowReceiveDistance.ToString();
                GraphicsSettings.active.cameraFarClippingPlane.text = gs_cameraFarClippingPlane.ToString();

                if (RenderMeshModels.active != null)
                {
                    RenderMeshModels.active.shadowCastDistance = gs_shadowCastDistance;
                    RenderMeshModels.active.shadowReceiveDistance = gs_shadowReceiveDistance;
                }

                GraphicsSettings.active.SubmitCameraFarClippingPlane();

                if (gs_waterDetails != GraphicsSettings.active.waterDetails.value)
                {
                    GraphicsSettings.active.waterDetails.value = gs_waterDetails;
                    GraphicsSettings.active.ChangeWaterShader();
                }

                if (gs_qualityPreset != GraphicsSettings.active.qualityPreset.value)
                {
                    GraphicsSettings.active.qualityPreset.value = gs_qualityPreset;
                    GraphicsSettings.active.SubmitQualityPreset();
                }

                if (gs_terrainResolution != GraphicsSettings.active.terrainResolution.value)
                {
                    GraphicsSettings.active.terrainResolution.value = gs_terrainResolution;
                    GraphicsSettings.active.ChangeTerrainResolution();
                }

                if (gs_fireLights != GraphicsSettings.active.fireLights.isOn)
                {
                    GraphicsSettings.active.fireLights.isOn = gs_fireLights;
                    GraphicsSettings.active.SwitchFireLights();
                }

                if (gs_fireArrowLights != GraphicsSettings.active.fireArrowLights.isOn)
                {
                    GraphicsSettings.active.fireArrowLights.isOn = gs_fireArrowLights;
                    GraphicsSettings.active.SwitchFireArrowLights();
                }

                if (gs_buildingLights != GraphicsSettings.active.buildingLights.isOn)
                {
                    GraphicsSettings.active.buildingLights.isOn = gs_buildingLights;
                    GraphicsSettings.active.SwitchBuildingLights();
                }
            }

            // Notifcations	
            if (isNotificationsSaved)
            {
                if (nt_taxes != NotificationsUI.active.taxes.isOn)
                {
                    NotificationsUI.active.taxes.isOn = nt_taxes;
                    NotificationsUI.active.SwitchTaxes();
                }

                if (nt_warWarning != NotificationsUI.active.warWarning.isOn)
                {
                    NotificationsUI.active.warWarning.isOn = nt_warWarning;
                    NotificationsUI.active.SwitchWarWarning();
                }
            }
        }

        public KeyCode StringToKeyCode(string keyString)
        {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), keyString);
        }

        public List<int> UnitParsToIntList(List<UnitPars> upQueries, List<UnitPars> upAll)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < upQueries.Count; i++)
            {
                list.Add(upAll.IndexOf(upQueries[i]));
            }

            return list;
        }

        public UnitPars IntToUnitPars(int query, List<UnitPars> upAll)
        {
            if (query <= 0 || query >= upAll.Count)
            {
                return null;
            }

            return upAll[query];
        }

        public int UnitParsToInt(UnitPars up, List<UnitPars> upAll)
        {
            if (up == null)
            {
                return -1;
            }

            return upAll.IndexOf(up);
        }

        public List<UnitPars> IntToUnitParsList(List<int> intQueries, List<UnitPars> upAll)
        {
            List<UnitPars> list = new List<UnitPars>();

            for (int i = 0; i < intQueries.Count; i++)
            {
                int j = intQueries[i];

                if (j >= 0)
                {
                    list.Add(upAll[j]);
                }
            }

            return list;
        }

        public List<List<int>> UnitParsToIntListList(List<List<UnitPars>> upQueries, List<UnitPars> upAll)
        {
            List<List<int>> list = new List<List<int>>();

            for (int i = 0; i < upQueries.Count; i++)
            {
                list.Add(new List<int>());
            }

            for (int i = 0; i < upQueries.Count; i++)
            {
                for (int j = 0; j < upQueries[i].Count; j++)
                {
                    list[i].Add(upAll.IndexOf(upQueries[i][j]));
                }
            }

            return list;
        }

        public List<List<UnitPars>> IntToUnitParsListList(List<List<int>> intQueries, List<UnitPars> upAll)
        {
            List<List<UnitPars>> list = new List<List<UnitPars>>();

            for (int i = 0; i < intQueries.Count; i++)
            {
                list.Add(new List<UnitPars>());
            }

            for (int i = 0; i < intQueries.Count; i++)
            {
                for (int j = 0; j < intQueries[i].Count; j++)
                {
                    int k = intQueries[i][j];

                    if (k >= 0)
                    {
                        list[i].Add(upAll[k]);
                    }
                }
            }

            return list;
        }

        public float[] ArrayFromVector(Vector3 vec)
        {
            float[] array = new float[3];
            array[0] = vec.x;
            array[1] = vec.y;
            array[2] = vec.z;

            return array;
        }
        public float[] ArrayFromQuaternion(Quaternion qt)
        {
            float[] array = new float[4];
            array[0] = qt.x;
            array[1] = qt.y;
            array[2] = qt.z;
            array[3] = qt.w;

            return array;
        }

        public Vector3 VectorFromArray(float[] array)
        {
            return (new Vector3(array[0], array[1], array[2]));
        }

        public Quaternion QuaternionFromArray(float[] array)
        {
            return (new Quaternion(array[0], array[1], array[2], array[3]));
        }
    }
}
