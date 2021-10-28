using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSToolkit;
using System;

public class BattleSystemCust : MonoBehaviour
{

    /// <summary>
    /// sample t for arrow time
    /// </summary>
    [SerializeField]
    public float t = 1f;

    public static BattleSystemCust active;

    public List<UnitParsCust> allUnits = new List<UnitParsCust>();

    public List<UnitParsCust> deadUnits = new List<UnitParsCust>();

    public GameObject deadUnitHolder;

    //i dont need the sinks

    public List<List<UnitParsCust>> targets = new List<List<UnitParsCust>>();

    public List<float> targetRefreshTimes = new List<float>();
    public List<KDTreeCust> targetKD = new List<KDTreeCust>();

    public int randomSeed = 0;

    public float searchUpdateFraction = 0.1f;
    public float retargetUpdateFraction = 0.01f;
    public float approachUpdateFraction = 0.1f;
    public float attackUpdateFraction = 0.1f;
    public float selfHealUpdateFraction = 0.01f;
    public float deathUpdateFraction = 0.05f;
    //public float sinkUpdateFraction = 1f; -- not needed

    // Start is called before the first frame update



    public GameObject player;

    public UnitParsCust playerUnitPars { get; set; }

    void Awake()
    {
        active = this;
        UnityEngine.Random.InitState(randomSeed); // not sure why this needs to be initialized

        playerControl = player.GetComponent<PlayerControl>();
        aiControl = this.GetComponent<AIControl>();
        playerUnitPars = player.GetComponent<UnitParsCust>();

    }

    public GameObject allyGameObjectToSpawn;
    public GameObject enemyGameObjectToSpawn;

    //enemy archer
    //public GameObject objectToSpawn3;


    public int unitCount = 1000;

    public int PlayerNation = 1;
    public PlayerControl playerControl { get; set; }

    private AIControl aiControl { get; set; }

    public class SpawnLoc
    {
        public int Index { get; set; }
        public Vector3 Location { get; set; }
        public GameObject  ObjectToSpawn{ get; set; }
        public bool IsArcher { get; set; }
        public bool IsEnemy { get; set; }
    }

    void Start()
    {


        //load resources
        UnitAnimDataCust.Init();


        //maximum amount of nodes processed each frame in pathfinding process
        UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;

        List<SpawnLoc> locations = new List<SpawnLoc>();// new Tuple<int, Vector3, bool>();

        float xAlly = -12;
        float xEnemy = 2;
        for (int i = 0; i < unitCount; i++)
        {
            Vector2 randPos = new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-5f, 5f));//Random.insideUnitCircle * 10;
            Vector3 pos = new Vector3(0, randPos.y, 0f);

            GameObject currentGameObject;
            bool isEnemy = false;
            bool isArcher = false;
            if (i % 2 == 0)
            {
                currentGameObject = enemyGameObjectToSpawn;
                isEnemy = true;
                if (i % 25 == 0)
                {
                    xEnemy++;
                }


                pos.x = xEnemy;
            }
            else
            {
                currentGameObject = allyGameObjectToSpawn;
                isEnemy = false;

                if (i % 25 == 0)
                {
                    xAlly--;

                }
                pos.x = xAlly;
            }

            if (i % 3 == 0)
            {
                isArcher = true;
            }


