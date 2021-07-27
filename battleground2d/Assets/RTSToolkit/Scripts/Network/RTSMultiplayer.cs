using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class RTSMultiplayer
#if URTS_UNET
    : NetworkBehaviour 
#else
    : MonoBehaviour
#endif
    {
        public static List<RTSMultiplayer> allRTSMultiplayers = new List<RTSMultiplayer>();

        public RTSMaster rtsm;

        [HideInInspector] public int isHost = -1;
        [HideInInspector] public List<float> healths = new List<float>();

#if URTS_UNET
	    [SyncVar]
#endif
        public int clientId = -1;
        Transform cameraTransform;

        void Start()
        {
#if URTS_UNET
            if (allRTSMultiplayers == null)
            {
                allRTSMultiplayers = new List<RTSMultiplayer>();
            }

            if (allRTSMultiplayers != null)
            {
                allRTSMultiplayers.Add(this);
            }

            if (isLocalPlayer)
            {

                SetRTSMasterMultiplayerFlag();

                IsHost();
                SetNation();
                DisableAIs();

                AddMultiplayerComponents();
                cameraTransform = Camera.main.transform;

            }

            if (isServer)
            {
                rtsm = RTSMaster.active;
            }
#endif
        }

        void OnDestroy()
        {
            if (allRTSMultiplayers != null)
            {
                allRTSMultiplayers.Remove(this);
            }
        }

        void IsHost()
        {
            RTSMultiplayer[] allObjects = Object.FindObjectsOfType<RTSMultiplayer>();

            if (allObjects.Length == 1)
            {
                isHost = 1;
            }

            if (allObjects.Length > 1)
            {
                isHost = 0;
            }

            clientId = allObjects.Length;
        }

        void SetRTSMasterMultiplayerFlag()
        {
            rtsm = RTSMaster.active;

            if (rtsm.isMultiplayer == false)
            {
                rtsm.isMultiplayer = true;
                rtsm.rtsCameraNetwork = this;
            }

            rtsm.isMultiplayer = true;
        }

        void SetNation()
        {
#if URTS_UNET
            if (isHost == 1)
            {
                SaveLoad.active.UnloadEverything();
                NationSpawner.active.ResetSpawned();
                NationSpawner.active.Spawn();
            }

            if (isHost == 0)
            {
                GameOver.active.runUpdate = false;

                NationPars[] allObjects = Object.FindObjectsOfType<NationPars>();
                int nnat = 0;

                for (int i = 0; i < allObjects.Length; i++)
                {
                    NationPars obj = allObjects[i];
                    if (obj.gameObject.GetComponent<NetworkIdentity>() != null)
                    {
                        nnat = nnat + 1;
                    }
                }

                Diplomacy.active.numberNations = nnat;

                nnat = 0;
                int nnat2 = 0;

                for (int i = 0; i < allObjects.Length; i++)
                {
                    NationPars obj = allObjects[i];
                    nnat = nnat + 1;

                    if (obj.gameObject.GetComponent<NetworkIdentity>() != null)
                    {
                        Diplomacy.active.ExpandRelationsList2();
                        NationListUI.active.SetNewNation(nnat2, NationSpawner.active.nations[nnat2].name);

                        nnat2++;
                        obj.SetAllNationComponents();
                        Diplomacy.active.AddNationToRTSMLists(obj.gameObject);
                        Economy.active.AddNewNationRes();

                        for (int j = 0; j < rtsm.nationPars.Count; j++)
                        {
                            rtsm.nationPars[j].ExpandLists();
                        }

                        rtsm.ExpandLists();
                        Scores.active.ExpandLists();
                    }
                }

                int nn11 = Diplomacy.active.numberNations;

                if (nn11 >= NationSpawner.active.nations.Count)
                {
                    nn11 = NationSpawner.active.nations.Count - 1;
                }

                Diplomacy.active.AddNewNation(
                    NationSpawner.active.nations[nn11].position,
                    Diplomacy.active.numberNations,
                    NationSpawner.active.nations[nn11].name,
                    Diplomacy.active.numberNations,
                    1
                );
            }
#endif
        }

        void DisableAIs()
        {
            if (isHost == 0)
            {
                for (int i = 0; i < rtsm.nationPars.Count; i++)
                {
                    if (rtsm.nationPars[i].nationAI != null)
                    {
                        rtsm.nationPars[i].nationAI.enabled = false;
                    }

                    if (i != Diplomacy.active.playerNation)
                    {
                        if (rtsm.nationPars[i].battleAI != null)
                        {
                            rtsm.nationPars[i].battleAI.enabled = false;
                        }
                    }

                    if (rtsm.nationPars[i].wandererAI != null)
                    {
                        rtsm.nationPars[i].wandererAI.enabled = false;
                    }
                }
            }
        }

        void AddMultiplayerComponents()
        {
#if URTS_UNET
            UnitPars[] allObjects = Object.FindObjectsOfType<UnitPars>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                UnitPars up = allObjects[i];

                if (up.gameObject.GetComponent<NetworkIdentity>() == null)
                {
                    if (isHost == 1)
                    {
                        RespawnFromNetworkPrefabs(up);
                    }

                    rtsm.DestroyUnit(up);
                }
                else
                {
                    if (!rtsm.allUnits.Contains(up))
                    {
                        if (isHost == 1)
                        {
                            SpawnPoint.SetParsImmediatelly(up.gameObject, Diplomacy.active.GetNationNameFromId(up.nation));
                        }
                    }
                }
            }
#endif
        }

        public void RespawnFromNetworkPrefabs(UnitPars up)
        {
            int rtsid = up.rtsUnitId;
            AddNetworkComponent(rtsid, up.transform.position, up.transform.rotation, up.nationName, 0);
        }

