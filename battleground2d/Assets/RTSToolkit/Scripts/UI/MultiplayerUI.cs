using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class MultiplayerUI : MonoBehaviour
    {
        public static MultiplayerUI active;

        public GameObject notConnected;
        public GameObject lanHost;
        public GameObject lanClient;
        public GameObject lanServerOnly;
        public GameObject matchMaker;
        public GameObject findInternetMatchMenu;

        public InputField lanIPToRead;
        public InputField lanPortToRead;

        public Text lanHostServerPortText;
        public Text lanHostClientAddressText;
        public Text lanHostClientPortText;

        public Text lanClientClientAddressText;
        public Text lanClientClientPortText;

        public Text lanServerServerPortText;

        public GameObject changeMatchMakerInner;
        public Text mmUrlText;

        public InputField matchMakerRoomName;

        public GameObject findMatchGridNode;
        List<GameObject> activeFindMatchGridNodes = new List<GameObject>();

        RTSNetworkManager rtsNM;
        RTSMaster rtsm;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsNM = RTSNetworkManager.GetActive();
            rtsm = RTSMaster.active;
        }

        void Update()
        {

        }

        public void StartHost()
        {
#if URTS_UNET
            rtsNM.phase = 1;

            if (lanIPToRead != null)
            {
                rtsNM.networkAddress = lanIPToRead.text;
            }

            if (lanPortToRead != null)
            {
                int port1 = int.Parse(lanPortToRead.text);
                rtsNM.networkPort = port1;
            }

            rtsNM.StartHost();
#endif
        }

        public void StartClient()
        {
#if URTS_UNET
            rtsNM.phase = 2;
            SaveLoad.active.UnloadEverything();

            if (lanIPToRead != null)
            {
                rtsNM.networkAddress = lanIPToRead.text;
            }

            if (lanPortToRead != null)
            {
                int port1 = int.Parse(lanPortToRead.text);
                rtsNM.networkPort = port1;
            }

            rtsNM.StartClient();
#endif
        }

        public void StartServerOnly()
        {
#if URTS_UNET
            rtsNM.phase = 3;

            if (lanIPToRead != null)
            {
                rtsNM.networkAddress = lanIPToRead.text;
            }

            if (lanPortToRead != null)
            {
                rtsNM.networkPort = int.Parse(lanPortToRead.text);
            }

            rtsNM.StartServer();
#endif
        }

        public void OpenMatchMaker()
        {
#if URTS_UNET
            DisableAll();
            matchMaker.SetActive(true);
            rtsNM.StartMatchMaker();
#endif
        }

        public void CreateInternetMatch()
        {
#if URTS_UNET
            rtsNM.phase = 1;
            rtsNM.matchMaker.CreateMatch(rtsNM.matchName, rtsNM.matchSize, true, "", "", "", 0, 0, rtsNM.OnMatchCreate);
#endif
        }

        public void BackToNotConnected()
        {
#if URTS_UNET
            DisableAll();
            notConnected.SetActive(true);
#endif
        }

        public void DisableMatchMaker()
        {
#if URTS_UNET
            rtsNM.StopMatchMaker();
            BackToNotConnected();
#endif
        }

        public void ChangeRoomName()
        {
#if URTS_UNET
		    rtsNM.matchName = matchMakerRoomName.text;
#endif
        }

        public void SwitchMMServerMode()
        {
#if URTS_UNET
		    SwitchMMServerModeInner(!changeMatchMakerInner.activeSelf);
#endif
        }

        void SwitchMMServerModeInner(bool setOn)
        {
#if URTS_UNET
		    mmUrlText.gameObject.SetActive(!setOn);
		    changeMatchMakerInner.SetActive(setOn);
#endif
        }

        public void SwitchMMToLocal()
        {
#if URTS_UNET
            rtsNM.SetMatchHost("localhost", 1337, false);
            mmUrlText.text = (rtsNM.matchMaker.baseUri).ToString();
            SwitchMMServerModeInner(false);
#endif
        }

        public void SwitchMMToInternet()
        {
#if URTS_UNET
            rtsNM.SetMatchHost("mm.unet.unity3d.com", 443, true);
            mmUrlText.text = (rtsNM.matchMaker.baseUri).ToString();
            SwitchMMServerModeInner(false);
#endif
        }

        public void SwitchMMToStaging()
        {
#if URTS_UNET
            rtsNM.SetMatchHost("staging-mm.unet.unity3d.com", 443, true);
            mmUrlText.text = (rtsNM.matchMaker.baseUri).ToString();
            SwitchMMServerModeInner(false);
#endif
        }

        public void FindInternetMatch()
        {
#if URTS_UNET
		    rtsNM.matchMaker.ListMatches(0, 20, "", false, 0, 0, rtsNM.OnMatchList);	
#endif
        }

        // only triggered from OnMatchList()
        public void FindInternetMatchInner()
        {
#if URTS_UNET
            DisableAll();
            findInternetMatchMenu.SetActive(true);
            Transform goPar = findMatchGridNode.transform.parent;

            foreach (var match in rtsNM.matches)
            {
                GameObject go = (GameObject)Instantiate(findMatchGridNode);
                go.transform.SetParent(goPar);
                go.SetActive(true);
                activeFindMatchGridNodes.Add(go);

                FindInternetMatchNode node = go.GetComponent<FindInternetMatchNode>();
                node.match = match;
                node.text.text = "Join Match:" + match.name;
            }
#endif
        }

        public void HostDisconnect()
        {
#if URTS_UNET
            rtsm.StopCoroutines();
            StartCoroutine(DisconnectUnloadChecker(1));
#endif
        }

        public void ClientDisconnect()
        {
#if URTS_UNET
		    rtsNM.StopClient();
#endif
        }

        public void ServerDisconnect()
        {
#if URTS_UNET
		    rtsNM.StopServer();
#endif
        }

        public void DisableAll()
        {
#if URTS_UNET
            notConnected.SetActive(false);
            lanHost.SetActive(false);
            lanClient.SetActive(false);
            lanServerOnly.SetActive(false);
            matchMaker.SetActive(false);
            findInternetMatchMenu.SetActive(false);

            for (int i = 0; i < activeFindMatchGridNodes.Count; i++)
            {
                Destroy(activeFindMatchGridNodes[i]);
            }

            activeFindMatchGridNodes.Clear();
#endif
        }

        [HideInInspector] public byte[] savedPlayerUnits = null;
        public IEnumerator DisconnectUnloadChecker(int mod)
        {
#if URTS_UNET
            int n = rtsm.allUnits.Count;

            if (mod == 1)
            {
                List<UnitPars> removals = new List<UnitPars>();

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    removals.Add(rtsm.allUnits[i]);
                }

                for (int i = 0; i < removals.Count; i++)
                {
                    rtsm.DestroyUnit(removals[i]);
                }

                removals.Clear();
            }
            else if (mod == 2)
            {
                savedPlayerUnits = SaveLoad.active.SavePlayerNationUnitsToBytes();
                GameOver.active.runUpdate = false;
                List<UnitPars> removals = new List<UnitPars>();

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    if (rtsm.allUnits[i].nation == Diplomacy.active.playerNation)
                    {
                        removals.Add(rtsm.allUnits[i]);
                    }
                }

                for (int i = 0; i < removals.Count; i++)
                {
                    rtsm.DestroyUnit(removals[i]);
                }

                removals.Clear();

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    removals.Add(rtsm.allUnits[i]);
                }

                for (int i = 0; i < removals.Count; i++)
                {
                    rtsm.DestroyUnitInner(removals[i]);
                }

                removals.Clear();
            }

            n = rtsm.allUnits.Count;

            int n1 = n;

            while (n1 > 0)
            {
                yield return new WaitForSeconds(0.2f);
                int n2 = 0;

                for (int i = 0; i < rtsm.allUnits.Count; i++)
                {
                    if (rtsm.allUnits[i] != null)
                    {
                        if (rtsm.allUnits[i].gameObject.GetComponent<NetworkIdentity>() != null)
                        {
                            if (mod == 1)
                            {
                                n2 = n2 + 1;
                            }
                            else if (mod == 2)
                            {
                                if (rtsm.allUnits[i].nation == Diplomacy.active.playerNation)
                                {
                                    n2 = n2 + 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        n2 = n2 + 1;
                    }
                }

                n1 = n2;
            }

            InstantInnerNationUnload(mod);

            if (mod == 1)
            {
                rtsNM.StopHost();
                rtsm.isMultiplayer = false;
                rtsm.StartCoroutines();
                NationSpawner.active.ResetSpawned();
            }
            else if (mod == 2)
            {
                rtsm.isMultiplayer = false;
                NationSpawner.active.ResetSpawned();
            }
#endif
            yield return null;
        }

        public void InstantInnerNationUnload(int mod)
        {
#if URTS_UNET
            int n = Diplomacy.active.numberNations;

            if (mod == 1)
            {
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
                            Diplomacy.active.RemoveNation(Diplomacy.active.GetNationNameFromId(i1));
                        }
                    }
                }

                Diplomacy.active.RemoveNation(Diplomacy.active.GetPlayerNationName());
            }
            else if (mod == 2)
            {
                if (rtsm.rtsCameraNetwork != null)
                {
                    rtsm.rtsCameraNetwork.Cmd_RemoveNation(Diplomacy.active.GetPlayerNationName());
                    StartCoroutine(NationUnloadWaiter(Diplomacy.active.playerNation));
                }
            }