            locations.Add(new SpawnLoc() { Index = i, Location = pos, ObjectToSpawn = currentGameObject, IsArcher = isArcher, IsEnemy = isEnemy});
        }


        foreach (SpawnLoc loc in locations)
        {

            GameObject go = Instantiate(loc.ObjectToSpawn, loc.Location, Quaternion.identity);
            UnitParsCust instanceUp = go.GetComponent<UnitParsCust>();


            if (instanceUp != null)
            {
                if (instanceUp.nation >= DiplomacyCust.active.numberNations)
                {
                    DiplomacyCust.active.AddNation();
                }
                instanceUp.isReady = true;

                instanceUp.IsEnemy = loc.IsEnemy;

                if (instanceUp.IsEnemy == true)
                {
                    if (loc.IsArcher)
                    {
                        instanceUp.UnitType = "Archer";
                    }
                }

                instanceUp.UniqueID = loc.Index;
                allUnits.Add(instanceUp);
                //instanceUp.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, instanceUp.transform.forward, default);

            }
        }








    }

    void Update()
    {
        UpdateWithoutStatistics();
    }


    void UpdateWithoutStatistics()
    {
        float deltaTime = Time.deltaTime;

        //commnad system?
        CommandPhase();

        //hold phase?
        //stop all movement before search even begins
        HoldPhase();

        //move phase?
        //move units to location phase




        SearchPhase(deltaTime);

        RetargetPhase();

        ApproachPhase();

        AttackPhase();

        SelfHealingPhase(deltaTime);
        DeathPhase();

        //SinkPhase(deltaTime); -- not needed

        //ManualMover();

        PrepSpriteSheetData();
        RenderAnimation();
    }


    /// <summary>
    /// command system to set units to listen to commands
    /// </summary>
    void CommandPhase()
    {
        if (allUnits.Count > 0)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                UnitParsCust unit = allUnits[i];

                string commandToFollow = "";

                if (unit.nation == PlayerNation)
                {
                    commandToFollow = playerControl.PlayerCommand;
                }
                else
                {
                    commandToFollow = aiControl.CurrentCommand;
                }

                if (unit.CurrentCommand != commandToFollow)
                {
                    unit.CurrentCommand = commandToFollow;
                    unit.PreviousCommand = commandToFollow;
                    switch (commandToFollow)
                    {
                        default:
                        case "Hold":

                            break;
                        case "Move":
                            //get player location to move to?
                            unit.nma.isStopped = false;
                            break;
                        case "Attack":
                            unit.nma.isStopped = false;
                            break;
                        case "Retreat":
                            break;
                    }
                }
                else
                {
                    unit.CurrentCommand = unit.PreviousCommand;
                }

            }
        }
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

        for (int i = 0; i < nToLoop; i++)
        {
            iSearchPhase++;

            if (iSearchPhase >= allUnits.Count)
            {
                iSearchPhase = 0;
            }

            UnitParsCust up = allUnits[iSearchPhase];
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

        for (int i = 0; i < nToLoop; i++)
        {
            iRetargetPhase++;

            if (iRetargetPhase >= allUnits.Count)
            {
                iRetargetPhase = 0;
            }

            UnitParsCust up = allUnits[iRetargetPhase];
            int nation = up.nation;

            if (up.isApproaching && up.target != null && targets[nation].Count > 0)
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
        for (int i = 0; i < nToLoop; i++)
        {
            iApproachPhase++;


            if (iApproachPhase >= allUnits.Count)
            {
                iApproachPhase = 0;
            }

            UnitParsCust apprPars = allUnits[iApproachPhase];


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
                                apprPars.target.attackers.Remove(apprPars);
                                apprPars.target.noAttackers = apprPars.target.attackers.Count;
                                apprPars.target = null;
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
                            apprNav.SetDestination(new Vector3(apprPars.transform.position.x, apprPars.transform.position.y, 0f));

                            //TODO: get coorect direction for arhcers
                            var direction = apprNav.destination - apprPars.transform.position;
                            apprPars.direction = direction;
                            // pre-setting for attacking
                            apprPars.isApproaching = false;
                            apprPars.isAttacking = true;
                            apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
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

                                    var rand = UnityEngine.Random.Range(0f, .25f);

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
                apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, apprPars.direction, default);
            }

        }
    }

    int iAttackPhase = 0;
    float fAttackPhase = 0f;

    // attacking phase set attackers to attack their targets and cause damage when they alreayd approached their targets

    public void AttackPhase()
    {
        fAttackPhase += allUnits.Count * attackUpdateFraction;

        int nToLoop = (int)fAttackPhase;
        fAttackPhase -= nToLoop;

        // checking through allUnits list which units are set to approach(isAttacking)
        for (int i = 0; i < nToLoop; i++)
        {
            iAttackPhase++;

            if (iAttackPhase >= allUnits.Count)
            {
                iAttackPhase = 0;
            }

            UnitParsCust attPars = allUnits[iAttackPhase];

            if (attPars.isAttacking && attPars.tag != null && attPars.target != null)
            {
                UnitParsCust targPars = attPars.target;

                UnityEngine.AI.NavMeshAgent attNav = attPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                UnityEngine.AI.NavMeshAgent targNav = targPars.GetComponent<UnityEngine.AI.NavMeshAgent>();

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

                    deadPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Die, deadPars.direction, default);
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

    //public List<Material> springAttractFrames;

    //int iRenderAnimationPhase = 0;
    //float fRenderAnimationPhase = 0f;

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

                //iRenderAnimationPhase++;

                //if (iRenderAnimationPhase >= allUnits.Count)
                //{
                //    iRenderAnimationPhase = 0;
                //}

                UnitParsCust renderAnimationParsCust = allUnits[i];

                List<Material> springAttractFrames = new List<Material>();

                MeshRenderer springAttractScreenRend = renderAnimationParsCust.springAttractScreenRend;
                //float springAttractFrameRefTime = renderAnimationParsCust.springAttractFrameRefTime;
                var spriteSheetAnimationData = renderAnimationParsCust.spriteSheetData;


                renderAnimationParsCust.spriteSheetData.frameTimer -= deltaTime;
                while (renderAnimationParsCust.spriteSheetData.frameTimer < 0)
                {
                    renderAnimationParsCust.spriteSheetData.frameTimer += renderAnimationParsCust.spriteSheetData.frameRate;
                    renderAnimationParsCust.spriteSheetData.currentFrame = ((renderAnimationParsCust.spriteSheetData.currentFrame + 1) % renderAnimationParsCust.spriteSheetData.frameCount);// + renderAnimationParsCust.spriteSheetData.horizontalCount;

                    if (renderAnimationParsCust.spriteSheetData.currentFrame >= (renderAnimationParsCust.spriteSheetData.frameCount))
                    {
                        renderAnimationParsCust.spriteSheetData.loopCount++;
                    }



                    //if (renderAnimationParsCust.IsEnemy)
                    //{
                    //    springAttractFrames = renderAnimationParsCust.spriteSheetData.materialsEnemy;
                    //}
                    //else
                    //{
                    springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;
                    //}


                    //UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;
                    Material[] newMats = null;
                    try
                    {
                        newMats = new Material[] { springAttractFrames[renderAnimationParsCust.spriteSheetData.currentFrame] };
                    }
                    catch (System.Exception)
                    {

                        throw;
                    }
                    renderAnimationParsCust.springAttractScreenRend.materials = newMats;
                    //if (renderAnimationParsCust.IsEnemy)
                    //{
                    //    renderAnimationParsCust.springAttractScreenRend.materials[0].color = Color.red;
                    //}
                }

            }
        }



        if (deadUnits.Count > 0)
        {
            var deltaTime = Time.deltaTime;
            ///
            for (int i = 0; i < deadUnits.Count; i++)
            {

                //iRenderAnimationPhase++;

                //if (iRenderAnimationPhase >= allUnits.Count)
                //{
                //    iRenderAnimationPhase = 0;
                //}

                UnitParsCust renderAnimationParsCust = deadUnits[i];

                List<Material> springAttractFrames = new List<Material>();

                MeshRenderer springAttractScreenRend = renderAnimationParsCust.springAttractScreenRend;
                //float springAttractFrameRefTime = renderAnimationParsCust.springAttractFrameRefTime;
                var spriteSheetAnimationData = renderAnimationParsCust.spriteSheetData;

                if (renderAnimationParsCust.spriteSheetData.activeBaseAnimTypeEnum == UnitAnimDataCust.BaseAnimMaterialType.Die)
                {

                }


                //cancel if deadtimer ?
                if (renderAnimationParsCust.spriteSheetData.currentFrame == renderAnimationParsCust.spriteSheetData.frameCount - 1)
                {
                    deadUnits.Remove(renderAnimationParsCust);
                    renderAnimationParsCust.springAttractScreenRend.materials[0].color =
                                            Color.Lerp(renderAnimationParsCust.springAttractScreenRend.materials[0].color, Color.black, .25f);
                    renderAnimationParsCust.springAttractScreenRend.sortingOrder = 9999999;
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
                    //springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;//UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;


                    //if (renderAnimationParsCust.IsEnemy)
                    //{
                    //    springAttractFrames = renderAnimationParsCust.spriteSheetData.materialsEnemy;
                    //}
                    //else
                    //{
                    springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;
                    //}


                    Material[] newMats = { springAttractFrames[renderAnimationParsCust.spriteSheetData.currentFrame] };
                    renderAnimationParsCust.springAttractScreenRend.materials = newMats;
                    //if (renderAnimationParsCust.IsEnemy)
                    //{
                    //    renderAnimationParsCust.springAttractScreenRend.materials[0].color = Color.red;
                    //}
                }

            }
        }




        //player animations





        //MeshRenderer springAttractScreenRendPlayer = playerUnitPars.springAttractScreenRend;
        ////float springAttractFrameRefTime = playerUnitPars.springAttractFrameRefTime;
        //var spriteSheetAnimationData = playerUnitPars.spriteSheetData;



        #region PLayer
        //cancel if deadtimer ?
        //if (playerUnitPars.spriteSheetData.currentFrame == playerUnitPars.spriteSheetData.frameCount - 1)
        //{
        //    deadUnits.Remove(playerUnitPars);
        //    playerUnitPars.springAttractScreenRend.materials[0].color =
        //                            Color.Lerp(playerUnitPars.springAttractScreenRend.materials[0].color, Color.black, .25f);
        //    playerUnitPars.springAttractScreenRend.sortingOrder = 9999999;
        //    playerUnitPars.transform.position = new Vector3(playerUnitPars.transform.position.x, playerUnitPars.transform.position.y, .01f);
        //    playerUnitPars.gameObject.transform.localPosition = new Vector3(playerUnitPars.gameObject.transform.position.x, playerUnitPars.gameObject.transform.position.y, 0);
        //    playerUnitPars.gameObject.transform.SetParent(deadUnitHolder.transform);
        //    //Object.Destroy(playerUnitPars.springAttractScreenRend);
        //    Object.Destroy(playerUnitPars.nma);
        //    //Object.Destroy(playerUnitPars.GetComponent<MeshFilter>());
        //    Object.Destroy(playerUnitPars);
        //    return;
        //}
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
            //springAttractFrames = playerUnitPars.spriteSheetData.materials;//UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;


            if (playerUnitPars.IsEnemy)
            {
                springAttractFramesPlayer = playerUnitPars.spriteSheetData.materialsEnemy;
            }
            else
            {
                springAttractFramesPlayer = playerUnitPars.spriteSheetData.materials;
            }


            Material[] newMats = { springAttractFramesPlayer[playerUnitPars.spriteSheetData.currentFrame] };
            playerUnitPars.springAttractScreenRend.materials = newMats;
            //if (playerUnitPars.IsEnemy)
            //{
            //    playerUnitPars.springAttractScreenRend.materials[0].color = Color.red;
            //}
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
            ((shootPosition2d.x <= targetPosition.x + 1 &&  shootPosition2d.x >= targetPosition2d.x - 1) 
             || (shootPosition2d.y <= targetPosition2d.y + 1 && shootPosition2d.y >= targetPosition2d.y -1)))
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

