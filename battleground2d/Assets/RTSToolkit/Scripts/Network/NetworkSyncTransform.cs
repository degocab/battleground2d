using UnityEngine;
using UnityEngine.Networking;

namespace RTSToolkit
{
    public class NetworkSyncTransform
#if URTS_UNET
    : NetworkBehaviour 
#else
    : MonoBehaviour
#endif
    {
        public static int nCmdSendPosition = 0;
        public static int nCmdSendRotation = 0;
        public static int nCmdSendAnimationName = 0;
        public static int nCmdSendNaveMeshDestination = 0;

        public static int nBytesCmdSendPosition = 0;
        public static int nBytesCmdSendRotation = 0;
        public static int nBytesCmdSendAnimationName = 0;
        public static int nBytesCmdSendNaveMeshDestination = 0;

        public static float bytesCmdSendPositionRate = 0;

        [SerializeField]
        float _posLerpRate = 1;
        [SerializeField]
        float _rotLerpRate = 1;

        [SerializeField]
        float _posThresholdLodMin = 0.5f;
        [SerializeField]
        float _posThresholdLodMax = 10f;

        [SerializeField]
        float _rotThresholdLodMin = 3f;
        [SerializeField]
        float _rotThresholdLodMax = 90f;

        public bool _syncNavMeshAgent = false;
        [SerializeField]
        float _navMeshDestinationThreshold = 3f;

        [SerializeField]
        float _posObjectToCameraDistanceMultiplier = 0.02f;
        [SerializeField]
        float _rotObjectToCameraDistanceMultiplier = 0.2f;

        [SerializeField]
        float _posUpdateTime = 1f;
        float lastPosUpdateTime = -1f;

        [SerializeField]
        float _rotUpdateTime = 2f;
        float lastRotUpdateTime = -1f;

        [SerializeField]
        float _animationNameUpdateTime = 4f;
        float lastAnimationNameUpdateTime = -1f;

        [SerializeField]
        float _navMeshDestinationUpdateTime = 2f;
        float lastNavMeshDestinationUpdateTime = -1f;

#if URTS_UNET
        [SyncVar]
#endif
        Vector3 _lastPosition;
#if URTS_UNET
        [SyncVar]
#endif
        Vector3 _lastRotation;
#if URTS_UNET
        [SyncVar]
#endif
        string _lastAnimationName;
#if URTS_UNET
        [SyncVar]
#endif
        Vector3 _lastNavMeshDestination;

        UnitPars up;
        UnitAnimation ua;
        UnityEngine.AI.NavMeshAgent nma;
        float closestCameraDistance = 0f;

        public static uint allowedBytesToPass = 250;

        void Start()
        {
            _lastPosition = transform.position;
            _lastRotation = transform.eulerAngles;

            ua = GetComponent<UnitAnimation>();
            up = GetComponent<UnitPars>();
            nma = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        void Update()
        {
            if (IsMaster)
            {
                return;
            }

            InterpolatePosition();
            InterpolateRotation();
            SetAnimationName();
            SetNavMeshDestination();
        }

        bool IsMaster
        {
#if URTS_UNET
            get { return isLocalPlayer || hasAuthority; }
#else
            get { return true; }
#endif
        }

        void FixedUpdate()
        {
            if (!IsMaster)
            {
                return;
            }

            float fixedDeltaTime = Time.fixedDeltaTime;
            closestCameraDistance = GetClosestCameraDistance(transform.position);

            bool posChanged = IsPositionChanged();
            if (posChanged)
            {
                nCmdSendPosition++;
                nBytesCmdSendPosition = nBytesCmdSendPosition + 12;
                CmdSendPosition(transform.position);
                _lastPosition = transform.position;
            }

            bool rotChanged = IsRotationChanged();
            if (rotChanged)
            {
                nCmdSendRotation++;
                nBytesCmdSendRotation = nBytesCmdSendRotation + 12;
                CmdSendRotation(transform.localEulerAngles);
                _lastRotation = transform.localEulerAngles;
            }

            bool animNameChanged = IsAnimationNameChanged();
            if (animNameChanged)
            {
                if (ua != null)
                {
                    nCmdSendAnimationName++;
                    nBytesCmdSendAnimationName = nBytesCmdSendAnimationName + 4 + 2 * ua.animName.Length;
                    CmdSendAnimationName(ua.animName);
                    _lastAnimationName = ua.animName;
                }
            }

            bool isNaveMeshAgentDestinationChanged = IsNaveMeshAgentDestinationChanged();
            if (isNaveMeshAgentDestinationChanged)
            {
                if (nma != null)
                {
                    nCmdSendNaveMeshDestination++;
                    nBytesCmdSendNaveMeshDestination = nBytesCmdSendNaveMeshDestination + 12;
                    CmdSendNavMeshDestination(nma.destination);
                    _lastNavMeshDestination = nma.destination;
                }
            }
        }

        void InterpolatePosition()
        {
            if (_syncNavMeshAgent == false)
            {
                transform.position = Vector3.Lerp(transform.position, _lastPosition, Time.deltaTime * _posLerpRate);
            }
        }

        void InterpolateRotation()
        {
            if (_syncNavMeshAgent == false)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(_lastRotation), Time.deltaTime * _rotLerpRate);
            }
        }