#if URTS_UNET
	    [ClientCallback]
#endif
        public void AddNetworkComponent(int rtsid, Vector3 vect3, Quaternion qt, string nationName, int auth)
        {
            // authorities
            // 0 server authority
            // 1 client authority
#if URTS_UNET
            if(isLocalPlayer)
            {
                GameObject player = this.gameObject;
                Cmd_AddNetworkComponent(rtsid, vect3, qt, nationName, player, auth, clientId);
                
            }
#endif
        }

        public static int n_Cmd_AddNation = 0;

#if URTS_UNET
	    [Command]
#endif
        public void Cmd_AddNation(GameObject nationPrefab, Vector3 centrePos, string newNationName, int newNationIcon, int isPlayerNat)
        {
#if URTS_UNET
            n_Cmd_AddNation++;

            GameObject pref1 = NationSpawner.active.nationCenterNetworkPrefab;
            GameObject go1 = (GameObject)Instantiate(pref1, centrePos, Quaternion.identity);
            go1.name = "NationCentre_" + newNationName;

            NetworkServer.SpawnWithClientAuthority(go1, this.connectionToClient);

            if (isPlayerNat == 1)
            {
                go1.GetComponent<NationCentreNetworkNode>().isPlayer = true;
            }

            go1.GetComponent<NationCentreNetworkNode>().nationName = newNationName;

            Rpc_AddNation(go1, newNationName, newNationIcon, isPlayerNat);
#endif
        }

        public static int n_Rpc_AddNation = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_AddNation(GameObject spawnedNation, string newNationName, int newNationIcon, int isPlayerNat)
        {
#if URTS_UNET
            n_Rpc_AddNation++;

            if (isPlayerNat == 1)
            {
                spawnedNation.GetComponent<NationCentreNetworkNode>().isPlayer = true;
            }

            spawnedNation.GetComponent<NationCentreNetworkNode>().nationName = newNationName;
            NationPars np = spawnedNation.GetComponent<NationPars>();

            np.SetAllNationComponents();
            Diplomacy.active.AddNationToRTSMLists(spawnedNation);
            Diplomacy.active.SetNationFromGameObject(spawnedNation, newNationName, newNationIcon, isPlayerNat);

            for (int i = 0; i < Diplomacy.active.numberNations; i++)
            {
                if (i < RTSMaster.active.nationPars.Count)
                {
                    RTSMaster.active.nationPars[i].ExpandLists();
                }
            }

            if (!this.hasAuthority)
            {

            }
            else
            {
                if (isPlayerNat == 1)
                {
                    Diplomacy.active.playerNation = Diplomacy.active.numberNations - 1;
                    Economy.active.RefreshResources();
                    GameOver.active.runUpdate = true;
                }
                else
                {
                    if (NationSpawner.active != null)
                    {
                        for (int i = 0; i < NationSpawner.active.nations.Count; i++)
                        {
                            NationSpawnerUnit nsu = NationSpawner.active.nations[i];

                            if (nsu.name == newNationName)
                            {
                                if (nsu.isWizzardNation)
                                {
                                    np.isWizzardNation = true;

                                    if (NationListUI.active != null)
                                    {
                                        NationListUI.active.UpdateAsWizzardNation(nsu.name);
                                    }
                                }
                            }
                        }
                    }
                }

                Diplomacy.active.EnableNationComponents(spawnedNation, isPlayerNat);
            }

            RTSMultiplayer[] allObjects = Object.FindObjectsOfType<RTSMultiplayer>();

            if (allObjects.Length > 1)
            {
                bool isAdded = false;

                for (int i = 0; i < allObjects.Length; i++)
                {
                    RTSMultiplayer obj = allObjects[i];

                    if (obj.hasAuthority)
                    {
                        if (obj.isHost == 0)
                        {
                            if (isAdded == false)
                            {
                                for (int j = 0; j < (Diplomacy.active.numberNations - 1); j++)
                                {
                                    for (int k = 0; k < Diplomacy.active.numberNations; k++)
                                    {
                                        rtsm.nationPars[k].nationAI.ExpandLists();
                                    }
                                }

                                isAdded = true;
                            }
                        }
                    }
                }
            }
#endif
        }

        public static int n_Cmd_AddNetworkComponent = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_AddNetworkComponent(int rtsid, Vector3 v3a, Quaternion qta, string nationName, GameObject player, int auth, int clId)
        {
#if URTS_UNET
            n_Cmd_AddNetworkComponent++;

            GameObject go1 = (GameObject)Instantiate(rtsm.rtsUnitTypePrefabsNetwork[rtsid], v3a, qta);

            NetworkUnique nu = go1.GetComponent<NetworkUnique>();
            nu.nationName = nationName;

            NetworkUnique[] allObjects = Object.FindObjectsOfType<NetworkUnique>();
            int iAvail = 0;

            for (int i = 0; i < allObjects.Length; i++)
            {
                NetworkUnique obj = allObjects[i];

                if (obj.id > iAvail)
                {
                    iAvail = obj.id;
                }
            }

            iAvail = iAvail + 1;
            nu.id = iAvail;

            if (auth == 0)
            {
                NetworkServer.Spawn(go1);
            }
            else if (auth == 1)
            {
                NetworkServer.SpawnWithClientAuthority(go1, player);
                nu.player = player.gameObject;
            }

            nu.id = iAvail;
            nu.clientId = clId;
#endif
        }

        void Update()
        {
#if URTS_UNET
            if (isLocalPlayer)
            {
                if (cameraTransform != null)
                {
                    transform.position = cameraTransform.position;
                    transform.rotation = cameraTransform.rotation;
                }
            }
#endif
        }

        public static int n_Cmd_UpdateUnitHealth = 0;
        public static int n_Cmd_BytesUpdateUnitHealth = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_UpdateUnitHealth(GameObject go, float health)
        {
#if URTS_UNET
            n_Cmd_UpdateUnitHealth++;
            if (go != null)
            {
                if (go.GetComponent<NetworkUnique>() != null)
                {
                    NetworkUnique obj = go.GetComponent<NetworkUnique>();
                    UnitPars up = go.GetComponent<UnitPars>();

                    float health2 = health;
                    if (health2 > up.maxHealth)
                    {
                        health2 = up.maxHealth;
                    }

                    obj.health = health2;
                }
            }
#endif
        }

        public static int n_Cmd_AddUnitHealth = 0;
        public static int n_Cmd_PlayAnimation = 0;

