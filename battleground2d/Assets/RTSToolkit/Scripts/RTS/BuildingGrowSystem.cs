using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class BuildingGrowSystem : MonoBehaviour
    {
        public static BuildingGrowSystem active;

        List<UnitPars> buildings = new List<UnitPars>();
        List<UnitPars> postUpdateRemovals = new List<UnitPars>();

        int innerLoopIndex = 0;
        public float updateFrequency = 0.3f;
        public float multiplayerUpdateFrequency = 0.03f;
        float deltaTime;
        float cheatModeMultiplier = 1f;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            if (Cheats.active != null)
            {
                if (Cheats.active.godMode == 1)
                {
                    cheatModeMultiplier = cheatModeMultiplier * 10f;
                }
            }
        }

        float updateHealthMultiplier = 1f;
        float updateProgress = 0f;

        void Update()
        {
            deltaTime = Time.deltaTime;

            updateHealthMultiplier = cheatModeMultiplier * deltaTime / updateFrequency;
            float updateProgressIncrement = updateFrequency * buildings.Count;

            if (RTSMaster.active.isMultiplayer)
            {
                updateHealthMultiplier = cheatModeMultiplier * deltaTime / multiplayerUpdateFrequency;
                updateProgressIncrement = multiplayerUpdateFrequency * buildings.Count;
            }

            updateProgress = updateProgress + updateProgressIncrement;
            int intUpdateProgress = (int)updateProgress;
            updateProgress = updateProgress - intUpdateProgress;
            int nToLoop = intUpdateProgress;

            for (int i = 0; i < nToLoop; i++)
            {
                if (buildings.Count < nToLoop)
                {
                    nToLoop = buildings.Count;
                }

                if (innerLoopIndex >= buildings.Count)
                {
                    innerLoopIndex = 0;
                }

                UpdateBuilding(buildings[innerLoopIndex]);

                innerLoopIndex++;
            }

            RemovePostUpdateRemovals();
        }

        void UpdateBuilding(UnitPars up)
        {
            if ((up.isDying == false) && (up.isSinking == false))
            {
                bool multiplayerPass = false;

                if (RTSMaster.active.isMultiplayer)
                {
                    string nationName = up.nationName;

                    NetworkUnique nu = up.GetComponent<NetworkUnique>();
                    if (nu != null)
                    {
                        nationName = nu.nationName;
                    }

                    if (RTSMultiplayer.BelongsToComputer(up.gameObject, nationName))
                    {
                        multiplayerPass = true;
                    }
                }
                else
                {
                    multiplayerPass = true;
                }

                if (multiplayerPass)
                {
                    float buildTime = up.unitParsType.buildTime;
                    float healAmount = updateHealthMultiplier * up.maxHealth / buildTime;

                    float healthNeededTillFull = up.maxHealth - up.health;

                    if (healAmount > healthNeededTillFull)
                    {
                        healAmount = healthNeededTillFull;
                        postUpdateRemovals.Add(up);
                        up.isBuildFinished = true;
                        up.FinishBuilding();

                        if (up.isSelected)
                        {
                            if (up.nation == Diplomacy.active.playerNation)
                            {
                                SelectionManager.active.ActivateBuildingsMenu(up.rtsUnitId);
                            }
                        }
                    }

                    up.UpdateHealth(up.health + healAmount);
                }
            }
            else
            {
                postUpdateRemovals.Add(up);
            }
        }

        void RemovePostUpdateRemovals()
        {
            if (postUpdateRemovals.Count > 0)
            {
                for (int i = 0; i < postUpdateRemovals.Count; i++)
                {
                    RemoveFromSystem(postUpdateRemovals[i]);
                }

                postUpdateRemovals.Clear();
            }
        }

        public void AddToSystem(UnitPars up)
        {
            RemoveFromSystem(up);
            up.UpdateHealth(0.1f * up.maxHealth);

            MeshRenderer mr = up.GetComponent<MeshRenderer>();

            if (mr != null)
            {
                if (mr.enabled == false)
                {
                    mr.enabled = true;
                }
            }

            buildings.Add(up);
            up.isBuildingGrowing = true;

            BuildSoundsPlaySystem.active.AddToSystem(up);
        }

        public void RemoveFromSystem(UnitPars up)
        {
            up.transform.position = TerrainProperties.TerrainVectorProc(up.transform.position);
            buildings.Remove(up);
            up.isBuildingGrowing = false;

            BuildSoundsPlaySystem.active.RemoveFromSystem(up);
        }
    }
}
