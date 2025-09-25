using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class BattleSystemCust : MonoBehaviour
{
    public static BattleSystemCust active;

    // Existing lists to hold units
    public List<UnitParsCust> allUnits = new List<UnitParsCust>();
    public List<UnitParsCust> deadUnits = new List<UnitParsCust>();

    public GameObject deadUnitHolder;
    public List<List<UnitParsCust>> targets = new List<List<UnitParsCust>>();
    public List<float> targetRefreshTimes = new List<float>();
    public List<KDTreeCust> targetKD = new List<KDTreeCust>();

    public int randomSeed = 0;
    public float searchUpdateFraction = 1f;  // Control how frequently units search for targets
    public float retargetUpdateFraction = 1f;  // Control retargeting rate
    public float approachUpdateFraction = 1f;  // Control movement phase frequency
    public float attackUpdateFraction = 1f;  // Control attack phase frequency
    public float selfHealUpdateFraction = 1f;  // Control healing frequency
    public float deathUpdateFraction = 1f;  // Control death check frequency

    public GameObject player;
    public UnitParsCust playerUnitPars { get; set; }

    private AIControl aiControl { get; set; }

    public class SpawnLoc
    {
        public int Index { get; set; }
        public Vector3 Location { get; set; }
        public UnitParsCust ObjectToSpawn { get; set; }
        public bool IsArcher { get; set; }
        public bool IsEnemy { get; set; }
        public int UnitRank = 2;
        public int[] UnitsToCommand { get; internal set; }
    }


    void Awake()
    {

        active = this;
        UnityEngine.Random.InitState(randomSeed); // not sure why this needs to be initialized

        playerControl = player.GetComponent<PlayerControl>();
        aiControl = this.GetComponent<AIControl>();
        playerUnitPars = player.GetComponent<UnitParsCust>();

    }





    public UnitParsCust allyGameObjectToSpawn;
    public UnitParsCust enemyGameObjectToSpawn;
    public int unitCount = 100;

    void Start()
    {
        int allyUnitCountAtRankTwo = 0;
        int enemyUnitCountAtRankTwo = 0;
        int unitRank = 2;

        UnitAnimDataCust.Init();  // Initialize animation data
        UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;  // Max nodes processed in pathfinding per frame

        List<SpawnLoc> locations = new List<SpawnLoc>();

        float xAlly = -12;
        float xEnemy = 12;

        List<int> unitsToCommand = new List<int>();
        List<int> unitsToCommandEnemy = new List<int>();

        // Loop through all units to assign spawn locations and behavior
        for (int i = 0; i < unitCount; i++)
        {
            Vector2 randPos = new Vector2(0, UnityEngine.Random.Range(-25f, 25f));
            Vector3 pos = new Vector3(0, randPos.y, 0f);

            UnitParsCust currentGameObject;
            bool isEnemy = false;
            bool isArcher = false;

            // Assign unit to ally or enemy
            if (i % 2 == 0)
            {
                currentGameObject = enemyGameObjectToSpawn;
                currentGameObject.CurrentCommand = "Attack";

                isEnemy = true;
                if (i % 100 == 0) xEnemy++;
                pos.x = xEnemy;
            }
            else
            {
                currentGameObject = allyGameObjectToSpawn;
                currentGameObject.CurrentCommand = "Attack";

                isEnemy = false;
                if (i % 100 == 0) xAlly--;
                pos.x = xAlly;
            }

            //if (i % 3 == 0 && isEnemy)
            //{
            //    isArcher = true;
            //}

            int remainingUnits = (unitCount - i) / 2;
            int maxUnitsToCommand = remainingUnits < 5 ? remainingUnits : 10;

            // Assign rank and update counters
            if ((isEnemy && enemyUnitCountAtRankTwo == maxUnitsToCommand) || (!isEnemy && allyUnitCountAtRankTwo == maxUnitsToCommand))
            {
                unitRank = 1;  // Make this a commander
                if (isEnemy) enemyUnitCountAtRankTwo = 0;
                else allyUnitCountAtRankTwo = 0;
            }
            else
            {
                unitRank = 2;  // Regular unit
                if (isEnemy)
                {
                    unitsToCommandEnemy.Add(i);
                    enemyUnitCountAtRankTwo++;
                }
                else
                {
                    unitsToCommand.Add(i);
                    allyUnitCountAtRankTwo++;
                }
            }

            // Add spawn location
            locations.Add(new SpawnLoc()
            {
                Index = i,
                Location = pos,
                ObjectToSpawn = currentGameObject,
                IsArcher = isArcher,
                IsEnemy = isEnemy,
                UnitRank = unitRank,
                UnitsToCommand = (unitRank == 1) ? (isEnemy ? unitsToCommandEnemy.ToArray() : unitsToCommand.ToArray()) : null
            });

            // Clear command lists if unit is a commander
            if (unitRank == 1)
            {
                if (isEnemy) unitsToCommandEnemy.Clear();
                else unitsToCommand.Clear();
            }
        }

        // Instantiate units and set their properties
        List<UnitParsCust> unitCommanders = new List<UnitParsCust>();
        List<UnitParsCust> units = new List<UnitParsCust>();

        // No need to sort locations if not required by gameplay logic
        foreach (SpawnLoc loc in locations)
        {
            UnitParsCust go = Instantiate(loc.ObjectToSpawn, loc.Location, Quaternion.identity);
            UnitParsCust instanceUp = go.GetComponent<UnitParsCust>();

            if (instanceUp != null)
            {
                if (instanceUp.nation >= DiplomacyCust.active.numberNations)
                {
                    DiplomacyCust.active.AddNation();
                }

                instanceUp.isReady = true;
                instanceUp.IsEnemy = loc.IsEnemy;

                if (loc.IsArcher) instanceUp.UnitType = "Archer";

                instanceUp.UniqueID = loc.Index;
                instanceUp.UnitRank = loc.UnitRank;

                if (loc.UnitRank == 1)
                {
                    instanceUp.UnitsToCommand = loc.UnitsToCommand;
                    unitCommanders.Add(instanceUp);
                }
                else
                {
                    units.Add(instanceUp);
                }

                allUnits.Add(instanceUp);
            }
        }

        // Assign units to their commanders
        foreach (var unitCommander in unitCommanders)
        {
            HashSet<int> commandSet = new HashSet<int>(unitCommander.UnitsToCommand);
            foreach (var unit in units)
            {
                if (commandSet.Contains(unit.UniqueID))
                {
                    unitCommander.SelectedUnitPars.Add(unit);
                }
            }
        }
    }



    //void Start()
    //{
    //    int allyUnitCountAtRankTwo = 0;
    //    int enemyUnitCountAtRankTwo = 0;
    //    int unitRank = 2;

    //    UnitAnimDataCust.Init();  // Initialize animation data
    //    UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;  // Max nodes processed in pathfinding per frame

    //    List<SpawnLoc> locations = new List<SpawnLoc>();

    //    float xAlly = -12;
    //    float xEnemy = 16;

    //    List<int> unitsToCommand = new List<int>();
    //    List<int> unitsToCommandEnemy = new List<int>();

    //    // Loop through all units to assign spawn locations and behavior
    //    for (int i = 0; i < unitCount; i++)
    //    {
    //        Vector2 randPos = new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-5f, 5f));
    //        Vector3 pos = new Vector3(0, randPos.y, 0f);

    //        UnitParsCust currentGameObject;
    //        bool isEnemy = false;
    //        bool isArcher = false;

    //        // Assign unit to ally or enemy
    //        if (i % 2 == 0)
    //        {
    //            currentGameObject = enemyGameObjectToSpawn;
    //            currentGameObject.CurrentCommand = "Attack";
    //            isEnemy = true;
    //            if (i % 25 == 0) xEnemy++;
    //            pos.x = xEnemy;
    //        }
    //        else
    //        {
    //            currentGameObject = allyGameObjectToSpawn;
    //            isEnemy = false;
    //            if (i % 25 == 0) xAlly--;
    //            pos.x = xAlly;
    //        }

    //        if (i % 3 == 0 && isEnemy)
    //        {
    //            isArcher = true;
    //        }

    //        int remainingUnits = (unitCount - i) % 2 == 0 ? (unitCount - i) / 2 : (unitCount - i + 1) / 2;
    //        int maxUnitsToCommand = (remainingUnits < 5) ? (remainingUnits == 0 ? (isEnemy ? enemyUnitCountAtRankTwo : allyUnitCountAtRankTwo) : 10 + remainingUnits) : 10;

    //        // Assign rank and update counters
    //        if ((isEnemy && enemyUnitCountAtRankTwo == maxUnitsToCommand) || (!isEnemy && allyUnitCountAtRankTwo == maxUnitsToCommand))
    //        {
    //            unitRank = 1;
    //            if (isEnemy) enemyUnitCountAtRankTwo = 0;
    //            else allyUnitCountAtRankTwo = 0;
    //        }
    //        else
    //        {
    //            unitRank = 2;
    //            if (isEnemy)
    //            {
    //                unitsToCommandEnemy.Add(i);
    //                enemyUnitCountAtRankTwo++;
    //            }
    //            else
    //            {
    //                unitsToCommand.Add(i);
    //                allyUnitCountAtRankTwo++;
    //            }
    //        }

    //        // Add spawn location
    //        locations.Add(new SpawnLoc()
    //        {
    //            Index = i,
    //            Location = pos,
    //            ObjectToSpawn = currentGameObject,
    //            IsArcher = isArcher,
    //            IsEnemy = isEnemy,
    //            UnitRank = unitRank,
    //            UnitsToCommand = (unitRank == 1) ? (isEnemy ? unitsToCommandEnemy.ToArray() : unitsToCommand.ToArray()) : null
    //        });

    //        // Clear command lists if unit is a commander
    //        if (unitRank == 1)
    //        {
    //            if (isEnemy) unitsToCommandEnemy.Clear();
    //            else unitsToCommand.Clear();
    //        }
    //    }

    //    // Instantiate units and set their properties
    //    List<UnitParsCust> unitCommanders = new List<UnitParsCust>();
    //    List<UnitParsCust> units = new List<UnitParsCust>();
    //    locations = locations.OrderBy(x => x.ObjectToSpawn.nation).ToList();
    //    foreach (SpawnLoc loc in locations)
    //    {
    //        UnitParsCust go = Instantiate(loc.ObjectToSpawn, loc.Location, Quaternion.identity);
    //        UnitParsCust instanceUp = go.GetComponent<UnitParsCust>();

    //        if (instanceUp != null)
    //        {
    //            if (instanceUp.nation >= DiplomacyCust.active.numberNations)
    //            {
    //                DiplomacyCust.active.AddNation();
    //            }

    //            instanceUp.isReady = true;
    //            instanceUp.IsEnemy = loc.IsEnemy;

    //            if (loc.IsArcher) instanceUp.UnitType = "Archer";

    //            instanceUp.UniqueID = loc.Index;
    //            instanceUp.UnitRank = loc.UnitRank;

    //            if (loc.UnitRank == 1)
    //            {
    //                instanceUp.UnitsToCommand = loc.UnitsToCommand;
    //                unitCommanders.Add(instanceUp);
    //            }
    //            else
    //            {
    //                units.Add(instanceUp);
    //            }

    //            allUnits.Add(instanceUp);
    //        }
    //    }

    //    // Assign units to their commanders
    //    foreach (var unitCommander in unitCommanders)
    //    {
    //        foreach (var unit in units)
    //        {
    //            if (unitCommander.UnitsToCommand.Contains(unit.UniqueID))
    //            {
    //                unitCommander.SelectedUnitPars.Add(unit);
    //            }
    //        }
    //    }
    //}

    void Update()
    {
        if (DiplomacyCust.active == null)
        {
            DiplomacyCust.active = new DiplomacyCust();
        }
        UpdateWithoutStatistics();
    }

    float interval = 0.2f;
    float nextTime = 0;

    //batching
    private int unitsProcessed = 0;
    private int batchSize = 20;



    //ifnewcommandisissued
    //else leave these two alone
    bool newCommandIssuedByAIOrPlayer = true; 


    void UpdateWithoutStatistics()
    {
        float deltaTime = Time.deltaTime;

            if (Time.time >= nextTime)
            {
                UnitCommanderReSelectUnits();  // Command phase
                CommandPhase();
                //HoldPhase();
                nextTime += interval;
            } 

        //// Now distribute phases across units
        //SearchPhase_New(deltaTime);
        //RetargetPhase_New(deltaTime);
        //ApproachPhase_New(deltaTime);
        //AttackPhase_New(deltaTime);
        //SelfHealingPhase_New(deltaTime);
        //DeathPhase_New();
        ProcessUnitPhase(deltaTime);
        UpdateAnimation(deltaTime);

    }

    void ProcessUnitPhase(float deltaTime)
    {
        // loop through units in chunks
        for (int i = unitsProcessed; i < unitsProcessed + batchSize && i < allUnits.Count; i++)
        {
            var unit = allUnits[i];



            unit.SearchForTargets(deltaTime);  // Process search phase for each unit
            unit.RetargetIfNeeded(deltaTime);          // Process retargeting
            unit.ApproachPhase(deltaTime);          // Process approach phase
            unit.AttackPhase(deltaTime);            // Process attack phase
            unit.SelfHealing(deltaTime);          // Process self-healing
            unit.HandleDeath();                // Process death phase
        }

        unitsProcessed += batchSize;

        if (unitsProcessed >= allUnits.Count)
        {
            unitsProcessed = 0;
        }


        //for (int i = 0; i < allUnits.Count; i++)
        //{
        //    var unit = allUnits[i];
        //    unit.ApproachPhase(deltaTime);          // Process approach phase
        //    unit.AttackPhase(deltaTime);            // Process attack phase
        //    unit.SelfHealing(deltaTime);          // Process self-healing
        //    unit.HandleDeath();                // Process death phase 
        //}
    }


    void SearchPhase_New(float deltaTime)
    {
        // Manage search logic across all units
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.SearchForTargets(deltaTime);  // Delegate to unit
        }
    }

    void RetargetPhase_New(float deltaTime)
    {
        // Manage retargeting across all units
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.RetargetIfNeeded(deltaTime);  // Delegate to unit
        }
    }

    void ApproachPhase_New(float deltaTime)
    {
        // Manage movement logic
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.ApproachPhase(deltaTime);  // Delegate to unit
        }
    }

    void AttackPhase_New(float deltaTime)
    {
        // Manage attack logic
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.AttackPhase(deltaTime);  // Delegate to unit
        }
    }

    void SelfHealingPhase_New(float deltaTime)
    {
        // Manage healing logic
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.SelfHealing(deltaTime);  // Delegate to unit
        }
    }

    void DeathPhase_New()
    {
        // Manage death logic
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.HandleDeath();  // Delegate to unit
        }
        //// Manage death logic
        //foreach (var unit in allUnits)
        //{
        //    unit.HandleDeath();  // Delegate to unit
        //}
    }

    void UpdateAnimation(float deltaTime)
    {
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            unit.UpdateAnimation(deltaTime);  // Delegate to unit
        }
    }


    //void UpdateWithoutStatistics()
    //{
    //    float deltaTime = Time.deltaTime;
    //    if (Time.time >= nextTime)
    //    {


    //        //refind closes units?
    //        UnitCommanderReSelectUnits();

    //        //command system?
    //        CommandPhase();

    //        //hold phase?
    //        //stop all movement before search even begins
    //        HoldPhase();

    //        //move phase?
    //        //move units to location phase
    //        nextTime += interval;

    //    }
    //    SearchPhase(deltaTime);
    //    //UpdateWithoutStatistics()


    //    RetargetPhase();



    //    ApproachPhase();

    //    AttackPhase();

    //    SelfHealingPhase(deltaTime);
    //    DeathPhase();

    //    //SinkPhase(deltaTime); -- not needed

    //    //ManualMover();





    //    PrepSpriteSheetData();
    //    RenderAnimation();
    //}

    void RunStuff(float deltaTime)
    {

    }


    public int PlayerNation = 1;
    public PlayerControl playerControl { get; set; }




    private List<UnitParsCust> unitCommanders = new List<UnitParsCust>();
    private List<UnitParsCust> unitList = new List<UnitParsCust>();

    private List<int> unitCommanderSet = new List<int>();// (1000, Allocator.Persistent);
    private List<int> unitsToCommandSet = new List<int>();// (1000, Allocator.Persistent);

    // Cache some frequently used data structures
    private List<UnitParsCust> selectedUnitsCache = new List<UnitParsCust>(1000);
    private List<UnitParsCust> unitsToCommandListCache = new List<UnitParsCust>(1000);

    // Handle unit selection/re-selection with caching and reuse
    void UnitCommanderReSelectUnits()
    {
        unitCommanders.Clear();
        unitList.Clear();

        // Categorize units with one pass
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitParsCust unit = allUnits[i];
            if (unit.UnitRank == 1)
            {
                unitCommanders.Add(unit);
            }
            else if (unit.UnitRank == 2)
            {
                unitList.Add(unit);
            }
        }

        // Use pre-allocated set to speed up unit lookups
        unitCommanderSet.Clear();
        foreach (var commander in unitCommanders)
        {
            unitCommanderSet.Add(commander.UniqueID);
        }

        // Only do re-selection if necessary
        foreach (var unitCommander in unitCommanders)
        {
            // If the selected units need to be updated
            if (unitCommander.UnitsToCommand.Length != unitCommander.SelectedUnitPars.Count)
            {
                unitsToCommandListCache.Clear();

                // Precompute units to command with a HashSet lookup
                unitsToCommandSet.Clear();
                foreach (var uniqueID in unitCommander.UnitsToCommand)
                {
                    unitsToCommandSet.Add(uniqueID);
                }

                foreach (var unit in unitList)
                {
                    if (unit.IsEnemy == unitCommander.IsEnemy && unitsToCommandSet.Contains(unit.UniqueID))
                    {
                        unitsToCommandListCache.Add(unit);
                    }
                }

                unitCommander.SelectedUnitPars.AddRange(unitsToCommandListCache);
            }
        }
    }

    // Simplified Command Phase (same optimization principles)
    void CommandPhase()
    {
        unitsToCommandListCache.Clear();

        // Cache all units that are selected by the player or AI
        unitsToCommandListCache.AddRange(playerControl.selectedUnits);
        unitsToCommandListCache.AddRange(aiControl.selectedUnits);

        foreach (var unitCommander in unitCommanders)
        {
            // Avoid redundant checks using the cached data
            if (unitCommander.CurrentCommand != unitCommander.PreviousCommand)
            {
                unitsToCommandListCache.Clear();

                // Process the units with fast lookups
                foreach (var unit in allUnits)
                {
                    if (unitCommander.UnitsToCommand.Contains(unit.UniqueID))
                    {
                        unitsToCommandListCache.Add(unit);
                    }
                }

                if (unitsToCommandListCache.Count > 0)
                {
                    unitCommander.PreviousCommand = unitCommander.CurrentCommand;
                }
            }

            // Handle commands (optimized)
            foreach (var unit in unitsToCommandListCache)
            {
                string commandToFollow = GetCommandForUnit(unit);

                if (unit.CurrentCommand != commandToFollow)
                {
                    unit.CurrentCommand = commandToFollow;
                    unit.PreviousCommand = commandToFollow;

                    ExecuteCommand(unit, commandToFollow);
                }
            }
        }

        // Handle Player's and AI's selected units
        HandlePlayerAndAICommands();
    }

    string GetCommandForUnit(UnitParsCust unit)
    {
        if (unit.nation == PlayerNation)
        {
            return playerControl.PlayerCommand;
        }
        else if (unit.nation == 1)
        {
            return aiControl.CurrentCommand;
        }
        return "Hold";  // Default command
    }

    void ExecuteCommand(UnitParsCust unit, string command)
    {
        // Execute the command efficiently by minimizing condition checks
        switch (command)
        {
            case "Hold":
                unit.isApproachable = true;
                unit.isApproaching = false;
                unit.isAttacking = false;
                unit.isReady = true;
                unit.nma.isStopped = true;
                break;

            case "Move":
            case "Attack":
                unit.nma.isStopped = false;
                break;

            case "Follow":
                unit.nma.isStopped = false;
                unit.isApproaching = true;
                unit.target = unit.IsEnemy ? unitCommanders.FirstOrDefault() : player.GetComponent<UnitParsCust>();
                break;
        }
    }

    // Handle player's and AI's units' command states in a batch
    void HandlePlayerAndAICommands()
    {
        selectedUnitsCache.Clear();
        selectedUnitsCache.AddRange(playerControl.selectedUnits);
        selectedUnitsCache.AddRange(aiControl.selectedUnits);

        foreach (var unit in selectedUnitsCache)
        {
            string command = GetCommandForUnit(unit);

            if (unit.CurrentCommand != command)
            {
                unit.CurrentCommand = command;
                unit.PreviousCommand = command;

                ExecuteCommand(unit, command);
            }
        }
    }

    // Cleanup to avoid memory leaks
    void OnDestroy()
    {
        //unitCommanderSet.Dispose();
        //unitsToCommandSet.Dispose();
    }