#if URTS_UNET
	    [Command]
#endif
        public void Cmd_PlayAnimation(GameObject go, string animationName)
        {
#if URTS_UNET
            n_Cmd_PlayAnimation++;
            Rpc_PlayAnimation(go, animationName);
#endif
        }

        public static int n_Rpc_PlayAnimation = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_PlayAnimation(GameObject go, string animationName)
        {
#if URTS_UNET
            n_Rpc_PlayAnimation++;
            if (go != null)
            {
                UnitAnimation sl = go.GetComponent<UnitAnimation>();
                UnitPars up = go.GetComponent<UnitPars>();

                if (sl.animName != "Death")
                {
                    if (up.health > 0)
                    {
                        if ((sl.animName != "Attack") && (animationName == "Attack"))
                        {
                            up.StartMultiplayerAttackAnimation();
                        }
                        else if (animationName != "Attack")
                        {
                            up.StopMultiplayerAttackAnimation();
                        }
                    }

                    sl.PlayAnimationCheckInner(animationName);
                }
            }
#endif
        }

        public static int n_Cmd_DestroyUnit = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_DestroyUnit(GameObject go)
        {
#if URTS_UNET
            n_Cmd_DestroyUnit++;
            Rpc_DestroyUnit(go);
#endif
        }

        public static int n_Rpc_DestroyUnit = 0;
