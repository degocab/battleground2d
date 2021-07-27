using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class BSystemStatisticsUI : MonoBehaviour
    {
        public Text bSystemText;
        public Text approachText;
        public Text attackText;
        public Text selfHealText;
        public Text deadText;
        public Text sinkText;

        public Text networkSyncTrasformPositionText;
        public Text networkSyncTrasformRotationText;
        public Text networkSyncTrasformNavMeshDestinationText;

        public Text textCmd_AddNation;
        public Text textCmd_AddNetworkComponent;
        public Text textCmd_UpdateUnitHealth;

        public Text textCmd_PlayAnimation;
        public Text textCmd_DestroyUnit;
        public Text textCmd_RemoveNation;
        public Text textCmd_FinishBuilding;
        public Text textCmd_IsPlayer;

        public Text textCmd_RemoveAttackers;
        public Text textCmd_AdjustAgent;
        public Text textCmd_SetRelation;
        public Text textCmd_SetNationName;
        public Text textCmd_SendNationMessage;
        public Text textCmd_LaunchArrow;

        public Text textTotalTraffic;

        public Text positionPassFractionText;
        public static float positionPassFraction = 0.5f;

        public Text rotationPassFractionText;
        public static float rotationPassFraction = 0.5f;

        public Text animationNamePassFractionText;
        public static float animationNamePassFraction = 0.5f;

        public Text healthPassFractionText;
        public static float healthPassFraction = 0.5f;

        public Text navMeshDestinationPassFractionText;
        public static float navMeshDestinationPassFraction = 0.5f;

        public static BSystemStatisticsUI active;

        void Start()
        {
            active = this;
            StartCoroutine(UpdateTexts());
        }

        float lastBytes = 0f;

        float last_positionBytes = 0f;
        float last_rotationBytes = 0f;
        float last_animationNameBytes = 0f;
        float last_healthBytes = 0f;
        float last_navMeshDestiationBytes = 0f;

        void FixedUpdate()
        {
            if (RTSMaster.active.isMultiplayer)
            {
                int nBytesSend =
                    NetworkSyncTransform.nBytesCmdSendPosition +
                    NetworkSyncTransform.nBytesCmdSendRotation +
                    NetworkSyncTransform.nBytesCmdSendAnimationName +
                    RTSMultiplayer.n_Cmd_BytesUpdateUnitHealth +
                    NetworkSyncTransform.nBytesCmdSendNaveMeshDestination;

                trafficSpeed = 0.99f * trafficSpeed + 0.01f * (nBytesSend - lastBytes) / Time.fixedDeltaTime;
                lastBytes = nBytesSend;

                positionSpeed = 0.99f * positionSpeed + 0.01f * (NetworkSyncTransform.nBytesCmdSendPosition - last_positionBytes) / Time.fixedDeltaTime;
                last_positionBytes = NetworkSyncTransform.nBytesCmdSendPosition;

                rotationSpeed = 0.99f * rotationSpeed + 0.01f * (NetworkSyncTransform.nBytesCmdSendRotation - last_rotationBytes) / Time.fixedDeltaTime;
                last_rotationBytes = NetworkSyncTransform.nBytesCmdSendPosition;

                animationNameSpeed = 0.99f * animationNameSpeed + 0.01f * (NetworkSyncTransform.nBytesCmdSendAnimationName - last_animationNameBytes) / Time.fixedDeltaTime;
                last_animationNameBytes = NetworkSyncTransform.nBytesCmdSendAnimationName;

                healthSpeed = 0.99f * healthSpeed + 0.01f * (RTSMultiplayer.n_Cmd_BytesUpdateUnitHealth - last_healthBytes) / Time.fixedDeltaTime;
                last_healthBytes = RTSMultiplayer.n_Cmd_BytesUpdateUnitHealth;

                navMeshDestiationSpeed = 0.99f * navMeshDestiationSpeed + 0.01f * (NetworkSyncTransform.nBytesCmdSendNaveMeshDestination - last_navMeshDestiationBytes) / Time.fixedDeltaTime;
                last_navMeshDestiationBytes = NetworkSyncTransform.nBytesCmdSendNaveMeshDestination;
            }
        }

        public static float trafficSpeed = 0f;

        public static float positionSpeed = 0f;
        public static float rotationSpeed = 0f;
        public static float animationNameSpeed = 0f;
        public static float healthSpeed = 0f;
        public static float navMeshDestiationSpeed = 0f;

        IEnumerator UpdateTexts()
        {
            BattleSystem battleSystem = BattleSystem.active;

            while (true)
            {
                if (battleSystem == null)
                {
                    battleSystem = BattleSystem.active;
                }

                if (battleSystem != null)
                {
                    bSystemText.text = battleSystem.message1;
                    approachText.text = battleSystem.message2;
                    attackText.text = battleSystem.message3;
                    selfHealText.text = battleSystem.message4;
                    deadText.text = battleSystem.message5;
                    sinkText.text = battleSystem.message6;
                }

                if (networkSyncTrasformPositionText.text != null)
                {
                    networkSyncTrasformPositionText.text =
                        "networkSyncTrasformPosition: " +
                        NetworkSyncTransform.nCmdSendPosition +
                        " ; " +
                        GetBytesString(NetworkSyncTransform.nBytesCmdSendPosition);
                }

                if (networkSyncTrasformRotationText.text != null)
                {
                    networkSyncTrasformRotationText.text =
                        "networkSyncTrasformRotation: " +
                        NetworkSyncTransform.nCmdSendRotation +
                        " ; " +
                        GetBytesString(NetworkSyncTransform.nBytesCmdSendRotation);
                }

                // RTSMultiplayer numbers
                if (RTSMaster.active.isMultiplayer)
                {
                    if (networkSyncTrasformNavMeshDestinationText.text != null)
                    {
                        networkSyncTrasformNavMeshDestinationText.text =
                            "networkSyncTrasformNavMeshDestinationText: " +
                            NetworkSyncTransform.nCmdSendNaveMeshDestination +
                            " ; " +
                            GetBytesString(NetworkSyncTransform.nBytesCmdSendNaveMeshDestination);
                    }

                    if (textCmd_AddNation != null)
                    {
                        textCmd_AddNation.text = "n_Cmd_AddNation: " + RTSMultiplayer.n_Cmd_AddNation + " Rpc: " + RTSMultiplayer.n_Rpc_AddNation;
                    }

                    if (textCmd_AddNetworkComponent != null)
                    {
                        textCmd_AddNetworkComponent.text = "n_Cmd_AddNetworkComponent: " + RTSMultiplayer.n_Cmd_AddNetworkComponent;
                    }

                    if (textCmd_UpdateUnitHealth != null)
                    {
                        textCmd_UpdateUnitHealth.text = "n_Cmd_UpdateUnitHealth: " + RTSMultiplayer.n_Cmd_UpdateUnitHealth;
                    }

                    if (textCmd_PlayAnimation != null)
                    {
                        textCmd_PlayAnimation.text =
                            "nCmdSendAnimationName: " +
                            NetworkSyncTransform.nCmdSendAnimationName +
                            " ; " +
                            GetBytesString(NetworkSyncTransform.nBytesCmdSendAnimationName);
                    }

                    if (textCmd_DestroyUnit != null)
                    {
                        textCmd_DestroyUnit.text = "n_Cmd_DestroyUnit: " + RTSMultiplayer.n_Cmd_DestroyUnit + " Rpc: " + RTSMultiplayer.n_Rpc_DestroyUnit;
                    }

                    if (textCmd_RemoveNation != null)
                    {
                        textCmd_RemoveNation.text = "n_Cmd_RemoveNation: " + RTSMultiplayer.n_Cmd_RemoveNation + " Rpc: " + RTSMultiplayer.n_Rpc_RemoveNation;
                    }

                    if (textCmd_FinishBuilding != null)
                    {
                        textCmd_FinishBuilding.text = "n_Cmd_FinishBuilding: " + RTSMultiplayer.n_Cmd_FinishBuilding;
                    }

                    if (textCmd_IsPlayer != null)
                    {
                        textCmd_IsPlayer.text = "n_Cmd_IsPlayer: " + RTSMultiplayer.n_Cmd_IsPlayer;
                    }

                    if (textCmd_RemoveAttackers != null)
                    {
                        textCmd_RemoveAttackers.text = "n_Cmd_RemoveAttackers: " + RTSMultiplayer.n_Cmd_RemoveAttackers + " Rpc: " + RTSMultiplayer.n_Rpc_RemoveAttackers;
                    }

                    if (textCmd_AdjustAgent != null)
                    {
                        textCmd_AdjustAgent.text = "n_Cmd_AdjustAgent: " + RTSMultiplayer.n_Cmd_AdjustAgent + " Rpc: " + RTSMultiplayer.n_Rpc_AdjustAgent;
                    }

                    if (textCmd_SetRelation != null)
                    {
                        textCmd_SetRelation.text = "n_Cmd_SetRelation: " + RTSMultiplayer.n_Cmd_SetRelation + " Rpc: " + RTSMultiplayer.n_Rpc_SetRelation;
                    }

                    if (textCmd_SetNationName != null)
                    {
                        textCmd_SetNationName.text = "n_Cmd_SetNationName: " + RTSMultiplayer.n_Cmd_SetNationName;
                    }

                    if (textCmd_SendNationMessage != null)
                    {
                        textCmd_SendNationMessage.text = "n_Cmd_SendNationMessage: " + RTSMultiplayer.n_Cmd_SendNationMessage + " Rpc: " + RTSMultiplayer.n_Rpc_SendNationMessage;
                    }

                    if (textCmd_LaunchArrow != null)
                    {
                        textCmd_LaunchArrow.text = "n_Cmd_LaunchArrow: " + RTSMultiplayer.n_Cmd_LaunchArrow + " Rpc: " + RTSMultiplayer.n_Rpc_LaunchArrow;
                    }

                    if (textTotalTraffic != null)
                    {
                        int nBytesSend =
                            NetworkSyncTransform.nBytesCmdSendPosition +
                            NetworkSyncTransform.nBytesCmdSendRotation +
                            NetworkSyncTransform.nBytesCmdSendAnimationName +
                            RTSMultiplayer.n_Cmd_BytesUpdateUnitHealth +
                            NetworkSyncTransform.nBytesCmdSendNaveMeshDestination;

                        textTotalTraffic.text = "Total traffic: " + GetBytesString(nBytesSend) + " rate: " + GetBytesString(trafficSpeed) + "/s";
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        public static string GetBytesString(int nbytes)
        {
            if (nbytes > 1000000)
            {
                float nb = (1f * nbytes) / 1000000;
                return nb.ToString("#.0") + " MB";
            }
            else if (nbytes > 1000)
            {
                float nb = (1f * nbytes) / 1000;
                return nb.ToString("#.0") + " kB";
            }

            return nbytes.ToString("#.0") + " B";
        }

        public static string GetBytesString(float nbytes)
        {
            if (nbytes > 1000000)
            {
                float nb = (1f * nbytes) / 1000000;
                return nb.ToString("#.0") + " MB";
            }
            else if (nbytes > 1000)
            {
                float nb = (1f * nbytes) / 1000;
                return nb.ToString("#.0") + " kB";
            }

            return nbytes.ToString("#.0") + " B";
        }

        public void ChangePositionPassFraction()
        {
            if (positionPassFractionText != null)
            {
                float pf = positionPassFraction;

                if (float.TryParse(positionPassFractionText.text, out pf))
                {
                    positionPassFraction = pf;
                }
            }
        }

        public void ChangeRotationPassFraction()
        {
            if (rotationPassFractionText != null)
            {
                float pf = rotationPassFraction;

                if (float.TryParse(rotationPassFractionText.text, out pf))
                {
                    rotationPassFraction = pf;
                }
            }
        }

        public void ChangeAnimationNamePassFraction()
        {
            if (animationNamePassFractionText != null)
            {
                float pf = animationNamePassFraction;

                if (float.TryParse(animationNamePassFractionText.text, out pf))
                {
                    animationNamePassFraction = pf;
                }
            }
        }

        public void ChangeHealthPassFraction()
        {
            if (healthPassFractionText != null)
            {
                float pf = healthPassFraction;

                if (float.TryParse(healthPassFractionText.text, out pf))
                {
                    healthPassFraction = pf;
                }
            }
        }

        public void ChangeNavMeshDestinationPassFraction()
        {
            if (navMeshDestinationPassFractionText != null)
            {
                float pf = navMeshDestinationPassFraction;

                if (float.TryParse(navMeshDestinationPassFractionText.text, out pf))
                {
                    navMeshDestinationPassFraction = pf;
                }
            }
        }
    }
}