#endif
        }

        IEnumerator NationUnloadWaiter(int nat)
        {
#if URTS_UNET
            int n = Diplomacy.active.numberNations;

            for (int i = 0; i < n; i++)
            {
                Diplomacy.active.RemoveNation(Diplomacy.active.GetNationNameFromId(0, true));
            }

            rtsm.isMultiplayer = false;
            NationSpawner.active.Spawn();
            rtsm.StartCoroutines();
#endif
            yield return new WaitForSeconds(0.2f);
        }

        public InputField changeNetworkBandwidthText;

        public void ChangeNetworkBandwidth()
        {
#if URTS_UNET
            if (changeNetworkBandwidthText != null)
            {
                if (RTSNetworkManager.active != null)
                {
                    uint bndw = 0;

                    if (uint.TryParse(changeNetworkBandwidthText.text, out bndw))
                    {
                        RTSNetworkManager.active.connectionConfig.InitialBandwidth = bndw;
                    }
                }
            }
#endif
        }

        public InputField changePassedBytesText;

        public void ChangePassedBytesText()
        {
#if URTS_UNET
            if (changePassedBytesText != null)
            {
                if (RTSNetworkManager.active != null)
                {
                    uint bndw = 0;

                    if (uint.TryParse(changePassedBytesText.text, out bndw))
                    {
                        NetworkSyncTransform.allowedBytesToPass = bndw;
                    }
                }
            }
#endif
        }
    }
}
