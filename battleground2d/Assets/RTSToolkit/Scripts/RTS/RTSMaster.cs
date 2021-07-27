using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class RTSMaster : MonoBehaviour
    {
        public static RTSMaster active;

        public int rtsUniqueId = 0;

        public List<GameObject> rtsUnitTypePrefabs = new List<GameObject>();
        [HideInInspector] public List<UnitPars> rtsUnitTypePrefabsUp = new List<UnitPars>();
        [HideInInspector] public List<UnitParsType> rtsUnitTypePrefabsUpt = new List<UnitParsType>();
        public List<GameObject> rtsUnitTypePrefabsNetwork = new List<GameObject>();

        public List<List<int>> numberOfUnitTypes = new List<List<int>>();
        public List<List<int>> numberOfUnitTypesPrev = new List<List<int>>();
        public List<List<int>> unitTypeLocking = new List<List<int>>();
        public List<List<float>> unitTypeLockingProgress = new List<List<float>>();

        public List<List<UnitPars>> unitsListByType = new List<List<UnitPars>>();

        [HideInInspector] public List<NationPars> nationPars = new List<NationPars>();

        [HideInInspector] public List<UnitPars> allUnits = new List<UnitPars>();
        public KDTree allUnitsKD;
        [HideInInspector] public float allUnitsKD_buildTime = 0f;

        [HideInInspector] public bool isMultiplayer = false;
        [HideInInspector] public RTSMultiplayer rtsCameraNetwork;

        public GameObject buildingFirePrefab;
        [HideInInspector] public bool useAStar = false;

        void Awake()
        {
            if (this.enabled)
            {
                active = this;

                numberOfUnitTypes = new List<List<int>>();
                numberOfUnitTypesPrev = new List<List<int>>();
                unitTypeLocking = new List<List<int>>();
                unitTypeLockingProgress = new List<List<float>>();

                for (int i = 0; i < rtsUnitTypePrefabs.Count; i++)
                {
                    unitsListByType.Add(new List<UnitPars>());
                }

                for (int i = 0; i < rtsUnitTypePrefabs.Count; i++)
                {
                    rtsUnitTypePrefabsUp.Add(rtsUnitTypePrefabs[i].GetComponent<UnitPars>());

                    UnitParsType upt = rtsUnitTypePrefabs[i].GetComponent<UnitParsType>();

                    if (upt != null)
                    {
                        upt.Initialize(i);
                    }

                    rtsUnitTypePrefabsUpt.Add(upt);
                }
            }
        }

        void Update()
        {
            float tCur = Time.time;

            if (tCur - allUnitsKD_buildTime > 1f)
            {
                if (allUnits.Count > 0)
                {
                    allUnitsKD = KDHelper.TreeFromUnitPars(allUnits);
                }
                else
                {
                    allUnitsKD = null;
                }

                allUnitsKD_buildTime = tCur;
            }
        }

        public UnitPars GetNearestUnit(Vector3 pos)
        {
            return KDHelper.FindNearestUP(pos, allUnits, allUnitsKD);
        }

        public void ExpandLists()
        {
            numberOfUnitTypes.Add(new List<int>());
            numberOfUnitTypesPrev.Add(new List<int>());
            unitTypeLocking.Add(new List<int>());
            unitTypeLockingProgress.Add(new List<float>());

            int i = numberOfUnitTypes.Count - 1;

            for (int j = 0; j < rtsUnitTypePrefabs.Count; j++)
            {
                numberOfUnitTypes[i].Add(0);
                numberOfUnitTypesPrev[i].Add(0);
                unitTypeLocking[i].Add(0);
                unitTypeLockingProgress[i].Add(0f);
            }
        }

        public void RemoveNation(string natName)
        {
            int natId = GetNationIdByName(natName);
            if (natId >= 0)
            {
                if (nationPars[natId] != null)
                {
                    numberOfUnitTypes.RemoveAt(natId);
                    numberOfUnitTypesPrev.RemoveAt(natId);
                    unitTypeLocking.RemoveAt(natId);
                    unitTypeLockingProgress.RemoveAt(natId);

                    GameObject go = nationPars[natId].gameObject;
                    nationPars.RemoveAt(natId);

                    Destroy(go);
                }
            }
        }

        public void DestroyUnit(UnitPars goPars)
        {
#if URTS_UNET
        if (goPars.gameObject.GetComponent<NetworkIdentity>() == null)
        {
            DestroyUnitInner(goPars);
        }
        else
        {
            if (rtsCameraNetwork != null)
            {
                rtsCameraNetwork.Cmd_DestroyUnit(goPars.gameObject);
            }
        }
#else
            DestroyUnitInner(goPars);
#endif
        }

        public void DestroyUnitInner(UnitPars goPars)
        {
            UnsetUnit(goPars);
            Destroy(goPars.gameObject);
        }

        public void UnsetUnit(UnitPars goPars)
        {
            if (goPars.thisUA != null)
            {
                UnitAnimation spL = goPars.thisUA;
                spL.UnsetSprite();

                if (PSpriteLoader.active != null)
                {
                    PSpriteLoader.active.RemoveAnimation(goPars.thisUA);
                }
            }

            BattleSystem.active.RemoveUnitFromBS(goPars);
            SelectionManager.active.DeselectObject(goPars);

            if ((goPars.nation < nationPars.Count) && (goPars.nation > -1))
            {
                nationPars[goPars.nation].nationAI.UnsetUnit(goPars);
                nationPars[goPars.nation].resourcesCollection.RemoveFromResourcesCollection(goPars);
                nationPars[goPars.nation].wandererAI.RemoveUnit(goPars);
            }

            bool uSubtract = false;

            if (unitsListByType[goPars.rtsUnitId].Contains(goPars))
            {
                uSubtract = true;
            }

            unitsListByType[goPars.rtsUnitId].Remove(goPars);
            UnitsMover.active.CompleteMovement(goPars);
            UnitsMover.active.RemoveCursedWalker(goPars);

            if ((goPars.nation < numberOfUnitTypes.Count) && (uSubtract))
            {
                UpdateNumberOfUnitTypes(goPars.nation, goPars.rtsUnitId);
            }

            UnitsGrouping.active.RemoveUnitFromGroup(goPars);
            Formations.active.RemoveUnitFromFormation(goPars);
            BuildingGrowSystem.active.RemoveFromSystem(goPars);
        }

        public void UpdateAllNumberOfUnitTypes()
        {
            for (int i = 0; i < numberOfUnitTypes.Count; i++)
            {
                for (int j = 0; j < numberOfUnitTypes[i].Count; j++)
                {
                    numberOfUnitTypes[i][j] = 0;
                }
            }

            for (int i = 0; i < allUnits.Count; i++)
            {
                if ((allUnits[i].nation > -1) && (allUnits[i].nation < numberOfUnitTypes.Count))
                {
                    if ((allUnits[i].rtsUnitId > -1) && (allUnits[i].rtsUnitId < numberOfUnitTypes[allUnits[i].nation].Count))
                    {
                        numberOfUnitTypes[allUnits[i].nation][allUnits[i].rtsUnitId] = numberOfUnitTypes[allUnits[i].nation][allUnits[i].rtsUnitId] + 1;
                    }
                }
            }
        }

        public void UpdateNumberOfUnitTypes(UnitPars up)
        {
            UpdateNumberOfUnitTypes(up.nation, up.rtsUnitId);
        }

        public void UpdateNumberOfUnitTypes(int nat, int rtsUnitId)
        {
            int n1 = 0;

            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i].nation == nat)
                {
                    if (allUnits[i].rtsUnitId == rtsUnitId)
                    {
                        n1 = n1 + 1;
                    }
                }
            }

            numberOfUnitTypes[nat][rtsUnitId] = n1;
        }

        public void StartCoroutines()
        {
            BattleSystem.active.lockUpdate = false;
            Economy.active.lockUpdate = false;
        }

        public void StopCoroutines()
        {
            BattleSystem.active.lockUpdate = true;
            Economy.active.lockUpdate = true;
        }

        public List<int> GetTypesCount(List<UnitPars> ups)
        {
            List<int> types = new List<int>();

            for (int i = 0; i < rtsUnitTypePrefabs.Count; i++)
            {
                types.Add(0);
            }

            for (int i = 0; i < ups.Count; i++)
            {
                int tp = ups[i].rtsUnitId;
                types[tp] = types[tp] + 1;
            }

            return types;
        }

        public static RTSMaster GetActive()
        {
            if (RTSMaster.active == null)
            {
                RTSMaster.active = UnityEngine.Object.FindObjectOfType<RTSMaster>();
            }

            return RTSMaster.active;
        }

        public void GetPlayerCameraNetwork()
        {
#if URTS_UNET
        RTSMultiplayer[] allObjects = Object.FindObjectsOfType<RTSMultiplayer>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            RTSMultiplayer obj = allObjects[i];
            if (obj.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
            {
                rtsCameraNetwork = obj;
            }
        }
#endif
        }

        public NationPars GetNationPars(string natName)
        {
            for (int i = 0; i < nationPars.Count; i++)
            {
                if (nationPars[i].GetNationName() == natName)
                {
                    return nationPars[i];
                }
            }

            return null;
        }

        public NationPars GetNationParsById(int natId)
        {
            if (natId > -1)
            {
                if (natId < nationPars.Count)
                {
                    return nationPars[natId];
                }
            }

            return null;
        }

        public int GetNationIdByName(string natName)
        {
            for (int i = 0; i < nationPars.Count; i++)
            {
                if (nationPars[i].GetNationName() == natName)
                {
                    return i;
                }
            }

            return -1;
        }

        public string GetNationNameById(int natId)
        {
            if (natId > -1)
            {
                if (natId < nationPars.Count)
                {
                    return nationPars[natId].GetNationName();
                }
            }

            return "";
        }

        public void SwitchPrefabsToUnityNavMesh()
        {
#if UNITY_EDITOR
            for (int i = 0; i < rtsUnitTypePrefabs.Count; i++)
            {
                AgentAstarUnitySwitcher.SwitchPrefabToUnityNavMesh(rtsUnitTypePrefabs[i]);
            }

            for (int i = 0; i < rtsUnitTypePrefabsNetwork.Count; i++)
            {
                AgentAstarUnitySwitcher.SwitchPrefabToUnityNavMesh(rtsUnitTypePrefabsNetwork[i]);
            }

            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SwitchPrefabsToAStar()
        {
#if UNITY_EDITOR
            for (int i = 0; i < rtsUnitTypePrefabs.Count; i++)
            {
                AgentAstarUnitySwitcher.SwitchPrefabToAStar(rtsUnitTypePrefabs[i]);
            }

            for (int i = 0; i < rtsUnitTypePrefabsNetwork.Count; i++)
            {
                AgentAstarUnitySwitcher.SwitchPrefabToAStar(rtsUnitTypePrefabsNetwork[i]);
            }

            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
