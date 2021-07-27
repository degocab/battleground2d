using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;

namespace RTSToolkit
{
    public class BattleAI : MonoBehaviour
    {
        [HideInInspector] public int nation = 0;
        [HideInInspector] public NationPars nationPars;

        [HideInInspector] public List<UnitPars> searchersPars = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> approachersPars = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> targetsPars = new List<UnitPars>();
        public KDTree targetsKD;
        public KDTreeStruct targetsKD_j;

        public List<UnitPars> allUnits = null;

        void Start()
        {
            GetNationPars();
            allUnits = BattleSystem.active.unitssUP;
        }

        float iTime1 = 0f;
        float iTime2 = 0f;

        float timeEnd1 = 0.57f;
        float timeEnd2 = 0.57f;

        void Update()
        {
            float dt = Time.deltaTime;
            iTime1 = iTime1 + dt;

            bool areFreeTargetsReset = false;

            if (iTime1 > timeEnd1)
            {
                ResetFreeTargets();
                areFreeTargetsReset = true;

                ResetSearchers();
                FindTargets();
                iTime1 = 0;
                timeEnd1 = Random.Range(0.57f, 0.63f);
            }

            iTime2 = iTime2 + dt;

            if (iTime2 > timeEnd2)
            {
                if (areFreeTargetsReset == false)
                {
                    ResetFreeTargets();
                    areFreeTargetsReset = true;
                }

                ResetApproachers();
                FindApproacherTargets();
                iTime2 = 0;
                timeEnd2 = Random.Range(0.57f, 0.63f);
            }
        }

        public void ResetFreeTargets()
        {
            // uses check for maximum number of attackers  
            int n1 = targetsPars.Count;
            int n2 = 0;

            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                if (i != nation)
                {
                    if (Diplomacy.active.relations[nation][i] == 1)
                    {
                        for (int j = 0; j < RTSMaster.active.nationPars[i].allNationUnits.Count; j++)
                        {
                            UnitPars up = RTSMaster.active.nationPars[i].allNationUnits[j];

                            if (up.attackers.Count < up.unitParsType.maxAttackers)
                            {
                                if ((up.isDying == false) && (up.isSinking == false))
                                {
                                    if (n2 < n1)
                                    {
                                        targetsPars[n2] = up;
                                    }
                                    else
                                    {
                                        targetsPars.Add(up);
                                    }

                                    n2++;
                                }
                            }
                        }
                    }
                }
            }

            if (n2 < n1)
            {
                for (int i = n2; i < n1; i++)
                {
                    targetsPars.RemoveAt(n2);
                }
            }

            RebuildTargetsKD();
        }

        public NativeArray<Vector3> targPosNative;

        public void RebuildTargetsKD()
        {
            if (UseJobSystem.useJobifiedKdtree_s)
            {
                int nTarg = targetsPars.Count;

                if (nTarg > 0)
                {
                    if (targPosNative.IsCreated)
                    {
                        if (targPosNative.Length != nTarg)
                        {
                            targPosNative.Dispose();
                            targPosNative = new NativeArray<Vector3>(nTarg, Allocator.Persistent);
                        }
                    }
                    else
                    {
                        targPosNative = new NativeArray<Vector3>(nTarg, Allocator.Persistent);
                    }

                    for (int i = 0; i < nTarg; i++)
                    {
                        if (targetsPars[i] != null)
                        {
                            targPosNative[i] = targetsPars[i].transform.position;
                        }
                    }

                    targetsKD_j.MakeFromPoints(targPosNative);
                }
            }
            else
            {
                int nTarg = targetsPars.Count;
                if (nTarg > 0)
                {
                    Vector3[] targPos = new Vector3[nTarg];

                    for (int i = 0; i < nTarg; i++)
                    {
                        if (targetsPars[i] != null)
                        {
                            targPos[i] = targetsPars[i].transform.position;
                        }
                    }

                    targetsKD = KDTree.MakeFromPoints(targPos);
                }
            }
        }

        public void RemoveTarget(UnitPars targ)
        {
            if (targ != null)
            {
                targetsPars.Remove(targ);
                RebuildTargetsKD();
            }
        }