void HoldPhase()
    {
        if (allUnits.Count > 0)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                UnitParsCust unit = allUnits[i];

                if (unit.CurrentCommand == "Hold")
                {
                    unit.isApproachable = true;
                    unit.isApproaching = false;
                    unit.isAttacking = false;
                    unit.isReady = true;
                    unit.nma.isStopped = true;
                }

            }
        }
    }

    void MovePhase()
    {

    }


    int iSearchPhase = 0;
    float fSearchPhase = 0f;


    // the main search method, which starts to search for nearest enemies neighbours and set them for attack
    // NN serach works with kdtreecust.cs NN search class, implemented by A. Stark at 2009
    // Target candidates are put on kdtree, while attackers used to search for them.
    // NN searches are based on position coordinates in 3D(2D) 
    void SearchPhase(float deltaTime)
    {
        // refresh targets list
        for (int i = 0; i < targetRefreshTimes.Count; i++)
        {

            targetRefreshTimes[i] -= deltaTime;

            //if its time to refresh target
            if (targetRefreshTimes[i] < 0f)
            {
                targetRefreshTimes[i] = 1f;

                List<UnitParsCust> nationTargets = new List<UnitParsCust>();
                List<Vector3> nationTargetPositions = new List<Vector3>();

                //loop through units
                for (int j = 0; j < allUnits.Count; j++)
                {
                    UnitParsCust up = allUnits[j];


                    //TODO: fix diplomcy being null

                    if (DiplomacyCust.active == null)
                    {
                        DiplomacyCust.active = new DiplomacyCust();
                    }


                    if (
                        up.nation != i && // not sure why this is checking against the target refresh time
                        up.isApproachable &&
                        up.health > 0f && // if still alive
                        up.attackers.Count < up.maxAttackers // if not reach trhe max attackers
                        && DiplomacyCust.active.relations[up.nation][i] == 1 //- I need to manage my own ally vs enemy 
                        )
                    {
                        nationTargets.Add(up);
                        nationTargetPositions.Add(up.transform.position);
                    }
                }


                targets[i] = nationTargets;
                targetKD[i] = KDTreeCust.MakeFromPoints(nationTargetPositions.ToArray());
            }
        }

        fSearchPhase += allUnits.Count * searchUpdateFraction;

        int nToLoop = (int)fSearchPhase;
        fSearchPhase -= nToLoop;

        for (int i = 0; i < allUnits.Count; i++)
        {
            //iSearchPhase++;

            //if (iSearchPhase >= allUnits.Count)
            //{
            //    iSearchPhase = 0;
            //}

            UnitParsCust up = allUnits[i];
            int nation = up.nation;

            if (up.isReady && targets[nation].Count > 0 && (new List<string> { "Attack" }).Contains(up.CurrentCommand))
            {
                int targetId = targetKD[nation].FindNearest(up.transform.position);
                UnitParsCust targetUp = targets[nation][targetId];

                if (
                    targetUp.health > 0f &&
                    targetUp.attackers.Count < targetUp.maxAttackers
                    )
                {
                    targetUp.attackers.Add(up);
                    targetUp.noAttackers = targetUp.attackers.Count;
                    up.target = targetUp;


                    var direction = targetUp.transform.position - up.transform.position;
                    up.direction = direction;

                    up.isReady = false;
                    up.isApproaching = true;
                }
            }

        }
    }

    int iRetargetPhase = 0;
    float fRetargetPhase = 0f;

    //similar to searchphas but is used to retarget approachers to closer targets
    public void RetargetPhase()
    {
        fRetargetPhase += allUnits.Count * retargetUpdateFraction;

        int nToLoop = (int)fRetargetPhase;
        fRetargetPhase -= nToLoop;

        for (int i = 0; i < allUnits.Count; i++)
        {
            //iRetargetPhase++;

            //if (iRetargetPhase >= allUnits.Count)
            //{
            //    iRetargetPhase = 0;
            //}

            UnitParsCust up = allUnits[i];
            int nation = up.nation;

            if (up.isApproaching && up.target != null && targets[nation].Count > 0 && up.CurrentCommand != "Follow")
            {
                int targetId = targetKD[nation].FindNearest(up.transform.position);
                UnitParsCust targetUp = targets[nation][targetId];

                if (
                    targetUp.health > 0f &&
                    targetUp.attackers.Count < targetUp.maxAttackers
                    )
                {
                    float oldTargetDistanceSq = (up.target.transform.position - up.transform.position).sqrMagnitude;
                    float newTargetDistanceSq = (targetUp.transform.position - up.transform.position).sqrMagnitude;

                    if (newTargetDistanceSq < oldTargetDistanceSq)
                    {
                        up.target.attackers.Remove(up);
                        up.target.noAttackers = up.target.attackers.Count;

                        targetUp.attackers.Add(up);
                        targetUp.noAttackers = targetUp.attackers.Count;
                        up.target = targetUp;
                        up.isReady = false;
                        up.isApproaching = true;
                    }
                }
            }
        }
    }

    int iApproachPhase = 0;
    float fApproachPhase = 0f;

    // this phase starting attackers to move towards their targets
    public void ApproachPhase()
    {

        fApproachPhase += allUnits.Count * approachUpdateFraction;

        int nToLoop = (int)fApproachPhase;
        fApproachPhase -= nToLoop;

        // checking through allUnits list which units are set to approach (isApproaching)
        for (int i = 0; i < allUnits.Count; i++)
        {
            //iApproachPhase++;


            //if (iApproachPhase >= allUnits.Count)
            //{
            //    iApproachPhase = 0;
            //}

            UnitParsCust apprPars = allUnits[i];


            if (apprPars.isApproaching && apprPars.target != null)
            {

                UnitParsCust targ = apprPars.target;

                UnityEngine.AI.NavMeshAgent apprNav = apprPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                UnityEngine.AI.NavMeshAgent targNav = targ.GetComponent<UnityEngine.AI.NavMeshAgent>();

                if (targ.isApproachable == true)
                {

                    //stop condition for navmesh

                    apprNav.stoppingDistance = .25f;// apprNav.radius / (apprPars.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                    // distance between approacher and target

                    float rTarget = (apprPars.transform.position - targ.transform.position).magnitude;
                    float stoppDistance = (apprPars.transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);


                    //if (apprPars.UnitType == "Archer")
                    //{
                    //    if (CanHitCoordinate(apprPars.transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f) == true)
                    //    {
                    //        stoppDistance = 1.25f*rTarget;
                    //    }
                    //    else
                    //    {
                    //        stoppDistance = 0f;
                    //    }
                    //}

                    // counting increased distances (failure to approch) between attacker and target
                    // if counter failedR becomes bigger than critFailedR, preparing for new target search
                    // basically what I was tring to do to stop units from targeting one until they reach it or die


                    //round?
                    var roundedPrevR = float.Parse(apprPars.prevR.ToString("0.0"));
                    var roundedrTarget = float.Parse(rTarget.ToString("0.0"));


                    //if (apprPars.prevR <= rTarget)
                    if (roundedPrevR < roundedrTarget)
                    {
                        apprPars.failedR = apprPars.failedR + 1;
                        if (apprPars.failedR > apprPars.critFailedR)
                        {
                            apprPars.isApproaching = false;
                            apprPars.isReady = true;
                            apprPars.failedR = 0;

                            //if target reset target to find new targvet
                            if (apprPars.target != null)
                            {
                                if (apprPars.CurrentCommand == "Follow")
                                {
                                    apprPars.isApproaching = true;
                                }
                                else
                                {
                                    apprPars.target.attackers.Remove(apprPars);
                                    apprPars.target.noAttackers = apprPars.target.attackers.Count;
                                    apprPars.target = null;
                                }
                            }

                        }
                    }
                    else
                    {



                        if (apprPars.UnitType == "Archer")
                        {
                            if (CanHitCoordinate(apprPars.transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f) == true)
                            {
                                stoppDistance = 1.25f * rTarget;
                            }
                            else
                            {
                                stoppDistance = rTarget;
                            }
                        }

                        // if approachers already close to their targets
                        if (rTarget < stoppDistance)
                        {
                            if (apprPars.CurrentCommand == "Attack")
                            {
                                apprNav.SetDestination(new Vector3(apprPars.transform.position.x, apprPars.transform.position.y, 0f));

                                //TODO: get coorect direction for arhcers
                                var direction = apprNav.destination - apprPars.transform.position;
                                apprPars.direction = direction;
                                // pre-setting for attacking
                                apprPars.isApproaching = false;
                                apprPars.isAttacking = true;
                                apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
                            }
                            else //if following
                            {
                                apprNav.SetDestination(new Vector3(apprPars.transform.position.x, apprPars.transform.position.y, 0f));

                                //TODO: get coorect direction for arhcers
                                var direction = apprNav.destination - apprPars.transform.position;
                                apprPars.direction = direction;
                                // pre-setting for attacking
                                apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
                            }
                        }
                        else
                        {

                            // starting to move
                            if (apprPars.isMovable)
                            {
                                Vector3 destination = apprNav.destination;
                                if ((destination - targ.transform.position).sqrMagnitude > .125f && apprPars.UnitType != "Archer"
                                    || (apprPars.UnitType == "Archer" && (destination - targ.transform.position).sqrMagnitude > .125f &&
                                    CanHitCoordinate(apprPars.transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f) != true))
                                {
                                    apprNav.SetDestination(new Vector3(targ.transform.position.x, targ.transform.position.y, 0f));
                                    var direction = targ.transform.position - apprPars.transform.position;
                                    apprPars.direction = direction;


                                    //store last known movement direction for dying ?
                                    apprPars.lastDirection = direction;

                                    var rand = UnityEngine.Random.Range(0f, .55f);


                                    //random frame start on run
                                    if (apprPars.playAnimationCust.baseAnimType != UnitAnimDataCust.BaseAnimMaterialType.Run)
                                    {
                                        apprPars.randomFrame = true;
                                    }


                                    apprNav.speed = 1f + rand;
                                    apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, direction, default);


                                }
                            }
                        }
                    }

                    //savubg previous R
                    apprPars.prevR = rTarget;
                }
                // condition for non approachable targets
                else
                {
                    apprPars.target = null;
                    apprNav.SetDestination(new Vector3(apprPars.transform.position.x, apprPars.transform.position.y, 0f));
                    //apprPars.isApproachable = false; -- this was making units stop moving after a while?
                    apprPars.isReady = true;
                    //apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, direction, default);
                }
            }


            else
            {

                //TODO: retarget?
                //TODO: this should be rewritten?


                //check if should be idle
                //if there are any units still alive, then keep moving else set to idle
                bool beIdle = (apprPars.IsEnemy && allUnits.Any(x => !x.IsEnemy)) || (!apprPars.IsEnemy && allUnits.Any(x => x.IsEnemy)) ? false : true;


                if (!beIdle)
                {
                    apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, apprPars.direction, default);

                    UnityEngine.AI.NavMeshAgent apprNav = apprPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    apprNav.SetDestination(new Vector3(apprPars.transform.position.x, apprPars.transform.position.y, 0f));
                }
            }

        }
    }

    int iAttackPhase = 0;
    float fAttackPhase = 0f;

    // attacking phase set attackers to attack their targets and cause damage when they alreayd appr    oached their targets

    public void AttackPhase()
    {
        fAttackPhase += allUnits.Count * attackUpdateFraction;

        int nToLoop = (int)fAttackPhase;
        fAttackPhase -= nToLoop;

        // checking through allUnits list which units are set to approach(isAttacking)
        for (int i = 0; i < allUnits.Count; i++)
        {
            //iAttackPhase++;

            //if (iAttackPhase >= allUnits.Count)
            //{
            //    iAttackPhase = 0;
            //}

            UnitParsCust attPars = allUnits[i];

            if (attPars.isAttacking && attPars.tag != null && attPars.target != null)
            {
                UnitParsCust targPars = attPars.target;

                UnityEngine.AI.NavMeshAgent attNav = attPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                UnityEngine.AI.NavMeshAgent targNav = targPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                //Debug.Log(attPars.transform.localScale.x);
                attNav.stoppingDistance = attNav.radius / (attPars.transform.localScale.x) + targNav.radius / (targPars.transform.localScale.x);

                // distance between attacker and target

                float rTarget = (attPars.transform.position - targPars.transform.position).magnitude;
                float stoppDistance = (2.5f + attPars.transform.localScale.x * targPars.transform.localScale.x * attNav.stoppingDistance);

                //archer
                if (attPars.UnitType == "Archer")
                {
                    if (CanHitCoordinate(attPars.transform.position, targPars.transform.position, Vector3.zero, 20.0f, 0.4f) == true)
                    {
                        stoppDistance = 1.25f * rTarget;
                    }
                    else
                    {
                        stoppDistance = rTarget;
                    }
                }


                // if target moves away, reset back to approach target phase
                if (rTarget > stoppDistance)
                {
                    attPars.isApproaching = true;
                    attPars.isAttacking = false;
                }
                // if target becomes immune, attacker is reset to start searching for new target
                else if (targPars.isImmune == true)
                {
                    attPars.isAttacking = false;
                    attPars.isReady = true;

                    targPars.attackers.Remove(attPars);
                    targPars.noAttackers = targPars.attackers.Count;
                }
                // attacker starts attking their target
                // TODO: figure out how to trigger attack animation here
                else
                {
                    float strength = attPars.strength;
                    float defence = attPars.defence;

                    // if attack passes target through target defence, cause damage to target
                    if (Time.time > attPars.nextAttack)
                    {
                        attPars.nextAttack = Time.time + (attPars.attackRate - attPars.randomAttackRange);

                        if (UnityEngine.Random.value > (strength / (strength + defence)))
                        {
                            if (attPars.UnitType == "Archer")
                            {
                                attPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Shoot45, attPars.direction, default);
                                attPars.LaunchArrowDelay(targPars, attPars.transform.position);
                            }
                            else
                            {
                                attPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Attack, attPars.direction, default);

                                //move to target script?
                                targPars.health = targPars.health - (10f + UnityEngine.Random.Range(0f, 15f));// targPars.health - 2.0f * strength * Random.value;

                            }
                        }
                    }
                    //else
                    //{
                    //    //defend?
                    //    attPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, attPars.direction, default);
                    //}
                }




            }

        }

    }

    // finish later

    int iSelfHealingPhase = 0;
    float fSelfHealingPhase = 0f;
    void SelfHealingPhase(float deltaTime)
    {
        //fSelfHealingPhase += allUnits.Count * selfHealUpdateFraction;

        //int nToLoop = (int)fSelfHealingPhase;
        //fSelfHealingPhase -= nToLoop;
        // checking which units are damaged	
        for (int i = 0; i < allUnits.Count; i++)
        {
            //iSelfHealingPhase++;

            //if (iSelfHealingPhase >= allUnits.Count)
            //{
            //    iSelfHealingPhase = 0;
            //}

            UnitParsCust shealPars = allUnits[i];

            if (shealPars.health < shealPars.maxHealth)
            {
                // if unit has less health than 0, preparing it to die
                if (shealPars.health < 0f)
                {
                    shealPars.isHealing = false;
                    shealPars.isImmune = true;
                    shealPars.isDying = true;
                }
                //// healing unit	
                //else
                //{
                //    shealPars.isHealing = true;
                //    shealPars.health += shealPars.selfHealFactor * deltaTime / selfHealUpdateFraction;

                //    // if unit health reaches maximum, unset self-healing
                //    if (shealPars.health >= shealPars.maxHealth)
                //    {
                //        shealPars.health = shealPars.maxHealth;
                //        shealPars.isHealing = false;
                //    }
                //}
            }
        }
    }

    int iDeathPhase = 0;
    float fDeathPhase = 0f;

    // Death phase unest all unit activity and prepare to die

    void DeathPhase()
    {


        //// fix target refhres times
        //fDeathPhase += allUnits.Count * deathUpdateFraction;

        //int nToLoop = (int)fDeathPhase;
        //fDeathPhase -= nToLoop;

        //for (int i = 0; i < allUnits.Count; i++)
        //{
        //    iDeathPhase++;

        //    if (iDeathPhase >= allUnits.Count)
        //    {
        //        iDeathPhase = 0;
        //    }
        //    for (int j = 0; j < targetRefreshTimes.Count; j++)
        //    {
        //        targetRefreshTimes[j] = -1f;
        //    }
        //}







        //fDeathPhase += allUnits.Count * deathUpdateFraction;

        //int nToLoop = (int)fDeathPhase;
        //fDeathPhase -= nToLoop;

        //get dying units
        for (int i = 0; i < allUnits.Count; i++)
        {
            //iDeathPhase++;

            //if (iDeathPhase >= allUnits.Count)
            //{
            //    iDeathPhase = 0;
            //}

            UnitParsCust deadPars = allUnits[i];

            if (deadPars.isDying)
            {



                // if unit is dead lon enough, prepare for rotting phase from the unit list
                // TODO: need to find a way to keep sprite and merge with others to create bigger sprite
                if (deadPars.deathCalls > deadPars.maxDeathCalls)
                {

                    deadPars.isDying = false;
                    deadPars.isSinking = true;

                    deadPars.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                    //sink.Add(deadPars);
                    allUnits.Remove(deadPars);

                    deadUnits.Add(deadPars);

                    //for (int j = 0; j < targetRefreshTimes.Count; j++)
                    //{
                    //    targetRefreshTimes[j] = -1f;
                    //}
                }
                ////unsetting unit activity and keep it dying
                else
                {

                    deadPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Die, deadPars.lastDirection, default);
                    deadPars.isMovable = false;
                    deadPars.isReady = false;
                    deadPars.isApproaching = false;
                    deadPars.isAttacking = false;
                    deadPars.isApproachable = false;
                    deadPars.isHealing = false;
                    deadPars.target = null;

                    // unselecting deads
                    // TODO: finish adding this
                    ManualControlCust manualControl = deadPars.GetComponent<ManualControlCust>();

                    if (manualControl != null)
                    {
                        manualControl.isSelected = false;
                        //UnitControls.active.Refresh();
                        //TODO: add this
                    }

                    deadPars.transform.gameObject.transform.tag = "Untagged";

                    UnityEngine.AI.NavMeshAgent nma = deadPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    nma.SetDestination(deadPars.transform.position);
                    //var direction = apprPars.transform.position - apprNav.destination;
                    //apprPars.direction = direction;
                    nma.avoidancePriority = 0;

                    deadPars.deathCalls++;


                }
            }
        }
    }


    int iPrepSpriteSheetDataPhase = 0;
    float fPrepSpriteSheetDataPhase = 0f;

    public float prepSpriteSheetDataFraction = 1f;

    private void PrepSpriteSheetData()
    {


        //fPrepSpriteSheetDataPhase += allUnits.Count * prepSpriteSheetDataFraction;

        //int nToLoop = (int)fPrepSpriteSheetDataPhase;
        //fPrepSpriteSheetDataPhase -= nToLoop;

        if (allUnits.Count > 0)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {

                //iPrepSpriteSheetDataPhase++;

                //if (iPrepSpriteSheetDataPhase >= allUnits.Count)
                //{
                //    iPrepSpriteSheetDataPhase = 0;
                //}

                UnitParsCust prepSheetUnitPars = allUnits[i];
                if (prepSheetUnitPars.IsEnemy)
                {

                }
                //play animations
                if (prepSheetUnitPars.playAnimationCust.forced)
                {
                    prepSheetUnitPars.spriteSheetData = UnitAnimationCust.PlayAnimForced(/*ref prepSheetUnitPars, */prepSheetUnitPars.playAnimationCust.baseAnimType, prepSheetUnitPars.playAnimationCust.animDir, prepSheetUnitPars.playAnimationCust.onComplete
                                                                                         , prepSheetUnitPars.UnitType, prepSheetUnitPars.IsEnemy);

                }
                else
                {
                    SpriteSheetAnimationDataCust currSpriteSheetData = prepSheetUnitPars.spriteSheetData;
                    SpriteSheetAnimationDataCust? newSpriteSheetData = UnitAnimationCust.PlayAnim(/*ref prepSheetUnitPars, */prepSheetUnitPars.playAnimationCust.baseAnimType, currSpriteSheetData, prepSheetUnitPars.playAnimationCust.animDir, prepSheetUnitPars.playAnimationCust.onComplete
                                                                                                  , prepSheetUnitPars.UnitType, prepSheetUnitPars.IsEnemy);

                    // if changes
                    if (newSpriteSheetData != null)
                    {
                        prepSheetUnitPars.spriteSheetData = newSpriteSheetData.Value;
                    }
                }
            }
        }




        #region Player
        // player
        if (playerUnitPars.playAnimationCust.forced)
        {
            //TODO add walking/run logic
            playerUnitPars.spriteSheetData = UnitAnimationCust.PlayAnimForced(/*ref prepSheetUnitPars, */playerUnitPars.playAnimationCust.baseAnimType, playerUnitPars.playAnimationCust.animDir, playerUnitPars.playAnimationCust.onComplete
                                                                             , playerUnitPars.UnitType, playerUnitPars.IsEnemy);

        }
        else
        {
            SpriteSheetAnimationDataCust currSpriteSheetData = playerUnitPars.spriteSheetData;
            //TODO: add idle logic
            SpriteSheetAnimationDataCust? newSpriteSheetData = UnitAnimationCust.PlayAnim(/*ref prepSheetUnitPars, */playerUnitPars.playAnimationCust.baseAnimType, currSpriteSheetData, playerUnitPars.playAnimationCust.animDir, playerUnitPars.playAnimationCust.onComplete
                                                                                        , playerUnitPars.UnitType, playerUnitPars.IsEnemy);

            // if changes
            if (newSpriteSheetData != null)
            {
                playerUnitPars.spriteSheetData = newSpriteSheetData.Value;
            }
        }

        #endregion
    }


    public float renderAnimationFraction = 1f;

    private void RenderAnimation()
    {

        //fRenderAnimationPhase += allUnits.Count * renderAnimationFraction;

        //int nToLoop = (int)fRenderAnimationPhase;
        //fRenderAnimationPhase -= nToLoop;

        if (allUnits.Count > 0)
        {
            var deltaTime = Time.deltaTime;
            ///
            for (int i = 0; i < allUnits.Count; i++)
            {


                UnitParsCust renderAnimationParsCust = allUnits[i];

                List<Material> springAttractFrames = new List<Material>();

                MeshRenderer springAttractScreenRend = renderAnimationParsCust.springAttractScreenRend;

                var spriteSheetAnimationData = renderAnimationParsCust.spriteSheetData;



                //random frame
                if (renderAnimationParsCust.randomFrame == true)
                {
                    renderAnimationParsCust.spriteSheetData.currentFrame = UnityEngine.Random.Range(0, renderAnimationParsCust.spriteSheetData.frameCount);
                    renderAnimationParsCust.randomFrame = false;
                }


                renderAnimationParsCust.spriteSheetData.frameTimer -= deltaTime;
                while (renderAnimationParsCust.spriteSheetData.frameTimer < 0)
                {
                    renderAnimationParsCust.spriteSheetData.frameTimer += renderAnimationParsCust.spriteSheetData.frameRate;
                    renderAnimationParsCust.spriteSheetData.currentFrame = ((renderAnimationParsCust.spriteSheetData.currentFrame + 1) % renderAnimationParsCust.spriteSheetData.frameCount);

                    if (renderAnimationParsCust.spriteSheetData.currentFrame >= (renderAnimationParsCust.spriteSheetData.frameCount))
                    {
                        renderAnimationParsCust.spriteSheetData.loopCount++;
                    }

                    springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;

                    Material[] newMats = null;
                    try
                    {
                        newMats = new Material[] { springAttractFrames[renderAnimationParsCust.spriteSheetData.currentFrame] };
                    }
                    catch (System.Exception)
                    {

                        throw;
                    }

                    //if (renderAnimationParsCust.UnitRank == 1)
                    //{
                    //    newMats[0].color = Color.Lerp(renderAnimationParsCust.springAttractScreenRend.materials[0].color, Color.red, .25f);
                    //    //}
                    //    //else
                    //    //{
                    //    //    newMats[0].color = Color.Lerp(renderAnimationParsCust.springAttractScreenRend.materials[0].color, Color.yellow, .25f);
                    //    //Debug.Log(newMats[0].color);

                    //}
                    //else
                    //{
                    //  newMats[0].color = new Color(1.000f, 1.000f, 1.000f, 1.000f);
                    //}
                    renderAnimationParsCust.springAttractScreenRend.materials = newMats;

                }

            }
        }



        if (deadUnits.Count > 0)
        {
            var deltaTime = Time.deltaTime;
            ///
            for (int i = 0; i < deadUnits.Count; i++)
            {


                UnitParsCust renderAnimationParsCust = deadUnits[i];

                List<Material> springAttractFrames = new List<Material>();

                MeshRenderer springAttractScreenRend = renderAnimationParsCust.springAttractScreenRend;
                //float springAttractFrameRefTime = renderAnimationParsCust.springAttractFrameRefTime;
                var spriteSheetAnimationData = renderAnimationParsCust.spriteSheetData;



                //cancel if deadtimer ?
                if (renderAnimationParsCust.spriteSheetData.currentFrame == renderAnimationParsCust.spriteSheetData.frameCount - 1)
                {
                    deadUnits.Remove(renderAnimationParsCust);
                    renderAnimationParsCust.springAttractScreenRend.materials[0].color =
                                            Color.Lerp(renderAnimationParsCust.springAttractScreenRend.materials[0].color, Color.black, .25f);
                    renderAnimationParsCust.springAttractScreenRend.sortingOrder = 9999997;
                    renderAnimationParsCust.transform.position = new Vector3(renderAnimationParsCust.transform.position.x, renderAnimationParsCust.transform.position.y, .01f);
                    renderAnimationParsCust.gameObject.transform.localPosition = new Vector3(renderAnimationParsCust.gameObject.transform.position.x, renderAnimationParsCust.gameObject.transform.position.y, 0);
                    renderAnimationParsCust.gameObject.transform.SetParent(deadUnitHolder.transform);
                    //Object.Destroy(renderAnimationParsCust.springAttractScreenRend);
                    UnityEngine.Object.Destroy(renderAnimationParsCust.nma);
                    //Object.Destroy(renderAnimationParsCust.GetComponent<MeshFilter>());
                    UnityEngine.Object.Destroy(renderAnimationParsCust);
                    return;
                }


                renderAnimationParsCust.spriteSheetData.frameTimer -= deltaTime;
                while (renderAnimationParsCust.spriteSheetData.frameTimer < 0)
                {
                    renderAnimationParsCust.spriteSheetData.frameTimer += renderAnimationParsCust.spriteSheetData.frameRate;
                    renderAnimationParsCust.spriteSheetData.currentFrame = ((renderAnimationParsCust.spriteSheetData.currentFrame + 1) % renderAnimationParsCust.spriteSheetData.frameCount);// + renderAnimationParsCust.spriteSheetData.horizontalCount;

                    if (renderAnimationParsCust.spriteSheetData.currentFrame >= (renderAnimationParsCust.spriteSheetData.frameCount))
                    {
                        renderAnimationParsCust.spriteSheetData.loopCount++;
                    }

                    springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;

                    Material[] newMats = { springAttractFrames[renderAnimationParsCust.spriteSheetData.currentFrame] };
                    renderAnimationParsCust.springAttractScreenRend.materials = newMats;

                }

            }
        }



        #region PLayer

        List<Material> springAttractFramesPlayer = new List<Material>();
        var deltaTime2 = Time.deltaTime;
        playerUnitPars.spriteSheetData.frameTimer -= deltaTime2;
        while (playerUnitPars.spriteSheetData.frameTimer < 0)
        {
            playerUnitPars.spriteSheetData.frameTimer += .1f;// playerUnitPars.spriteSheetData.frameRate;
            playerUnitPars.spriteSheetData.currentFrame = ((playerUnitPars.spriteSheetData.currentFrame + 1) % playerUnitPars.spriteSheetData.frameCount);// + playerUnitPars.spriteSheetData.horizontalCount;

            if (playerUnitPars.spriteSheetData.currentFrame >= (playerUnitPars.spriteSheetData.frameCount))
            {
                playerUnitPars.spriteSheetData.loopCount++;
            }




            springAttractFramesPlayer = playerUnitPars.spriteSheetData.materials;



            Material[] newMats = { springAttractFramesPlayer[playerUnitPars.spriteSheetData.currentFrame] };
            playerUnitPars.springAttractScreenRend.materials = newMats;

        }
        #endregion

    }




    void ManualMover()
    {
        throw new System.NotImplementedException();
    }

    public void AddNation()
    {
        targets.Add(new List<UnitParsCust>());
        targetRefreshTimes.Add(-1f);
        targetKD.Add(null);
    }

    //TODO change this
    public bool CanHitCoordinate(Vector3 shooterPosition, Vector3 targetPosition, Vector3 targetVolocity, float launchSpeed, float distanceIncrement)
    {


        //TODO: DO A  CHECK ON WETHER WE ARE PERPENDICULAR OR NOT

        bool canHit = false;

        float vini = launchSpeed;
        float g = 9.81f;


        Vector3 shootPosition2d = new Vector3(shooterPosition.x, shooterPosition.y, 0);
        Vector3 targetPosition2d = new Vector3(targetPosition.x, targetPosition.y, 0);

        float rTarget2d = (targetPosition2d - shootPosition2d).magnitude;
        rTarget2d = rTarget2d + distanceIncrement * rTarget2d;
        float sqrt = (vini * vini * vini * vini) - (g * (g * (rTarget2d * rTarget2d) + 2 * (targetPosition.y - shooterPosition.y) * (vini * vini)));

        if (/*sqrt >= 0 &&*/
            ((shootPosition2d.x <= targetPosition.x + 1 && shootPosition2d.x >= targetPosition2d.x - 1)
             || (shootPosition2d.y <= targetPosition2d.y + 1 && shootPosition2d.y >= targetPosition2d.y - 1)))
        {
            canHit = true;
        }

        return canHit;
    }



    public void LaunchArrow(UnitParsCust attPars, UnitParsCust targPars, Vector3 launchPoint)
    {
        if ((attPars != null) && (targPars != null))
        {
            LaunchArrowInner(attPars, targPars, launchPoint, false);

        }
    }

    public void LaunchArrowInner(UnitParsCust attPars, UnitParsCust targPars, Vector3 launchPoint1, bool isCosmetic)
    {
        Quaternion rot = new Quaternion(0f, 0.0f, 0.0f, 0.0f);
        Vector3 launchPoint = launchPoint1 + Vector3.zero;


        if (attPars != null && targPars != null)
        {
            Vector3 arrForce2 = LaunchDirection(launchPoint, targPars.transform.position, targPars.velocityVector, attPars.unitParsTypeCust.velArrow);
            float failureError = 0f;

            if (attPars.unitParsTypeCust.arrow != null)
            {
                ArrowParsCust arp = attPars.unitParsTypeCust.arrow.GetComponent<ArrowParsCust>();
                if (arp != null)
                {

                }
            }



            float magBeforeError = arrForce2.magnitude;


            var rand = UnityEngine.Random.insideUnitSphere;
            arrForce2 = arrForce2 + new Vector3(rand.x, rand.y, 0) * arrForce2.magnitude * failureError;
            arrForce2 = arrForce2.normalized * magBeforeError;

            arrForce2.z = 0f;
            if ((arrForce2.sqrMagnitude > 0.0f) && (arrForce2.y != -Mathf.Infinity) && (arrForce2.y != Mathf.Infinity))
            {
                if (attPars.unitParsTypeCust.arrow != null)
                {
                    GameObject arroww = (GameObject)Instantiate(attPars.unitParsTypeCust.arrow, launchPoint, rot);
                    //arroww.GetComponent<>

                    ArrowParsCust arrPars = arroww.GetComponent<ArrowParsCust>();

                    if (arrPars != null)
                    {
                        arrPars.attPars = attPars;
                        arrPars.targPars = targPars;



                        //set random tolerance
                        Vector3 posWithTolerance = targPars.transform.position;
                        posWithTolerance.x = posWithTolerance.x + UnityEngine.Random.Range(-.2f, .2f);
                        posWithTolerance.y = posWithTolerance.y + UnityEngine.Random.Range(-.2f, .2f);

                        arrPars.targetPos = posWithTolerance;
                        arrPars.Init(1.5f, arrForce2);

                    }
                }
            }

        }


    }

    public Vector3 LaunchDirection(Vector3 shooterPosition, Vector3 targetPosition, Vector3 targetVelocity, float launchSpeed)
    {
        float vini = launchSpeed;


        // horizontal plane projections	
        Vector3 shooterPosition2d = new Vector3(shooterPosition.x, 0f, shooterPosition.z);
        Vector3 targetPosition2d = new Vector3(targetPosition.x, 0f, targetPosition.z);

        float Rtarget2d = (targetPosition2d - shooterPosition2d).magnitude;

        //shooter and target coordinates
        float ax = shooterPosition.x;
        float ay = shooterPosition.y;
        float az = 0;

        float tx = targetPosition.x;
        float ty = targetPosition.y;
        float tz = 0;

        float g = 9.81f;

        float sqrt = (vini * vini * vini * vini) - (g * (g * (Rtarget2d * Rtarget2d) + 2 * (ty - ay) * (vini * vini)));
        sqrt = Mathf.Sqrt(sqrt);

        float angleInRadians = Mathf.Atan((vini * vini + sqrt) / (g * Rtarget2d));
        float angleInDegrees = angleInRadians * Mathf.Rad2Deg;

        if (angleInDegrees > 45f)
        {
            angleInDegrees = 90f - angleInDegrees;
        }

        if (angleInDegrees < 0f)
        {
            angleInDegrees = -angleInDegrees;
        }

        Vector3 rotAxis = Vector3.Cross((targetPosition - shooterPosition), new Vector3(0f, 1f, 0f));
        Vector3 arrForce = (GenericMath.RotAround(-angleInDegrees, (targetPosition - shooterPosition), rotAxis)).normalized;

        // shoting time
        float shTime = Mathf.Sqrt(
            ((tx - ax) * (tx - ax) + (tz - az) * (tz - az)) /
            ((vini * arrForce.x) * (vini * arrForce.x) + (vini * arrForce.z) * (vini * arrForce.z))
        );

        Vector3 finalDirection = vini * arrForce + 0.5f * shTime * targetVelocity;
        return finalDirection;
    }
}

