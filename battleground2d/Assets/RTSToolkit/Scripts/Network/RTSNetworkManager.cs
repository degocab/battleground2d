using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class RTSNetworkManager
#if URTS_UNET
    : NetworkManager 
#else
    : MonoBehaviour
#endif
    {
        public static RTSNetworkManager active;
        public int phase = 0;

        public static RTSNetworkManager GetActive()
        {
            if (RTSNetworkManager.active == null)
            {
                RTSNetworkManager.active = UnityEngine.Object.FindObjectOfType<RTSNetworkManager>();
            }

            return RTSNetworkManager.active;
        }

        void Start()
        {
            active = this;

#if URTS_UNET
            NetworkMigrationManager migrationManager = this.gameObject.GetComponent<NetworkMigrationManager>();

            if (migrationManager != null)
            {
                if (migrationManager.enabled)
                {
                    SetupMigrationManager(migrationManager);
                }
            }

            this.connectionConfig.InitialBandwidth = 3000;
            this.connectionConfig.MaxSentMessageQueueSize = 51200;
            this.connectionConfig.MaxCombinedReliableMessageCount = 30;
            this.connectionConfig.MaxCombinedReliableMessageSize = 200;
#endif
        }

#if URTS_UNET
        public override void OnStartHost()
        {
            if (phase == 1)
            {
                MultiplayerUI.active.DisableAll();
                MultiplayerUI.active.lanHost.SetActive(true);
                MultiplayerUI.active.lanHostServerPortText.text = "Server: port=" + networkPort;
                MultiplayerUI.active.lanHostClientAddressText.text = "address=" + networkAddress;
                MultiplayerUI.active.lanHostClientPortText.text = "port=" + networkPort;

            }
        }

        public override void OnStartClient(NetworkClient client)
        {
            if (phase == 2)
            {
                MultiplayerUI.active.DisableAll();
                MultiplayerUI.active.lanClient.SetActive(true);
                MultiplayerUI.active.lanClientClientAddressText.text = "address=" + networkAddress;
                MultiplayerUI.active.lanClientClientPortText.text = "port=" + networkPort;
                StartCoroutine(ReloadPlayerUnits());
            }
        }

        IEnumerator ReloadPlayerUnits()
        {
            yield return new WaitForSeconds(3f);

            if (RTSMaster.active.isMultiplayer)
            {
                if (MultiplayerUI.active.savedPlayerUnits != null)
                {
                    if (MultiplayerUI.active.savedPlayerUnits.Length > 0)
                    {
                        SaveLoad.active.LoadNationFromBytes(MultiplayerUI.active.savedPlayerUnits);
                        MultiplayerUI.active.savedPlayerUnits = null;
                    }
                }
            }
        }

        public override void OnStartServer()
        {
            if (phase == 3)
            {
                MultiplayerUI.active.DisableAll();
                MultiplayerUI.active.lanServerOnly.SetActive(true);
                MultiplayerUI.active.lanServerServerPortText.text = "Server: port=" + networkPort;
            }
        }

        public override void OnStopHost()
        {
            if (phase == 1)
            {
                phase = 0;
                MultiplayerUI.active.DisableAll();
                MultiplayerUI.active.notConnected.SetActive(true);
            }
        }

        public override void OnStopClient()
        {
            if (phase == 2)
            {
                RTSMaster.active.StopCoroutines();
                StartCoroutine(MultiplayerUI.active.DisconnectUnloadChecker(2));
                phase = 0;
                MultiplayerUI.active.DisableAll();
                MultiplayerUI.active.notConnected.SetActive(true);
            }
        }

        public override void OnStopServer()
        {
            if (phase == 3)
            {
                phase = 0;
                MultiplayerUI.active.DisableAll();
                MultiplayerUI.active.notConnected.SetActive(true);
            }
        }

        public override void OnMatchList(bool success, string extendedInfo, List<UnityEngine.Networking.Match.MatchInfoSnapshot> matchList)
        {
            matches = matchList;
            MultiplayerUI.active.FindInternetMatchInner();
        }
#endif

        void Update()
        {

        }
    }
}