        int i_ResetSearchers = 0;
        public void ResetSearchers()
        {
            searchersPars.Clear();

            int n = 200;
            if (n > nationPars.allNationUnits.Count)
            {
                n = nationPars.allNationUnits.Count;
            }

            for (int i = 0; i < n; i++)
            {
                i_ResetSearchers++;

                if (i_ResetSearchers >= nationPars.allNationUnits.Count)
                {
                    i_ResetSearchers = 0;
                }

                UnitPars up = nationPars.allNationUnits[i_ResetSearchers];

                if (up.militaryMode == 10)
                {
                    if (up.unitParsType.isBuilding == false)
                    {
                        if (up.unitParsType.isWorker == false)
                        {
                            searchersPars.Add(up);
                        }
                    }
                }
            }
        }

        int i_ResetApproachers = 0;
        public void ResetApproachers()
        {
            approachersPars.Clear();

            int n = 40;
            if (n > nationPars.allNationUnits.Count)
            {
                n = nationPars.allNationUnits.Count;
            }

            for (int i = 0; i < n; i++)
            {
                i_ResetApproachers++;

                if (i_ResetApproachers >= nationPars.allNationUnits.Count)
                {
                    i_ResetApproachers = 0;
                }

                UnitPars up = nationPars.allNationUnits[i_ResetApproachers];

                if (up.militaryMode == 20)
                {
                    if (up.unitParsType.isBuilding == false)
                    {
                        if (up.strictApproachMode == false)
                        {
                            if (up.unitParsType.isWorker == false)
                            {
                                approachersPars.Add(up);
                            }
                        }
                    }
                }
            }
        }

        NativeArray<Vector3> attackerPos;
        NativeArray<int> attackerNeigh;

        public void FindTargets()
        {
            // assign simple neighbour targets
            float timeNow = Time.time;

            if (UseJobSystem.useJobifiedKdtree_s)
            {
                if (targetsPars.Count > 0)
                {
                    int nAtt = searchersPars.Count;

                    if (attackerPos.IsCreated)
                    {
                        if (attackerPos.Length != nAtt)
                        {
                            attackerPos.Dispose();
                            attackerPos = new NativeArray<Vector3>(nAtt, Allocator.Persistent);
                        }
                    }
                    else
                    {
                        attackerPos = new NativeArray<Vector3>(nAtt, Allocator.Persistent);
                    }

                    if (attackerNeigh.IsCreated)
                    {
                        if (attackerNeigh.Length != nAtt)
                        {
                            attackerNeigh.Dispose();
                            attackerNeigh = new NativeArray<int>(nAtt, Allocator.Persistent);
                        }
                    }
                    else
                    {
                        attackerNeigh = new NativeArray<int>(nAtt, Allocator.Persistent);
                    }

                    for (int i = 0; i < nAtt; i++)
                    {
                        attackerPos[i] = searchersPars[i].transform.position;
                        attackerNeigh[i] = -1;
                    }

                    int processorCount = System.Environment.ProcessorCount;
                    var job = new KdSearchJob
                    {
                        kd_job = targetsKD_j,
                        queries_job = attackerPos,
                        answers_job = attackerNeigh
                    };

                    JobHandle jobHandle = job.Schedule(nAtt, processorCount);
                    jobHandle.Complete();

                    for (int i = 0; i < nAtt; i++)
                    {
                        int k = attackerNeigh[i];

                        if ((k >= 0) && (k < targetsPars.Count))
                        {
                            Vector3 attPos = attackerPos[i];

                            if (i >= nAtt)
                            {
                                Debug.Log("i >= nAtt");
                            }
                            if (i < 0)
                            {
                                Debug.Log("i < 0");
                            }
                            if (k >= targetsPars.Count)
                            {
                                Debug.Log("k >= targetsPars.Count");
                            }
                            if (k < 0)
                            {
                                Debug.Log("k < 0");
                            }

                            float r = (attPos - targetsPars[k].transform.position).sqrMagnitude;

                            if (nationPars != null)
                            {
                                if ((attPos - transform.position).sqrMagnitude > (0.5f * nationPars.nationSize) * (0.5f * nationPars.nationSize))
                                {
                                    searchersPars[i].sqrSearchDistance = (attPos - transform.position).sqrMagnitude;
                                }
                            }

                            if (r < searchersPars[i].sqrSearchDistance)
                            {
                                AssignTarget_MaxCheck(searchersPars[i], targetsPars[k]);
                            }
                            else if (IsInsideNation(targetsPars[k].transform.position, nation) &&
                                    IsInsideNation(attPos, nation))
                            {
                                AssignTarget_MaxCheck(searchersPars[i], targetsPars[k]);
                            }
                            else if (IsInsideNation(targetsPars[k].transform.position, targetsPars[k].nation) &&
                                    IsInsideNation(attPos, targetsPars[k].nation))
                            {
                                AssignTarget_MaxCheck(searchersPars[i], targetsPars[k]);
                            }
                        }
                    }
                }
            }
            else
            {
                if (targetsPars.Count > 0)
                {
                    int nAtt = searchersPars.Count;
                    Vector3 nationPos = transform.position;

                    for (int i = 0; i < nAtt; i++)
                    {
                        UnitPars up = searchersPars[i];
                        Vector3 attPos = up.transform.position;
                        int k = targetsKD.FindNearest(attPos);

                        if ((k >= 0) && (k < targetsPars.Count))
                        {
                            if (i >= nAtt)
                            {
                                Debug.Log("i >= nAtt");
                            }
                            if (i < 0)
                            {
                                Debug.Log("i < 0");
                            }
                            if (k >= targetsPars.Count)
                            {
                                Debug.Log("k >= targetsPars.Count");
                            }
                            if (k < 0)
                            {
                                Debug.Log("k < 0");
                            }

                            UnitPars targPars = targetsPars[k];
                            Vector3 targPos = targPars.transform.position;

                            float r = (attPos - targPos).sqrMagnitude;

                            if (nationPars != null)
                            {
                                if (nationPars.nationSize > 0)
                                {
                                    float attPos_nationPos_mag = (attPos - nationPos).sqrMagnitude;
                                    float natSize = 0.5f * nationPars.nationSize;
                                    if (attPos_nationPos_mag > natSize * natSize)
                                    {
                                        up.sqrSearchDistance = attPos_nationPos_mag;
                                    }
                                }
                            }

                            if (r < up.sqrSearchDistance)
                            {
                                AssignTarget_MaxCheck(up, targPars);
                            }
                            else if (IsInsideNation(targPos, nation) &&
                                    IsInsideNation(attPos, nation))
                            {
                                AssignTarget_MaxCheck(up, targPars);

                            }
                            else if (IsInsideNation(targPos, targPars.nation) &&
                                    IsInsideNation(attPos, targPars.nation))
                            {
                                AssignTarget_MaxCheck(up, targPars);
                            }
                        }
                    }
                }
            }
        }

