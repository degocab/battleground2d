using System.Collections.Generic;
using UnityEngine;

public class BattleSystemCust : MonoBehaviour
{
    public static BattleSystemCust active;

    public List<UnitParsCust> allUnits = new List<UnitParsCust>();


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
    void Start()
    {

        //load resources
        UnitAnimDataCust.Init();


        //maximum amount of nodes processed each frame in pathfinding process
        UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;


        int unitCount = 4;

        for (int i = 0; i < unitCount; i++)
        {
            Vector2 randPos = Random.insideUnitCircle * 4;
            Vector3 pos = new Vector3(randPos.x, randPos.y, 0f);


            GameObject currentGameObject;
            if (i % 2 == 0)
            {
                currentGameObject = objectToSpawn2;
            }
            else
            {
                currentGameObject = objectToSpawn;
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
                allUnits.Add(instanceUp);
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

        //SelfHealingPhase(deltaTime); // they might all need this

        DeathPhase();

        //SinkPhase(deltaTime); -- not needed

        //ManualMover();

        //run sprite animations i guess
        PrepAnimation();
        PrepSpriteSheetData();
        RenderAnimation();
    }

    private void PrepAnimation()
    {
        var deltaTime = Time.deltaTime;

        if (allUnits.Count > 0)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                //play nimations
                if (allUnits[i].isApproaching)
                {
                    //TODO add walking/run logic
                    allUnits[i].playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, allUnits[i].transform.forward, default);

                }
                else
                {
                    //TODO: add idle logic
                    allUnits[i].playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, allUnits[i].transform.forward, default);

                }
            }
        }
    }


    private void PrepSpriteSheetData()
    {
        var deltaTime = Time.deltaTime;

        if (allUnits.Count > 0)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                //play nimations
                if (allUnits[i].playAnimationCust.forced)
                {
                    //TODO add walking/run logic
                    allUnits[i].spriteSheetData = UnitAnimationCust.PlayAnimForced(/*ref allUnits[i], */allUnits[i].playAnimationCust.baseAnimType, allUnits[i].playAnimationCust.animDir, allUnits[i].playAnimationCust.onComplete);

                }
                else
                {
                    SpriteSheetAnimationDataCust currSpriteSheetData = allUnits[i].spriteSheetData;
                    //TODO: add idle logic
                    SpriteSheetAnimationDataCust? newSpriteSheetData = UnitAnimationCust.PlayAnim(/*ref allUnits[i], */allUnits[i].playAnimationCust.baseAnimType, currSpriteSheetData, allUnits[i].playAnimationCust.animDir, allUnits[i].playAnimationCust.onComplete);

                    // if changes
                    if (newSpriteSheetData != null)
                    {
                        allUnits[i].spriteSheetData = newSpriteSheetData.Value;
                    }
                }
            }
        }
    }

    public List<Material> springAttractFrames;
    float springAttractFrameTime = 0.2f;

    private float springAttractFrameRefTime;
    private void RenderAnimation()
    {
        if (allUnits.Count > 0)
        {
            //MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            //Vector4[] uv = new Vector4[1];
            //Camera camera = Camera.main;
            ////Mesh quadMesh = GameHandler.GetInstance().quadMesh;
            ////Material material = GameHandler.GetInstance().walkingSpriteSheetMaterial;
            for (int i = 0; i < allUnits.Count; i++)
            {
                MeshRenderer springAttractScreenRend = allUnits[i].springAttractScreenRend;
                //float springAttractFrameRefTime = allUnits[i].springAttractFrameRefTime;
                var deltaTime = Time.deltaTime;
                var spriteSheetAnimationData = allUnits[i].spriteSheetData;


                spriteSheetAnimationData.frameTimer -= deltaTime;
                while (spriteSheetAnimationData.frameTimer < 0)
                {
                    spriteSheetAnimationData.frameTimer += .1f;// spriteSheetAnimationData.frameRate;
                    spriteSheetAnimationData.currentFrame = ((spriteSheetAnimationData.currentFrame + 1) % spriteSheetAnimationData.frameCount);// + spriteSheetAnimationData.horizontalCount;

                    if (spriteSheetAnimationData.currentFrame >= (spriteSheetAnimationData.frameCount))
                    {
                        spriteSheetAnimationData.loopCount++;
                    }
                    springAttractFrames = spriteSheetAnimationData.materials;//UnitAnimDataCust.GetAnimTypeData(UnitAnimDataCust.AnimMaterialTypeEnum.RunRight).Materials;
                    Material[] newMats = { springAttractFrames[spriteSheetAnimationData.currentFrame] };
                    springAttractScreenRend.materials = newMats;
                }





                //if (Time.time - springAttractFrameRefTime > springAttractFrameTime)
                //{
                //    curSpringAttractFrameIndex = (curSpringAttractFrameIndex - 1 + springAttractFrames.Count) % springAttractFrames.Count;

                //    Material[] newMats = { springAttractFrames[curSpringAttractFrameIndex] };
                //    springAttractScreenRend.materials = newMats;

                //    springAttractFrameRefTime = Time.time;
                //}






                //Mesh quadMesh = allUnits[i].quadMesh;
                //Material material = allUnits[i].walkingSpriteSheetMaterial;

                //var spriteSheetAnimationData = allUnits[i].spiteSheetData;
                //uv[0] = spriteSheetAnimationData.uv;
                //materialPropertyBlock.SetVectorArray("_MainTex_UV", uv);

                //Graphics.DrawMesh(
                //    quadMesh,
                //    spriteSheetAnimationData.matrix,
                //    material,
                //    0, // Layer
                //    camera,
                //    0, // Submesh index
                //    materialPropertyBlock
                //);
            }
            }
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
    void ApproachPhase()
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


                    apprNav.stoppingDistance = apprNav.radius / (apprPars.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                    // distance between approacher and target

                    float rTarget = (apprPars.transform.position - targ.transform.position).magnitude;
                    float stoppDistance = .25f;// (2f + apprPars.transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);

                    // counting increased distances (failure to approch) between attacker and target
                    // if counter failedR becomes bigger than critFailedR, preparing for new target search
                    // basically what I was tring to do to stop units from targeting one until they reach it or die

                    if (apprPars.prevR <= rTarget)
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
                            apprNav.SetDestination(apprPars.transform.position);

                            // pre-setting for attacking
                            apprPars.isApproaching = false;
                            apprPars.isAttacking = true;

                        }
                        else
                        {

                            // starting to move
                            if (apprPars.isMovable)
                            {
                                Vector3 destination = apprNav.destination;
                                if ((destination - targ.transform.position).sqrMagnitude > .125f)
                                {
                                    apprNav.SetDestination(targ.transform.position);
                                    apprNav.speed = 1f;
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
                    apprNav.SetDestination(apprPars.transform.position);

                    apprPars.isApproachable = false;
                    apprPars.isReady = true;

                }
            }

        }
    }

    int iAttackPhase = 0;
    float fAttackPhase = 0f;

    // attacking phase set attackers to attack their targets and cause damage when they alreayd approached their targets

    void AttackPhase()
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
                    if (Random.value > (strength / (strength + defence)))
                    {
                        targPars.health = targPars.health - 2.0f * strength * Random.value;
                    }
                }




            }

        }

    }

    // finish later
    void SelfHealingPhase(float deltaTime)
    {
        throw new System.NotImplementedException();
    }

    int iDeathPhase = 0;
    float fDeathPhase = 0f;

    // Death phase unest all unit activity and prepare to die

    void DeathPhase()
    {
        fDeathPhase += allUnits.Count * deathUpdateFraction;

        int nToLoop = (int)fDeathPhase;
        fDeathPhase -= nToLoop;

        //get dying units
        for (int i = 0; i < nToLoop; i++)
        {
            iDeathPhase++;

            if (iDeathPhase >= allUnits.Count)
            {
                iDeathPhase = 0;
            }

            UnitParsCust deadPars = allUnits[iDeathPhase];

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

                    for (int j = 0; j < targetRefreshTimes.Count; j++)
                    {
                        targetRefreshTimes[j] = -1f;
                    }
                }
                //unsetting unit activity and keep it dying
                else
                {
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
                    nma.avoidancePriority = 0;

                    deadPars.deathCalls++;


                }
            }
        }
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
