using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class Diplomacy : MonoBehaviour
    {
        public static Diplomacy active;

        [HideInInspector] public int numberNations = 0;
        [HideInInspector] public int playerNation = 0;

        public bool useWarNoticeWarning = true;

        public List<List<int>> relations = new List<List<int>>();
        RTSMaster rtsm;

        /////////// relation values and meanings //////////////////
        // relations[i,j] = -1; - undiscovered
        // relations[i,j] =  0; - peace
        // relations[i,j] =  1; - war
        // relations[i,j] =  2; - slavery
        // relations[i,j] =  3; - mastery
        // relations[i,j] =  4; - alliance
        ///////////////////////////////////////////////////////////

        ////////// unitPars statusBS values and meanings //////////
        // statusBS = 0; - is not on BS
        // statusBS = 1; - is on BS
        // statusBS = 2; - remove from BS
        // statusBS = 3; - set to BS
        ///////////////////////////////////////////////////////////

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        public void SaveTileNationsToMemory(Vector2i tile)
        {
            if (!SaveLoad.active.savedNationTileIndices.Contains(tile))
            {
                SaveLoad.active.SaveTerrainTileNations(tile);
                RemoveNationsFromTile(tile);
            }
        }

        public void LoadTileNationsFromMemory(Vector2i tile)
        {
            if (SaveLoad.active.savedNationTileIndices.Contains(tile))
            {
                if (GenerateTerrain.active.IsTileVisible(tile, Camera.main.transform.position))
                {
                    SaveLoad.active.LoadTerrainTileNations(tile);
                }
            }
        }

        public void SetAllPeace()
        {
            for (int i = 0; i < numberNations; i++)
            {
                for (int j = 0; j < numberNations; j++)
                {
                    if (i != j)
                    {
                        relations[i][j] = 0;
                    }
                }
            }
        }

        public void SetAllWar()
        {
            for (int i = 0; i < numberNations; i++)
            {
                for (int j = 0; j < numberNations; j++)
                {
                    if (i != j)
                    {
                        relations[i][j] = 1;
                    }
                }
            }
        }

        public void SetRelation(string firstNationName, string secondNationName, int relation)
        {
            int firstNation = GetNationIdFromName(firstNationName);
            int secondNation = GetNationIdFromName(secondNationName);

            if ((firstNation > -1) && (firstNation < relations.Count))
            {
                if ((secondNation > -1) && (secondNation < relations[firstNation].Count))
                {
                    if ((firstNation != secondNation) && (relation != relations[firstNation][secondNation]))
                    {
                        if (relation == 2)
                        {
                            SlavedNation(firstNation, secondNation);
                            relations[firstNation][secondNation] = 2;
                            relations[secondNation][firstNation] = 3;
                        }
                        else if (relation == 3)
                        {
                            SlavedNation(secondNation, firstNation);
                            relations[firstNation][secondNation] = 3;
                            relations[secondNation][firstNation] = 2;

                        }
                        else
                        {
                            LeaveSlaveryStraight(firstNation, secondNation);
                            relations[firstNation][secondNation] = relation;
                            relations[secondNation][firstNation] = relation;
                        }

                        if (rtsm.isMultiplayer)
                        {
                            RTSMaster.active.rtsCameraNetwork.Cmd_SetRelation(firstNationName, secondNationName, relation);
                        }
                    }
                }
            }
        }

        public void ResetUnitsBehaviour(int firstNation, int secondNation, int relation)
        {
            BattleSystem bs = BattleSystem.active;
            List<UnitPars> allUnits = bs.unitssUP;

            if (relation == 1)
            {
                for (int i = 0; i < allUnits.Count; i++)
                {
                    UnitPars goPars = allUnits[i];

                    if (goPars.unitParsType.isWorker == false)
                    {
                        if (goPars.isMovingMC == false)
                        {
                            if (goPars.nation == firstNation)
                            {
                                bs.ResetSearching(goPars);
                            }
                            else if (goPars.nation == secondNation)
                            {
                                bs.ResetSearching(goPars);
                            }
                        }
                    }
                }
            }
            else
            {
                NationPars np1 = RTSMaster.active.GetNationParsById(firstNation);
                if (np1 != null)
                {
                    np1.isWarWarningIssued = false;
                }

                NationPars np2 = RTSMaster.active.GetNationParsById(secondNation);
                if (np2 != null)
                {
                    np2.isWarWarningIssued = false;
                }

                for (int i = 0; i < allUnits.Count; i++)
                {
                    UnitPars goPars = allUnits[i];

                    if (goPars.unitParsType.isWorker == false)
                    {

                        if (goPars.isMovingMC == false)
                        {
                            // remove enemy targets on peace

                            if (goPars.nation == firstNation)
                            {
                                bs.ResetSearching(goPars);
                            }
                            else if (goPars.nation == secondNation)
                            {
                                bs.ResetSearching(goPars);
                            }
                        }

                        if (goPars.strictApproachMode == true)
                        {
                            UnitPars tgPars = goPars.targetUP;

                            if (tgPars != null)
                            {
                                if (tgPars.nation != goPars.nation)
                                {
                                    if (goPars.nation == firstNation)
                                    {
                                        goPars.strictApproachMode = false;
                                    }
                                    else if (goPars.nation == secondNation)
                                    {
                                        goPars.strictApproachMode = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void SlavedNation(int slave, int master)
        {
            for (int i = 0; i < numberNations; i++)
            {
                if ((i != slave) && (i != master))
                {
                    if (rtsm.nationPars[i].nationAI.masterNationId != master)
                    {
                        if ((relations[slave][i] == 1) || (relations[i][slave] == 1))
                        {
                            relations[master][i] = 1;
                            relations[i][master] = 1;
                        }
                    }
                }
            }

            LeaveSlavery(slave);
            rtsm.nationPars[slave].nationAI.masterNationId = master;
        }

        void LeaveSlavery(int slave)
        {
            int master = rtsm.nationPars[slave].nationAI.masterNationId;

            if (master > -1)
            {
                relations[slave][master] = 0;
                relations[master][slave] = 0;
            }
        }

        void LeaveSlaveryStraight(int slave, int master)
        {
            if (rtsm.nationPars[slave].nationAI.masterNationId == master)
            {
                relations[slave][master] = 0;
                relations[master][slave] = 0;
            }
        }

        public void AddNewNationCheckName(Vector3 centrePos, int newNation, string newNationName, int newNationIcon)
        {
            if (GetNationIdFromName(newNationName) == -1)
            {
                AddNewNation(centrePos, newNation, newNationName, newNationIcon);
            }
        }

        public void AddNewNation(Vector3 centrePos, int newNation, string newNationName, int newNationIcon)
        {
            AddNewNation(centrePos, newNation, newNationName, newNationIcon, 0);
        }

        public void AddNewNation(Vector3 centrePos, int newNation, string newNationName, int newNationIcon, int isPlayerNat)
        {
            if (RTSMaster.active.isMultiplayer == false)
            {
                GameObject nationCenter = new GameObject("NationCentre_" + newNationName);
                nationCenter.transform.position = centrePos;

                AddNationComponents(nationCenter);
                AddNationToRTSMLists(nationCenter);

                SetNationFromGameObject(nationCenter, newNationName, newNationIcon, isPlayerNat);
                if (isPlayerNat == 1)
                {
                    playerNation = numberNations - 1;
                }
            }
            else
            {
                if (RTSMaster.active.rtsCameraNetwork != null)
                {
                    RTSMaster.active.rtsCameraNetwork.Cmd_AddNation(NationSpawner.active.nationCenterNetworkPrefab, centrePos, newNationName, newNationIcon, isPlayerNat);
                }
            }
        }

        public void SetNationFromGameObject(GameObject nationCenter, string newNationName, int newNationIcon, int isPlayerNation)
        {
            SpawnPoint sp = nationCenter.GetComponent<SpawnPoint>();
            NationPars np = nationCenter.GetComponent<NationPars>();
            BattleAI bai = nationCenter.GetComponent<BattleAI>();
            NationAI nai = nationCenter.GetComponent<NationAI>();
            WandererAI wai = nationCenter.GetComponent<WandererAI>();

            sp.nation = numberNations;
            bai.nation = numberNations;

            wai.nation = numberNations;
            wai.SetLists();
            wai.ExpandLists();

            for (int j = 0; j < RTSMaster.active.nationPars.Count; j++)
            {
                RTSMaster.active.nationPars[j].wandererAI.ExpandLists();
            }

            nai.nation = numberNations;

            nai.FillLists();
            nai.ExpandLists();

            for (int j = 0; j < RTSMaster.active.nationPars.Count; j++)
            {
                RTSMaster.active.nationPars[j].nationAI.ExpandLists();
            }

            for (int j = 0; j < numberNations + 1; j++)
            {
                np.rNations.Add(0f);
                np.neighboursDistanceFrac.Add(0f);
            }

            for (int j = 0; j < RTSMaster.active.nationPars.Count; j++)
            {
                if (RTSMaster.active.isMultiplayer == false)
                {
                    RTSMaster.active.nationPars[j].ExpandLists();
                }

                RTSMaster.active.nationPars[j].rNations.Add(0f);
                RTSMaster.active.nationPars[j].neighboursDistanceFrac.Add(0f);
            }

            sp.model = RTSMaster.active.rtsUnitTypePrefabs[0];
            sp.numberOfObjects = 0;

            nai.nationName = newNationName;
            np.SetNationName(newNationName);
            np.nationIcon = newNationIcon;
            np.terrainTile = GenerateTerrain.active.GetChunkPosition(nationCenter.transform.position);

            ExpandRelationsList();

            Economy.active.AddNewNationRes();
            Scores.active.ExpandLists();
            RTSMaster.active.ExpandLists();

            NationListUI.active.SetNewNation(newNationIcon, newNationName);

            numberNations = numberNations + 1;

            if (RTSMaster.active.isMultiplayer == false)
            {
                EnableNationComponents(nationCenter, isPlayerNation);
            }
        }

        public void ExpandRelationsList()
        {
            relations.Add(new List<int> { -1 });

            for (int j = 0; j < numberNations + 1; j++)
            {
                relations[numberNations].Add(-1);
            }
            for (int j = 0; j < numberNations; j++)
            {
                relations[j].Add(-1);
            }
        }

        void AddNationComponents(GameObject nationCentre)
        {
            nationCentre.AddComponent<SpawnPoint>().enabled = false;
            nationCentre.AddComponent<ResourcesCollection>().enabled = false;
            nationCentre.AddComponent<NationAI>().enabled = false;
            nationCentre.AddComponent<NationPars>().enabled = false;
            nationCentre.AddComponent<BattleAI>().enabled = false;
            nationCentre.AddComponent<WandererAI>().enabled = false;

            nationCentre.GetComponent<NationPars>().SetAllNationComponents();
        }

        public void AddNationToRTSMLists(GameObject nationCentre)
        {
            RTSMaster.active.nationPars.Add(nationCentre.GetComponent<NationPars>());
        }

        public void EnableNationComponents(GameObject nationCentre, int isPlayerNation)
        {
            nationCentre.GetComponent<SpawnPoint>().enabled = true;
            nationCentre.GetComponent<ResourcesCollection>().enabled = true;
            nationCentre.GetComponent<NationPars>().enabled = true;
            nationCentre.GetComponent<BattleAI>().enabled = true;
            bool enabler = true;

            if (isPlayerNation == 1)
            {
                enabler = false;
            }

            nationCentre.GetComponent<NationAI>().enabled = enabler;
            nationCentre.GetComponent<WandererAI>().enabled = enabler;
        }

        public void RemoveNationsFromTile(Vector2i tile)
        {
            int in1 = numberNations;

            List<string> removals = new List<string>();

            for (int i = 0; i < in1; i++)
            {
                if (i != playerNation)
                {
                    if (i < numberNations)
                    {
                        if (Vector2i.IsEqual(rtsm.nationPars[i].terrainTile, tile))
                        {
                            string nat_name = GetNationNameFromId(i);
                            if (!removals.Contains(nat_name))
                            {
                                removals.Add(nat_name);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < removals.Count; i++)
            {
                RemoveNation(removals[i]);
            }

            RemoveNationUnitsOnTile(GetPlayerNationName(), tile);
        }

        public void RemoveNation(string natNameRem)
        {
            int natId = GetNationIdFromNameIndex(natNameRem);
            RemoveAllNationUnits(natNameRem);

            if ((natId > -1) && (natId < rtsm.nationPars.Count))
            {
                NationPars natPars = rtsm.nationPars[natId];

                if (natPars.nationAI != null)
                {
                    natPars.nationAI.StopAllCoroutines();
                    natPars.nationAI.enabled = false;
                }

                if (natPars != null)
                {
                    natPars.StopAllCoroutines();
                    natPars.enabled = false;
                }

                if (natPars.wandererAI != null)
                {
                    natPars.wandererAI.StopAllCoroutines();
                    natPars.wandererAI.enabled = false;
                }

                if (natPars.battleAI != null)
                {
                    natPars.battleAI.StopAllCoroutines();
                    natPars.battleAI.enabled = false;
                }

                if (natPars.resourcesCollection != null)
                {
                    natPars.resourcesCollection.StopAllCoroutinesHere();
                    natPars.resourcesCollection.enabled = false;
                }

                if (natPars.spawnPoint != null)
                {
                    natPars.spawnPoint.enabled = false;
                }

                if (natId < numberNations)
                {
                    for (int i = (natId + 1); i < numberNations; i++)
                    {
                        rtsm.nationPars[i].nation = rtsm.nationPars[i].nation - 1;
                        rtsm.nationPars[i].nationAI.nation = rtsm.nationPars[i].nationAI.nation - 1;
                        rtsm.nationPars[i].wandererAI.nation = rtsm.nationPars[i].wandererAI.nation - 1;
                        rtsm.nationPars[i].battleAI.nation = rtsm.nationPars[i].battleAI.nation - 1;
                        SpawnPoint sp = rtsm.nationPars[i].spawnPoint;
                        sp.nation = sp.nation - 1;
                    }

                    for (int i = 0; i < rtsm.allUnits.Count; i++)
                    {
                        UnitPars up = rtsm.allUnits[i];
                        if (up.nation > natId)
                        {
                            up.nation = up.nation - 1;
                            if (up.thisSpawn != null)
                            {
                                up.thisSpawn.nation = up.thisSpawn.nation - 1;
                            }
                        }
                    }

                    for (int i = 0; i < BattleSystem.active.sinks.Count; i++)
                    {
                        UnitPars up = BattleSystem.active.sinks[i];
                        if (up.nation > natId)
                        {
                            up.nation = up.nation - 1;
                            if (up.thisSpawn != null)
                            {
                                up.thisSpawn.nation = up.thisSpawn.nation - 1;
                            }
                        }
                    }
                }

                for (int i = 0; i < rtsm.nationPars.Count; i++)
                {
                    if (i != natId)
                    {
                        rtsm.nationPars[i].RemoveNation(natId);
                        rtsm.nationPars[i].nationAI.RemoveNation(natId);
                        rtsm.nationPars[i].wandererAI.RemoveNation(natId);
                        rtsm.nationPars[i].wandererAI.ReturnFromTargetImmediatelly(natId);
                    }
                }

                for (int i = 0; i < rtsm.nationPars.Count; i++)
                {
                    if (i != natId)
                    {
                        if (i < relations.Count)
                        {
                            if ((natId > -1) && (natId < relations[i].Count))
                            {
                                relations[i].RemoveAt(natId);
                            }
                        }
                    }
                }

                if ((natId > -1) && (natId < relations.Count))
                {
                    relations.RemoveAt(natId);
                }

                Economy.active.RemoveNation(natId);
                Scores.active.RemoveNation(natId);
                rtsm.RemoveNation(natNameRem);
                NationListUI.active.RemoveNation(natNameRem);

                numberNations = numberNations - 1;
            }
            else
            {
                Debug.Log("Nation not removed " + natNameRem + " " + natId + " " + rtsm.nationPars.Count);
            }
        }

        public void RemoveAllNationUnits(string natName)
        {
            List<UnitPars> unitsToRemove = new List<UnitPars>();
            UnitPars[] allUnits_1 = UnityEngine.Object.FindObjectsOfType<UnitPars>();

            for (int i = 0; i < allUnits_1.Length; i++)
            {
                UnitPars up = allUnits_1[i];

                if (up.nationName == natName)
                {
                    unitsToRemove.Add(up);
                }
            }

            for (int i = 0; i < unitsToRemove.Count; i++)
            {
                rtsm.DestroyUnit(unitsToRemove[i]);
            }
        }

        void RemoveNationUnitsOnTile(string natName, Vector2i tile)
        {
            int natId = GetNationIdFromName(natName);
            List<UnitPars> unitsToRemove = new List<UnitPars>();

            for (int i = 0; i < rtsm.allUnits.Count; i++)
            {
                UnitPars up = rtsm.allUnits[i];

                if (up.nation == natId)
                {
                    if (Vector2i.IsEqual(GenerateTerrain.active.GetChunkPosition(up.transform.position), tile))
                    {
                        unitsToRemove.Add(up);
                    }
                }
            }

            int n = unitsToRemove.Count;

            for (int i = 0; i < n; i++)
            {
                UnitPars up = unitsToRemove[0];
                unitsToRemove.Remove(up);
                rtsm.DestroyUnit(up);
            }
        }

        public int GetNationIdFromName(string natName)
        {
            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                if (RTSMaster.active.nationPars[i].GetNationName() == natName)
                {
                    return RTSMaster.active.nationPars[i].GetNationId();
                }
            }

            return -1;
        }

        public int GetNationIdFromNameIndex(string natName)
        {
            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                if (GetNationNameFromId(i) == natName)
                {
                    return i;
                }
            }

            return -1;
        }

        public string GetNationNameFromId(int id)
        {
            return GetNationNameFromId(id, false);
        }

        public string GetNationNameFromId(int id, bool forceSearchMultiplayer)
        {
            if (RTSMaster.active.nationPars[id] != null)
            {
                return RTSMaster.active.nationPars[id].GetNationName(forceSearchMultiplayer);
            }

            return "";
        }

        public string GetPlayerNationName()
        {
            if (Diplomacy.active.playerNation < 0)
            {
                return "";
            }
            else if (Diplomacy.active.playerNation >= RTSMaster.active.nationPars.Count)
            {
                return "";
            }

            return RTSMaster.active.nationPars[Diplomacy.active.playerNation].GetNationName();
        }

        public static List<string> GetNationsDiscoveredByPlayerNames()
        {
            List<string> nats = new List<string>();

            for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
            {
                NationPars np = RTSMaster.active.nationPars[i];
                string nname = np.GetNationName();
                int nid = np.GetNationId();

                if (nname != Diplomacy.active.GetPlayerNationName())
                {
                    if (Diplomacy.active.relations[Diplomacy.active.playerNation][nid] > -1)
                    {
                        nats.Add(nname);
                    }
                }
            }

            return nats;
        }
    }
}