#if URTS_UNET
	    [ClientRpc]
#endif

        public void Rpc_DestroyUnit(GameObject go)
        {
#if URTS_UNET
            n_Rpc_DestroyUnit++;

            if (rtsm == null)
            {
                rtsm = RTSMaster.active;
            }

            if (go != null)
            {
                rtsm.DestroyUnitInner(go.GetComponent<UnitPars>());
            }
#endif
        }

        public static int n_Cmd_RemoveNation = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_RemoveNation(string nationName)
        {
#if URTS_UNET
            n_Cmd_RemoveNation++;
            Rpc_RemoveNation(nationName);
#endif
        }

        public static int n_Rpc_RemoveNation = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_RemoveNation(string nationName)
        {
#if URTS_UNET
            n_Rpc_RemoveNation++;
            Diplomacy.active.RemoveNation(nationName);
#endif
        }

        public static int n_Cmd_FinishBuilding = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_FinishBuilding(GameObject go)
        {
#if URTS_UNET
            n_Cmd_FinishBuilding++;
            go.GetComponent<NetworkUnique>().isBuildFinished = true;
#endif
        }

        public static int n_Cmd_IsPlayer = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_IsPlayer(GameObject go)
        {
#if URTS_UNET
            n_Cmd_IsPlayer++;
            go.GetComponent<NetworkUnique>().isPlayer = true;
#endif
        }

        public static int n_Cmd_RemoveAttackers = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_RemoveAttackers(GameObject go)
        {
#if URTS_UNET
            n_Cmd_RemoveAttackers++;
            Rpc_RemoveAttackers(go);
#endif
        }

        public static int n_Rpc_RemoveAttackers = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_RemoveAttackers(GameObject go)
        {
#if URTS_UNET
            n_Rpc_RemoveAttackers++;

            if (go != null)
            {
                UnitPars up = go.GetComponent<UnitPars>();

                if (up != null)
                {
                    up.CleanAttackersInner();
                }
            }
#endif
        }

        public static int n_Cmd_AdjustAgent = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_AdjustAgent(GameObject go, string nationName)
        {
#if URTS_UNET
            n_Cmd_AdjustAgent++;
            Rpc_AdjustAgent(go,nationName);
#endif
        }

        public static int n_Rpc_AdjustAgent = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_AdjustAgent(GameObject go, string nationName)
        {
#if URTS_UNET
            n_Rpc_AdjustAgent++;

            if (go.GetComponent<AgentPars>() != null)
            {
                if (string.IsNullOrEmpty(nationName) == false)
                {
                    NationPars np = RTSMaster.active.GetNationPars(nationName);
                    NationCentreNetworkNode ncn = np.gameObject.GetComponent<NationCentreNetworkNode>();

                    if (RTSMaster.active.rtsCameraNetwork.isHost == 1)
                    {
                        if (nationName != Diplomacy.active.GetPlayerNationName())
                        {
                            if (ncn.isPlayer == true)
                            {
                                go.GetComponent<AgentPars>().RemoveFromManualRVOs();
                                go.GetComponent<AgentPars>().enabled = false;
                            }
                        }
                    }
                    else if (RTSMaster.active.rtsCameraNetwork.isHost == 0)
                    {
                        if (nationName != Diplomacy.active.GetPlayerNationName())
                        {
                            go.GetComponent<AgentPars>().RemoveFromManualRVOs();
                            go.GetComponent<AgentPars>().enabled = false;
                        }
                    }
                }
            }
#endif
        }

        public static bool BelongsToComputer(GameObject go, string nationName)
        {
#if URTS_UNET
            if (RTSMaster.active.isMultiplayer == false)
            {
                return true;
            }
            if (string.IsNullOrEmpty(nationName) == false)
            {
                NationPars np = RTSMaster.active.GetNationPars(nationName);
                if (np == null)
                {
                    Debug.Log("np == null " + nationName + " " + RTSMaster.active.nationPars.Count);

                    for (int i = 0; i < RTSMaster.active.nationPars.Count; i++)
                    {
                        Debug.Log(i + " " + RTSMaster.active.nationPars[i].GetNationName());
                    }
                }

                NationCentreNetworkNode ncn = np.gameObject.GetComponent<NationCentreNetworkNode>();

                if (RTSMaster.active.rtsCameraNetwork.isHost == 1)
                {
                    if (nationName != Diplomacy.active.GetPlayerNationName())
                    {
                        if (ncn.isPlayer == true)
                        {
                            return false;
                        }
                    }
                }
                else if (RTSMaster.active.rtsCameraNetwork.isHost == 0)
                {
                    if (nationName != Diplomacy.active.GetPlayerNationName())
                    {
                        return false;
                    }
                }
            }
#endif
            return true;
        }

        public static int n_Cmd_SetRelation = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_SetRelation(string firstNation, string secondNation, int relation)
        {
#if URTS_UNET
            n_Cmd_SetRelation++;
            Diplomacy.active.SetRelation(firstNation, secondNation, relation);
            Rpc_SetRelation(firstNation, secondNation, relation);
#endif
        }

        public static int n_Rpc_SetRelation = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_SetRelation(string firstNation, string secondNation, int relation)
        {
#if URTS_UNET
            n_Rpc_SetRelation++;
            Diplomacy.active.SetRelation(firstNation,secondNation,relation);
#endif
        }

        public static int n_Cmd_SetNationName = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_SetNationName(GameObject go, string natName)
        {
#if URTS_UNET
            n_Cmd_SetNationName++;
            go.GetComponent<NationCentreNetworkNode>().nationName = natName;
#endif
        }

        public static int n_Cmd_SendNationMessage = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_SendNationMessage(string sendingNationName, string receivingNationName, string messageKey)
        {
#if URTS_UNET
            n_Cmd_SendNationMessage++;
            Rpc_SendNationMessage(sendingNationName, receivingNationName, messageKey);
#endif
        }

        public static int n_Rpc_SendNationMessage = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_SendNationMessage(string sendingNationName, string receivingNationName, string messageKey)
        {
#if URTS_UNET
            n_Rpc_SendNationMessage++;

            if (rtsm == null)
            {
                rtsm = RTSMaster.active;
            }
            if (receivingNationName == Diplomacy.active.GetPlayerNationName())
            {
                DiplomacyReportsUI.active.MakeProposal(sendingNationName, messageKey);
            }
#endif
        }


        public static int n_Cmd_LaunchArrow = 0;
#if URTS_UNET
	    [Command]
#endif
        public void Cmd_LaunchArrow(GameObject attacker, GameObject target, Vector3 launchPoint, string playerNationOrig)
        {
#if URTS_UNET
            n_Cmd_LaunchArrow++;
            Rpc_LaunchArrow(attacker,target,launchPoint,playerNationOrig);
#endif
        }

        public static int n_Rpc_LaunchArrow = 0;
#if URTS_UNET
	    [ClientRpc]
#endif
        public void Rpc_LaunchArrow(GameObject attacker, GameObject target, Vector3 launchPoint, string playerNationOrig)
        {
#if URTS_UNET
            n_Rpc_LaunchArrow++;

            if (attacker != null)
            {
                if (target != null)
                {
                    if (rtsm == null)
                    {
                        rtsm = RTSMaster.active;
                    }

                    if (playerNationOrig != Diplomacy.active.GetPlayerNationName())
                    {
                        BattleSystem.active.LaunchArrowInner(attacker.GetComponent<UnitPars>(), target.GetComponent<UnitPars>(), launchPoint, true);
                    }
                }
            }
#endif
        }

#if URTS_UNET
        public override float GetNetworkSendInterval(){
            return 0.9f;
        }
#endif
    }
}
