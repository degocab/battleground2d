using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace RTSToolkit
{
    public class NationAI : MonoBehaviour
    {
        [HideInInspector] public string nationName;

        [HideInInspector] public GameObject model;
        [HideInInspector] public int maxNumBuildings = 20;

        [HideInInspector] public GameObject testGo = null;

        [HideInInspector] public List<int> numb = new List<int>();
        [HideInInspector] public List<int> maxNumb = new List<int>();

        [HideInInspector] public List<int> minMaxNumb = new List<int>();
        [HideInInspector] public List<int> maxMaxNumb = new List<int>();

        [HideInInspector] public float size = 100f;
        [HideInInspector] public List<float> buildingRadii = new List<float>();

        [HideInInspector] public List<UnitPars> spawnedBuildings = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> barracks = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> factories = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> stables = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> centralBuildings = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> workers = new List<UnitPars>();

        [HideInInspector] public List<List<UnitPars>> workersRes = new List<List<UnitPars>>();

        [HideInInspector] public List<int> countAllianceWarning = new List<int>();

        [HideInInspector] public List<List<UnitPars>> resMiningPoints = new List<List<UnitPars>>();
        [HideInInspector] public List<int> nResMiningPoints = new List<int>();
        [HideInInspector] public List<int> nMaxResMiningPoints = new List<int>();

        RTSMaster rtsm;
        [HideInInspector] public int nation = 2;

        [HideInInspector] public List<UnitPars> militaryUnits = new List<UnitPars>();

        [HideInInspector] public List<int> beatenUnits = new List<int>();
        [HideInInspector] public List<int> allianceAcceptanceTimes = new List<int>();

        [HideInInspector] public int masterNationId = -1;

        [HideInInspector] public int nEntitiesUnderAttack = 0;

        [HideInInspector] public bool isFirstTime = true;

        [HideInInspector] public NationPars nationPars;
        SpawnPoint thisSpawnPoint;

        // spawn waiters    
        float townBuilder_skip_waiter = 1f; //1f
        float townBuilder_full_waiter_min = 10f; //10f
        float townBuilder_full_waiter_max = 20f; //20f

        float workersSpawner_waiter = 0.2f; //0.2f

        float troopsControl_waiter = 0.1f; //0.1f

        float miningPointsControl_waiter_min = 1f; //1f
        float miningPointsControl_waiter_max = 3f; //3f
        float miningPointsControl_full_waiter = 0.2f; //0.2f

        float setWorkers_waiter = 2f; //2f

        float recalculateLimits_waiter = 3f; //3f

        int positionFoundFailCount = 0;

        void Start()
        {
            rtsm = RTSMaster.active;
            GetNationPars();
            thisSpawnPoint = this.gameObject.GetComponent<SpawnPoint>();

            resMiningPoints.Clear();
            nResMiningPoints.Clear();
            nMaxResMiningPoints.Clear();

            for (int i = 0; i < ResourcePoint.active.resourcePointTypes.Count; i++)
            {
                resMiningPoints.Add(new List<UnitPars>());
                nResMiningPoints.Add(0);
                nMaxResMiningPoints.Add(1);
            }

            workersRes.Clear();

            for (int i = 0; i < Economy.active.resources.Count; i++)
            {
                workersRes.Add(new List<UnitPars>());
            }

            numb.Clear();
            maxNumb.Clear();
            minMaxNumb.Clear();
            maxMaxNumb.Clear();
            buildingRadii.Clear();

            if (numb.Count == 0)
            {
                for (int i = 0; i < rtsm.rtsUnitTypePrefabs.Count; i++)
                {
                    numb.Add(0);
                    maxNumb.Add(0);
                    minMaxNumb.Add(0);
                    maxMaxNumb.Add(0);
                    buildingRadii.Add(0f);
                }
            }

            maxNumb[0] = 1;
            maxNumb[1] = 1;
            maxNumb[2] = 1;
            maxNumb[3] = 10;
            maxNumb[4] = 1;
            maxNumb[6] = 1;
            maxNumb[7] = 1;
            maxNumb[8] = 4;
            maxNumb[11] = 5;
            maxNumb[12] = 5;
            maxNumb[13] = 5;
            maxNumb[14] = 5;
            maxNumb[15] = 10;
            maxNumb[16] = 5;
            maxNumb[17] = 5;
            maxNumb[18] = 5;

            minMaxNumb[0] = 1;
            minMaxNumb[1] = 1;
            minMaxNumb[2] = 1;
            minMaxNumb[3] = 10;
            minMaxNumb[4] = 1;
            minMaxNumb[6] = 1;
            minMaxNumb[7] = 1;
            minMaxNumb[8] = 4;
            minMaxNumb[11] = 5;
            minMaxNumb[12] = 5;
            minMaxNumb[13] = 5;
            minMaxNumb[14] = 5;
            minMaxNumb[15] = 10;
            minMaxNumb[16] = 5;
            minMaxNumb[17] = 5;
            minMaxNumb[18] = 5;

            maxMaxNumb[0] = 1;
            maxMaxNumb[1] = 1;
            maxMaxNumb[2] = 1;
            maxMaxNumb[3] = 25;
            maxMaxNumb[4] = 1;
            maxMaxNumb[6] = 1;
            maxMaxNumb[7] = 1;
            maxMaxNumb[8] = 4;
            maxMaxNumb[11] = 50;
            maxMaxNumb[12] = 50;
            maxMaxNumb[13] = 50;
            maxMaxNumb[14] = 50;
            maxMaxNumb[15] = 20;
            maxMaxNumb[16] = 50;
            maxMaxNumb[17] = 50;
            maxMaxNumb[18] = 50;

            if (nation != Diplomacy.active.playerNation)
            {
                if (nationPars.isWizzardNation)
                {
                    runWizzardCor = true;
                }
                else
                {
                    runWizzardCor = false;
                }
            }

            wizzardAICorMode = 1;
            isRunningAboveOneSecond = false;
            timeOneSecond = 0f;
            isDefeatActivated_CheckForNationDefeat = false;
            isDefeated_CheckForNationDefeat = false;
            isFirstTime = false;
        }

        bool runWizzardCor = false;
        bool isRunningAboveOneSecond = false;
        float timeOneSecond = 0f;

        float deltaTime;

        void Update()
        {
            deltaTime = Time.deltaTime;

            MilitaryCheckWar();
            MilitaryCheckOther();

            if (isRunningAboveOneSecond)
            {
                if (runWizzardCor)
                {
                    WizzardAICor();
                    CheckForNationDefeat();
                }
                else
                {
                    CheckForNationDefeat();
                    TownBuilder();
                    WorkersSpawner();
                    TroopsControl();
                    MiningPointsControl();
                    SetWorkers();
                    RecalculateLimits();
                    RestoreDamagedBuildings();
                }
            }
            else
            {
                timeOneSecond = timeOneSecond + deltaTime;
                if (timeOneSecond > 1f)
                {
                    isRunningAboveOneSecond = true;
                }
            }
        }

        public NationPars GetNationPars()
        {
            if (nationPars == null)
            {
                nationPars = GetComponent<NationPars>();
            }

            return nationPars;
        }

        int wizzardAICorMode = 0;
        UnitPars wizzardUnit;
        float tWizzardAICor = 0f;
        float lastTWizzardAICor = 5f;

        void WizzardAICor()
        {
            tWizzardAICor = tWizzardAICor + deltaTime;

            if (wizzardAICorMode == 1)
            {
                float randAngle = Random.Range(0f, 360f);
                Quaternion randomRotation = Quaternion.Euler(0f, randAngle, 0f);

                if (nationPars.isWizzardSpawned == false)
                {
                    thisSpawnPoint.model = rtsm.rtsUnitTypePrefabs[19];
                    thisSpawnPoint.isManualPosition = true;
                    thisSpawnPoint.manualPosition.Add(transform.position);
                    thisSpawnPoint.manualRotation.Add(randomRotation);
                    thisSpawnPoint.numberOfObjects = 1;
                    thisSpawnPoint.StartSpawning();
                    nationPars.isWizzardSpawned = true;
                }

                wizzardAICorMode = 2;
            }

            if (wizzardAICorMode == 2)
            {
                if (tWizzardAICor > 1f)
                {
                    for (int i = 0; i < rtsm.allUnits.Count; i++)
                    {
                        if (rtsm.allUnits[i].nationName == nationPars.GetNationName())
                        {
                            wizzardUnit = rtsm.allUnits[i];
                            wizzardAICorMode = 3;
                        }
                    }

                    tWizzardAICor = 0f;
                }
            }

            if (wizzardAICorMode == 3)
            {
                if (tWizzardAICor > lastTWizzardAICor)
                {
                    lastTWizzardAICor = Random.Range(5f, 10f);

                    if (wizzardUnit.militaryMode != 30)
                    {
                        Vector3 cir3 = TerrainProperties.RandomTerrainVectorCircleProc(transform.position, 150f);
                        float cir3mag = 1f;

                        if (wizzardUnit != null)
                        {
                            cir3mag = (wizzardUnit.transform.position - cir3).magnitude / 1.5f;
                        }

                        UnitsMover.active.AddMilitaryAvoider(wizzardUnit, cir3, 0);
                        lastTWizzardAICor = Random.Range(cir3mag, cir3mag + 7f);
                    }

                    tWizzardAICor = 0f;
                }
            }
        }

        float tCheckForNationDefeat = 0f;
        bool isDefeatActivated_CheckForNationDefeat = false;
        bool isDefeated_CheckForNationDefeat = false;

        void CheckForNationDefeat()
        {
            if (isDefeated_CheckForNationDefeat == false)
            {
                tCheckForNationDefeat = tCheckForNationDefeat + deltaTime;

                if (tCheckForNationDefeat > 1f)
                {
                    tCheckForNationDefeat = 0f;

                    if (isDefeatActivated_CheckForNationDefeat == false)
                    {
                        if (nationPars != null)
                        {
                            if (nationPars.allNationUnits != null)
                            {
                                if (nationPars.allNationUnits.Count > 10)
                                {
                                    isDefeatActivated_CheckForNationDefeat = true;
                                }
                                else if (runWizzardCor)
                                {
                                    if (nationPars.allNationUnits.Count > 0)
                                    {
                                        isDefeatActivated_CheckForNationDefeat = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (nationPars != null)
                        {
                            if (nationPars.allNationUnits != null)
                            {
                                if (NationDefeatConditions())
                                {
                                    string natName = nationPars.GetNationName();

                                    if (string.IsNullOrEmpty(natName) == false)
                                    {
                                        if (nation != Diplomacy.active.playerNation)
                                        {
                                            if (
                                                (Diplomacy.active.relations[nation][Diplomacy.active.playerNation] != -1) &&
                                                (Diplomacy.active.relations[Diplomacy.active.playerNation][nation] != -1)
                                            )
                                            {
                                                if (runWizzardCor)
                                                {
                                                    if (
                                                        (Diplomacy.active.relations[nation][Diplomacy.active.playerNation] == 1) ||
                                                        (Diplomacy.active.relations[Diplomacy.active.playerNation][nation] == 1)
                                                    )
                                                    {
                                                        DiplomacyReportsUI.active.MakeProposal(natName, "WizardRetreatInWar");
                                                    }
                                                    else
                                                    {
                                                        DiplomacyReportsUI.active.MakeProposal(natName, "WizardRetreatInPeace");
                                                    }
                                                }
                                                else
                                                {
                                                    DiplomacyReportsUI.active.MakeProposal(natName, "Retreat");
                                                }
                                            }
                                        }

                                        Diplomacy.active.RemoveNation(natName);
                                        isDefeated_CheckForNationDefeat = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool NationDefeatConditions()
        {
            // No nation units left
            if (nationPars.allNationUnits.Count == 0)
            {
                return true;
            }

            // Only small number of scattered units are remaining
            if (nationPars.allNationUnitsKD != null)
            {
                if (nationPars.allNationUnits.Count < 10)
                {
                    if (runWizzardCor == false)
                    {
                        if (nationPars.allNationUnitsKD.FindNearest_R(nationPars.transform.position) > 2 * nationPars.nationSize)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void FillLists()
        {
            beatenUnits.Clear();
            allianceAcceptanceTimes.Clear();
            countAllianceWarning.Clear();

            for (int i = 0; i < Diplomacy.active.numberNations; i++)
            {
                ExpandLists();
            }
        }

        public void ExpandLists()
        {
            beatenUnits.Add(0);
            allianceAcceptanceTimes.Add(0);
            countAllianceWarning.Add(0);
        }

        public void RemoveNation(int natId)
        {
            if (natId > -1)
            {
                if (natId < beatenUnits.Count)
                {
                    beatenUnits.RemoveAt(natId);
                }
                if (natId < allianceAcceptanceTimes.Count)
                {
                    allianceAcceptanceTimes.RemoveAt(natId);
                }
                if (natId < countAllianceWarning.Count)
                {
                    countAllianceWarning.RemoveAt(natId);
                }
            }
        }

        bool NetworkPass()
        {
            bool pass = true;

            if (RTSMaster.active.isMultiplayer)
            {
                if (RTSMaster.active.rtsCameraNetwork == null)
                {
                    RTSMaster.active.GetPlayerCameraNetwork();
                }
                if (RTSMaster.active.rtsCameraNetwork.isHost == 0)
                {
                    pass = false;

                    if (nationName == Diplomacy.active.GetPlayerNationName())
                    {
                        pass = true;
                    }
                }
            }

            return pass;
        }

        [HideInInspector] public bool runTownBuilder = true;
        float tTownBuilder = 0f;
        float lastTTownBuilder = 0.5f;

        void TownBuilder()
        {
            if (runTownBuilder)
            {
                tTownBuilder = tTownBuilder + deltaTime;

                if (tTownBuilder > lastTTownBuilder)
                {
                    tTownBuilder = 0f;
                    lastTTownBuilder = Random.Range(townBuilder_full_waiter_min, townBuilder_full_waiter_max);

                    if (NetworkPass())
                    {
                        int ii = BuilderDecider();

                        if (ii >= 0)
                        {
                            SetBuilding(ii);
                        }
                        else
                        {
                            lastTTownBuilder = townBuilder_skip_waiter;
                        }
                    }
                }
            }
        }

        int strictSavingType = -1;

        public int BuilderDecider()
        {
            int buildingId = -1;

            if (centralBuildings.Count == 0)
            {
                if (numb[0] < maxNumb[0])
                {
                    buildingId = 0;
                }
            }
            else
            {
                strictSavingType = -1;
                List<int> allowedModes = new List<int>();

                if (numb[1] < maxNumb[1])
                {
                    allowedModes.Add(1);
                    if (numb[3] > 6)
                    {
                        if (strictSavingType == -1)
                        {
                            strictSavingType = 1;
                        }
                    }
                }

                if (numb[2] < maxNumb[2])
                {
                    allowedModes.Add(2);

                    if (numb[3] > 6)
                    {
                        if (strictSavingType == -1)
                        {
                            strictSavingType = 2;
                        }
                    }
                }

                if (numb[4] < maxNumb[4])
                {
                    if (numb[3] > 4)
                    {
                        allowedModes.Add(4);

                        if (numb[3] > 8)
                        {
                            if (strictSavingType == -1)
                            {
                                strictSavingType = 4;
                            }
                        }
                    }
                }

                if (numb[4] > 0)
                {
                    if (numb[6] < maxNumb[6])
                    {
                        if (numb[3] > 5)
                        {
                            allowedModes.Add(6);

                            if (numb[3] > 20)
                            {
                                if (strictSavingType == -1)
                                {
                                    strictSavingType = 6;
                                }
                            }
                        }
                    }
                }

                if (numb[6] > 0)
                {
                    if (numb[7] < maxNumb[7])
                    {
                        if (numb[3] > 6)
                        {
                            allowedModes.Add(7);

                            if (numb[3] > 24)
                            {
                                if (strictSavingType == -1)
                                {
                                    strictSavingType = 7;
                                }
                            }
                        }
                    }
                }

                if (numb[7] > 0)
                {
                    if (numb[8] < maxNumb[8])
                    {
                        if (numb[3] > 7)
                        {
                            allowedModes.Add(8);

                            if (numb[3] > 26)
                            {
                                if (strictSavingType == -1)
                                {
                                    strictSavingType = 8;
                                }
                            }
                        }
                    }
                }

                if (numb[6] > 0)
                {
                    if (numb[8] > 0)
                    {
                        if (numb[3] > 9)
                        {
                            for (int i = 0; i < numb[3] - 9; i++)
                            {
                                allowedModes.Add(10);
                            }
                        }
                    }
                }

                if (strictSavingType == -1)
                {
                    if (Economy.active != null)
                    {
                        if (Economy.active.resources != null)
                        {
                            for (int i = 0; i < Economy.active.resources.Count; i++)
                            {
                                if (Economy.active.resources[i].producers != null)
                                {
                                    for (int j = 0; j < Economy.active.resources[i].producers.Count; j++)
                                    {
                                        int rtsid = Economy.active.resources[i].producers[j].rtsId;
                                        int totalAmountPerUnit = Economy.active.resources[i].producers[j].totalAmountPerUnit;

                                        if ((rtsid > -1) && (rtsid < numb.Count))
                                        {
                                            if ((nation > -1) && (nation < Economy.active.nationResources.Count))
                                            {
                                                if (totalAmountPerUnit * numb[rtsid] < (Economy.active.nationResources[nation][rtsid].amount + 5))
                                                {
                                                    if (numb[rtsid] < 15)
                                                    {
                                                        allowedModes.Add(rtsid);
                                                    }
                                                    else if (numb[rtsid] < militaryUnits.Count)
                                                    {
                                                        allowedModes.Add(rtsid);
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

                if (allowedModes.Count > 0)
                {
                    int chosenMode = Random.Range(0, allowedModes.Count);
                    buildingId = allowedModes[chosenMode];
                }
            }

            // If central building is not completed	
            if (buildingId > 0)
            {
                if (centralBuildings.Count > 0)
                {
                    if (centralBuildings[0].isBuildFinished == false)
                    {
                        buildingId = -1;
                    }
                }
            }

            return buildingId;
        }

        [HideInInspector] public bool runWorkersSpawner = true;
        float tWorkersSpawner = 0f;

        void WorkersSpawner()
        {
            if (runWorkersSpawner)
            {
                tWorkersSpawner = tWorkersSpawner + deltaTime;
                if (tWorkersSpawner > workersSpawner_waiter)
                {
                    tWorkersSpawner = 0f;

                    if (NetworkPass())
                    {
                        if (numb[0] > 0)
                        {
                            if (numb[15] < maxNumb[15])
                            {
                                if (centralBuildings.Count > 0)
                                {
                                    if (centralBuildings[0].thisSpawn.isSpawning == false)
                                    {
                                        if (centralBuildings[0].thisSpawn.model == null)
                                        {
                                            if (rtsm.isMultiplayer == false)
                                            {
                                                centralBuildings[0].thisSpawn.model = rtsm.rtsUnitTypePrefabs[15];
                                            }
                                            else
                                            {
                                                centralBuildings[0].thisSpawn.model = rtsm.rtsUnitTypePrefabsNetwork[15];
                                            }
                                        }

                                        centralBuildings[0].thisSpawn.count = 0;
                                        centralBuildings[0].thisSpawn.numberOfObjects = 1;
                                        centralBuildings[0].thisSpawn.StartSpawning();

                                        RecalculateSpawnedNumbers(15);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void RecalculateSpawnedNumbers(int id)
        {
            for (int i = 0; i < numb.Count; i++)
            {
                numb[i] = 0;
            }

            if (nationPars != null)
            {
                for (int i = 0; i < nationPars.allNationUnits.Count; i++)
                {
                    if (nationPars.allNationUnits[i].rtsUnitId < numb.Count)
                    {
                        numb[nationPars.allNationUnits[i].rtsUnitId] = numb[nationPars.allNationUnits[i].rtsUnitId] + 1;
                    }
                }
            }
            else
            {
                for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
                {
                    if (RTSMaster.active.allUnits[i].rtsUnitId < numb.Count)
                    {
                        if (RTSMaster.active.allUnits[i].nationName == nationName)
                        {
                            numb[RTSMaster.active.allUnits[i].rtsUnitId] = numb[RTSMaster.active.allUnits[i].rtsUnitId] + 1;
                        }
                    }
                }
            }
        }

        [HideInInspector] public bool runTroopsControl = true;

        float tTroopsControl = 0f;
        float lastTTroopsControl = 0.5f;

        void TroopsControl()
        {
            if (runTroopsControl)
            {
                tTroopsControl = tTroopsControl + deltaTime;

                if (tTroopsControl > lastTTroopsControl)
                {
                    tTroopsControl = 0f;

                    if ((Economy.active.GetExpectedProductionDifference(nation) > Random.Range(0, 60)) && (NetworkPass()))
                    {
                        List<int> posibleTypes = new List<int>();
                        if (barracks.Count > 0)
                        {
                            for (int k = 0; k < 5; k++)
                            {
                                posibleTypes.Add(11);
                                posibleTypes.Add(12);

                                if (rtsm.unitTypeLocking[nation][13] == 1)
                                {
                                    posibleTypes.Add(13);
                                }
                                if (rtsm.unitTypeLocking[nation][14] == 1)
                                {
                                    posibleTypes.Add(14);
                                }
                            }
                        }

                        if (factories.Count > 0)
                        {
                            posibleTypes.Add(16);
                            posibleTypes.Add(17);
                        }

                        if (stables.Count > 0)
                        {
                            for (int k = 0; k < 5; k++)
                            {
                                posibleTypes.Add(18);
                            }
                        }

                        if (posibleTypes.Count > 0)
                        {
                            int randType = posibleTypes[Random.Range(0, posibleTypes.Count)];
                            UnitPars spawner = null;

                            if (
                                randType == 11 ||
                                randType == 12 ||
                                randType == 13 ||
                                randType == 14
                            )
                            {
                                spawner = barracks[Random.Range(0, barracks.Count)];
                            }
                            else if (
                                randType == 16 ||
                                randType == 17
                            )
                            {
                                spawner = factories[Random.Range(0, factories.Count)];
                            }
                            else if (
                                randType == 18
                            )
                            {
                                spawner = stables[Random.Range(0, stables.Count)];
                            }

                            if (spawner != null)
                            {
                                if (spawner.thisSpawn.isSpawning == false)
                                {
                                    spawner.thisSpawn.model = rtsm.rtsUnitTypePrefabs[randType];
                                    spawner.thisSpawn.count = 0;
                                    spawner.thisSpawn.numberOfObjects = 1;
                                    spawner.thisSpawn.StartSpawning();
                                    RecalculateSpawnedNumbers(randType);
                                }
                            }
                        }
                    }

                    lastTTroopsControl = troopsControl_waiter;

                    if (strictSavingType != -1)
                    {
                        int i_attacking = 1;

                        for (int i = 0; i < militaryUnits.Count; i++)
                        {
                            if (militaryUnits[i].attackers != null)
                            {
                                if (militaryUnits[i].attackers.Count > 0)
                                {
                                    i_attacking = i_attacking + 1;
                                }
                            }
                        }

                        lastTTroopsControl = troopsControl_waiter + (15f / (1f + i_attacking));
                    }
                }
            }
        }

        public int GetUnlockedMilitaryUnitsTypeCount()
        {
            int unlocked = 2;

            if (rtsm.unitTypeLocking[nation][14] == 1)
            {
                unlocked = unlocked + 1;
            }

            if (rtsm.unitTypeLocking[nation][15] == 1)
            {
                unlocked = unlocked + 1;
            }

            if (numb[6] > 0)
            {
                unlocked = unlocked + 2;
            }

            if (numb[7] > 0)
            {
                unlocked = unlocked + 1;
            }

            return unlocked;
        }

        [HideInInspector] public bool runMiningPointsControl = true;
        float tMiningPointsControl = 0f;
        float lastTMiningPointsControl = 0.5f;

        void MiningPointsControl()
        {
            if (runMiningPointsControl)
            {
                tMiningPointsControl = tMiningPointsControl + deltaTime;

                if (tMiningPointsControl > lastTMiningPointsControl)
                {
                    tMiningPointsControl = 0f;
                    lastTMiningPointsControl = miningPointsControl_full_waiter;

                    if (NetworkPass())
                    {
                        int miningPointMode = Random.Range(0, 2);

                        if (CreateMiningPoint(miningPointMode))
                        {
                            lastTMiningPointsControl = miningPointsControl_full_waiter + Random.Range(miningPointsControl_waiter_min, miningPointsControl_waiter_max);
                        }
                    }
                }
            }
        }

        public bool CreateMiningPoint(int miningPointMode)
        {
            bool locationFound = false;
            Vector3 candidateLocation = Vector3.zero;
            float distToCentralBuilding = 0f;

            if (centralBuildings.Count > 0)
            {
                int nMiningPoints = 0;
                int nMaxMiningPoints = 0;
                KDTree miningPointsKD = null;
                List<ResourcePointObject> miningPointsLoc = null;

                nMiningPoints = nResMiningPoints[miningPointMode];
                nMaxMiningPoints = nMaxResMiningPoints[miningPointMode];
                miningPointsKD = ResourcePoint.active.resourcePointTypes[miningPointMode].kd_catLocations;
                miningPointsLoc = ResourcePoint.active.resourcePointTypes[miningPointMode].categorizedResourcePoints;

                if (nMiningPoints < nMaxMiningPoints)
                {
                    int i = miningPointsKD.FindNearestK(centralBuildings[0].transform.position, Random.Range(1, 3));
                    Vector3 pointLocation = miningPointsLoc[i].position;
                    distToCentralBuilding = (pointLocation - centralBuildings[0].transform.position).magnitude;

                    float accepted_dist = (size + 200f) / (nMiningPoints + 1);

                    if (distToCentralBuilding < accepted_dist)
                    {
                        candidateLocation = TerrainProperties.RandomTerrainVectorCircleProc(pointLocation, 7f);

                        if (GetNeighbourDist(candidateLocation) > 30f)
                        {
                            if (TerrainProperties.active.TerrainSteepness(candidateLocation, 7f) < 30f)
                            {
                                if (thisSpawnPoint.isSpawning == false)
                                {
                                    locationFound = true;
                                }
                            }
                        }
                    }
                }
            }

            if (locationFound == true)
            {
                if (centralBuildings.Count > 0)
                {
                    if (centralBuildings[0].isBuildFinished == false)
                    {
                        locationFound = false;
                    }
                }
            }

            if (locationFound == true)
            {
                if (numb[3] < 5)
                {
                    locationFound = false;
                }
            }

            if (locationFound == true)
            {
                if (centralBuildings.Count > 0)
                {
                    if (centralBuildings[0].isBuildFinished == true)
                    {
                        float randAngle = Random.Range(0f, 360f);
                        Quaternion randomRotation = Quaternion.Euler(0f, randAngle, 0f) * rtsm.rtsUnitTypePrefabs[5].transform.rotation;

                        thisSpawnPoint.model = rtsm.rtsUnitTypePrefabs[5];
                        thisSpawnPoint.isManualPosition = true;
                        thisSpawnPoint.manualPosition.Add(candidateLocation);
                        thisSpawnPoint.manualRotation.Add(randomRotation);
                        thisSpawnPoint.numberOfObjects = 1;
                        thisSpawnPoint.StartSpawning();

                        nResMiningPoints[miningPointMode] = nResMiningPoints[miningPointMode] + 1;
                    }
                }
            }

            return locationFound;
        }

        public float GetNeighbourDist(Vector3 query)
        {
            float r = float.MaxValue;
            int nfailed = 0;
            int nfailed2 = 0;
            int nfailed3 = 0;

            for (int j = 0; j < Diplomacy.active.numberNations; j++)
            {
                if (rtsm.nationPars[j] != null)
                {
                    for (int i = 0; i < rtsm.nationPars[j].nationAI.spawnedBuildings.Count; i++)
                    {
                        if (rtsm.nationPars[j].nationAI.spawnedBuildings[i] != null)
                        {
                            Vector3 target = rtsm.nationPars[j].nationAI.spawnedBuildings[i].transform.position;
                            float rcand = (query - target).magnitude;

                            if (rcand < r)
                            {
                                r = rcand;
                            }
                            else
                            {
                                nfailed3++;
                            }
                        }
                        else
                        {
                            nfailed2++;
                        }
                    }
                }
                else
                {
                    nfailed++;
                }
            }

            return r;
        }

        [HideInInspector] public bool runSetWorkers = true;
        float tSetWorkers = 0f;

        void SetWorkers()
        {
            if (runSetWorkers)
            {
                tSetWorkers = tSetWorkers + deltaTime;

                if (tSetWorkers > setWorkers_waiter)
                {
                    tSetWorkers = 0f;

                    if (NetworkPass())
                    {
                        if (workers.Count > 0)
                        {
                            List<int> wTypes = new List<int>();
                            List<int> wTypesIndex = new List<int>();

                            for (int i1 = 0; i1 < resMiningPoints.Count; i1++)
                            {
                                wTypes.Add(workersRes[i1].Count);
                                wTypesIndex.Add(i1);
                            }

                            if (numb[2] > 0)
                            {
                                wTypes.Add(workersRes[2].Count / 2);
                                wTypesIndex.Add(2);
                            }

                            if (wTypes.Count > 0)
                            {
                                int mod = wTypes.IndexOf(wTypes.Min());
                                mod = wTypesIndex[mod];

                                if (mod > -1)
                                {
                                    UnitPars worker = workers[0];
                                    workersRes[mod].Add(worker);
                                    workers.Remove(worker);

                                    rtsm.nationPars[nation].resourcesCollection.SetAutoCollection(worker, mod);
                                }
                            }

                            List<UnitPars> returners = new List<UnitPars>();

                            for (int i = 0; i < workersRes.Count; i++)
                            {
                                for (int j = 0; j < workersRes[i].Count; j++)
                                {
                                    if (workersRes[i][j].resourceType == -1)
                                    {
                                        if (workersRes[i][j].chopTreePhase == -1)
                                        {
                                            returners.Add(workersRes[i][j]);
                                        }
                                    }
                                }
                            }

                            for (int i = 0; i < returners.Count; i++)
                            {
                                workers.Remove(returners[i]);
                                workers.Add(returners[i]);

                                for (int j = 0; j < workersRes.Count; j++)
                                {
                                    workersRes[j].Remove(returners[i]);
                                }
                            }
                        }

                        // attempting to rebalance workers
                        if (Economy.active != null)
                        {
                            if (Economy.active.resources != null)
                            {
                                if (Economy.active.resources.Count > 0)
                                {
                                    List<UnitPars> allWorkers = rtsm.nationPars[nation].resourcesCollection.up_workers;

                                    // getting collectable resource types from Economy
                                    List<int> collectables = new List<int>();

                                    for (int i = 0; i < Economy.active.resources.Count; i++)
                                    {
                                        if ((Economy.active.resources[i].collectionRtsUnitId >= 0) || (Economy.active.resources[i].deliveryRtsUnitId >= 0))
                                        {
                                            collectables.Add(i);
                                        }
                                    }

                                    if (collectables.Count > 0)
                                    {
                                        // finding how many workers are currently working on each resource type
                                        int[] workerResourceCounts = new int[collectables.Count];

                                        for (int i = 0; i < workerResourceCounts.Length; i++)
                                        {
                                            workerResourceCounts[i] = 0;
                                        }

                                        for (int i = 0; i < allWorkers.Count; i++)
                                        {
                                            UnitPars worker = allWorkers[i];

                                            if (worker.resourceType > -1)
                                            {
                                                for (int j = 0; j < workerResourceCounts.Length; j++)
                                                {
                                                    if (worker.resourceType == collectables[j])
                                                    {
                                                        workerResourceCounts[j] = workerResourceCounts[j] + 1;
                                                    }
                                                }
                                            }
                                        }

                                        // finding which resource type is mostly collected
                                        int mostCollected = -1;
                                        int nMostCollected = 0;

                                        for (int i = 0; i < workerResourceCounts.Length; i++)
                                        {
                                            if (workerResourceCounts[i] > nMostCollected)
                                            {
                                                nMostCollected = workerResourceCounts[i];
                                                mostCollected = i;
                                            }
                                        }

                                        if (mostCollected > -1)
                                        {
                                            // finding which resource type is least collected
                                            int leastCollected = -1;
                                            int nLeastCollected = nMostCollected;

                                            for (int i = 0; i < workerResourceCounts.Length; i++)
                                            {
                                                if (workerResourceCounts[i] < nLeastCollected)
                                                {
                                                    nLeastCollected = workerResourceCounts[i];
                                                    leastCollected = i;
                                                }
                                            }

                                            if (leastCollected > -1)
                                            {
                                                if (nLeastCollected <= (nMostCollected - 2))
                                                {
                                                    // finding first worker which collects most collected resource
                                                    int iWorker = -1;

                                                    for (int i = 0; i < allWorkers.Count; i++)
                                                    {
                                                        if (iWorker == -1)
                                                        {
                                                            if (allWorkers[i].resourceType == mostCollected)
                                                            {
                                                                if ((allWorkers[i].chopTreePhase == 11) || (allWorkers[i].chopTreePhase == 16))
                                                                {
                                                                    iWorker = i;
                                                                }
                                                            }
                                                        }
                                                    }

                                                    if (iWorker != -1)
                                                    {
                                                        UnitPars worker = allWorkers[iWorker];
                                                        workersRes[worker.resourceType].Remove(worker);
                                                        workers.Remove(worker);

                                                        workersRes[leastCollected].Add(worker);
                                                        rtsm.nationPars[nation].resourcesCollection.SetAutoCollection(worker, leastCollected);
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
        }

        public void SetUnit(UnitPars goPars)
        {
            if (nationPars == null)
            {
                nationPars = GetComponent<NationPars>();
            }

            nationPars.AddNationUnit(goPars);

            if (goPars.unitParsType.isBuilding == true)
            {
                if (spawnedBuildings.Contains(goPars) == false)
                {
                    spawnedBuildings.Add(goPars);
                }
            }

            if (goPars.rtsUnitId == 1)
            {
                if (barracks.Contains(goPars) == false)
                {
                    barracks.Add(goPars);
                }
            }

            if (goPars.rtsUnitId == 6)
            {
                if (factories.Contains(goPars) == false)
                {
                    factories.Add(goPars);
                }
            }

            if (goPars.rtsUnitId == 7)
            {
                if (stables.Contains(goPars) == false)
                {
                    stables.Add(goPars);
                }
            }

            if (goPars.rtsUnitId == 0)
            {
                if (centralBuildings.Contains(goPars) == false)
                {
                    centralBuildings.Add(goPars);
                }
            }

            if ((goPars.rtsUnitId >= 11) && (goPars.rtsUnitId <= 14))
            {
                if (militaryUnits.Contains(goPars) == false)
                {
                    militaryUnits.Add(goPars);
                }
            }

            if ((goPars.rtsUnitId == 16) || (goPars.rtsUnitId == 17) || (goPars.rtsUnitId == 18))
            {
                if (militaryUnits.Contains(goPars) == false)
                {
                    militaryUnits.Add(goPars);
                }
            }

            if (goPars.unitParsType.isWorker)
            {
                if (workers.Contains(goPars) == false)
                {
                    workers.Add(goPars);
                }
            }

            if (goPars.rtsUnitId == 5)
            {
                if (this.enabled)
                {
                    StartCoroutine(ResourceTypeDelayMiningPoint(goPars));
                }
            }
        }

        public void UnsetUnit(UnitPars goPars)
        {
            if (this == null)
            {
                return;
            }

            if (nationPars == null)
            {
                if (this.gameObject != null)
                {
                    nationPars = GetComponent<NationPars>();
                }
            }

            nationPars.RemoveNationUnit(goPars);

            if (this != null)
            {
                int id = goPars.rtsUnitId;

                if (id != -1)
                {
                    int idThis = id;

                    if ((id >= 11) && (id <= 15))
                    {
                        idThis = 11;
                    }

                    if (goPars.isDying != true)
                    {
                        spawnedBuildings.Remove(goPars);
                    }

                    if (id == 1)
                    {
                        barracks.Remove(goPars);
                    }

                    if (id == 6)
                    {
                        factories.Remove(goPars);
                    }

                    if (id == 7)
                    {
                        stables.Remove(goPars);
                    }

                    if (id == 0)
                    {
                        centralBuildings.Remove(goPars);
                    }

                    if ((id >= 11) && (id <= 14))
                    {
                        militaryUnits.Remove(goPars);
                    }

                    if (id == 15)
                    {
                        workers.Remove(goPars);

                        for (int i = 0; i < workersRes.Count; i++)
                        {
                            workersRes[i].Remove(goPars);
                        }
                    }

                    if (id == 5)
                    {
                        if ((goPars.resourceType > -1) && (goPars.resourceType < resMiningPoints.Count))
                        {
                            resMiningPoints[goPars.resourceType].Remove(goPars);
                        }

                        if (this.enabled)
                        {
                            if (id < numb.Count)
                            {
                                StartCoroutine(UnsetDelayMiningPoint(goPars.resourceType));
                            }
                        }
                    }

                    if (id != 5)
                    {
                        if (this.enabled)
                        {
                            if (id < numb.Count)
                            {
                                StartCoroutine(UnsetDelay(idThis));
                            }
                        }
                    }
                }
            }
        }

        public IEnumerator UnsetDelay(int id)
        {
            yield return new WaitForSeconds(10f);
            RecalculateSpawnedNumbers(id);
        }

        public IEnumerator UnsetDelayMiningPoint(int id)
        {
            yield return new WaitForSeconds(10f);

            if (id < nResMiningPoints.Count)
            {
                nResMiningPoints[id] = nResMiningPoints[id] - 1;
            }
        }

        public IEnumerator ResourceTypeDelayMiningPoint(UnitPars goPars)
        {
            yield return new WaitForSeconds(3f);

            if ((goPars.resourceType > -1) && (goPars.resourceType < resMiningPoints.Count))
            {
                if (resMiningPoints[goPars.resourceType].Contains(goPars) == false)
                {
                    resMiningPoints[goPars.resourceType].Add(goPars);
                }
            }
        }

        float tRecalculateLimits = 0;

        void RecalculateLimits()
        {
            tRecalculateLimits = tRecalculateLimits + deltaTime;

            if (tRecalculateLimits > recalculateLimits_waiter)
            {
                tRecalculateLimits = 0f;

                if (NetworkPass())
                {
                    if (Economy.active.nationResources.Count == RTSMaster.active.nationPars.Count)
                    {
                        if ((isFirstTime == false) && (nation > -1) && (nation < Economy.active.nationResources.Count) && (Economy.active.nationResources[nation].Count > 0))
                        {
                            int minRes = Economy.active.nationResources[nation][0].amount;

                            for (int i = 0; i < Economy.active.nationResources[nation].Count; i++)
                            {
                                if (Economy.active.nationResources[nation][i].producers.Count == 0)
                                {
                                    if (Economy.active.nationResources[nation][i].amount < minRes)
                                    {
                                        minRes = Economy.active.nationResources[nation][i].amount;
                                    }
                                }
                            }

                            // wood cutters
                            maxNumb[2] = (int)(numb[3] * 1f / 12f + 1);
                            if (maxNumb[2] > 5)
                            {
                                maxNumb[2] = 5;
                            }

                            // houses
                            maxNumb[3] = GetMaxNumb(1, 200, minRes, 1, 100);
                            // workers
                            maxNumb[15] = (int)Mathf.Pow(1f * numb[3], 0.9f);
                            // barracks
                            maxNumb[1] = (int)(numb[3] * 1f / 20f + 1);

                            // nation size    
                            float predictedSize = 20f * Mathf.Pow((1f * numb[3]), 0.5f);
                            if (predictedSize < 40f)
                            {
                                size = 40f;
                            }
                            else if (predictedSize > 150f)
                            {
                                size = 150f;
                            }
                            else
                            {
                                size = predictedSize;
                            }

                            nationPars.nationSize = size;
                            nationPars.RefreshStaticPars();

                            buildingRadii[0] = 40f;
                            buildingRadii[1] = size;
                            buildingRadii[2] = size;
                            buildingRadii[3] = 2f * size;

                            buildingRadii[4] = size;
                            buildingRadii[6] = size;
                            buildingRadii[7] = size;
                            buildingRadii[8] = size;
                            buildingRadii[10] = 1.2f * size;
                        }
                    }
                }
            }
        }

        public int GetExpectedMilitariesCount()
        {
            if (maxNumb.Count < 4)
            {
                return (1);
            }
            else
            {
                return (maxNumb[11] * GetUnlockedMilitaryUnitsTypeCount());
            }
        }

        public int GetMaxNumb(int value, int tot, int c1, int limMin, int limMax)
        {
            int maxNumb = 0;

            if (((int)(1f * c1 * value / (1f * tot))) < limMin)
            {
                maxNumb = limMin;
            }
            else if (((int)(1f * c1 * value / (1f * tot))) > limMax)
            {
                maxNumb = limMax;
            }
            else
            {
                maxNumb = (int)(1f * c1 * value / (1f * tot));
            }

            return maxNumb;
        }

        float tMilitaryCheckWar = 0f;

        void MilitaryCheckWar()
        {
            tMilitaryCheckWar = tMilitaryCheckWar + deltaTime;

            if (tMilitaryCheckWar < 0.5f)
            {
                return;
            }

            tMilitaryCheckWar = 0f;

            if (NetworkPass())
            {
                // threat of growing nation	
                int nn = RTSMaster.active.nationPars.Count;

                for (int i = 0; i < nn; i++)
                {
                    if ((i != nation) && (nation > -1) && (nation < Diplomacy.active.relations.Count))
                    {
                        if (i < Diplomacy.active.relations[nation].Count)
                        {
                            // war conditions
                            if (Diplomacy.active.relations[nation][i] == 0)
                            {
                                float r = (rtsm.nationPars[i].transform.position - rtsm.nationPars[nation].transform.position).magnitude;

                                if (r < (size + rtsm.nationPars[i].nationAI.size))
                                {
                                    if (rtsm.nationPars[i].nationAI.militaryUnits.Count < militaryUnits.Count)
                                    {
                                        if (rtsm.nationPars[i].nationAI.militaryUnits.Count > 25)
                                        {
                                            Diplomacy.active.SetRelation(nationPars.GetNationName(), rtsm.nationPars[i].GetNationName(), 1);

                                            if ((nation != Diplomacy.active.playerNation) && (i == Diplomacy.active.playerNation))
                                            {
                                                DiplomacyReportsUI.active.MakeProposal(nationPars.GetNationName(), "War");

                                                if (RTSMaster.active.isMultiplayer)
                                                {
                                                    RTSMaster.active.rtsCameraNetwork.Cmd_SendNationMessage(nationPars.GetNationName(), rtsm.nationPars[i].GetNationName(), "War");
                                                }
                                            }
                                        }
                                    }
                                }

                                if (i < rtsm.nationPars[nation].wandererAI.nOponentsInside.Count)
                                {
                                    if (rtsm.nationPars[i].allNationUnits.Count > 6)
                                    {
                                        if (militaryUnits.Count > 15)
                                        {
                                            bool isProposed = false;

                                            if (UseJobSystem.useJobifiedKdtree_s)
                                            {
                                                int n = nationPars.allNationUnits.Count;

                                                if (n > 0)
                                                {
                                                    List<Vector3> pos2 = new List<Vector3>();
                                                    for (int j = 0; j < maxNUnits; j++)
                                                    {
                                                        if (nunits >= 0 && nunits < nationPars.allNationUnits.Count)
                                                        {
                                                            pos2.Add(nationPars.allNationUnits[nunits].transform.position);
                                                        }

                                                        nunits++;

                                                        if (nunits >= n)
                                                        {
                                                            nunits = 0;
                                                        }
                                                    }

                                                    n = pos2.Count;

                                                    if (allNationUnitsPos.IsCreated)
                                                    {
                                                        if (allNationUnitsPos.Length != n)
                                                        {
                                                            allNationUnitsPos.Dispose();
                                                            allNationUnitsPos = new NativeArray<Vector3>(n, Allocator.Persistent);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        allNationUnitsPos = new NativeArray<Vector3>(n, Allocator.Persistent);
                                                    }

                                                    if (distances.IsCreated)
                                                    {
                                                        if (distances.Length != n)
                                                        {
                                                            distances.Dispose();
                                                            distances = new NativeArray<float>(n, Allocator.Persistent);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        distances = new NativeArray<float>(n, Allocator.Persistent);
                                                    }

                                                    for (int j = 0; j < n; j++)
                                                    {
                                                        allNationUnitsPos[j] = pos2[j];
                                                        distances[j] = 100000000000f;
                                                    }

                                                    int processorCount = System.Environment.ProcessorCount;
                                                    var job = new KdSearchJobK_R()
                                                    {
                                                        kd_job = rtsm.nationPars[i].allNationUnitsKD_j,
                                                        queries_job = allNationUnitsPos,
                                                        k = 6,
                                                        answers_job = distances
                                                    };

                                                    JobHandle jobHandle = job.Schedule(n, processorCount);
                                                    jobHandle.Complete();

                                                    for (int j = 0; j < n; j++)
                                                    {
                                                        if (distances[j] < size)
                                                        {
                                                            isProposed = true;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                int n = nationPars.allNationUnits.Count;
                                                List<UnitPars> up2 = new List<UnitPars>();

                                                for (int j = 0; j < maxNUnits; j++)
                                                {
                                                    if ((nunits > -1) && (nunits < nationPars.allNationUnits.Count))
                                                    {
                                                        up2.Add(nationPars.allNationUnits[nunits]);
                                                        nunits++;
                                                        if (nunits >= n)
                                                        {
                                                            nunits = 0;
                                                        }
                                                    }
                                                }

                                                for (int j = 0; j < up2.Count; j++)
                                                {
                                                    UnitPars up = up2[j];

                                                    if (rtsm.nationPars[i].allNationUnitsKD != null)
                                                    {
                                                        float rOp = rtsm.nationPars[i].allNationUnitsKD.FindNearestK_R(up.transform.position, 6);

                                                        if (rOp < size)
                                                        {
                                                            if (isProposed == false)
                                                            {
                                                                isProposed = true;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (isProposed)
                                            {
                                                Diplomacy.active.SetRelation(nationPars.GetNationName(), rtsm.nationPars[i].GetNationName(), 1);

                                                if ((nation != Diplomacy.active.playerNation) && (i == Diplomacy.active.playerNation))
                                                {
                                                    DiplomacyReportsUI.active.MakeProposal(nationPars.GetNationName(), "War");

                                                    if (RTSMaster.active.isMultiplayer)
                                                    {
                                                        RTSMaster.active.rtsCameraNetwork.Cmd_SendNationMessage(nationPars.GetNationName(), rtsm.nationPars[i].GetNationName(), "War");
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
        }

        float tMilitaryCheckOther = 0f;

        void MilitaryCheckOther()
        {
            tMilitaryCheckOther = tMilitaryCheckOther + deltaTime;

            if (tMilitaryCheckOther < 0.5f)
            {
                return;
            }

            tMilitaryCheckOther = 0f;

            if (NetworkPass())
            {
                // threat of growing nation	
                for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                {
                    if ((i != nation) && (nation > -1) && (nation < Diplomacy.active.relations.Count))
                    {
                        if (i < Diplomacy.active.relations[nation].Count)
                        {
                            // war change checks
                            if ((Diplomacy.active.relations[nation][i] == 1) && (rtsm.nationPars[nation].isWizzardNation == false))
                            {
                                // alliance condition					
                                if (
                                    (rtsm.nationPars[i].nationAI.beatenUnits[nation] > Scores.active.nUnits[nation] * 1f / (allianceAcceptanceTimes[i] + 1)) &&
                                    (Scores.active.nUnits[i] > 40) &&
                                    (rtsm.nationPars[nation].wandererAI.nOponentsInside[i] < 10)
                                )
                                {
                                    if (i != Diplomacy.active.playerNation)
                                    {
                                        Diplomacy.active.SetRelation(rtsm.nationPars[nation].GetNationName(), rtsm.nationPars[i].GetNationName(), 4);

                                        rtsm.nationPars[i].nationAI.beatenUnits[nation] = 0;
                                        beatenUnits[i] = 0;

                                        allianceAcceptanceTimes[i] = allianceAcceptanceTimes[i] + 1;
                                        rtsm.nationPars[i].nationAI.allianceAcceptanceTimes[nation] = rtsm.nationPars[i].nationAI.allianceAcceptanceTimes[nation] + 1;
                                    }
                                    else
                                    {
                                        DiplomacyReportsUI.active.MakeProposal(rtsm.nationPars[nation].GetNationName(), "AskAlliance");
                                    }
                                }

                                // slavery conditions	
                                else if ((rtsm.nationPars[nation].wandererAI.nOponentsInside[i] > 15) && (rtsm.nationPars[nation].wandererAI.guardsPars.Count < 6))
                                {
                                    if (i != Diplomacy.active.playerNation)
                                    {
                                        Diplomacy.active.SetRelation(rtsm.nationPars[nation].GetNationName(), rtsm.nationPars[i].GetNationName(), 2);
                                    }
                                    else
                                    {
                                        DiplomacyReportsUI.active.MakeProposal(rtsm.nationPars[nation].GetNationName(), "BegMercy");
                                    }

                                    rtsm.nationPars[i].nationAI.beatenUnits[nation] = 0;
                                    beatenUnits[i] = 0;
                                }
                            }

                            // leaving slavery
                            else if (Diplomacy.active.relations[nation][i] == 2)
                            {
                                if ((rtsm.nationPars[nation].wandererAI.nOponentsInside[i] < 10) || (rtsm.nationPars[nation].wandererAI.guardsPars.Count > 8))
                                {
                                    Diplomacy.active.SetRelation(rtsm.nationPars[nation].GetNationName(), rtsm.nationPars[i].GetNationName(), 0);
                                    masterNationId = -1;

                                    if (i == Diplomacy.active.playerNation)
                                    {
                                        // Mercy leave
                                        DiplomacyReportsUI.active.MakeProposal(rtsm.nationPars[nation].GetNationName(), "MercyLeave");
                                    }
                                }
                            }

                            // leaving alliance if in 40 seconds other nation does not take back its units		
                            else if (Diplomacy.active.relations[nation][i] == 4)
                            {
                                if (rtsm.nationPars[nation].wandererAI.nOponentsInside[i] > 15)
                                {
                                    if (militaryUnits.Count > 15)
                                    {
                                        countAllianceWarning[i] = countAllianceWarning[i] + 1;
                                        if (countAllianceWarning[i] > 40)
                                        {
                                            countAllianceWarning[i] = 0;
                                            Diplomacy.active.SetRelation(rtsm.nationPars[nation].GetNationName(), rtsm.nationPars[i].GetNationName(), 0);

                                            if (i == Diplomacy.active.playerNation)
                                            {
                                                // Alliance leave
                                                DiplomacyReportsUI.active.MakeProposal(rtsm.nationPars[nation].GetNationName(), "AllianceLeave");
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

        NativeArray<Vector3> allNationUnitsPos;
        NativeArray<float> distances;

        int nunits = 0;
        int maxNUnits = 4;

        [HideInInspector] public bool runRestoreDamagedBuildings = true;
        float tRestoreDamagedBuildings = 0f;

        void RestoreDamagedBuildings()
        {
            if (runRestoreDamagedBuildings)
            {

                tRestoreDamagedBuildings = tRestoreDamagedBuildings + deltaTime;

                if (tRestoreDamagedBuildings > 2)
                {
                    tRestoreDamagedBuildings = 0f;

                    for (int i = 0; i < spawnedBuildings.Count; i++)
                    {
                        if (spawnedBuildings[i] != null)
                        {
                            if (spawnedBuildings[i].isBuildFinished)
                            {
                                if ((spawnedBuildings[i].isDying == false) && (spawnedBuildings[i].isSinking == false))
                                {
                                    if (spawnedBuildings[i].health < spawnedBuildings[i].maxHealth)
                                    {
                                        if (spawnedBuildings[i].attackers.Count < 3)
                                        {
                                            if (spawnedBuildings[i].isRestoring == false)
                                            {
                                                spawnedBuildings[i].RestoreBuilding();
                                                return;
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

        void SetBuilding(int id)
        {
            Vector3 randomPosition = TerrainProperties.RandomTerrainVectorCircleProc(transform.position, buildingRadii[id]);
            bool positionFound = FindPosition(ref randomPosition, id, 30);

            if (rtsm.rtsUnitTypePrefabs[id].GetComponent<UnitPars>().IsEnoughResources(nation) == false)
            {
                positionFound = false;
            }

            if (positionFound)
            {
                float randAngle = Random.Range(0f, 360f);
                Quaternion randomRotation = Quaternion.Euler(0f, randAngle, 0f) * rtsm.rtsUnitTypePrefabs[id].transform.rotation;

                thisSpawnPoint.model = rtsm.rtsUnitTypePrefabs[id];
                thisSpawnPoint.isManualPosition = true;
                thisSpawnPoint.manualPosition.Add(randomPosition);
                thisSpawnPoint.manualRotation.Add(randomRotation);
                thisSpawnPoint.numberOfObjects = 1;
                thisSpawnPoint.StartSpawning();

                RecalculateSpawnedNumbers(id);

                if (id == 0)
                {
                    if (rtsm.nationPars[nation] != null)
                    {
                        rtsm.nationPars[nation].transform.position = randomPosition;

                        for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                        {
                            rtsm.nationPars[i].RefreshDistances();
                        }
                    }
                }

                positionFoundFailCount = 0;
            }
            else
            {
                positionFoundFailCount++;
                lastTTownBuilder = townBuilder_skip_waiter;
            }
        }

        int neighDistFail = 0;
        public bool FindPosition(ref Vector3 position, int id, int nIterations)
        {
            for (int i = 0; i < nIterations; i++)
            {
                Vector3 randomPosition = TerrainProperties.RandomTerrainVectorCircleProc(transform.position, buildingRadii[id]);
                float neighDist = GetNeighbourDist(randomPosition);

                if (
                    ((neighDist > 30f) && (neighDist < 36f) && (id != 0)) ||
                    ((neighDist > 30f) && (id == 0))
                )
                {
                    neighDistFail = 0;
                    ResourcePointObject tt = ResourcePointObject.FindNearestTerrainTreeProc(randomPosition);

                    if (tt != null)
                    {
                        if ((randomPosition - tt.position).sqrMagnitude > 900f)
                        {
                            if (ResourcePoint.active.kd_allResLocations != null)
                            {
                                if (
                                    (randomPosition - ResourcePoint.active.resourcePoints[ResourcePoint.active.kd_allResLocations.FindNearest(randomPosition)].position).sqrMagnitude
                                    >
                                    1296f
                                )
                                {
                                    if (TerrainProperties.active.TerrainSteepness(randomPosition, 30f) < 30f)
                                    {
                                        position = randomPosition;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    neighDistFail++;
                }
            }

            return false;
        }

        public void SetPlayerCamera()
        {
            Transform camTransform = Camera.main.transform;
            CameraLooker cl = new CameraLooker();
            cl.camTransform = camTransform;
            cl.LookAtTransform(rtsm.nationPars[Diplomacy.active.playerNation].transform.position, 80f, 0f, -25f);
        }

        void OnApplicationQuit()
        {
            if (allNationUnitsPos.IsCreated)
            {
                allNationUnitsPos.Dispose();
            }
            if (distances.IsCreated)
            {
                distances.Dispose();
            }
        }

        void OnDestroy()
        {
            if (allNationUnitsPos.IsCreated)
            {
                allNationUnitsPos.Dispose();
            }
            if (distances.IsCreated)
            {
                distances.Dispose();
            }
        }
    }
}
