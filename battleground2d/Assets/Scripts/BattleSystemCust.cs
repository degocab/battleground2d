using System.Collections.Generic;
using UnityEngine;

public class BattleSystemCust : MonoBehaviour
{
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
    void Awake()
    {
        active = this;
        Random.InitState(randomSeed); // not sure why this needs to be initialized



    }

    public GameObject objectToSpawn;
    public GameObject objectToSpawn2;


    public int unitCount = 1000;


    void Start()
    {

        //load resources
        UnitAnimDataCust.Init();


        //maximum amount of nodes processed each frame in pathfinding process
        UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;


        float xAlly = -2;
        float xEnemy = 2;
        for (int i = 0; i < unitCount; i++)
        {
            Vector2 randPos = new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));//Random.insideUnitCircle * 10;
            Vector3 pos = new Vector3(0, randPos.y, 0f);


            GameObject currentGameObject;
            bool isEnemy = false;
            if (i % 2 == 0)
            {
                currentGameObject = objectToSpawn;
                isEnemy = true;
                if (i % 25 == 0)
                {
                    xEnemy++;
                }
                pos.x = xEnemy;
            }
            else
            {
                currentGameObject = objectToSpawn2;
                isEnemy = false;


                if (i % 25 == 0)
                {
                    xAlly--;

                }
                pos.x = xAlly;
            }


            GameObject go = Instantiate(currentGameObject, pos, Quaternion.identity);
            UnitParsCust instanceUp = go.GetComponent<UnitParsCust>();

            //freeze rotation
            //var transf = go.GetComponentInChildren<Transform>();


