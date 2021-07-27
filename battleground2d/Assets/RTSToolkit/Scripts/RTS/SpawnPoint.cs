using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class SpawnPoint : MonoBehaviour
    {
        [HideInInspector] public GameObject model;
        UnitPars modelPars;
        UnitParsType modelParsType;

        [HideInInspector] public bool readynow = true;
        [HideInInspector] public float timestep = 0.5f;
        [HideInInspector] public int count = 0;
        [HideInInspector] public int numberOfObjects = 0;

        [HideInInspector] public int formationSize = 1;

        [HideInInspector] public float size = 1.0f;

        private float timeStart_loc = 0f;
        private float timeStart_gl = 0f;

        [HideInInspector] public float progress_loc = 0f;
        [HideInInspector] public float progress_gl = 0f;

        private bool isUpdateTimerRunning = false;

        [HideInInspector] public bool enoughResources = true;

        [HideInInspector] public int nation = 0;
        [HideInInspector] public string nationName = "";


        [HideInInspector] public bool addToBS = true;

        [HideInInspector] public bool isSpawning = false;


        [HideInInspector] public bool useAutoRespawn = false;
        [HideInInspector] public int autoRespawnThreshold = 50;

        RTSMaster rtsm;
        Diplomacy diplomacy;
        Economy economy;

        [HideInInspector] public bool isManualPosition = false;
        [HideInInspector] public List<Vector3> manualPosition = new List<Vector3>();
        [HideInInspector] public List<Quaternion> manualRotation = new List<Quaternion>();
        [HideInInspector] public UnitPars thisPars;
        [HideInInspector] public NationAI thisNationAI;

        [HideInInspector] public NationPars nationPars;

        public Transform spawnPointPositionTransform;

        void Start()
        {
            rtsm = RTSMaster.active;
            diplomacy = Diplomacy.active;
            economy = Economy.active;

            GetNationPars();

            if ((nation >= 0) && (nation < rtsm.nationPars.Count))
            {
                if (rtsm.nationPars[nation] != null)
                {
                    if (rtsm.nationPars[nation].gameObject.GetComponent<NationAI>() != null)
                    {
                        thisNationAI = rtsm.nationPars[nation].gameObject.GetComponent<NationAI>();
                    }
                }
            }

            thisPars = this.gameObject.GetComponent<UnitPars>();
            RefreshTimeStep();

            if (thisPars != null)
            {
                nationName = thisPars.nationName;
            }

            if (nationPars != null)
            {
                nationName = nationPars.GetNationName();
            }

            if (spawnPointPositionTransform == null)
            {
                spawnPointPositionTransform = transform;
            }

            StartCoroutine(Spawner());
        }

        public NationPars GetNationPars()
        {
            if (nationPars == null)
            {
                nationPars = GetComponent<NationPars>();
            }

            return nationPars;
        }

        public IEnumerator Spawner()
        {
            isSpawning = true;
            rtsm = RTSMaster.active;
            economy = Economy.active;
            diplomacy = Diplomacy.active;

            count = 0;

            if (model != null)
            {
                modelPars = model.GetComponent<UnitPars>();
                modelParsType = model.GetComponent<UnitParsType>();

                if (isUpdateTimerRunning == false)
                {
                    StartCoroutine(UpdateTimer());
                }

                timeStart_gl = Time.time;
                int nSplits = 1;

                if (modelParsType.isBuilding == false)
                {
                    nSplits = formationSize;
                }

                int nIter = (int)(1f * numberOfObjects / nSplits);

                for (int i = 0; i < nIter; i++)
                {
                    bool pass1 = true;

                    if ((nation > -1) && (nation < economy.nationResources.Count))
                    {
                        for (int j = 0; j < modelParsType.costs.Count; j++)
                        {
                            for (int k = 0; k < economy.nationResources[nation].Count; k++)
                            {
                                if (economy.nationResources[nation][k].name == modelParsType.costs[j].name)
                                {
                                    if ((economy.nationResources[nation][k].amount - modelParsType.costs[j].amount) < 0)
                                    {
                                        pass1 = false;
                                    }
                                }
                            }
                        }
                    }

                    if (thisPars != null)
                    {
                        if (thisPars.isBuildFinished == false)
                        {
                            pass1 = false;
                        }
                    }

                    if (pass1)
                    {
                        timeStart_loc = Time.time;
                        enoughResources = true;

                        timestep = RefreshTimeStep();

                        List<UnitPars> formUnits = new List<UnitPars>();
                        yield return new WaitForSeconds(timestep * nSplits);

                        for (int j = 0; j < nSplits; j++)
                        {
                            if (nation >= economy.nationResources.Count)
                            {
                                while (nation >= economy.nationResources.Count)
                                {
                                    yield return new WaitForSeconds(0.02f);
                                }
                            }

                            for (int j1 = 0; j1 < modelParsType.costs.Count; j1++)
                            {
                                for (int k = 0; k < economy.nationResources[nation].Count; k++)
                                {
                                    if (economy.nationResources[nation][k].name == modelParsType.costs[j1].name)
                                    {
                                        economy.nationResources[nation][k].amount = economy.nationResources[nation][k].amount - modelParsType.costs[j1].amount;
                                    }
                                }
                            }

                            if (nation == diplomacy.playerNation)
                            {
                                economy.RefreshResources();
                            }

                            readynow = false;

                            Vector3 randomPosition = TerrainProperties.TerrainVectorProc(spawnPointPositionTransform.position + (0.1f * Random.insideUnitSphere));

                            float randAngle = Random.Range(0f, 360f);
                            Quaternion randomRotation = Quaternion.Euler(0f, randAngle, 0f);

                            GameObject go = null;

                            Vector3 pos1 = Vector3.zero;
                            Quaternion rot1 = Quaternion.identity;

                            if (isManualPosition == false)
                            {
                                pos1 = randomPosition;
                                rot1 = randomRotation;
                            }
                            else
                            {
                                Vector3 pos = manualPosition[0];
                                randAngle = manualRotation[0].eulerAngles.y;

                                pos1 = pos;
                                rot1 = manualRotation[0];

                                manualPosition.RemoveAt(0);
                                manualRotation.RemoveAt(0);

                                if (manualPosition.Count < 1)
                                {
                                    isManualPosition = false;
                                }
                            }

                            GameObject model2 = model;
                            string natName = nationName;

                            if (rtsm.isMultiplayer)
                            {
                                int rtsid1 = model.GetComponent<UnitPars>().rtsUnitId;
                                model2 = rtsm.rtsUnitTypePrefabsNetwork[rtsid1];

                                rtsm.rtsCameraNetwork.AddNetworkComponent(rtsid1, TerrainProperties.TerrainVectorProc(pos1), rot1, natName, 1);
                            }

                            else
                            {
                                go = Instantiate(model2, TerrainProperties.TerrainVectorProc(pos1), rot1);
                                UnitPars goPars = go.GetComponent<UnitPars>();

                                if (goPars != null)
                                {
                                    rtsm.rtsUniqueId++;
                                    goPars.nationName = natName;
                                    goPars.Spawn(natName);

                                    if (modelParsType.isBuilding == false)
                                    {
                                        if (formationSize > 1)
                                        {
                                            formUnits.Add(goPars);
                                        }
                                    }

                                    if (modelParsType.isBuilding)
                                    {
                                        UnitPars up_orig = GetComponent<UnitPars>();

                                        if (up_orig != null)
                                        {
                                            up_orig.AddExp(3, 15f);
                                        }
                                        else
                                        {
                                            if (nation == Diplomacy.active.playerNation)
                                            {
                                                if (SelectionManager.active.selectedGoPars.Count > 0)
                                                {
                                                    UnitPars up_orig2 = SelectionManager.active.selectedGoPars[0];

                                                    if (up_orig2 != null)
                                                    {
                                                        up_orig2.AddExp(3, 15f);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                NationAI nai = GetComponent<NationAI>();

                                                if (nai != null)
                                                {
                                                    if (
                                                        (goPars.rtsUnitId == 1) ||
                                                        (goPars.rtsUnitId == 2) ||
                                                        (goPars.rtsUnitId == 3) ||
                                                        (goPars.rtsUnitId == 4) ||
                                                        (goPars.rtsUnitId == 5) ||
                                                        (goPars.rtsUnitId == 6) ||
                                                        (goPars.rtsUnitId == 7) ||
                                                        (goPars.rtsUnitId == 8)
                                                    )
                                                    {
                                                        if (nai.centralBuildings.Count > 0)
                                                        {
                                                            UnitPars centralBuildingUp = nai.centralBuildings[0];

                                                            if (centralBuildingUp != null)
                                                            {
                                                                centralBuildingUp.AddExp(3, 15f);
                                                            }
                                                        }
                                                    }
                                                    else if (
                                                        (goPars.rtsUnitId == 9) ||
                                                        (goPars.rtsUnitId == 10)
                                                    )
                                                    {
                                                        if (nai.factories.Count > 0)
                                                        {
                                                            UnitPars factoryUp = nai.factories[0];

                                                            if (factoryUp != null)
                                                            {
                                                                factoryUp.AddExp(3, 15f);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        UnitPars up_orig = GetComponent<UnitPars>();

                                        if (up_orig != null)
                                        {
                                            up_orig.AddExp(3, 3f);
                                        }
                                    }
                                }
                            }

                            readynow = true;
                            count = count + 1;
                        }

                        if (formUnits.Count > 1)
                        {
                            Formations.active.CreateNewStrictFormation(formUnits);
                        }
                    }
                    else
                    {
                        enoughResources = false;
                        timeStart_gl = timeStart_gl + 0.2f;
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                isUpdateTimerRunning = false;
                if (ProgressCounterUI.active.isActive)
                {
                    if (thisPars != null)
                    {
                        if (thisPars.isSelected == true)
                        {
                            if (thisPars.nation == Diplomacy.active.playerNation)
                            {
                                CancelSpawnUI.active.DeActivate();
                                ProgressCounterUI.active.DeActivate();
                                SpawnGridUI.active.OpenBuildingMenu(thisPars.rtsUnitId);
                            }
                        }
                    }
                }

            }

            isSpawning = false;
            yield return new WaitForSeconds(0.02f);
        }

        public static GameObject InstantiateImmediatelly(int id, Vector3 pos1, Quaternion rot1, int nation1)
        {
            RTSMaster rtsm1 = RTSMaster.active;
            GameObject goPref1 = null;

            if (rtsm1.isMultiplayer)
            {
                goPref1 = rtsm1.rtsUnitTypePrefabsNetwork[id];
            }
            else
            {
                goPref1 = rtsm1.rtsUnitTypePrefabs[id];
            }

            GameObject go1 = Instantiate(
                goPref1, pos1, rot1
            );

            SpawnPoint.SetParsImmediatelly(go1, Diplomacy.active.GetNationNameFromId(nation1));

            return go1;
        }

        public static void SetParsImmediatelly(GameObject go, string nationName1)
        {
            UnitPars goPars = go.GetComponent<UnitPars>();
            goPars.Spawn(nationName1);
        }

        public IEnumerator UpdateTimer()
        {
            isUpdateTimerRunning = true;

            while (isUpdateTimerRunning == true)
            {
                if (modelPars != null)
                {
                    int nSplits = 1;
                    if (RTSMaster.active.rtsUnitTypePrefabsUpt[modelPars.rtsUnitId].isBuilding == false)
                    {
                        nSplits = formationSize;
                    }

                    progress_loc = (Time.time - timeStart_loc) / (timestep * nSplits);
                    if (progress_loc > 1f)
                    {
                        progress_loc = 1f;
                    }

                    progress_gl = (1f * count + progress_loc * nSplits) / numberOfObjects;
                    ProgressCounterUI pcUI = ProgressCounterUI.active;

                    if (pcUI.isActive)
                    {
                        if (thisPars != null)
                        {
                            if (thisPars.isSelected == true)
                            {
                                if (thisPars.nation == Diplomacy.active.playerNation)
                                {
                                    pcUI.UpdateText(GetProgressString());
                                    pcUI.UpdateLocalValue(progress_loc);
                                    pcUI.UpdateGlobalValue(progress_gl);
                                }
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(0.02f);
            }

            isUpdateTimerRunning = false;
        }

        public string GetProgressString()
        {
            int nSplits = 1;

            if (modelPars != null)
            {
                if (RTSMaster.active.rtsUnitTypePrefabsUpt[modelPars.rtsUnitId].isBuilding == false)
                {
                    nSplits = formationSize;
                }
            }

            return ((count + nSplits).ToString() + "/" + numberOfObjects.ToString());
        }

        public void StopSpawning()
        {
            numberOfObjects = 0;

            if (isSpawning == true)
            {
                StopCoroutine("Spawner");
            }

            isSpawning = false;
            isUpdateTimerRunning = false;
            int id = thisPars.rtsUnitId;
            SelectionManager.active.ActivateBuildingsMenu(id);
        }

        public void StartSpawning()
        {

            if (isSpawning == true)
            {
                StopCoroutine("Spawner");
            }

            if (this != null)
            {
                StartCoroutine("Spawner");
            }
        }

        public float RefreshTimeStep()
        {
            float tStep = timestep;

            if (model != null)
            {
                UnitParsType unitParsType = model.GetComponent<UnitParsType>();

                if (unitParsType.isBuilding == false)
                {
                    int numberWindmills = RTSMaster.active.numberOfUnitTypes[nation][8] + 1;

                    tStep = unitParsType.buildTime;

                    if (numberWindmills > 0)
                    {
                        tStep = unitParsType.buildTime / numberWindmills;
                    }

                    if (tStep < 0.1f * unitParsType.buildTime)
                    {
                        tStep = 0.1f * unitParsType.buildTime;
                    }

                    if (Cheats.active.godMode == 1)
                    {
                        if (nation == Diplomacy.active.playerNation)
                        {
                            tStep = 0.03f * unitParsType.buildTime;
                        }
                    }

                    if (thisPars != null)
                    {
                        float lvlTimeReduce = 1f / (0.5f * thisPars.levelValues[3] + 1f);
                        tStep = tStep * lvlTimeReduce;
                    }
                }
            }

            return tStep;
        }
    }
}
