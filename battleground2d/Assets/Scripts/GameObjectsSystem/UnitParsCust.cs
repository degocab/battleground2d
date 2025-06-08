using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UnitParsCust : MonoBehaviour
{
    [SerializeField]
    public int UniqueID = 0;
    public bool IsEnemy { get; set; }


    [HideInInspector] public UnitParsTypeCust unitParsTypeCust;
    public string UnitType { get; set; }

    public bool isMovable = true;

    public bool isReady = true;
    public bool isApproaching = false;
    public bool isAttacking = false;
    public bool isApproachable = true;
    public bool isHealing = false;
    public bool isImmune = false;
    public bool isDying = false;
    public bool isSinking = false;

    public UnitParsCust target = null;
    public List<UnitParsCust> attackers = new List<UnitParsCust>();

    public int noAttackers = 0;
    public int maxAttackers = 3;

    [HideInInspector] public float prevR;
    [HideInInspector] public int failedR = 0;
    public int critFailedR = 100;

    public float health = 100.0f;
    public float maxHealth = 100.0f;
    public float selfHealFactor = 10.0f;

    public float strength = 10.0f;
    public float defence = 10.0f;

    [HideInInspector] public int deathCalls = 0;
    public int maxDeathCalls = 5;

    [HideInInspector] public int sinkCalls = 0;
    public int maxSinkCalls = 5;

    [HideInInspector] public bool changeMaterial = true;

    public int nation = 1;

    public Transform childTransform;
    public Animator childAnimator;
    public SpriteRenderer childSpriteRenderer;
    bool change = true;
    public SpriteSheetAnimationDataCust spriteSheetData;
    public Mesh quadMesh;
    public Material walkingSpriteSheetMaterial;
    internal int curSpringAttractFrameIndex;
    public MeshRenderer springAttractScreenRend;
    internal float frameTimer;
    internal int currentFrame;
    internal int loopCount;


    public PlayAnimationCust playAnimationCust;
    public Vector3 direction;


    public float nextAttack { get; set; }


    public float randomAttackRange { get; set; }
    private float _attackRate;

    public void SetAttackRate(float attackRate)
    {
        _attackRate = attackRate;
    }

    public float attackRate
    {
        get => _attackRate;
        set
        {
            if (UnitType.Contains("Archer"))
                _attackRate = value * 2;
            else
                _attackRate = value;
        }
    }

    public UnityEngine.AI.NavMeshAgent nma;
    public Vector3 velocityVector = Vector3.zero;
    internal bool randomFrame;
    internal Vector3 lastDirection;
    private string _currentCommand = "";
    [SerializeField]
    public string CurrentCommand
    {
        get; set;
    }
    [SerializeField]
    public string PreviousCommand
    {
        get; set;
    }
    /// <summary>
    /// Unit Commander(1) vs unit(2)
    /// </summary>
    public int UnitRank { get; set; }
    public List<UnitParsCust> SelectedUnitPars = new List<UnitParsCust>();

    /// <summary>
    /// If UnitRank = 1 then this will be used.
    /// </summary>
    [SerializeField]
    public int[] UnitsToCommand;




    [SerializeField]
    public List<Material> selectionRings;

    void Start()
    {
        Material selectRing1 = (Material)AssetDatabase.LoadAssetAtPath("Assets/Animations/2D/HorizontalRingCommander.mat", typeof(Material));
        Material selectRing2 = (Material)AssetDatabase.LoadAssetAtPath("Assets/Animations/2D/VerticalRingCommander.mat", typeof(Material));

        selectionRings.Add(selectRing1);
        selectionRings.Add(selectRing2);

        unitParsTypeCust = BattleSystemCust.active.allUnits.FirstOrDefault(x => x.UniqueID == UniqueID).GetComponent<UnitParsTypeCust>();


        spriteSheetData = new SpriteSheetAnimationDataCust
        {
            currentFrame = UnityEngine.Random.Range(0, 5),
            frameCount = 6,
            frameTimer = 0,// UnityEngine.Random.Range(0f, 1f),
            frameTimerMax = .1f
        };
        playAnimationCust = new PlayAnimationCust();
        nma = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (nma != null)
        {
            nma.enabled = true;
        }
        childTransform = this.GetComponentInChildren<Transform>();
        // childAnimator = this.GetComponentInChildren<Animator>();
        ///childSpriteRenderer = this.GetComponentInChildren<SpriteRenderer>();

        springAttractScreenRend = this.GetComponent<MeshRenderer>();

        playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Walk, transform.forward, default);
        randomAttackRange = UnityEngine.Random.Range(0f, 2f);
        //attackRate = 1;
        SetAttackRate(3);
        nextAttack = 0;

    }

    void Update()
    {


        //if (CheckForChanges())
        //{
        var newRot = Quaternion.Euler(0.0f, 0.0f, this.transform.rotation.z * -1.0f);
        newRot.z = 0;
        childTransform.rotation = Quaternion.Euler(0.0f, 0.0f, this.transform.rotation.z * -1.0f);
        //transform.position = new Vector3(childTransform.position.x, childTransform.position.y, 0f);

        springAttractScreenRend.sortingOrder = Mathf.RoundToInt(transform.position.y * 100f) * -1;


        if (UnitRank == 1)
        {
            Material selectionRing;

            //horizontal dir
            if (new int[] { 1, 2 }.Contains(playAnimationCust.animDir))
            {
                selectionRing = selectionRings[0];
            }
            else
            {
                selectionRing = selectionRings[1];
            }
            Material curMat = springAttractScreenRend.material;

            springAttractScreenRend.materials = new Material[2] { curMat, selectionRing };

        }
    }

    private void FixedUpdate()
    {
        Vector3 nextPos = nma.nextPosition;
        Vector3 correctPos = new Vector3(nextPos.x, nextPos.y, 0f); // do all modifications you need for nextPos
        transform.position = correctPos;
    }

    private int SetMovementDirection(float horizontal, float vertical)
    {
        int direction = 0;

        float maxValue = Mathf.Max(Mathf.Abs(horizontal), Mathf.Abs(vertical));

        if (vertical > 0 && Mathf.Abs(vertical) == maxValue) // up
        {
            direction = 3;

        }
        else if (vertical < 0 && Mathf.Abs(vertical) == maxValue) // down
        {
            direction = 4;
        }
        else if (horizontal > 0 && Mathf.Abs(horizontal) == maxValue) // right
        {
            //sprite flip
            childSpriteRenderer.flipX = false;
            direction = 1;

        }
        else if (horizontal < 0 && Mathf.Abs(horizontal) == maxValue) // left
        {
            //sprite flip
            childSpriteRenderer.flipX = true;
            direction = 2;
        }

        return direction;
    }

    private bool CheckForChanges()
    {
        if (isMovable != true ||
            isReady != false ||
            isApproaching != false ||
            isAttacking != false ||
            isApproachable != true ||
            isHealing != false ||
            isImmune != false ||
            isDying != false ||
            isSinking != false
            )
        {
            return true;
        }

        return false;
    }



    public void LaunchArrowDelay(UnitParsCust targPars, Vector3 launchPoint)
    {
        BattleSystemCust.active.LaunchArrow(this, targPars, launchPoint);
        //StartCoroutine(LaunchArrowDelayCor(targPars, launchPoint));
    }

    IEnumerator LaunchArrowDelayCor(UnitParsCust targPars, Vector3 launchPoint)
    {
        yield return new WaitForSeconds(1.25f);
        BattleSystemCust.active.LaunchArrow(this, targPars, launchPoint);
    }




    private float targetRefreshTime = 1f; // Time between target refreshes (can be adjusted as needed)
    private float timeSinceLastTargetRefresh = 0f; // Track time since the last target refresh

    public KDTreeCust targetKD; // KD-Tree for fast nearest-neighbor search
    public List<UnitParsCust> targets = new List<UnitParsCust>(); // List of current potential targets

    private float timeSinceLastRetarget = 0f; // Time since last retarget attempt
    public float retargetTimeInterval = 0.5f; // Time between retargeting attempts, can be adjusted

    private float lastSearchTime = 0f;
    private float searchInterval = 0.5f;  // Only search every 0.5 seconds

    internal void SearchForTargets(float deltaTime)
    {
        //// Refresh targets after the defined time interval
        //timeSinceLastTargetRefresh += deltaTime;

        //// Only refresh targets after the refresh time interval
        //if (timeSinceLastTargetRefresh >= targetRefreshTime)
        //{
        //    timeSinceLastTargetRefresh = 0f;

        // Create a list for valid target units (optimized)
        List<UnitParsCust> nationTargets = new List<UnitParsCust>();
        List<Vector3> nationTargetPositions = new List<Vector3>();

        // Check against all other units only once per refresh cycle
        foreach (var unit in BattleSystemCust.active.allUnits)
        {
            if (unit == this) continue; // Skip self

            // Ensure the unit is a valid target
            if (unit.nation != nation && unit.isApproachable && unit.health > 0f && unit.attackers.Count < unit.maxAttackers)
            {
                // Only check diplomatic relations if needed
                if (DiplomacyCust.active?.relations[unit.nation][nation] == 1)
                {
                    // Add the valid target to our list
                    nationTargets.Add(unit);
                    nationTargetPositions.Add(unit.transform.position);
                }
            }
        }

        // Only update the target lists and KD-Tree if valid targets were found
        if (nationTargets.Count > 0)
        {
            targets = nationTargets;
            targetKD = KDTreeCust.MakeFromPoints(nationTargetPositions.ToArray());
        }
        //}

        // Now, perform the actual targeting logic (finding the nearest target)
        if (isReady && targets.Count > 0 && !string.IsNullOrEmpty(CurrentCommand) && CurrentCommand.Contains("Attack"))
        {
            // If we already have a target, check if it's still valid
            if (target != null && target.health > 0f && target.attackers.Count < target.maxAttackers)
            {
                // If the current target is valid, retain it
                return;
            }

            // Find the nearest enemy using the KD-Tree if the current target is invalid or no target exists
            int targetId = targetKD.FindNearest(transform.position);
            UnitParsCust targetUnit = targets[targetId];

            // Only assign the target if it's still valid (alive and not over-crowded)
            if (targetUnit.health > 0f && targetUnit.attackers.Count < targetUnit.maxAttackers)
            {
                // Assign this unit as an attacker
                targetUnit.attackers.Add(this);
                targetUnit.noAttackers = targetUnit.attackers.Count;
                target = targetUnit;

                // Calculate direction to target and set the approaching state
                direction = targetUnit.transform.position - transform.position;
                isReady = false;
                isApproaching = true; // Assuming this is a flag for your approach phase
            }
        }

    }


    //internal void SearchForTargets(float deltaTime)
    //{
    //    // Refresh targets after the defined time interval
    //    timeSinceLastTargetRefresh += deltaTime;
    //    if (timeSinceLastTargetRefresh >= targetRefreshTime)
    //    {
    //        // Reset the time and refresh targets
    //        timeSinceLastTargetRefresh = 0f;

    //        List<UnitParsCust> nationTargets = new List<UnitParsCust>();
    //        List<Vector3> nationTargetPositions = new List<Vector3>();

    //        // Check against all other units
    //        foreach (var unit in BattleSystemCust.active.allUnits)
    //        {
    //            if (unit == this) continue; // Skip self

    //            // Check if unit is a valid target
    //            if (unit.nation != nation && unit.isApproachable && unit.health > 0f && unit.attackers.Count < unit.maxAttackers)
    //            {
    //                try
    //                {
    //                    if (DiplomacyCust.active == null)
    //                    {
    //                        DiplomacyCust.active = new DiplomacyCust();
    //                    }

    //                    // Check the diplomatic relations (for allies/enemies)
    //                    if (DiplomacyCust.active != null && DiplomacyCust.active.relations[unit.nation][nation] == 1)
    //                    {
    //                        // Add valid target unit
    //                        nationTargets.Add(unit);
    //                        nationTargetPositions.Add(unit.transform.position);
    //                    }
    //                }
    //                catch (Exception ex)
    //                {

    //                    throw;
    //                }
    //            }
    //        }

    //        // Update the targets list and KD-Tree for this unit's nation
    //        targets = nationTargets;
    //        targetKD = KDTreeCust.MakeFromPoints(nationTargetPositions.ToArray());
    //    }

    //    // Now, perform the actual targeting logic (finding the nearest target)
    //    if (isReady && targets.Count > 0 && !string.IsNullOrEmpty(CurrentCommand) && CurrentCommand.Contains("Attack"))
    //    {
    //        // Find the nearest enemy based on KD-Tree
    //        int targetId = targetKD.FindNearest(transform.position);
    //        UnitParsCust targetUnit = targets[targetId];

    //        if (targetUnit.health > 0f && targetUnit.attackers.Count < targetUnit.maxAttackers)
    //        {
    //            // Assign target to this unit
    //            targetUnit.attackers.Add(this);
    //            targetUnit.noAttackers = targetUnit.attackers.Count;
    //            target = targetUnit;

    //            // Calculate direction to target and set the approaching state
    //            direction = targetUnit.transform.position - transform.position;
    //            isReady = false;
    //            isApproaching = true; // Assuming this is a flag for your approach phase
    //        }
    //    }
    //}

    internal void RetargetIfNeeded(float deltaTime)
    {
        //// Time-based retargeting to avoid checking too frequently
        //timeSinceLastRetarget += deltaTime;

        //// If it's time to retarget
        //if (timeSinceLastRetarget >= retargetTimeInterval)
        //{
        //    timeSinceLastRetarget = 0f; // Reset the timer

        // Check if unit is currently approaching a target and is not following
        if (isApproaching && target != null && CurrentCommand != "Follow" && targets.Count > 0)
        {
            // Find the nearest target from the KD-Tree
            int targetId = targetKD.FindNearest(transform.position);
            UnitParsCust targetUp = targets[targetId];

            // Check if the new target is valid (alive and not too many attackers)
            if (targetUp.health > 0f && targetUp.attackers.Count < targetUp.maxAttackers)
            {
                // Calculate the distance to the old target and new target
                float oldTargetDistanceSq = (target.transform.position - transform.position).sqrMagnitude;
                float newTargetDistanceSq = (targetUp.transform.position - transform.position).sqrMagnitude;

                // Only retarget if the new target is closer
                if (newTargetDistanceSq < oldTargetDistanceSq)
                {
                    // Remove this unit from its old target's attackers
                    if (target != null)
                    {
                        target.attackers.Remove(this);
                        target.noAttackers = target.attackers.Count;
                    }

                    // Add this unit to the new target's attackers list
                    targetUp.attackers.Add(this);
                    targetUp.noAttackers = targetUp.attackers.Count;

                    // Update the target for this unit
                    target = targetUp;

                    // Set the unit as not ready and mark it as approaching the new target
                    isReady = false;
                    isApproaching = true;
                }
            }
        }
        //}
    }


    // Existing fields
    public UnityEngine.AI.NavMeshAgent navAgent;
    private float stoppDistance;

    // You can make this interval adjustable based on your game's needs
    private float approachUpdateFraction = 0.01f;
    int iApproachPhase = 0;
    float fApproachPhase = 0f;

    internal void ApproachPhase(float deltaTime)
    {
        // Increment approach phase based on the number of units
        fApproachPhase += BattleSystemCust.active.allUnits.Count * approachUpdateFraction;

        int nToLoop = (int)fApproachPhase;
        fApproachPhase -= nToLoop;
        UnityEngine.AI.NavMeshAgent apprNav = navAgent;

        // Only process if the unit is approaching and the target is not null
        if (isApproaching && target != null)
        {
            UnitParsCust targ = target;

            // Check if the target is still approachable (alive and valid)
            if (targ.isApproachable)
            {
                // Set the stopping distance for the NavMeshAgent
                apprNav.stoppingDistance = 1.25f;

                // Calculate the distance to the target
                float rTarget = (transform.position - targ.transform.position).magnitude;
                float stoppDistance = (transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);

                // If the target distance has increased (failure to approach), reset the approach
                var roundedPrevR = Mathf.Round(prevR * 10f) / 10f;
                var roundedrTarget = Mathf.Round(rTarget * 10f) / 10f;

                if (roundedPrevR > roundedrTarget)
                {
                    failedR += 1;

                    // If the failure threshold is reached, stop the approach and evaluate further
                    if (failedR > critFailedR)
                    {
                        isApproaching = false; // Stop approaching
                        isReady = true; // Mark unit as ready again
                        failedR = 0;

                        // Reset the target if necessary
                        if (target != null && target.health > 0f && target.attackers.Count < target.maxAttackers)
                        {
                            if (CurrentCommand == "Follow" || CurrentCommand == "Attack")
                            {
                                isApproaching = true; // Continue approaching
                            }
                            else
                            {
                                // Remove this unit from the target's attacker list if no longer attacking
                                target.attackers.Remove(this);
                                target.noAttackers = target.attackers.Count;
                                target = null; // Reset target if no longer valid
                            }
                        }
                    }
                }

                else
                {
                    // If the unit is an Archer, adjust the stopping distance based on range
                    if (UnitType == "Archer")
                    {
                        if (BattleSystemCust.active.CanHitCoordinate(transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f))
                        {
                            stoppDistance = 1.25f * rTarget; // Modify distance for Archer range
                        }
                        else
                        {
                            stoppDistance = rTarget;
                        }
                    }

                    // If the unit is close to the target, stop approaching and start attacking
                    if (rTarget < stoppDistance)
                    {
                        if (CurrentCommand == "Attack")
                        {
                            apprNav.SetDestination(transform.position); // Stop movement once in range (set to self position)
                            direction = apprNav.destination - transform.position;
                            playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);

                            isApproaching = false; // Stop the approach
                            isAttacking = true; // Start attacking
                        }
                        else
                        {
                            apprNav.SetDestination(transform.position); // Stop movement and stay put
                            direction = apprNav.destination - transform.position;
                            playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
                        }
                    }
                    else
                    {
                        // Start moving towards the target
                        if (isMovable)
                        {
                            Vector3 destination = targ.transform.position; // Set the target position as the destination

                            // Only update destination if the distance to target is large enough to warrant it
                            if ((apprNav.destination - targ.transform.position).sqrMagnitude > .125f && UnitType != "Archer" ||
                                (UnitType == "Archer" &&
                                 (apprNav.destination - targ.transform.position).sqrMagnitude > .125f &&
                                 !BattleSystemCust.active.CanHitCoordinate(transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f)))
                            {
                                apprNav.SetDestination(targ.transform.position); // Move toward the target
                                direction = targ.transform.position - transform.position;

                                // Store the last known direction for later use
                                lastDirection = direction;

                                // Add some randomness to the movement speed
                                var rand = UnityEngine.Random.Range(0f, .55f);

                                if (playAnimationCust.baseAnimType != UnitAnimDataCust.BaseAnimMaterialType.Run)
                                {
                                    randomFrame = true;
                                }

                                apprNav.speed = 1f + rand; // Randomize the speed slightly
                                playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, direction, default);
                            }
                        }
                    }

                    // Save the previous distance for comparison next frame
                    prevR = rTarget;
                }
            }
            else
            {
                // If the target is no longer approachable, reset the target
                target = null;
                apprNav.SetDestination(transform.position); // Stop the agent's movement (set to self position)
                isReady = true; // Mark unit as ready
            }
        }
        else
        {
            // If unit is not approaching or doesn't have a target
            bool shouldBeIdle = (IsEnemy && BattleSystemCust.active.allUnits.Any(x => !x.IsEnemy)) ||
                                (!IsEnemy && BattleSystemCust.active.allUnits.Any(x => x.IsEnemy)) ? false : true;

            // If should be idle, stop moving and play idle animation
            if (shouldBeIdle)
            {
                playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
                apprNav.SetDestination(transform.position); // Stop movement (set to self position)
            }
        }
    }





    //internal void ApproachPhase(float deltaTime)
    //{
    //    fApproachPhase += BattleSystemCust.active.allUnits.Count * approachUpdateFraction;

    //    int nToLoop = (int)fApproachPhase;
    //    fApproachPhase -= nToLoop;

    //    // Only process if approaching and target exists
    //    if (isApproaching && target != null)
    //    {
    //        UnitParsCust targ = target;
    //        UnityEngine.AI.NavMeshAgent apprNav = navAgent;
    //        UnityEngine.AI.NavMeshAgent targNav = targ.navAgent;

    //        if (targ.isApproachable)
    //        {
    //            // Determine the stopping distance
    //            apprNav.stoppingDistance = .25f;

    //            // Calculate the distance to the target
    //            float rTarget = (transform.position - targ.transform.position).magnitude;
    //            float stoppDistance = (transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);

    //            // If the target distance has increased (failed to approach), reset approach
    //            var roundedPrevR = float.Parse(prevR.ToString("0.0"));
    //            var roundedrTarget = float.Parse(rTarget.ToString("0.0"));

    //            if (roundedPrevR < roundedrTarget)
    //            {
    //                failedR += 1;
    //                if (failedR > critFailedR)
    //                {
    //                    isApproaching = false;
    //                    isReady = true;
    //                    failedR = 0;

    //                    if (target != null)
    //                    {
    //                        if (CurrentCommand == "Follow")
    //                        {
    //                            isApproaching = true;
    //                        }
    //                        else
    //                        {
    //                            target.attackers.Remove(this);
    //                            target.noAttackers = target.attackers.Count;
    //                            target = null;
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                // If the unit is an Archer, adjust stopping distance if it's within range
    //                if (UnitType == "Archer")
    //                {
    //                    if (BattleSystemCust.active.CanHitCoordinate(transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f))
    //                    {
    //                        stoppDistance = 1.25f * rTarget;
    //                    }
    //                    else
    //                    {
    //                        stoppDistance = rTarget;
    //                    }
    //                }

    //                // If the unit is close to the target, stop approaching and start attacking
    //                if (rTarget < stoppDistance)
    //                {
    //                    if (CurrentCommand == "Attack")
    //                    {
    //                        apprNav.SetDestination(new Vector3(transform.position.x, transform.position.y, 0f));
    //                        direction = apprNav.destination - transform.position;
    //                        playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);

    //                        isApproaching = false;
    //                        isAttacking = true;
    //                    }
    //                    else
    //                    {
    //                        apprNav.SetDestination(new Vector3(transform.position.x, transform.position.y, 0f));
    //                        direction = apprNav.destination - transform.position;
    //                        playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
    //                    }
    //                }
    //                else
    //                {
    //                    // Start moving towards the target
    //                    if (isMovable)
    //                    {
    //                        Vector3 destination = apprNav.destination;
    //                        if ((destination - targ.transform.position).sqrMagnitude > .125f && UnitType != "Archer"
    //                            || (UnitType == "Archer" && (destination - targ.transform.position).sqrMagnitude > .125f &&
    //                                !BattleSystemCust.active.CanHitCoordinate(transform.position, targ.transform.position, Vector3.zero, 20.0f, 0.4f)))
    //                        {
    //                            apprNav.SetDestination(new Vector3(targ.transform.position.x, targ.transform.position.y, 0f));
    //                            direction = targ.transform.position - transform.position;

    //                            // Store last known direction for later use
    //                            lastDirection = direction;

    //                            var rand = UnityEngine.Random.Range(0f, .55f);

    //                            if (playAnimationCust.baseAnimType != UnitAnimDataCust.BaseAnimMaterialType.Run)
    //                            {
    //                                randomFrame = true;
    //                            }

    //                            apprNav.speed = 1f + rand;
    //                            playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, direction, default);
    //                        }
    //                    }
    //                }

    //                // Save previous distance for next comparison
    //                prevR = rTarget;
    //            }
    //        }
    //        else
    //        {
    //            // If target is non-approachable, stop the approach
    //            target = null;
    //            apprNav.SetDestination(new Vector3(transform.position.x, transform.position.y, 0f));
    //            isReady = true;
    //        }
    //    }
    //    else
    //    {
    //        // If unit is not approaching or doesn't have a target
    //        bool shouldBeIdle = (IsEnemy && BattleSystemCust.active.allUnits.Any(x => !x.IsEnemy)) || (!IsEnemy && BattleSystemCust.active.allUnits.Any(x => x.IsEnemy)) ? false : true;

    //        if (shouldBeIdle)
    //        {
    //            playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
    //            UnityEngine.AI.NavMeshAgent apprNav = navAgent;
    //            apprNav.SetDestination(new Vector3(transform.position.x, transform.position.y, 0f));
    //        }
    //    }
    //}


    // Attack function to handle attack logic
    internal void AttackPhase(float deltaTime)
    {
        if (isAttacking || target == null)
            return;

        UnitParsCust targPars = target;

        UnityEngine.AI.NavMeshAgent attNav = navAgent;
        UnityEngine.AI.NavMeshAgent targNav = targPars.navAgent;

        // Calculate stopping distance for attackers
        attNav.stoppingDistance = attNav.radius / transform.localScale.x + targNav.radius / targPars.transform.localScale.x;

        float rTarget = (transform.position - targPars.transform.position).magnitude;
        float stoppDistance = (2.5f + transform.localScale.x * targPars.transform.localScale.x * attNav.stoppingDistance);

        // Archer specific handling
        if (UnitType == "Archer")
        {
            if (BattleSystemCust.active.CanHitCoordinate(transform.position, targPars.transform.position, Vector3.zero, 20.0f, 0.4f))
            {
                stoppDistance = 1.25f * rTarget;
            }
            else
            {
                stoppDistance = rTarget;
            }
        }

        // If target moves out of range, stop attacking and re-approach
        if (rTarget > stoppDistance)
        {

            isApproaching = true;
            isAttacking = false;
            return;
        }

        // If target is immune, stop attacking and reset
        if (targPars.isImmune)
        {
            isAttacking = false;
            isReady = true;

            targPars.attackers.Remove(this);
            targPars.noAttackers = targPars.attackers.Count;
            return;
        }

        // If it's time for an attack, calculate damage
        if (Time.time > nextAttack)
        {
            nextAttack = Time.time + (attackRate - randomAttackRange);

            // Attack strength vs defence check (random factor)
            if (UnityEngine.Random.value > (strength / (strength + defence)))
            {
                // Archer attack
                if (UnitType == "Archer")
                {
                    playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Shoot45, direction, default);
                    LaunchArrowDelay(targPars, transform.position);
                }
                // Melee attack
                else
                {
                    playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Attack, direction, default);
                    targPars.health -= (10f + UnityEngine.Random.Range(0f, 15f)); // Damage range
                }
            }
        }
        else
        {
            // If no attack, idle or do other logic if needed (like defending)
            // playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, direction, default);
        }
    }


    // The healing function which handles health regeneration
    internal void SelfHealing(float deltaTime)
    {
        // If health is less than max health, start healing
        if (health < maxHealth)
        {
            // If the unit is already dead (health < 0), stop healing and set immune state
            if (health < 0f)
            {
                isHealing = false;
                isImmune = true;
                isDying = true;
            }
            else
            {
                // If health is still below max health, perform healing
                isHealing = true;
                health += selfHealFactor * deltaTime;

                // Ensure health doesn't exceed maxHealth
                if (health > maxHealth)
                {
                    health = maxHealth;
                    isHealing = false;  // Stop healing once max health is reached
                }
            }
        }
    }

    public bool IsDead { get; set; }
    internal void UpdateAnimation(float deltaTime)
    {

        if (this.IsEnemy)
        {

        }
        //play animations
        if (this.playAnimationCust.forced)
        {
            this.spriteSheetData = UnitAnimationCust.PlayAnimForced(/*ref prepSheetUnitPars, */this.playAnimationCust.baseAnimType, this.playAnimationCust.animDir, this.playAnimationCust.onComplete
                                                                                 , this.UnitType, this.IsEnemy);

        }
        else
        {
            SpriteSheetAnimationDataCust currSpriteSheetData = this.spriteSheetData;
            SpriteSheetAnimationDataCust? newSpriteSheetData = UnitAnimationCust.PlayAnim(/*ref prepSheetUnitPars, */this.playAnimationCust.baseAnimType, currSpriteSheetData, this.playAnimationCust.animDir, this.playAnimationCust.onComplete
                                                                                          , this.UnitType, this.IsEnemy);

            // if changes
            if (newSpriteSheetData != null)
            {
                this.spriteSheetData = newSpriteSheetData.Value;
            }
        }




        if (IsDead) return; // Skip animation update if unit is dead

        // Random frame logic
        if (randomFrame)
        {
            currentFrame = UnityEngine.Random.Range(0, spriteSheetData.frameCount);
            randomFrame = false;
        }

        // Update frame timer
        frameTimer -= deltaTime;
        while (frameTimer < 0)
        {
            frameTimer += spriteSheetData.frameRate;
            currentFrame = (currentFrame + 1) % spriteSheetData.frameCount;

            // If the frame count is exceeded, increment loop count
            if (currentFrame >= spriteSheetData.frameCount)
            {
                loopCount++;
            }

            // Update the material with the current frame
            var frameMaterial = spriteSheetData.materials[currentFrame];
            springAttractScreenRend.materials = new Material[] { frameMaterial };
        }
    }


    public float deathTime; // time unit was dead
    public void HandleDeath()
    {
        if (isDying)
        {
            // If unit has been dying for too long, move to the sinking phase and remove it
            if (deathCalls > maxDeathCalls)
            {
                isDying = false;
                isSinking = true;

                // Disable NavMeshAgent to stop the unit from moving
                UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.enabled = false;
                }

                // Add to deadUnits and remove from active units
                BattleSystemCust.active.allUnits.Remove(this);
                BattleSystemCust.active.deadUnits.Add(this);

                // Optional: You can add logic here to free up resources or trigger any clean-up animations.
            }
            else
            {
                // If not ready for sinking, just keep the unit in the dying state
                PlayDeathAnimation();

                // Stop the unit from moving or interacting with the world
                isMovable = false;
                isReady = false;
                isApproaching = false;
                isAttacking = false;
                isApproachable = false;
                isHealing = false;
                target = null;

                // Unselect the unit and remove from manual control
                ManualControlCust manualControl = GetComponent<ManualControlCust>();
                if (manualControl != null)
                {
                    manualControl.isSelected = false;
                }

                // Make sure the unit is untagged, so it won't interact with other game elements
                gameObject.tag = "Untagged";

                // Set destination to its current position to stop movement
                UnityEngine.AI.NavMeshAgent nma = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (nma != null)
                {
                    nma.SetDestination(transform.position);
                    nma.avoidancePriority = 0;
                }

                // Increment the deathCalls to track how long it's been in the dying state
                deathCalls++;
            }
        }
    }

    private void PlayDeathAnimation()
    {
        //if (playAnimationCust != null)
        //{
        playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Die, lastDirection, default);
        //}
    }


}





