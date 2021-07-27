using UnityEngine;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class NetworkUnique
#if URTS_UNET
    : NetworkBehaviour 
#else
    : MonoBehaviour
#endif
    {
#if URTS_UNET
	    [SyncVar]
#endif
        public int id;

#if URTS_UNET
	    [SyncVar]
#endif
        public string nationName;
#if URTS_UNET
	    [SyncVar (hook="HealthChanged")]
#endif
        public float health;
#if URTS_UNET
	    [SyncVar]
#endif
        public bool isBuildFinished;

        [HideInInspector] public UnitPars unitPars;
#if URTS_UNET
	    [HideInInspector] public NetworkIdentity networkIdentity;
#endif

#if URTS_UNET
	    [SyncVar]
#endif
        public int clientId = -1;
#if URTS_UNET
	    [SyncVar]
#endif
        public GameObject player;
#if URTS_UNET
	    [SyncVar]
#endif
        public bool isPlayer = false;

        void Start()
        {
#if URTS_UNET
            networkIdentity = GetComponent<NetworkIdentity>();

            if (networkIdentity != null)
            {
                RTSMaster rtsm = RTSMaster.active;

                SpawnPoint.SetParsImmediatelly(this.gameObject, nationName);

                if (rtsm.rtsCameraNetwork == null)
                {
                    RTSMultiplayer[] allObjects = Object.FindObjectsOfType<RTSMultiplayer>();
                    if (allObjects.Length > 0)
                    {
                        rtsm.rtsCameraNetwork = allObjects[0];
                    }
                }

                if (GetComponent<AgentPars>() != null)
                {
                    if (this.gameObject.GetComponent<UnitPars>() != null)
                    {
                        string natName = this.gameObject.GetComponent<UnitPars>().nationName;
                        rtsm.rtsCameraNetwork.Cmd_AdjustAgent(this.gameObject, natName);
                    }
                }
            }
#endif
        }

        void HealthChanged(float a)
        {
#if URTS_UNET
            if (unitPars == null)
            {
                unitPars = this.gameObject.GetComponent<UnitPars>();
            }
            if (networkIdentity == null)
            {
                networkIdentity = this.gameObject.GetComponent<NetworkIdentity>();
            }

            unitPars.unitParsType = RTSMaster.active.rtsUnitTypePrefabsUpt[unitPars.rtsUnitId];

            if (unitPars.mf == null)
            {
                unitPars.mf = unitPars.GetComponent<MeshFilter>();
            }

            if (unitPars.meshRenderer == null)
            {
                unitPars.meshRenderer = unitPars.GetComponent<MeshRenderer>();
            }

            if (unitPars.unitParsType.isBuilding)
            {
                if (a > unitPars.health)
                {
                    unitPars.UpdateBuildSequenceMesh(a);
                }
                else if (a < unitPars.health)
                {
                    unitPars.UpdateDestroySequenceMesh(a);
                }
            }

            unitPars.health = a;

            if (unitPars.nation == Diplomacy.active.playerNation)
            {
                unitPars.CheckBuildingRestore();
            }
#endif
        }

#if URTS_UNET
        public override float GetNetworkSendInterval()
        {
            return 1f;
        }
#endif
    }
}