        void SetAnimationName()
        {
            if (ua != null)
            {
                if (ua.animName != _lastAnimationName)
                {
                    if (up != null)
                    {
                        if ((ua.animName != "Attack") && (_lastAnimationName == "Attack"))
                        {
                            up.StartMultiplayerAttackAnimation();
                        }
                        else if (_lastAnimationName != "Attack")
                        {
                            up.StopMultiplayerAttackAnimation();
                        }
                    }

                    ua.PlayAnimationCheck(_lastAnimationName);
                }
            }
        }

        void SetNavMeshDestination()
        {
            if (_syncNavMeshAgent)
            {
                if (nma != null)
                {
                    if (nma.enabled)
                    {
                        if (nma.destination != _lastNavMeshDestination)
                        {
                            if (up != null)
                            {
                                UnitsMover.active.AddMilitaryAvoider(up, _lastNavMeshDestination, 0);
                            }
                        }
                    }
                }
            }
        }

#if URTS_UNET
        [Command(channel = Channels.DefaultUnreliable)]
#endif
        void CmdSendPosition(Vector3 pos)
        {
            _lastPosition = pos;
        }

#if URTS_UNET
        [Command(channel = Channels.DefaultUnreliable)]
#endif
        void CmdSendRotation(Vector3 rot)
        {
            _lastRotation = rot;
        }

#if URTS_UNET
        [Command(channel = Channels.DefaultUnreliable)]
#endif
        void CmdSendAnimationName(string aName)
        {
            _lastAnimationName = aName;
        }

#if URTS_UNET
        [Command(channel = Channels.DefaultUnreliable)]
#endif
        void CmdSendNavMeshDestination(Vector3 dest)
        {
            _lastNavMeshDestination = dest;
        }

        bool IsPositionChanged()
        {
            if (_syncNavMeshAgent)
            {
                return false;
            }

            float posThr = _posObjectToCameraDistanceMultiplier * closestCameraDistance;

            if (posThr < _posThresholdLodMin)
            {
                posThr = _posThresholdLodMin;
            }
            if (posThr > _posThresholdLodMax)
            {
                posThr = _posThresholdLodMax;
            }

            if (Time.time - lastPosUpdateTime < _posUpdateTime)
            {
                return false;
            }

            bool isPositionChanged = Vector3.Distance(transform.position, _lastPosition) > posThr;
            isPositionChanged = TrafficLimitFilter(isPositionChanged);

            if (BSystemStatisticsUI.positionSpeed >= BSystemStatisticsUI.positionPassFraction * BSystemStatisticsUI.trafficSpeed)
            {
                isPositionChanged = false;
            }

            if (isPositionChanged)
            {
                lastPosUpdateTime = Time.time;
            }

            return isPositionChanged;
        }

