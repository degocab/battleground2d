using UnityEngine;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class NationCentreNetworkNode
#if URTS_UNET
    : NetworkBehaviour 
#else
    : MonoBehaviour
#endif
    {
#if URTS_UNET
	    [SyncVar]
#endif
        public bool isPlayer = false;
#if URTS_UNET
	    [SyncVar]
#endif
        public string nationName;

        void Start()
        {

        }
#if URTS_UNET
        public override float GetNetworkSendInterval()
        {
            return 1f;
        }
#endif
    }
}
