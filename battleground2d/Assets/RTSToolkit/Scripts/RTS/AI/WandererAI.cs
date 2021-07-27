using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class WandererAI : MonoBehaviour
    {
        [HideInInspector] public List<UnitPars> guardsPars = new List<UnitPars>();
        [HideInInspector] public int nation = -1;
        [HideInInspector] public int wanderingNation = -1;

        [HideInInspector] public float sumOfAllDistances = 0f;
        [HideInInspector] public float nationDistanceRatio = 0f;

        [HideInInspector] public List<float> nationDistanceRatios = new List<float>();
        [HideInInspector] public List<int> maxWanderersCounts = new List<int>();

        [HideInInspector] public List<UnitPars> allUnits = null;

        [HideInInspector] public List<int> nOponentsInside = new List<int>();

        [HideInInspector] public List<Wanderer> wanderers = new List<Wanderer>();
        [HideInInspector] public List<Wanderer> returners = new List<Wanderer>();

        [HideInInspector] public bool areListsReady = false;

        [HideInInspector] public NationPars nationPars;

        void Start()
        {
            GetNationPars();

            if (nation != Diplomacy.active.playerNation)
            {
                if (allUnits == null)
                {
                    allUnits = BattleSystem.active.unitssUP;
                }

                if (RTSMaster.active.nationPars[nation].nationAI.isFirstTime)
                {
                    SetLists();
                }

                areListsReady = true;
            }
        }

        float tWandererPhase = 0f;
        float lastTWandererPhase = 0.57f;

        float tReturnGuardsToTheirPositions = 0f;
        float lastTReturnGuardsToTheirPositions = 2f;

        void Update()
        {
            float dt = Time.deltaTime;
            tWandererPhase = tWandererPhase + dt;

            if (tWandererPhase > lastTWandererPhase)
            {
                tWandererPhase = 0f;
                lastTWandererPhase = Random.Range(0.57f, 0.63f);
                WandererPhase();
            }

            tReturnGuardsToTheirPositions = tReturnGuardsToTheirPositions + dt;

            if (tReturnGuardsToTheirPositions > lastTReturnGuardsToTheirPositions)
            {
                tReturnGuardsToTheirPositions = 0f;
                lastTReturnGuardsToTheirPositions = Random.Range(2f, 5f);
                ReturnGuardsToTheirPositions();
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

        public void SetLists()
        {
            if (areListsReady == false)
            {
                maxWanderersCounts = new List<int>();
                nOponentsInside = new List<int>();

                for (int i = 0; i < Diplomacy.active.numberNations; i++)
                {
                    ExpandLists();
                }

                wanderers = new List<Wanderer>();
                returners = new List<Wanderer>();

                areListsReady = true;
            }
        }

        public void ExpandLists()
        {
            maxWanderersCounts.Add(0);
            nOponentsInside.Add(0);
        }

        public void RemoveNation(int natId)
        {
            if (natId < maxWanderersCounts.Count)
            {
                maxWanderersCounts.RemoveAt(natId);
                nOponentsInside.RemoveAt(natId);
            }
        }

        [HideInInspector] public bool runWandererPhase = true;
        void WandererPhase()
        {
            if (runWandererPhase)
            {
                Guarding();
                CountOponentsInside();

                for (int i = 0; i < (Diplomacy.active.numberNations - 1); i++)
                {
                    if (RTSMaster.active.nationPars[nation].isReady)
                    {
                        RTSMaster.active.nationPars[nation].RefreshDistances();
                        if (i < RTSMaster.active.nationPars[nation].sortedNationNeighbours.Count)
                        {
                            wanderingNation = RTSMaster.active.nationPars[nation].sortedNationNeighbours[i];

                            if (wanderingNation >= 0)
                            {
                                ResetMaxWanderersCount();
                            }
                            else
                            {
                                RTSMaster.active.nationPars[nation].SortNeighbourNations();
                            }
                        }
                        else
                        {
                            Debug.Log(
                                "i >= RTSMaster.active.nationPars[nation].sortedNationNeighbours.Count : " +
                                i +
                                " " +
                                RTSMaster.active.nationPars[nation].sortedNationNeighbours.Count
                            );
                        }
                    }
                }

                int mostDangerousNation = GetMostDangerousNation();

                if (mostDangerousNation > -1)
                {
                    StartWanderers(mostDangerousNation);
                }

                RefreshWanderers();
                ReturnWanderers();
                CleanReturners();
            }
        }

        [HideInInspector] public bool runReturnGuardsToTheirPositions = true;
        void ReturnGuardsToTheirPositions()
        {
            if (runReturnGuardsToTheirPositions)
            {
                if (runWandererPhase == false)
                {
                    Guarding();
                }

                for (int i = 0; i < guardsPars.Count; i++)
                {
                    if (guardsPars[i].militaryMode == 10)
                    {
                        if (guardsPars[i].GetCurrentSpeed() <= 0f)
                        {
                            if ((guardsPars[i].transform.position - guardsPars[i].guardingPosition).sqrMagnitude > 64f)
                            {
                                bool contain = false;

                                for (int j = 0; j < wanderers.Count; j++)
                                {
                                    if (wanderers[j].WandererContains(guardsPars[i]))
                                    {
                                        contain = true;
                                    }
                                }

                                for (int j = 0; j < returners.Count; j++)
                                {
                                    if (returners[j].WandererContains(guardsPars[i]))
                                    {
                                        contain = true;
                                    }
                                }

                                if (contain == false)
                                {
                                    UnitsMover.active.AddMilitaryAvoider(guardsPars[i], guardsPars[i].guardingPosition, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        public int GetMostDangerousNation()
        {
            float unitFlow = 0f;
            int mostDangerousNation = -1;

            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                if (i != nation)
                {
                    if ((RTSMaster.active.nationPars[nation] != null) && (RTSMaster.active.nationPars[i] != null))
                    {
                        float ourUnitCount = RTSMaster.active.nationPars[nation].allNationUnits.Count;
                        float theirUnitCount = RTSMaster.active.nationPars[i].allNationUnits.Count;

                        float dist = (RTSMaster.active.nationPars[nation].transform.position - RTSMaster.active.nationPars[i].transform.position).magnitude;
                        float newUnitFlow = theirUnitCount / dist;

                        if (newUnitFlow > unitFlow)
                        {
                            unitFlow = newUnitFlow;
                            mostDangerousNation = i;
                        }
                    }
                }
            }

            return mostDangerousNation;
        }

        float prevWanderersTime = -1000f;
        float timeToWaitForWanderers = 0f;

        public void StartWanderers(int opponentNat)
        {
            int nOnNation = 0;
            int oponentNationUnitsCount = 0;

            if (Time.time > (prevWanderersTime + timeToWaitForWanderers))
            {
                if ((wanderingNation >= 0) && (wanderingNation < maxWanderersCounts.Count))
                {
                    int iddleGuardCount = GetIddleGuardCountLight();

                    if (iddleGuardCount > 10)
                    {
                        if (iddleGuardCount > 0.5f * guardsPars.Count)
                        {
                            for (int i = 0; i < wanderers.Count; i++)
                            {
                                Wanderer wanderer = wanderers[i];

                                if (wanderer.nationToWander == opponentNat)
                                {
                                    nOnNation = nOnNation + wanderer.wanderersPars.Count;
                                }
                            }

                            oponentNationUnitsCount = RTSMaster.active.nationPars[opponentNat].allNationUnits.Count;

                            if ((nOnNation < 2f * oponentNationUnitsCount))
                            {
                                wanderers.Add(new Wanderer(this));
                                int wCount = wanderers.Count - 1;
                                int gCount = guardsPars.Count;
                                int gCount2 = 0;

                                for (int i = 0; i < gCount; i++)
                                {
                                    if (
                                        (guardsPars[i].isWandering == 0) &&
                                        (gCount2 < 0.5f * iddleGuardCount) &&
                                        ((nOnNation + gCount2 < 2f * oponentNationUnitsCount))
                                    )
                                    {
                                        if (IsInsideNation(guardsPars[i].transform.position, nation))
                                        {
                                            if (guardsPars[i].rtsUnitId != 20)
                                            {
                                                gCount2++;
                                                wanderers[wCount].AddUnit(guardsPars[i]);
                                                guardsPars[i].wanderingMode = -Mathf.Abs(guardsPars[i].wanderingMode);
                                            }
                                        }
                                    }
                                }

                                wanderers[wCount].SendUnitsCircle(RTSMaster.active.nationPars[opponentNat].transform.position, 3f * Mathf.Sqrt(0.5f * iddleGuardCount));
                                wanderers[wCount].nationToWander = opponentNat;
                                prevWanderersTime = Time.time;
                                timeToWaitForWanderers = Random.Range(5f, 120f);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshWanderers()
        {
            for (int i = 0; i < wanderers.Count; i++)
            {
                wanderers[i].centreOfMass = Vector3.zero;
                wanderers[i].centreOfMassVel = Vector3.zero;
                wanderers[i].angleToXAxis = 0;
                int ncom = 0;

                for (int j = 0; j < wanderers[i].wanderersPars.Count; j++)
                {
                    if (wanderers[i].wanderersPars[j] != null)
                    {
                        wanderers[i].centreOfMass = wanderers[i].centreOfMass + wanderers[i].wanderersPars[j].transform.position;
                        wanderers[i].centreOfMassVel = wanderers[i].centreOfMassVel + wanderers[i].wanderersPars[j].velocityVector;
                        ncom = ncom + 1;
                    }
                }

                if (ncom > 0)
                {
                    wanderers[i].centreOfMass = wanderers[i].centreOfMass / ncom;
                    wanderers[i].centreOfMassVel = wanderers[i].centreOfMassVel / ncom;

                    wanderers[i].angleToXAxis = GenericMath.Angle360(
                        new Vector3(1, 0, 0),
                        new Vector3(wanderers[i].centreOfMassVel.x, 0, wanderers[i].centreOfMassVel.z),
                        new Vector3(0, 1, 0)
                    );
                }

                for (int j = 0; j < wanderers[i].wanderersPars.Count; j++)
                {
                    UnitPars up = wanderers[i].wanderersPars[j];

                    if (up.wanderingMode != 110)
                    {
                        if (up.militaryMode == 10)
                        {
                            if (up.hasPath == false)
                            {
                                UnitsMover.active.AddMilitaryAvoider(up, wanderers[i].wanderingPosition, 0);
                            }
                            else if (up.GetCurrentSpeed() <= 0f)
                            {
                                UnitsMover.active.AddMilitaryAvoider(up, wanderers[i].wanderingPosition, 0);
                            }

                            if ((up.rtsUnitId > -1) && (up.rtsUnitId < RTSMaster.active.rtsUnitTypePrefabsUp.Count))
                            {
                                if (RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId] != null)
                                {
                                    Vector3 comVect = up.transform.position - wanderers[i].centreOfMass;
                                    comVect = GenericMath.RotAround(wanderers[i].angleToXAxis, comVect, new Vector3(0, 1, 0));
                                    float dist = comVect.x;

                                    if (!RTSMaster.active.useAStar)
                                    {
                                        if (RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId].thisNMA != null)
                                        {
                                            float origSpeed = RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId].thisNMA.speed;

                                            if (up.thisNMA != null)
                                            {
                                                up.lockForestSpeedChanges = true;
                                                float forestCoeff = 1f;

                                                if (Forest.active.IsPointInsideForest(up.transform.position))
                                                {
                                                    forestCoeff = 0.5f;
                                                }

                                                up.thisNMA.speed = forestCoeff * GenericMath.InterpolateClamped(dist, -25, 5, 0.5f * origSpeed, 1.1f * origSpeed);
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

        public void ReturnWanderers()
        {
            for (int i = 0; i < wanderers.Count; i++)
            {
                if (wanderers[i].wanderersPars.Count > 0)
                {
                    int targNation = wanderers[i].nationToWander;

                    float maxVel = wanderers[i].centreOfMassVel.magnitude;
                    if (maxVel <= 0)
                    {
                        wanderers[i].nCentreVelocityFail++;
                    }

                    if (RTSMaster.active.useAStar)
                    {
                        maxVel = wanderers[i].centreOfMassVel.magnitude;
                    }
                    else
                    {
                        maxVel = wanderers[i].wanderersPars[0].thisNMA.speed;
                    }

                    if (
                        (
                            0.5f * maxVel * wanderers[i].TimeSpend() + 100f >
                            (
                                TerrainProperties.TerrainVectorProc(RTSMaster.active.nationPars[targNation].transform.position) -
                                TerrainProperties.TerrainVectorProc(RTSMaster.active.nationPars[nation].transform.position)
                            ).magnitude
                        )
                        || (wanderers[i].nCentreVelocityFail > 20)
                    )
                    {
                        bool areAllWanderersIdle = true;

                        for (int j = 0; j < wanderers[i].wanderersPars.Count; j++)
                        {
                            if (wanderers[i].wanderersPars[j].militaryMode == 20 || wanderers[i].wanderersPars[j].militaryMode == 30)
                            {
                                areAllWanderersIdle = false;
                            }
                        }

                        if (areAllWanderersIdle)
                        {
                            returners.Add(new Wanderer(this));
                            int rCount = returners.Count - 1;

                            for (int j = 0; j < wanderers[i].wanderersPars.Count; j++)
                            {
                                UnitPars up = wanderers[i].wanderersPars[j];
                                returners[rCount].AddUnit(up);
                                up.wanderingMode = 110;

                                ResetUnitSpeed(up);
                            }

                            wanderers[i].CleanWanderers();
                            wanderers.Remove(wanderers[i]);
                        }
                    }
                }
                else
                {
                    wanderers.Remove(wanderers[i]);
                }
            }
        }

        public void ReturnFromTargetImmediatelly(int tNation)
        {
            for (int i = 0; i < wanderers.Count; i++)
            {
                if (wanderers[i].wanderersPars.Count > 0)
                {
                    if (wanderers[i].nationToWander == tNation)
                    {
                        returners.Add(new Wanderer(this));
                        int rCount = returners.Count - 1;

                        for (int j = 0; j < wanderers[i].wanderersPars.Count; j++)
                        {
                            UnitPars up = wanderers[i].wanderersPars[j];
                            returners[rCount].AddUnit(up);
                            up.wanderingMode = 110;

                            ResetUnitSpeed(up);
                        }

                        wanderers[i].CleanWanderers();
                        wanderers.Remove(wanderers[i]);
                    }
                }
                else
                {
                    wanderers.Remove(wanderers[i]);
                }
            }
        }

        public void ResetUnitSpeed(UnitPars up)
        {
            if ((up.rtsUnitId > -1) && (up.rtsUnitId < RTSMaster.active.rtsUnitTypePrefabsUp.Count))
            {
                if (RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId] != null)
                {
                    if (!RTSMaster.active.useAStar)
                    {
                        if (RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId].thisNMA != null)
                        {
                            float origSpeed = RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId].thisNMA.speed;
                            up.lockForestSpeedChanges = false;

                            if (up.thisNMA != null)
                            {
                                float forestCoeff = 1f;

                                if (Forest.active.IsPointInsideForest(up.transform.position))
                                {
                                    forestCoeff = 0.5f;
                                }

                                up.thisNMA.speed = forestCoeff * origSpeed;
                            }
                        }
                    }
                }
            }
        }

        public void CleanReturners()
        {
            for (int i = 0; i < returners.Count; i++)
            {
                if (returners[i].wanderersPars.Count > 0)
                {
                    float maxVel = 0f;
                    if (RTSMaster.active.useAStar)
                    {
                        maxVel = returners[i].wanderersPars[0].agentPars.maxSpeed;
                    }
                    else
                    {
                        maxVel = returners[i].wanderersPars[0].thisNMA.speed;
                    }

                    if (
                        0.5f * maxVel * returners[0].TimeSpend() + 100f >
                        (
                            TerrainProperties.TerrainVectorProc(RTSMaster.active.nationPars[RTSMaster.active.nationPars[nation].sortedNationNeighbours[0]].transform.position) -
                            TerrainProperties.TerrainVectorProc(RTSMaster.active.nationPars[nation].transform.position)
                        ).magnitude
                    )
                    {
                        returners[i].CleanWanderers();
                        returners.Remove(returners[i]);
                    }
                }
                else
                {
                    returners.Remove(returners[i]);
                }
            }
        }

        public int GetIddleGuardCount()
        {
            int count1 = 0;

            for (int i = 0; i < guardsPars.Count; i++)
            {
                bool pass = true;

                for (int j = 0; j < wanderers.Count; j++)
                {
                    if (wanderers[j].WandererContains(guardsPars[i]) == true)
                    {
                        pass = false;
                    }
                }

                for (int j = 0; j < returners.Count; j++)
                {
                    if (returners[j].WandererContains(guardsPars[i]) == true)
                    {
                        pass = false;
                    }
                }

                if (pass == true)
                {
                    count1++;
                }
            }

            return count1;
        }

        public int GetIddleGuardCountLight()
        {
            int count1 = 0;

            for (int j = 0; j < wanderers.Count; j++)
            {
                count1 = count1 + wanderers[j].wanderersPars.Count;
            }

            count1 = guardsPars.Count - count1;

            if (count1 < 0)
            {
                count1 = 0;
            }

            return count1;
        }

        public int ExpectedGuardCount()
        {
            return (RTSMaster.active.nationPars[nation].nationAI.GetExpectedMilitariesCount());
        }

        public void Guarding()
        {
            for (int i = 0; i < guardsPars.Count; i++)
            {
                if (RTSMaster.active.useAStar)
                {
                    if (guardsPars[i] == null)
                    {
                        Debug.Log("guardsPars[i] == null" + nation);
                    }
                    else if (guardsPars[i].agentPars == null)
                    {
                        Debug.Log("guardsPars[i].agentPars == null" + nation);
                    }
                }

                if (guardsPars[i].militaryMode == 10)
                {
                    if (guardsPars[i].wanderingMode == 110)
                    {
                        bool pass = false;

                        if (guardsPars[i].guardResetCount < 0)
                        {
                            if (RTSMaster.active.nationPars[nation] != null)
                            {
                                guardsPars[i].guardingPosition = TerrainProperties.RandomTerrainVectorCircleProc(RTSMaster.active.nationPars[nation].transform.position, RTSMaster.active.nationPars[nation].nationSize);
                            }

                            pass = true;
                        }
                        else if (guardsPars[i].guardResetCount < Random.Range(6, 10))
                        {
                            guardsPars[i].guardResetCount = guardsPars[i].guardResetCount + 1;
                        }
                        else
                        {
                            pass = true;
                        }

                        if (pass == true)
                        {
                            guardsPars[i].guardResetCount = 0;
                            guardsPars[i].wanderingMode = 120;
                            UnitsMover.active.AddMilitaryAvoider(guardsPars[i], guardsPars[i].guardingPosition, 0);

                            if (guardsPars[i].guardingPosition.magnitude < 0.5f)
                            {
                                Debug.Log(guardsPars[i].guardingPosition.magnitude);
                            }
                        }
                    }
                    else if (guardsPars[i].wanderingMode == 120)
                    {
                        if (CheckCriticalDistance(guardsPars[i], guardsPars[i].guardingPosition, 8f))
                        {
                            guardsPars[i].wanderingMode = 130;
                        }
                    }
                }
            }
        }

        public void CountOponentsInside()
        {
            for (int i = 0; i < Diplomacy.active.numberNations; i++)
            {
                if (i >= nOponentsInside.Count)
                {
                    nOponentsInside.Add(0);
                }

                nOponentsInside[i] = 0;
            }

            if (allUnits == null)
            {
                allUnits = BattleSystem.active.unitssUP;
            }

            for (int i = 0; i < allUnits.Count; i++)
            {
                UnitPars up = allUnits[i];
                int nat = up.nation;

                if (up.unitParsType.isBuilding == false)
                {
                    if (up.unitParsType.isWorker == false)
                    {
                        if (IsInsideNation(up.transform.position, nation) == true)
                        {
                            if ((nat > -1) && (nat < nOponentsInside.Count))
                            {
                                nOponentsInside[nat] = nOponentsInside[nat] + 1;
                            }
                        }
                    }
                }
            }
        }

        public void RemoveUnit(UnitPars up)
        {
            guardsPars.Remove(up);

            for (int i = 0; i < wanderers.Count; i++)
            {
                List<UnitPars> upL = wanderers[i].wanderersPars;
                upL.Remove(up);
            }

            for (int i = 0; i < returners.Count; i++)
            {
                List<UnitPars> upL = returners[i].wanderersPars;
                upL.Remove(up);
            }
        }

        public bool CheckCriticalDistance(UnitPars up, Vector3 dest, float critR)
        {
            bool isInside = false;
            float r = (up.transform.position - dest).sqrMagnitude;

            if (r < critR * critR)
            {
                isInside = true;
            }

            return isInside;
        }

        public bool IsInsideNation(Vector3 pos, int nationId)
        {
            bool isInside = false;

            if (RTSMaster.active.nationPars[nationId] != null)
            {
                float rSq = (pos - RTSMaster.active.nationPars[nationId].transform.position).sqrMagnitude;
                float nationSize = RTSMaster.active.nationPars[nationId].nationAI.size * RTSMaster.active.nationPars[nationId].nationAI.size;

                if (rSq < nationSize)
                {
                    isInside = true;
                }
            }

            return isInside;
        }

        public void ResetMaxWanderersCount()
        {
            List<UnitPars> militaryUnits = RTSMaster.active.nationPars[nation].nationAI.militaryUnits;

            for (int i = 0; i < Diplomacy.active.numberNations; i++)
            {
                if (i < RTSMaster.active.nationPars[nation].neighboursDistanceFrac.Count)
                {
                    if (i != nation)
                    {
                        int k11 = 0;

                        while ((i >= maxWanderersCounts.Count) && (k11 < 100))
                        {
                            k11++;
                            maxWanderersCounts.Add(0);
                        }

                        maxWanderersCounts[i] = (int)(0.5f * militaryUnits.Count * RTSMaster.active.nationPars[nation].neighboursDistanceFrac[i]);
                    }
                }
            }
        }
    }

    public class Wanderer
    {
        public WandererAI wandererAI;
        public List<UnitPars> wanderersPars = new List<UnitPars>();
        public Vector3 wanderingPosition;

        public int nationToWander = -1;

        public float zeroTime = 0f;

        public Vector3 centreOfMass = Vector3.zero;
        public Vector3 centreOfMassVel = Vector3.zero;
        public float angleToXAxis = 0f;
        public int nCentreVelocityFail = 0;

        public Wanderer(WandererAI wAI)
        {
            wandererAI = wAI;
            zeroTime = Time.time;
        }

        public void AddUnit(UnitPars up)
        {
            if (wanderersPars.Contains(up))
            {
                Debug.Log("wanderersPars already contains up");
            }

            wanderersPars.Add(up);
            up.isWandering = 1;
        }

        public void RemoveUnit(UnitPars up)
        {
            wanderersPars.Remove(up);
            up.isWandering = 0;
        }

        public void SendUnitsCircle(Vector3 pos, float size)
        {
            wanderingPosition = pos;

            for (int i = 0; i < wanderersPars.Count; i++)
            {
                UnitsMover.active.AddMilitaryAvoider(wanderersPars[i], TerrainProperties.RandomTerrainVectorCircleProc(pos, size), 0);
            }
        }

        public void CleanWanderers()
        {
            for (int i = 0; i < wanderersPars.Count; i++)
            {
                wanderersPars[i].isWandering = 0;
                UnitsMover.active.CompleteMovement(wanderersPars[i]);
            }

            wanderersPars.Clear();
        }

        public float TimeSpend()
        {
            return (Time.time - zeroTime);
        }

        public bool WandererContains(UnitPars up)
        {
            return (wanderersPars.Contains(up));
        }
    }
}