        bool IsRotationChanged()
        {
            if (_syncNavMeshAgent)
            {
                return false;
            }

            float rotThr = _rotObjectToCameraDistanceMultiplier * closestCameraDistance;

            if (rotThr < _rotThresholdLodMin)
            {
                rotThr = _rotThresholdLodMin;
            }
            if (rotThr > _rotThresholdLodMax)
            {
                rotThr = _rotThresholdLodMax;
            }

            if (Time.time - lastRotUpdateTime < _rotUpdateTime)
            {
                return false;
            }

            bool isRotationChanged = Vector3.Distance(transform.localEulerAngles, _lastRotation) > rotThr;
            isRotationChanged = TrafficLimitFilter(isRotationChanged);

            if (BSystemStatisticsUI.rotationSpeed >= BSystemStatisticsUI.rotationPassFraction * BSystemStatisticsUI.trafficSpeed)
            {
                isRotationChanged = false;
            }

            if (isRotationChanged)
            {
                lastRotUpdateTime = Time.time;
            }

            return isRotationChanged;
        }

        bool IsAnimationNameChanged()
        {
            if (ua == null)
            {
                return false;
            }

            if (ua.animName != _lastAnimationName)
            {
                if (Time.time - lastAnimationNameUpdateTime < _animationNameUpdateTime)
                {
                    return false;
                }

                if (TrafficLimitFilter(true) && (BSystemStatisticsUI.animationNameSpeed <= BSystemStatisticsUI.animationNamePassFraction * BSystemStatisticsUI.trafficSpeed))
                {
                    lastAnimationNameUpdateTime = Time.time;
                    return true;
                }
            }

            return false;
        }

        bool IsNaveMeshAgentDestinationChanged()
        {
            if (_syncNavMeshAgent == false)
            {
                return false;
            }

            if (nma == null)
            {
                return false;
            }

            if (Time.time - lastNavMeshDestinationUpdateTime < _navMeshDestinationUpdateTime)
            {
                return false;
            }

            if ((nma.destination - _lastNavMeshDestination).magnitude > _navMeshDestinationThreshold)
            {
                if (TrafficLimitFilter(true) && (BSystemStatisticsUI.navMeshDestiationSpeed <= BSystemStatisticsUI.navMeshDestinationPassFraction * BSystemStatisticsUI.trafficSpeed))
                {
                    lastNavMeshDestinationUpdateTime = Time.time;
                    return true;
                }
            }

            return false;
        }

        public static bool TrafficLimitFilter(bool pass)
        {
            if (pass)
            {
                float topLimit = allowedBytesToPass;
                float interp = GenericMath.Interpolate(BSystemStatisticsUI.trafficSpeed, 0.3f * topLimit, topLimit, 1, 0);

                if (Random.value > interp)
                {
                    return false;
                }
            }

            return pass;
        }

        public static float GetClosestCameraDistance(Vector3 point)
        {
            float dist = 10000f;

            if (RTSMultiplayer.allRTSMultiplayers != null)
            {
                if (RTSMultiplayer.allRTSMultiplayers.Count > 0)
                {
                    if (RTSMultiplayer.allRTSMultiplayers[0] != null)
                    {
                        if (RTSMultiplayer.allRTSMultiplayers[0].transform != null)
                        {
                            dist = (point - RTSMultiplayer.allRTSMultiplayers[0].transform.position).magnitude;
                        }
                    }

                    for (int i = 0; i < RTSMultiplayer.allRTSMultiplayers.Count; i++)
                    {
                        if (RTSMultiplayer.allRTSMultiplayers[i] != null)
                        {
                            if (RTSMultiplayer.allRTSMultiplayers[i].transform != null)
                            {
                                float distnew = (point - RTSMultiplayer.allRTSMultiplayers[i].transform.position).magnitude;

                                if (distnew < dist)
                                {
                                    dist = distnew;
                                }
                            }
                        }
                    }
                }
            }

            return dist;
        }

#if URTS_UNET
        public override int GetNetworkChannel()
        {
            return Channels.DefaultUnreliable;
        }

        public override float GetNetworkSendInterval()
        {
            return 1f;
        }
#endif
    }
}
