using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class Scores : MonoBehaviour
    {
        public static Scores active;

        [HideInInspector] public List<int> nUnits;
        [HideInInspector] public List<int> nBuildings;

        [HideInInspector] public List<int> unitsLost;
        [HideInInspector] public List<int> buildingsLost;

        [HideInInspector] public List<float> damageMade;
        [HideInInspector] public List<float> damageObtained;

        [HideInInspector] public float masterScore = 0;
        [HideInInspector] public float masterScoreDiff = 0;


        [HideInInspector] public List<float> masterScores = new List<float>();
        [HideInInspector] public List<float> masterScoresDiff = new List<float>();

        public List<TechTreeLocker> techTreeLockers = new List<TechTreeLocker>();

        RTSMaster rtsm;

        void Awake()
        {
            active = this;
        }

        public void ExpandLists()
        {
            nUnits.Add(0);
            nBuildings.Add(0);

            unitsLost.Add(0);
            buildingsLost.Add(0);

            damageMade.Add(0f);
            damageObtained.Add(0f);

            masterScores.Add(0f);
            masterScoresDiff.Add(0f);
        }

        public void RemoveNation(int natId)
        {
            if (natId > -1)
            {
                if (natId < nUnits.Count)
                {
                    nUnits.RemoveAt(natId);
                }
                if (natId < nBuildings.Count)
                {
                    nBuildings.RemoveAt(natId);
                }

                if (natId < unitsLost.Count)
                {
                    unitsLost.RemoveAt(natId);
                }
                if (natId < buildingsLost.Count)
                {
                    buildingsLost.RemoveAt(natId);
                }

                if (natId < damageMade.Count)
                {
                    damageMade.RemoveAt(natId);
                }
                if (natId < damageObtained.Count)
                {
                    damageObtained.RemoveAt(natId);
                }

                if (natId < masterScores.Count)
                {
                    masterScores.RemoveAt(natId);
                }
                if (natId < masterScoresDiff.Count)
                {
                    masterScoresDiff.RemoveAt(natId);
                }
            }
        }

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        float iUpdateUnitsTimeExp = 0f;
        float iUpdateUnitLockings = 0f;
        float iUpdateMasterScores = 0f;
        void Update()
        {
            float dt = Time.deltaTime;

            iUpdateUnitsTimeExp = iUpdateUnitsTimeExp + dt;
            if (iUpdateUnitsTimeExp > 0.2f)
            {
                UpdateUnitsTimeExp();
                iUpdateUnitsTimeExp = 0f;
            }

            iUpdateUnitLockings = iUpdateUnitLockings + dt;
            if (iUpdateUnitLockings > 1f)
            {
                UpdateUnitLockings();
                iUpdateUnitLockings = 0f;
            }

            iUpdateMasterScores = iUpdateMasterScores + dt;
            if (iUpdateMasterScores > 1f)
            {
                UpdateMasterScore();
                iUpdateMasterScores = 0f;
            }
        }

        void UpdateUnitsTimeExp()
        {
            for (int i = 0; i < rtsm.allUnits.Count; i++)
            {
                UnitPars up = rtsm.allUnits[i];

                for (int j = 0; j < up.levelExp.Length; j++)
                {
                    if ((up.nation >= 0) && (up.nation < rtsm.numberOfUnitTypes.Count))
                    {
                        if (rtsm.numberOfUnitTypes[up.nation][4] > 0)
                        {
                            if (j < up.unitParsType.levelExpTimeGain.Count)
                            {
                                up.AddExp(j, Random.Range(up.unitParsType.levelExpTimeGain[j].x, up.unitParsType.levelExpTimeGain[j].y) * 0.2f);
                            }
                        }
                        else
                        {
                            if (j < up.unitParsType.levelExpTimeGain.Count)
                            {
                                up.AddExp(j, Random.Range(up.unitParsType.levelExpTimeGain[j].x, up.unitParsType.levelExpTimeGain[j].y) * 0.1f);
                            }
                        }
                    }
                }
            }
        }

        void UpdateUnitLockings()
        {
            for (int i = 0; i < rtsm.nationPars.Count; i++)
            {
                if (rtsm.numberOfUnitTypes.Count > 0)
                {
                    if (i < rtsm.unitTypeLocking.Count)
                    {
                        for (int j = 0; j < rtsm.unitTypeLocking[i].Count; j++)
                        {
                            int i_tech = DefinedTechTreeId(j);

                            if (i_tech < 0)
                            {
                                if (rtsm.unitTypeLocking[i][j] == 0)
                                {
                                    if (i == Diplomacy.active.playerNation)
                                    {
                                        SpawnGridUI.active.EnableElement(j);
                                    }
                                }

                                rtsm.unitTypeLocking[i][j] = 1;
                            }
                            else
                            {
                                techTreeLockers[i_tech].UpdateTechTreeLocker(i);
                            }
                        }

                        for (int j = 0; j < rtsm.unitTypeLocking[i].Count; j++)
                        {
                            int i_tech = DefinedTechTreeId(j);

                            if (i_tech >= 0)
                            {
                                techTreeLockers[i_tech].RefreshPreviousCounts(i);
                            }
                        }
                    }
                }
            }
        }

        void UpdateMasterScore()
        {
            masterScore = masterScore + masterScoreDiff;
            masterScoreDiff = 0f;

            for (int i = 0; i < masterScores.Count; i++)
            {
                masterScores[i] = masterScores[i] + masterScoresDiff[i];
                masterScoresDiff[i] = 0f;
            }
        }

        public void AddToMasterScoreDiff(float diff, int nat)
        {
            if (nat == Diplomacy.active.playerNation)
            {
                masterScoreDiff = masterScoreDiff + diff;
            }

            if ((nat > -1) && (nat < masterScoresDiff.Count))
            {
                masterScoresDiff[nat] = masterScoresDiff[nat] + diff;
            }
        }

        public int DefinedTechTreeId(int rtsUnitId)
        {
            for (int i = 0; i < techTreeLockers.Count; i++)
            {
                if (techTreeLockers[i].rtsUnitId == rtsUnitId)
                {
                    return i;
                }
            }

            return -1;
        }

        [System.Serializable]
        public class TechTreeLocker
        {
            public int rtsUnitId = 0;
            public int existanceRtsUnitId = 0;
            public int existanceRtsUnitCount = 1;
            public int conditionalRtsUnitId = 0;
            public float conditionalProgressIncrement = 0.2f;
            public float randomProbability = 1f;

            public bool useExistancePass = true;
            public bool useConditionalPass = true;

            public void UpdateTechTreeLocker(int nat)
            {
                RTSMaster rtsm = RTSMaster.active;
                bool existancePass = false;

                if ((existanceRtsUnitId < 0) || (existanceRtsUnitId >= rtsm.numberOfUnitTypes[nat].Count))
                {
                    existancePass = true;
                }
                else
                {
                    if (rtsm.numberOfUnitTypes[nat][existanceRtsUnitId] >= existanceRtsUnitCount)
                    {
                        existancePass = true;
                    }
                }

                if (useExistancePass == false)
                {
                    existancePass = true;
                }

                bool conditionalPass = false;

                if (existancePass)
                {
                    if (rtsm.numberOfUnitTypes[nat][conditionalRtsUnitId] > rtsm.numberOfUnitTypesPrev[nat][conditionalRtsUnitId])
                    {
                        if (rtsm.unitTypeLocking[nat][rtsUnitId] == 0)
                        {
                            if (Random.value <= randomProbability)
                            {
                                rtsm.unitTypeLockingProgress[nat][rtsUnitId] = rtsm.unitTypeLockingProgress[nat][rtsUnitId] + conditionalProgressIncrement;

                                if (rtsm.unitTypeLockingProgress[nat][rtsUnitId] >= 1f)
                                {
                                    conditionalPass = true;
                                }
                            }
                        }
                    }
                }

                if (useConditionalPass == false)
                {
                    conditionalPass = true;
                }

                if (existancePass && conditionalPass)
                {
                    if (nat == Diplomacy.active.playerNation)
                    {
                        SpawnGridUI.active.EnableElement(rtsUnitId);
                    }

                    rtsm.unitTypeLocking[nat][rtsUnitId] = 1;
                }
            }

            public void RefreshPreviousCounts(int nat)
            {
                RTSMaster rtsm = RTSMaster.active;
                rtsm.numberOfUnitTypesPrev[nat][conditionalRtsUnitId] = rtsm.numberOfUnitTypes[nat][conditionalRtsUnitId];
            }
        }
    }
}
