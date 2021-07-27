using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class LevelElementsManager : MonoBehaviour
    {
        public static LevelElementsManager active;

        public LevelElementUI attackLevel;
        public LevelElementUI buildLevel;
        public LevelElementUI healthLevel;
        public LevelElementUI defenceLevel;

        public Text advancedLevelInfo;
        int levelDisplaying = -1;

        int isEnabled = 0;

        SelectionManager sm;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            sm = SelectionManager.active;
        }

        void Update()
        {
            CheckDeActivate();
            Activate();
            UpdateTexts();
            UpdateLevelInfoText();
        }

        void CheckDeActivate()
        {
            if (sm.selectedGoPars.Count == 0)
            {
                if (isEnabled > 0)
                {
                    DeActivate();
                }
            }
        }

        public void DeActivate()
        {
            attackLevel.gameObject.SetActive(false);
            buildLevel.gameObject.SetActive(false);
            healthLevel.gameObject.SetActive(false);
            defenceLevel.gameObject.SetActive(false);
            isEnabled = 0;
        }

        void ActivateAndResetParent(GameObject go)
        {
            go.SetActive(true);
            Transform par = go.transform.parent;
            go.transform.SetParent(this.transform);
            go.transform.SetParent(par);
        }

        void Activate()
        {
            if (sm.selectedGoPars.Count == 1)
            {
                UnitPars up = sm.selectedGoPars[0];

                if (up.nation == Diplomacy.active.playerNation)
                {
                    if (up.unitParsType.isBuilding)
                    {
                        ActivateAndResetParent(buildLevel.gameObject);
                        ActivateAndResetParent(healthLevel.gameObject);
                        ActivateAndResetParent(defenceLevel.gameObject);
                    }
                    else if (up.unitParsType.isWorker)
                    {
                        ActivateAndResetParent(buildLevel.gameObject);
                        ActivateAndResetParent(healthLevel.gameObject);
                        ActivateAndResetParent(defenceLevel.gameObject);
                    }
                    else
                    {
                        ActivateAndResetParent(attackLevel.gameObject);
                        ActivateAndResetParent(healthLevel.gameObject);
                        ActivateAndResetParent(defenceLevel.gameObject);
                    }

                    isEnabled = 1;
                }
            }
            else if (sm.selectedGoPars.Count > 1)
            {
                if (AreAllSelectedUnitsMilitary())
                {
                    attackLevel.gameObject.SetActive(true);
                }

                if (AreAllSelectedUnitsNonMilitary())
                {
                    buildLevel.gameObject.SetActive(true);
                }

                healthLevel.gameObject.SetActive(true);
                defenceLevel.gameObject.SetActive(true);
                isEnabled = 2;
            }
        }

        void UpdateTexts()
        {
            if (isEnabled > 0)
            {
                attackLevel.text.text = GetMeanLevelValue(1).ToString();
                buildLevel.text.text = GetMeanLevelValue(3).ToString();
                healthLevel.text.text = GetMeanLevelValue(0).ToString();
                defenceLevel.text.text = GetMeanLevelValue(2).ToString();
            }
        }

        int GetMeanLevelValue(int id)
        {
            if (isEnabled == 1)
            {
                return sm.selectedGoPars[0].levelValues[id];
            }

            float fMean = 0f;

            for (int i = 0; i < sm.selectedGoPars.Count; i++)
            {
                fMean = fMean + 1f * sm.selectedGoPars[i].levelValues[id];
            }

            return (int)(fMean / sm.selectedGoPars.Count);
        }

        int GetTotLevelValue(int id)
        {
            if (isEnabled == 1)
            {
                return sm.selectedGoPars[0].levelValues[id];
            }

            float fMean = 0f;

            for (int i = 0; i < sm.selectedGoPars.Count; i++)
            {
                fMean = fMean + 1f * sm.selectedGoPars[i].levelValues[id];
            }

            return (int)(fMean);
        }

        int GetTotExpValue(int id)
        {
            if (isEnabled == 1)
            {
                return (int)(sm.selectedGoPars[0].levelExp[id]);
            }

            float fMean = 0f;

            for (int i = 0; i < sm.selectedGoPars.Count; i++)
            {
                fMean = fMean + sm.selectedGoPars[i].levelExp[id];
            }

            return (int)(fMean);
        }

        public void EnableLevelInfo(LevelElementUI el)
        {
            if (el == attackLevel)
            {
                levelDisplaying = 0;
            }

            if (el == buildLevel)
            {
                levelDisplaying = 1;
            }

            if (el == healthLevel)
            {
                levelDisplaying = 2;
            }

            if (el == defenceLevel)
            {
                levelDisplaying = 3;
            }

            advancedLevelInfo.gameObject.SetActive(true);
        }

        public void DisableLevelInfo()
        {
            advancedLevelInfo.gameObject.SetActive(false);
            levelDisplaying = -1;
        }

        void UpdateLevelInfoText()
        {
            if (levelDisplaying != -1)
            {
                if (levelDisplaying == 0)
                {
                    advancedLevelInfo.text = "Attack " + GetLevelInfoString(1);
                }

                if (levelDisplaying == 1)
                {
                    advancedLevelInfo.text = "Build " + GetLevelInfoString(3);
                }

                if (levelDisplaying == 2)
                {
                    advancedLevelInfo.text = "Health " + GetLevelInfoString(0);
                }

                if (levelDisplaying == 3)
                {
                    advancedLevelInfo.text = "Defence " + GetLevelInfoString(2);
                }
            }
        }

        string GetLevelInfoString(int id)
        {
            if (isEnabled == 2)
            {
                return "Mean level " + GetMeanLevelValue(id) + "     Total level " + GetTotLevelValue(id) + "     XP: " + GetTotExpValue(id) + ";";
            }

            string nlexp = "0";
            string rlexp = "0";

            if (sm.selectedGoPars.Count > 0)
            {
                nlexp = ((int)(sm.selectedGoPars[0].NextLevelExp(id))).ToString();
                rlexp = ((int)(sm.selectedGoPars[0].RemainingExpTillNextLevel(id))).ToString();
            }

            return "Level " + GetMeanLevelValue(id) + "     XP: " + GetTotExpValue(id) + ";     Next level: " + nlexp + ";     Rem: " + rlexp + " ";
        }

        bool AreAllSelectedUnitsMilitary()
        {
            for (int i = 0; i < sm.selectedGoPars.Count; i++)
            {
                if (sm.selectedGoPars[i].unitParsType.isBuilding)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].unitParsType.isWorker)
                {
                    return false;
                }
            }

            return true;
        }

        bool AreAllSelectedUnitsNonMilitary()
        {
            for (int i = 0; i < sm.selectedGoPars.Count; i++)
            {
                if (sm.selectedGoPars[i].rtsUnitId == 11)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 12)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 13)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 14)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 16)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 17)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 18)
                {
                    return false;
                }

                if (sm.selectedGoPars[i].rtsUnitId == 19)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