            if (instanceUp != null)
            {
                if (instanceUp.nation >= DiplomacyCust.active.numberNations)
                {
                    DiplomacyCust.active.AddNation();
                }
                instanceUp.isReady = true;
                instanceUp.IsEnemy = isEnemy;
                instanceUp.UniqueID = i;
                allUnits.Add(instanceUp);
                //instanceUp.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, instanceUp.transform.forward, default);

            }
        }

        //GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Default");
        //foreach (GameObject go in gameObjects)
        //{
        //    UnitParsCust instanceUp = go.GetComponent<UnitParsCust>();

        //    //freeze rotation
        //    //var transf = go.GetComponentInChildren<Transform>();


        //    if (instanceUp != null)
        //    {
        //        if (instanceUp.nation >= DiplomacyCust.active.numberNations)
        //        {
        //            DiplomacyCust.active.AddNation();
        //        }
        //        instanceUp.isReady = true;
        //        allUnits.Add(instanceUp);
        //    }
        //}

    }

    void Update()
    {
        UpdateWithoutStatistics();
    }


    void UpdateWithoutStatistics()
    {
        float deltaTime = Time.deltaTime;

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

            if (up.isReady && targets[nation].Count > 0)
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


                    //TODO: see if this fixed all units facing up
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



            if (apprPars.UniqueID == 311)
            {

            }

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
                        // if approachers already close to their targets
                        if (rTarget < stoppDistance)
                        {
                            apprNav.SetDestination(new Vector3(targ.transform.position.x, targ.transform.position.y, 0f));
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
                                if ((destination - targ.transform.position).sqrMagnitude > .125f)
                                {
                                    apprNav.SetDestination(new Vector3(targ.transform.position.x, targ.transform.position.y, 0f));
                                    var direction = targ.transform.position - apprPars.transform.position;
                                    apprPars.direction = direction;
                                    apprNav.speed = 1f;
                                    apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, direction, default);
                                }
                                //else
                                //{
                                //    // TEST 
                                //    apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, apprPars.direction, default);
                                //}
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
                    apprPars.isApproachable = false;
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

            if (attPars.isAttacking && attPars.tag != null)
            {
                UnitParsCust targPars = attPars.target;

                UnityEngine.AI.NavMeshAgent attNav = attPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                UnityEngine.AI.NavMeshAgent targNav = targPars.GetComponent<UnityEngine.AI.NavMeshAgent>();

                attNav.stoppingDistance = attNav.radius / (attPars.transform.localScale.x) + targNav.radius / (targPars.transform.localScale.x);

                // distance between attacker and target

                float rTarget = (attPars.transform.position - targPars.transform.position).magnitude;
                float stoppDistance = (2.5f + attPars.transform.localScale.x * targPars.transform.localScale.x * attNav.stoppingDistance);

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

                        if (Random.value > (strength / (strength + defence)))
                        {
                            attPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Attack, attPars.direction, default);
                            targPars.health = targPars.health - (50f + Random.Range(0f, 15f));// targPars.health - 2.0f * strength * Random.value;
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


        // fix target refhres times
        fDeathPhase += allUnits.Count * deathUpdateFraction;

        int nToLoop = (int)fDeathPhase;
        fDeathPhase -= nToLoop;

        for (int i = 0; i < allUnits.Count; i++)
        {
            iDeathPhase++;

            if (iDeathPhase >= allUnits.Count)
            {
                iDeathPhase = 0;
            }
            for (int j = 0; j < targetRefreshTimes.Count; j++)
            {
                targetRefreshTimes[j] = -1f;
            }
        }







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

                //play animations
                if (prepSheetUnitPars.playAnimationCust.forced)
                {
                    //TODO add walking/run logic
                    prepSheetUnitPars.spriteSheetData = UnitAnimationCust.PlayAnimForced(/*ref prepSheetUnitPars, */prepSheetUnitPars.playAnimationCust.baseAnimType, prepSheetUnitPars.playAnimationCust.animDir, prepSheetUnitPars.playAnimationCust.onComplete);

                }
                else
                {
                    SpriteSheetAnimationDataCust currSpriteSheetData = prepSheetUnitPars.spriteSheetData;
                    //TODO: add idle logic
                    SpriteSheetAnimationDataCust? newSpriteSheetData = UnitAnimationCust.PlayAnim(/*ref prepSheetUnitPars, */prepSheetUnitPars.playAnimationCust.baseAnimType, currSpriteSheetData, prepSheetUnitPars.playAnimationCust.animDir, prepSheetUnitPars.playAnimationCust.onComplete);

                    // if changes
                    if (newSpriteSheetData != null)
                    {
                        prepSheetUnitPars.spriteSheetData = newSpriteSheetData.Value;
                    }
                }
            }
        }

    }

    public List<Material> springAttractFrames;

    int iRenderAnimationPhase = 0;
    float fRenderAnimationPhase = 0f;

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



                MeshRenderer springAttractScreenRend = renderAnimationParsCust.springAttractScreenRend;
                //float springAttractFrameRefTime = renderAnimationParsCust.springAttractFrameRefTime;
                var spriteSheetAnimationData = renderAnimationParsCust.spriteSheetData;

                if (renderAnimationParsCust.spriteSheetData.activeBaseAnimTypeEnum == UnitAnimDataCust.BaseAnimMaterialType.Die)
                {

                }


                renderAnimationParsCust.spriteSheetData.frameTimer -= deltaTime;
                while (renderAnimationParsCust.spriteSheetData.frameTimer < 0)
                {
                    renderAnimationParsCust.spriteSheetData.frameTimer += .1f;// renderAnimationParsCust.spriteSheetData.frameRate;
                    renderAnimationParsCust.spriteSheetData.currentFrame = ((renderAnimationParsCust.spriteSheetData.currentFrame + 1) % renderAnimationParsCust.spriteSheetData.frameCount);// + renderAnimationParsCust.spriteSheetData.horizontalCount;

                    if (renderAnimationParsCust.spriteSheetData.currentFrame >= (renderAnimationParsCust.spriteSheetData.frameCount))
                    {
                        renderAnimationParsCust.spriteSheetData.loopCount++;
                    }
                    springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;//UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;
                    Material[] newMats = { springAttractFrames[renderAnimationParsCust.spriteSheetData.currentFrame] };
                    renderAnimationParsCust.springAttractScreenRend.materials = newMats;
                    if (renderAnimationParsCust.IsEnemy)
                    {
                        renderAnimationParsCust.springAttractScreenRend.materials[0].color = Color.red;
                    }
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
Color.Lerp(renderAnimationParsCust.springAttractScreenRend.materials[0].color, Color.black, .5f);
                    renderAnimationParsCust.transform.position = new Vector3(renderAnimationParsCust.transform.position. x, renderAnimationParsCust.transform.position.y, .01f);
                    renderAnimationParsCust.gameObject.transform.SetParent(deadUnitHolder.transform);
                    //Object.Destroy(renderAnimationParsCust.springAttractScreenRend);
                    Object.Destroy(renderAnimationParsCust.nma);
                    //Object.Destroy(renderAnimationParsCust.GetComponent<MeshFilter>());
                    Object.Destroy(renderAnimationParsCust);
                    return;
                }


                renderAnimationParsCust.spriteSheetData.frameTimer -= deltaTime;
                while (renderAnimationParsCust.spriteSheetData.frameTimer < 0)
                {
                    renderAnimationParsCust.spriteSheetData.frameTimer += .1f;// renderAnimationParsCust.spriteSheetData.frameRate;
                    renderAnimationParsCust.spriteSheetData.currentFrame = ((renderAnimationParsCust.spriteSheetData.currentFrame + 1) % renderAnimationParsCust.spriteSheetData.frameCount);// + renderAnimationParsCust.spriteSheetData.horizontalCount;

                    if (renderAnimationParsCust.spriteSheetData.currentFrame >= (renderAnimationParsCust.spriteSheetData.frameCount))
                    {
                        renderAnimationParsCust.spriteSheetData.loopCount++;
                    }
                    springAttractFrames = renderAnimationParsCust.spriteSheetData.materials;//UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;
                    Material[] newMats = { springAttractFrames[renderAnimationParsCust.spriteSheetData.currentFrame] };
                    renderAnimationParsCust.springAttractScreenRend.materials = newMats;
                    if (renderAnimationParsCust.IsEnemy)
                    {
                        renderAnimationParsCust.springAttractScreenRend.materials[0].color = Color.red;
                    }
                }

            }
        }

        //if (allUnits.Count > 0)
        //{
        //    var deltaTime = Time.deltaTime;
        //    ///
        //    for (int i = 0; i < allUnits.Count; i++)
        //    {
        //        MeshRenderer springAttractScreenRend = allUnits[i].springAttractScreenRend;
        //        //float springAttractFrameRefTime = allUnits[i].springAttractFrameRefTime;
        //        var spriteSheetAnimationData = allUnits[i].spriteSheetData;

        //        if (allUnits[i].spriteSheetData.activeBaseAnimTypeEnum == UnitAnimDataCust.BaseAnimMaterialType.Die)
        //        {

        //        }


        //        allUnits[i].spriteSheetData.frameTimer -= deltaTime;
        //        while (allUnits[i].spriteSheetData.frameTimer < 0)
        //        {
        //            allUnits[i].spriteSheetData.frameTimer += .1f;// allUnits[i].spriteSheetData.frameRate;
        //            allUnits[i].spriteSheetData.currentFrame = ((allUnits[i].spriteSheetData.currentFrame + 1) % allUnits[i].spriteSheetData.frameCount);// + allUnits[i].spriteSheetData.horizontalCount;

        //            if (allUnits[i].spriteSheetData.currentFrame >= (allUnits[i].spriteSheetData.frameCount))
        //            {
        //                allUnits[i].spriteSheetData.loopCount++;
        //            }
        //            springAttractFrames = allUnits[i].spriteSheetData.materials;//UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;
        //            Material[] newMats = { springAttractFrames[allUnits[i].spriteSheetData.currentFrame] };
        //            allUnits[i].springAttractScreenRend.materials = newMats;
        //            if (allUnits[i].IsEnemy)
        //            {
        //                allUnits[i].springAttractScreenRend.materials[0].color = Color.red;
        //            }
        //        }

        //    }
        //}


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
}
