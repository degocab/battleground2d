using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace RTSToolkit
{
    public class NationPars : MonoBehaviour
    {
        [HideInInspector] public int nation = 0;

        [HideInInspector] public float nationSize = 0f;
        [HideInInspector] public float sumOfAllNationsDistances = 0f;
        [HideInInspector] public float rSafe = 0f;

        [HideInInspector] public List<float> rNations = new List<float>();
        [HideInInspector] public List<float> neighboursDistanceFrac = new List<float>();

        [HideInInspector] public List<Vector3> safeAttackPoints = new List<Vector3>();

        [HideInInspector] public bool isReady = false;

        RTSMaster rtsm;

        [HideInInspector] public List<Vector3> nationPos = new List<Vector3>();
        [HideInInspector] public KDTree nationPosTree = null;
        [HideInInspector] public List<int> sortedNationNeighbours = new List<int>();

        [HideInInspector] public int nationIcon;
        string nationName;

        [HideInInspector] public bool isWizzardNation = false;
        [HideInInspector] public bool isWizzardSpawned = false;

        [HideInInspector] public Vector2i terrainTile;

        [HideInInspector] public List<UnitPars> allNationUnits = new List<UnitPars>();

        [HideInInspector] public KDTree allNationUnitsKD;
        public KDTreeStruct allNationUnitsKD_j;

        float discoveryDistance = 200f;

        NationCentreNetworkNode nationParsNetwork;

        [HideInInspector] public NationAI nationAI;
        [HideInInspector] public BattleAI battleAI;
        [HideInInspector] public WandererAI wandererAI;
        [HideInInspector] public SpawnPoint spawnPoint;
        [HideInInspector] public ResourcesCollection resourcesCollection;

        [HideInInspector] public bool isWarWarningIssued = false;

        [HideInInspector] public NationSpawnerDialogsGroup dialogGroup;
        [HideInInspector] public Color nationColor;

        void Start()
        {
            rtsm = RTSMaster.active;
            SetAllNationComponents();

            for (int i = 0; i < Diplomacy.active.numberNations; i++)
            {
                ExpandLists();
            }

            RefreshDistances();
            RefreshStaticPars();
            isReady = true;

            if (nation == 0)
            {
                nation = RTSMaster.active.nationPars.IndexOf(this);
            }

            GetNationNameAndSpawnHero();
        }

        public void GetNationNameAndSpawnHero()
        {
            if (isWizzardNation == false)
            {
                if (spawnPoint != null)
                {
                    Vector3 pos = TerrainProperties.TerrainVectorProc(transform.position);
                    Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    if (rtsm.isMultiplayer)
                    {
                        if (string.IsNullOrEmpty(nationName))
                        {
                            NationCentreNetworkNode ncnn = GetComponent<NationCentreNetworkNode>();

                            if (ncnn != null)
                            {
                                nationName = ncnn.nationName;
                            }
                        }

                        if (IsHeroPresent() == false)
                        {
                            rtsm.rtsCameraNetwork.AddNetworkComponent(20, TerrainProperties.TerrainVectorProc(pos), rot, nationName, 1);
                        }
                    }
                    else
                    {
                        if (IsHeroPresent() == false)
                        {
                            GameObject go = Instantiate(rtsm.rtsUnitTypePrefabs[20], pos, rot);
                            go.GetComponent<UnitPars>().Spawn(nationName);
                        }
                    }
                }
            }
        }

        float deltaTime;
        void Update()
        {
            deltaTime = Time.deltaTime;
            RefreshAllNationUnitsKD();
            DiscoverOtherNations();
        }

        public bool IsHeroPresent()
        {
            for (int i = 0; i < allNationUnits.Count; i++)
            {
                if (allNationUnits[i].rtsUnitId == 20)
                {
                    return true;
                }
            }

            return false;
        }

        public NationAI GetNationAI()
        {
            if (nationAI == null)
            {
                nationAI = GetComponent<NationAI>();
            }

            return nationAI;
        }

        public BattleAI GetBattleAI()
        {
            if (battleAI == null)
            {
                battleAI = GetComponent<BattleAI>();
            }

            return battleAI;
        }

        public WandererAI GetWandererAI()
        {
            if (wandererAI == null)
            {
                wandererAI = GetComponent<WandererAI>();
            }

            return wandererAI;
        }

        public SpawnPoint GetSpawnPoint()
        {
            if (spawnPoint == null)
            {
                spawnPoint = GetComponent<SpawnPoint>();
            }

            return spawnPoint;
        }

        public ResourcesCollection GetResourcesCollection()
        {
            if (resourcesCollection == null)
            {
                resourcesCollection = GetComponent<ResourcesCollection>();
            }

            return resourcesCollection;
        }

        public void SetAllNationComponents()
        {
            GetNationAI();
            GetBattleAI();
            GetWandererAI();
            GetSpawnPoint();
            GetResourcesCollection();
        }

        public void ExpandLists()
        {
            int i = safeAttackPoints.Count;

            if (rtsm != null)
            {
                if (i < Diplomacy.active.numberNations)
                {

                    if (RTSMaster.active.nationPars[i] != null)
                    {
                        safeAttackPoints.Add(RTSMaster.active.nationPars[i].transform.position);
                        nationPos.Add(RTSMaster.active.nationPars[i].transform.position);
                    }

                    sortedNationNeighbours.Add(0);
                }
            }
        }

        public void RemoveNation(int natId)
        {
            if (natId < safeAttackPoints.Count)
            {
                safeAttackPoints.RemoveAt(natId);
            }

            if (natId < nationPos.Count)
            {
                nationPos.RemoveAt(natId);
            }

            if (natId < sortedNationNeighbours.Count)
            {
                sortedNationNeighbours.RemoveAt(natId);
            }
        }

        public void AddNationUnit(UnitPars up)
        {
            if (!allNationUnits.Contains(up))
            {
                allNationUnits.Add(up);
            }
        }

        public void RemoveNationUnit(UnitPars up)
        {
            allNationUnits.Remove(up);
        }

        public void RefreshDistances()
        {
            sumOfAllNationsDistances = 0f;

            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                if (i < rNations.Count)
                {
                    if (i != nation)
                    {
                        if ((nation < RTSMaster.active.nationPars.Count) && (RTSMaster.active.nationPars[nation] != null) && (RTSMaster.active.nationPars[i] != null))
                        {
                            rNations[i] = (RTSMaster.active.nationPars[nation].transform.position - RTSMaster.active.nationPars[i].transform.position).magnitude;
                            sumOfAllNationsDistances = sumOfAllNationsDistances + rNations[i];
                        }
                    }
                    else
                    {
                        rNations[i] = 0f;
                    }
                }
            }

            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                if ((i < neighboursDistanceFrac.Count) && (i < rNations.Count))
                {
                    if (i != nation)
                    {

                        neighboursDistanceFrac[i] = 1f - (rNations[i] / sumOfAllNationsDistances);
                    }
                    else
                    {
                        neighboursDistanceFrac[i] = 0f;
                    }
                }
            }

            SortNeighbourNations();
        }

        public void SortNeighbourNations()
        {
            int nn = Diplomacy.active.numberNations;

            if (nn <= nationPos.Count)
            {
                if (nation < nationPos.Count)
                {
                    for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                    {
                        if (RTSMaster.active.nationPars[i] != null)
                        {
                            if (i < nationPos.Count)
                            {
                                nationPos[i] = RTSMaster.active.nationPars[i].transform.position;
                            }
                        }
                    }

                    nationPosTree = KDTree.MakeFromPoints(nationPos.ToArray());
                    sortedNationNeighbours = nationPosTree.FindNearestsK(nationPos[nation], Diplomacy.active.numberNations).ToList();
                }
            }
        }

        public void RefreshStaticPars()
        {
            if (isReady == true)
            {
                rSafe = nationSize + 40f;

                for (int i = 0; i < Diplomacy.active.numberNations; i++)
                {
                    if (i < rtsm.nationPars.Count)
                    {
                        if (i < safeAttackPoints.Count)
                        {
                            if (rtsm.nationPars[i] != null)
                            {
                                safeAttackPoints[i] = rtsm.nationPars[i].transform.position;

                                if (i != nation)
                                {
                                    safeAttackPoints[i] = GetSafePoint(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public Vector3 GetSafePoint(int iNat)
        {
            Vector3 v3 = rtsm.nationPars[iNat].transform.position;

            if (iNat < rNations.Count)
            {
                if (nation < rtsm.nationPars.Count)
                {
                    if (rNations[iNat] > (rSafe + rtsm.nationPars[iNat].rSafe))
                    {
                        Vector3 dr = rtsm.nationPars[iNat].transform.position - rtsm.nationPars[nation].transform.position;
                        float ratio = (rNations[iNat] - (rSafe + rtsm.nationPars[iNat].rSafe)) / rNations[iNat];
                        Vector3 dr2 = ratio * dr;

                        v3 = TerrainProperties.TerrainVectorProc(rtsm.nationPars[nation].transform.position + dr2);
                    }
                }
            }
            else
            {
                Debug.Log("GetSafePoint " + iNat + " " + nation);
            }

            return v3;
        }

        public Vector3 GetSafePointV3(int iNat, Vector3 unitPos)
        {
            Vector3 v3 = rtsm.nationPars[iNat].transform.position;
            float rToNation = (rtsm.nationPars[iNat].transform.position - unitPos).magnitude;

            if (rToNation > rtsm.nationPars[iNat].rSafe)
            {
                Vector3 dr = rtsm.nationPars[iNat].transform.position - unitPos;
                float ratio = (rToNation - rtsm.nationPars[iNat].rSafe) / rNations[iNat];
                Vector3 dr2 = ratio * dr;

                v3 = TerrainProperties.TerrainVectorProc(unitPos + dr2);
            }

            return v3;
        }

        NativeArray<Vector3> allNationUnitsPos;

        float tRefreshAllNationUnitsKD = 0f;
        void RefreshAllNationUnitsKD()
        {
            tRefreshAllNationUnitsKD = tRefreshAllNationUnitsKD + deltaTime;

            if (tRefreshAllNationUnitsKD > 1f)
            {
                tRefreshAllNationUnitsKD = 0f;

                if (UseJobSystem.useJobifiedKdtree_s)
                {
                    int n = allNationUnits.Count;

                    if (n > 0)
                    {
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

                        for (int i = 0; i < n; i++)
                        {
                            allNationUnitsPos[i] = allNationUnits[i].transform.position;
                        }

                        allNationUnitsKD_j.MakeFromPoints(allNationUnitsPos);
                    }
                }
                else
                {
                    allNationUnitsKD = KDHelper.TreeFromUnitPars(allNationUnits);
                }
            }
        }

        int iDiscoverOtherNations = 0;
        int jDiscoverOtherNations = 0;

        void DiscoverOtherNations()
        {
            int nToLoop = 2;
            if (allNationUnits.Count < nToLoop)
            {
                nToLoop = allNationUnits.Count;
            }

            if ((RTSMaster.active.nationPars.Count > 0) && (allNationUnits.Count > 0))
            {

                if (iDiscoverOtherNations >= RTSMaster.active.nationPars.Count)
                {
                    iDiscoverOtherNations = 0;
                }

                NationPars other = RTSMaster.active.nationPars[iDiscoverOtherNations];

                if ((other != null) && (Diplomacy.active.relations[other.nation][nation] == -1))
                {
                    bool otherKDUnitsPass = true;

                    if (UseJobSystem.useJobifiedKdtree_s)
                    {
                        if (other.allNationUnits.Count <= 0)
                        {
                            otherKDUnitsPass = false;
                        }
                    }
                    else
                    {
                        if (other.allNationUnitsKD == null)
                        {
                            otherKDUnitsPass = false;
                        }
                    }

                    if (otherKDUnitsPass)
                    {
                        float sqrDiscoveryDistance = discoveryDistance * discoveryDistance;

                        for (int j = 0; j < nToLoop; j++)
                        {
                            if (jDiscoverOtherNations >= allNationUnits.Count)
                            {
                                jDiscoverOtherNations = 0;
                                iDiscoverOtherNations++;
                            }

                            UnitPars upThis = allNationUnits[jDiscoverOtherNations];
                            jDiscoverOtherNations++;

                            if (upThis != null)
                            {
                                UnitPars nup = other.FindNearestUnit(upThis.transform.position);

                                if ((nup != null) && ((nup.transform.position - upThis.transform.position).sqrMagnitude < sqrDiscoveryDistance))
                                {

                                    string this_natName = string.Empty;

                                    if (upThis.networkUnique != null)
                                    {
                                        this_natName = upThis.networkUnique.nationName;
                                    }
                                    if (string.IsNullOrEmpty(this_natName))
                                    {
                                        this_natName = GetNationName();
                                    }

                                    string o_natName = string.Empty;

                                    if (nup.networkUnique != null)
                                    {
                                        o_natName = nup.networkUnique.nationName;
                                    }

                                    if (string.IsNullOrEmpty(o_natName))
                                    {
                                        o_natName = other.GetNationName();
                                    }

                                    if (RTSMaster.active.isMultiplayer == false)
                                    {
                                        Diplomacy.active.SetRelation(this_natName, o_natName, 0);
                                    }
                                    else
                                    {
                                        RTSMaster.active.rtsCameraNetwork.Cmd_SetRelation(this_natName, o_natName, 0);
                                    }

                                    string nonpNationName = string.Empty;
                                    string pNationName = string.Empty;

                                    if (this_natName != o_natName)
                                    {
                                        if (this_natName == Diplomacy.active.GetPlayerNationName())
                                        {
                                            nonpNationName = o_natName;
                                            pNationName = this_natName;
                                        }
                                        else if (o_natName == Diplomacy.active.GetPlayerNationName())
                                        {
                                            nonpNationName = this_natName;
                                            pNationName = o_natName;
                                        }
                                    }

                                    if (pNationName != string.Empty)
                                    {
                                        DiplomacyReportsUI.active.MakeProposal(nonpNationName, "Greetings");

                                        if (RTSMaster.active.isMultiplayer)
                                        {
                                            RTSMaster.active.rtsCameraNetwork.Cmd_SendNationMessage(pNationName, nonpNationName, "Greetings");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        iDiscoverOtherNations++;
                    }
                }
                else
                {
                    iDiscoverOtherNations++;
                }
            }
        }

        public UnitPars FindNearestUnit(Vector3 pos)
        {
            if (UseJobSystem.useJobifiedKdtree_s)
            {
                int id = allNationUnitsKD_j.FindNearest(pos);
                if (id > -1 && id < allNationUnits.Count)
                {
                    return allNationUnits[id];
                }
            }
            else
            {
                int id = allNationUnitsKD.FindNearest(pos);
                if (id > -1 && id < allNationUnits.Count)
                {
                    return allNationUnits[id];
                }
            }

            return null;
        }

        public int GetNationId()
        {
            if (RTSMaster.active.isMultiplayer)
            {
                if (nationParsNetwork == null)
                {
                    nationParsNetwork = GetComponent<NationCentreNetworkNode>();
                }
            }

            return RTSMaster.active.nationPars.IndexOf(this);
        }

        public string GetNationName()
        {
            return GetNationName(false);
        }

        public string GetNationName(bool forceSearchMultiplayer)
        {
            if (RTSMaster.active.isMultiplayer || forceSearchMultiplayer)
            {
                if (nationParsNetwork == null)
                {
                    if (this == null)
                    {
                        return "";
                    }

                    nationParsNetwork = GetComponent<NationCentreNetworkNode>();
                }

                if (nationParsNetwork != null)
                {
                    return nationParsNetwork.nationName;
                }
            }

            return nationName;
        }

        public int GetNationIconId()
        {
            if (nationIcon == 0)
            {
                for (int i = 0; i < NationSpawner.active.nations.Count; i++)
                {
                    NationSpawnerUnit nsu = NationSpawner.active.nations[i];

                    if (nsu.name == GetNationName())
                    {
                        nationIcon = nsu.icon;
                        return nationIcon;
                    }
                }
            }

            return nationIcon;
        }

        public void SetNationId(int id)
        {
#if URTS_UNET
        if (RTSMaster.active.isMultiplayer)
        {
            if (this.gameObject.GetComponent<NetworkIdentity>() != null)
            {

            }
            else
            {
                nation = id;
            }
        }
        else
        {
            nation = id;
        }
#else
            nation = id;
#endif
        }

        public void SetNationName(string nm)
        {
            if (RTSMaster.active.isMultiplayer)
            {
                if (RTSMaster.active.rtsCameraNetwork != null)
                {
                    RTSMaster.active.rtsCameraNetwork.Cmd_SetNationName(this.gameObject, nm);
                }
            }
            else
            {
                nationName = nm;
            }
        }

        void OnApplicationQuit()
        {
            if (allNationUnitsPos.IsCreated)
            {
                allNationUnitsPos.Dispose();
            }

            allNationUnitsKD_j.DisposeArrays();
        }

        void OnDestroy()
        {
            if (allNationUnitsPos.IsCreated)
            {
                allNationUnitsPos.Dispose();
            }

            allNationUnitsKD_j.DisposeArrays();
        }
    }
}
