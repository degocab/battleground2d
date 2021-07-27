using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class FindInternetMatchNode : MonoBehaviour
    {
#if URTS_UNET
	    public UnityEngine.Networking.Match.MatchInfoSnapshot match;
#endif
        public Text text;

        void Start()
        {

        }

        public void RunMatch()
        {
#if URTS_UNET
            RTSNetworkManager manager = RTSNetworkManager.active;
            manager.phase = 2;
            SaveLoad.active.UnloadEverything();
            manager.matchName = match.name;
            manager.matchSize = (uint)match.currentSize;
            manager.matchMaker.JoinMatch(match.networkId, "", "", "", 0, 0, manager.OnMatchJoined);
#endif
        }
    }
}