        NativeArray<Vector3> approacherPos;
        NativeArray<int> approacherNeigh;

        public void FindApproacherTargets()
        {
            // assign simple neighbour targets
            if (UseJobSystem.useJobifiedKdtree_s)
            {
                if (targetsPars.Count > 0)
                {
                    int nApp = approachersPars.Count;

                    if (approacherPos.IsCreated)
                    {
                        if (approacherPos.Length != nApp)
                        {
                            approacherPos.Dispose();
                            approacherPos = new NativeArray<Vector3>(nApp, Allocator.Persistent);
                        }
                    }
                    else
                    {
                        approacherPos = new NativeArray<Vector3>(nApp, Allocator.Persistent);
                    }

                    if (approacherNeigh.IsCreated)
                    {
                        if (approacherNeigh.Length != nApp)
                        {
                            approacherNeigh.Dispose();
                            approacherNeigh = new NativeArray<int>(nApp, Allocator.Persistent);
                        }
                    }
                    else
                    {
                        approacherNeigh = new NativeArray<int>(nApp, Allocator.Persistent);
                    }

                    for (int i = 0; i < nApp; i++)
                    {
                        approacherPos[i] = approachersPars[i].transform.position;
                        approacherNeigh[i] = -1;
                    }

                    int processorCount = System.Environment.ProcessorCount;
                    var job = new KdSearchJob
                    {
                        kd_job = targetsKD_j,
                        queries_job = approacherPos,
                        answers_job = approacherNeigh
                    };

                    JobHandle jobHandle = job.Schedule(nApp, processorCount);
                    jobHandle.Complete();

                    for (int i = 0; i < nApp; i++)
                    {
                        int k = approacherNeigh[i];
                        if ((k >= 0) && (k < targetsPars.Count))
                        {
                            if (approachersPars[i].targetUP != null)
                            {
                                float r1 = (approacherPos[i] - targetsPars[k].transform.position).sqrMagnitude;
                                float r2 = (approacherPos[i] - approachersPars[i].targetUP.transform.position).sqrMagnitude;

                                if (r1 < 0.25f * r2)
                                {
                                    AssignApproacherTarget_MaxCheck(approachersPars[i], targetsPars[k]);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (targetsPars.Count > 0)
                {
                    for (int i = 0; i < approachersPars.Count; i++)
                    {
                        UnitPars up = approachersPars[i];
                        if (up.targetUP != null)
                        {
                            Vector3 upPos = up.transform.position;
                            int k = targetsKD.FindNearest(upPos);

                            if ((k >= 0) && (k < targetsPars.Count))
                            {
                                UnitPars targPars = targetsPars[k];

                                float r1 = (upPos - targPars.transform.position).sqrMagnitude;
                                float r2 = (upPos - up.targetUP.transform.position).sqrMagnitude;

                                if (r1 < 0.25f * r2)
                                {
                                    AssignApproacherTarget_MaxCheck(up, targPars);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AssignTarget_MaxCheck(UnitPars attPars, UnitPars targPars)
        {
            attPars.AssignTarget(targPars);
            attPars.militaryMode = 20;

            if ((attPars.wanderingMode > 10) && (attPars.wanderingMode < 100))
            {
                attPars.wanderingMode = 10;
            }
            else if (attPars.wanderingMode > 110)
            {
                attPars.wanderingMode = 110;
            }

            if (CheckMaxumumAttackersNumber(targPars, 1) == false)
            {
                RemoveTarget(targPars);
            }
        }

        public void AssignApproacherTarget_MaxCheck(UnitPars attPars, UnitPars targPars)
        {
            attPars.AssignTarget(targPars);

            if (CheckMaxumumAttackersNumber(targPars, 1) == false)
            {
                RemoveTarget(targPars);
            }
        }

        public bool CheckMaxumumAttackersNumber(UnitPars up, int mode)
        {
            bool check = false;

            if (mode == 1)
            {
                if (up.attackers.Count < up.unitParsType.maxAttackers)
                {
                    check = true;
                }
            }

            if (mode == 2)
            {
                if (up.attackers.Count < up.maxHealth * 0.2f + 5f)
                {
                    check = true;
                }
            }

            return check;
        }

        public bool IsInsideNation(Vector3 pos, int nationId)
        {
            bool isInside = false;
            NationPars np = RTSMaster.active.nationPars[nationId];
            float rsq = (pos - np.transform.position).sqrMagnitude;
            float nationSize = 1.5f * np.nationAI.size;

            if (rsq < (nationSize * nationSize))
            {
                isInside = true;
            }

            return isInside;
        }

        public NationPars GetNationPars()
        {
            if (nationPars == null)
            {
                nationPars = GetComponent<NationPars>();
            }

            return nationPars;
        }

        public UnitPars GetTarget(Vector3 attPos)
        {
            UnitPars targ = null;

            if (targetsPars.Count > 0)
            {
                if (UseJobSystem.useJobifiedKdtree_s)
                {
                    int k = targetsKD_j.FindNearest(attPos);
                    targ = targetsPars[k];
                }
                else
                {
                    int k = targetsKD.FindNearest(attPos);
                    targ = targetsPars[k];
                }
            }

            return targ;
        }

        void OnApplicationQuit()
        {
            ApplicationQuitAction();
        }

        void OnDestroy()
        {
            ApplicationQuitAction();
        }

        void ApplicationQuitAction()
        {
            if (targPosNative.IsCreated)
            {
                targPosNative.Dispose();
            }

            if (attackerPos.IsCreated)
            {
                attackerPos.Dispose();
            }
            if (attackerNeigh.IsCreated)
            {
                attackerNeigh.Dispose();
            }

            if (approacherPos.IsCreated)
            {
                approacherPos.Dispose();
            }
            if (approacherNeigh.IsCreated)
            {
                approacherNeigh.Dispose();
            }

            targetsKD_j.DisposeArrays();
        }
    }
}
