using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class NationSpawner : MonoBehaviour
    {
        public static NationSpawner active;

        RTSMaster rtsm;
        Diplomacy diplomacy;

        public List<NationSpawnerUnit> nations;
        public List<NationSpawnerDialogsGroup> nationDialogGroups;
        public GameObject nationCenterNetworkPrefab;

        [HideInInspector] public NationNames nationNames;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            diplomacy = Diplomacy.active;

            for (int i = 0; i < nationDialogGroups.Count; i++)
            {
                nationDialogGroups[i].InitializeDictionaries();
            }
        }

        public void Spawn()
        {
            int j = 0;

            for (int i = 0; i < nations.Count; i++)
            {
                NationSpawnerUnit nsu = nations[i];

                if ((nsu.spawn) && (nsu.isSpawned == false))
                {
                    if (TerrainProperties.GetTerrainBellow(nsu.position) != null)
                    {
                        if (diplomacy.GetNationIdFromName(nsu.name) == -1)
                        {
                            int isPlayNat = 0;

                            if (nsu.isPlayerNation)
                            {
                                isPlayNat = 1;
                            }

                            diplomacy.AddNewNation(nsu.position, i, nsu.name, nsu.icon, isPlayNat);

                            for (int k = 0; k < nationDialogGroups.Count; k++)
                            {
                                if (nationDialogGroups[k].key == nsu.dialogGroup)
                                {
                                    if (j <= rtsm.nationPars.Count)
                                    {
                                        rtsm.nationPars[j].dialogGroup = nationDialogGroups[k];
                                    }
                                }
                            }

                            nsu.isSpawned = true;

                            if (nsu.isWizzardNation)
                            {
                                if (rtsm.isMultiplayer == false)
                                {
                                    if (j > rtsm.nationPars.Count)
                                    {
                                        Debug.Log(j);
                                    }

                                    rtsm.nationPars[j].isWizzardNation = true;

                                    if (NationListUI.active != null)
                                    {
                                        NationListUI.active.UpdateAsWizzardNation(nsu.name);
                                    }
                                }
                            }

                            if (j < rtsm.nationPars.Count)
                            {
                                rtsm.nationPars[j].nationColor = nsu.nationColor;
                            }

                            j++;
                        }
                        else if (nsu.isPlayerNation)
                        {
                            NationPars np = RTSMaster.active.GetNationPars(nsu.name);

                            if (np != null)
                            {
                                if (np.IsHeroPresent() == false)
                                {
                                    if (UnityNavigation.IsAsyncRunning())
                                    {
                                        np.GetNationNameAndSpawnHero();
                                        nsu.isSpawned = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ResetSpawned()
        {
            for (int i = 0; i < nations.Count; i++)
            {
                nations[i].isSpawned = false;
            }
        }

        float iSpawnRefresher = 0f;
        void Update()
        {
            float dt = Time.deltaTime;
            iSpawnRefresher = iSpawnRefresher + dt;

            if (iSpawnRefresher > 2f)
            {
                Spawn();
                iSpawnRefresher = 0f;
            }
        }
    }

    [System.Serializable]
    public class NationSpawnerUnit
    {
        public string name;
        public Vector3 position;
        public int icon;
        public bool spawn = true;
        public bool isPlayerNation = false;
        public bool isWizzardNation = false;
        public Color nationColor = Color.red;
        [HideInInspector] public bool isSpawned = false;
        public string dialogGroup = string.Empty;

        public NationSpawnerUnit(Vector3 pos, string nm, int ic, bool ipn)
        {
            position = pos;
            name = nm;
            icon = ic;
            isPlayerNation = ipn;
        }
    }

    [System.Serializable]
    public class NationNames
    {
        public List<string> playerNames = new List<string>();
        public List<string> aiNames = new List<string>();
        public List<string> wizzardNames = new List<string>();

        [HideInInspector] public List<string> playerNamesUsed = new List<string>();
        [HideInInspector] public List<string> aiNamesUsed = new List<string>();
        [HideInInspector] public List<string> wizzardNamesUsed = new List<string>();

        public string GetNextPlayerName()
        {
            if (playerNames.Count > 0)
            {
                string nm = playerNames[0];
                playerNamesUsed.Add(nm);
                playerNames.Remove(nm);
                return nm;
            }

            return "";
        }

        public string GetNextAIName()
        {
            if (aiNames.Count > 0)
            {
                string nm = aiNames[0];
                aiNamesUsed.Add(nm);
                aiNames.Remove(nm);
                return nm;
            }

            return "";
        }

        public string GetNextWizzardName()
        {
            if (wizzardNames.Count > 0)
            {
                string nm = wizzardNames[0];
                wizzardNamesUsed.Add(nm);
                wizzardNames.Remove(nm);
                return nm;
            }

            return "";
        }

        public void GetDefaultNames()
        {
            playerNames = PlayerNationNames();
            aiNames = AINationNames();
            wizzardNames = WizzardNationNames();
        }

        public static List<string> PlayerNationNames()
        {
            List<string> names = new List<string>();
            names.Add("Thomas");
            names.Add("Jean");
            names.Add("Danial");
            names.Add("Dario");
            names.Add("Carey");
            names.Add("Joseph");
            return names;
        }

        public static List<string> AINationNames()
        {
            List<string> names = new List<string>();
            names.Add("Eli");
            names.Add("Vseslav");
            names.Add("Omer");
            names.Add("Teore");
            names.Add("Rapomi");
            names.Add("Cesar");
            names.Add("Rosario");
            return names;
        }

        public static List<string> WizzardNationNames()
        {
            List<string> names = new List<string>();
            names.Add("Mage");
            names.Add("Irwin");
            names.Add("Elias");
            names.Add("Efrain");
            names.Add("Icon");
            return names;
        }
    }

    [System.Serializable]
    public class NationSpawnerDialogsGroup
    {
        public string key = string.Empty;
        public List<RandomDiplomacyTexts> diplomacyTexts = new List<RandomDiplomacyTexts>();
        public List<OurProposalsNode> ourProposals = new List<OurProposalsNode>();
        public List<ProposalNode> theirProposals = new List<ProposalNode>();
        public List<ProposalNodeGroup> ourAnswersToTheirProposals = new List<ProposalNodeGroup>();
        public List<DiplomacyReportUI> diplomacyReports = new List<DiplomacyReportUI>();

        [HideInInspector] public Dictionary<string, RandomDiplomacyTexts> diplomacyTextsByKey = new Dictionary<string, RandomDiplomacyTexts>();
        [HideInInspector] public Dictionary<string, ProposalNode> theirProposalsByActionKey = new Dictionary<string, ProposalNode>();
        [HideInInspector] public Dictionary<string, ProposalNodeGroup> ourAnswersToTheirProposalsByKey = new Dictionary<string, ProposalNodeGroup>();
        [HideInInspector] public Dictionary<string, DiplomacyReportUI> diplomacyReportsByName = new Dictionary<string, DiplomacyReportUI>();

        public void InitializeDictionaries()
        {
            // diplomacy texts
            diplomacyTextsByKey = new Dictionary<string, RandomDiplomacyTexts>();

            for (int i = 0; i < diplomacyTexts.Count; i++)
            {
                RandomDiplomacyTexts diplomacyText = diplomacyTexts[i];

                if (!diplomacyTextsByKey.ContainsKey(diplomacyText.key))
                {
                    diplomacyTextsByKey.Add(diplomacyText.key, diplomacyText);
                }
            }

            // their proposals
            theirProposalsByActionKey = new Dictionary<string, ProposalNode>();

            for (int i = 0; i < theirProposals.Count; i++)
            {
                ProposalNode theirProposal = theirProposals[i];

                if (!theirProposalsByActionKey.ContainsKey(theirProposal.actionKey))
                {
                    theirProposalsByActionKey.Add(theirProposal.actionKey, theirProposal);
                }
            }

            // our answers to their proposals
            ourAnswersToTheirProposalsByKey = new Dictionary<string, ProposalNodeGroup>();

            for (int i = 0; i < ourAnswersToTheirProposals.Count; i++)
            {
                ProposalNodeGroup ourAnswersToTheirProposal = ourAnswersToTheirProposals[i];

                if (!ourAnswersToTheirProposalsByKey.ContainsKey(ourAnswersToTheirProposal.groupKey))
                {
                    ourAnswersToTheirProposalsByKey.Add(ourAnswersToTheirProposal.groupKey, ourAnswersToTheirProposal);
                }
            }

            // diplomacy reports
            diplomacyReportsByName = new Dictionary<string, DiplomacyReportUI>();

            for (int i = 0; i < diplomacyReports.Count; i++)
            {
                if (!diplomacyReportsByName.ContainsKey(diplomacyReports[i].key))
                {
                    diplomacyReportsByName.Add(diplomacyReports[i].key, diplomacyReports[i]);
                }
            }
        }
    }
}
