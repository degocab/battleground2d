using UnityEngine;
using System.Collections.Generic;

// BSystem is core component for simulating RTS battles
// It has 6 phases for attack and gets all different game objects parameters inside.
// Attack phases are: Search, Approach target, Attack, Self-Heal, Die, Rot (Sink to ground).
// All 6 phases are running all the time and checking if object is matching criteria, then performing actions
// Movements between different phases are also described

namespace RTSToolkitFree
{
    public class BattleSystem : MonoBehaviour
    {
        public static BattleSystem active;

        public bool showStatistics = false;

        public List<UnitPars> allUnits = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> sinks = new List<UnitPars>();

        List<List<UnitPars>> targets = new List<List<UnitPars>>();
        List<float> targetRefreshTimes = new List<float>();
        List<KDTree> targetKD = new List<KDTree>();

        public int randomSeed = 0;

        public float searchUpdateFraction = 0.1f;
        public float retargetUpdateFraction = 0.01f;
        public float approachUpdateFraction = 0.1f;
        public float attackUpdateFraction = 0.1f;
        public float selfHealUpdateFraction = 0.01f;
        public float deathUpdateFraction = 0.05f;
        public float sinkUpdateFraction = 1f;

        void Awake()
        {
            active = this;
            Random.InitState(randomSeed);
        }

        void Start()
        {
            UnityEngine.AI.NavMesh. pathfindingIterationsPerFrame = 10000;
        }

        void Update()
        {
            if (showStatistics)
            {
                BattleSystemStatistics.UpdateWithStatistics(this, Time.deltaTime);
            }
            else
            {
                UpdateWithoutStatistics();
            }
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
            SinkPhase(deltaTime);
            ManualMover();
        }

        void OnGUI()
        {
            // Display performance UI
            if (showStatistics)
            {
                BattleSystemStatistics.OnGUI();
            }
        }

        Rect GUIRect(float height)
        {
            return new Rect(Screen.width * 0.05f, Screen.height * height, 500f, 20f);
        }

        int iSearchPhase = 0;
        float fSearchPhase = 0f;

        // The main search method, which starts to search for nearest enemies neighbours and set them for attack
        // NN search works with kdtree.cs NN search class, implemented by A. Stark at 2009.
        // Target candidates are put on kdtree, while attackers used to search for them.
        // NN searches are based on position coordinates in 3D.
        public void SearchPhase(float deltaTime)
        {
            // Refresh targets list
            for (int i = 0; i < targetRefreshTimes.Count; i++)
            {
                targetRefreshTimes[i] -= deltaTime;
                if (targetRefreshTimes[i] < 0f)
                {
                    targetRefreshTimes[i] = 1f;

                    List<UnitPars> nationTargets = new List<UnitPars>();
                    List<Vector3> nationTargetPositions = new List<Vector3>();

                    for (int j = 0; j < allUnits.Count; j++)
                    {
                        UnitPars up = allUnits[j];

                        if (
                            up.nation != i &&
                            up.isApproachable &&
                            up.health > 0f &&
                            up.attackers.Count < up.maxAttackers &&
                            Diplomacy.active.relations[up.nation][i] == 1
                        )
                        {
                            nationTargets.Add(up);
                            nationTargetPositions.Add(up.transform.position);
                        }
                    }

                    targets[i] = nationTargets;
                    targetKD[i] = KDTree.MakeFromPoints(nationTargetPositions.ToArray());
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

                UnitPars up = allUnits[iSearchPhase];
                int nation = up.nation;

                if (up.isReady && targets[nation].Count > 0)
                {
                    int targetId = targetKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = targets[nation][targetId];

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

        // Similar as SearchPhase but is used to retarget approachers to closer targets.
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

                UnitPars up = allUnits[iRetargetPhase];
                int nation = up.nation;

                if (up.isApproaching && up.target != null && targets[nation].Count > 0)
                {
                    int targetId = targetKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = targets[nation][targetId];

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

                UnitPars apprPars = allUnits[iApproachPhase];

                if (apprPars.isApproaching && apprPars.target != null)
                {

                    UnitPars targ = apprPars.target;

                    UnityEngine.AI.NavMeshAgent apprNav = apprPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targ.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    if (targ.isApproachable == true)
                    {
                        // stopping condition for NavMesh

                        apprNav.stoppingDistance = apprNav.radius / (apprPars.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                        // distance between approacher and target

                        float rTarget = (apprPars.transform.position - targ.transform.position).magnitude;
                        float stoppDistance = (2f + apprPars.transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);

                        // counting increased distances (failure to approach) between attacker and target;
                        // if counter failedR becomes bigger than critFailedR, preparing for new target search.

                        if (apprPars.prevR <= rTarget)
                        {
                            apprPars.failedR = apprPars.failedR + 1;
                            if (apprPars.failedR > apprPars.critFailedR)
                            {
                                apprPars.isApproaching = false;
                                apprPars.isReady = true;
                                apprPars.failedR = 0;

                                if (apprPars.target != null)
                                {
                                    apprPars.target.attackers.Remove(apprPars);
                                    apprPars.target.noAttackers = apprPars.target.attackers.Count;
                                    apprPars.target = null;
                                }

                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.yellow;
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

                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.red;
                                }
                            }
                            else
                            {
                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.green;
                                }

                                // starting to move
                                if (apprPars.isMovable)
                                {
                                    Vector3 destination = apprNav.destination;
                                    if ((destination - targ.transform.position).sqrMagnitude > 1f)
                                    {
                                        apprNav.SetDestination(targ.transform.position);
                                        apprNav.speed = 3.5f;
                                    }
                                }
                            }
                        }

                        // saving previous R
                        apprPars.prevR = rTarget;
                    }
                    // condition for non approachable targets	
                    else
                    {
                        apprPars.target = null;
                        apprNav.SetDestination(apprPars.transform.position);

                        apprPars.isApproaching = false;
                        apprPars.isReady = true;

                        if (apprPars.changeMaterial)
                        {
                            apprPars.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                }
            }
        }

        int iAttackPhase = 0;
        float fAttackPhase = 0f;

        // Attacking phase set attackers to attack their targets and cause damage when they already approached their targets
        public void AttackPhase()
        {
            fAttackPhase += allUnits.Count * attackUpdateFraction;

            int nToLoop = (int)fAttackPhase;
            fAttackPhase -= nToLoop;

            // checking through allUnits list which units are set to approach (isAttacking)
            for (int i = 0; i < nToLoop; i++)
            {
                iAttackPhase++;

                if (iAttackPhase >= allUnits.Count)
                {
                    iAttackPhase = 0;
                }

                UnitPars attPars = allUnits[iAttackPhase];

                if (attPars.isAttacking && attPars.target != null)
                {
                    UnitPars targPars = attPars.target;

                    UnityEngine.AI.NavMeshAgent attNav = attPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targPars.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    attNav.stoppingDistance = attNav.radius / (attPars.transform.localScale.x) + targNav.radius / (targPars.transform.localScale.x);

                    // distance between attacker and target

                    float rTarget = (attPars.transform.position - targPars.transform.position).magnitude;
                    float stoppDistance = (2.5f + attPars.transform.localScale.x * targPars.transform.localScale.x * attNav.stoppingDistance);

                    // if target moves away, resetting back to approach target phase

                    if (rTarget > stoppDistance)
                    {
                        attPars.isApproaching = true;
                        attPars.isAttacking = false;
                    }
                    // if targets becomes immune, attacker is reset to start searching for new target 	
                    else if (targPars.isImmune == true)
                    {
                        attPars.isAttacking = false;
                        attPars.isReady = true;

                        targPars.attackers.Remove(attPars);
                        targPars.noAttackers = targPars.attackers.Count;

                        if (attPars.changeMaterial)
                        {
                            attPars.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                    // attacker starts attacking their target	
                    else
                    {
                        if (attPars.changeMaterial)
                        {
                            attPars.GetComponent<Renderer>().material.color = Color.red;
                        }

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

        int iSelfHealingPhase = 0;
        float fSelfHealingPhase = 0f;

        // Self-Healing phase heals damaged units over time
        public void SelfHealingPhase(float deltaTime)
        {
            fSelfHealingPhase += allUnits.Count * selfHealUpdateFraction;

            int nToLoop = (int)fSelfHealingPhase;
            fSelfHealingPhase -= nToLoop;

            // checking which units are damaged	
            for (int i = 0; i < nToLoop; i++)
            {
                iSelfHealingPhase++;

                if (iSelfHealingPhase >= allUnits.Count)
                {
                    iSelfHealingPhase = 0;
                }

                UnitPars shealPars = allUnits[iSelfHealingPhase];

                if (shealPars.health < shealPars.maxHealth)
                {
                    // if unit has less health than 0, preparing it to die
                    if (shealPars.health < 0f)
                    {
                        shealPars.isHealing = false;
                        shealPars.isImmune = true;
                        shealPars.isDying = true;
                    }
                    // healing unit	
                    else
                    {
                        shealPars.isHealing = true;
                        shealPars.health += shealPars.selfHealFactor * deltaTime / selfHealUpdateFraction;

                        // if unit health reaches maximum, unset self-healing
                        if (shealPars.health >= shealPars.maxHealth)
                        {
                            shealPars.health = shealPars.maxHealth;
                            shealPars.isHealing = false;
                        }
                    }
                }
            }
        }

        int iDeathPhase = 0;
        float fDeathPhase = 0f;

        // Death phase unset all unit activity and prepare to die
        public void DeathPhase()
        {
            fDeathPhase += allUnits.Count * deathUpdateFraction;

            int nToLoop = (int)fDeathPhase;
            fDeathPhase -= nToLoop;

            // Getting dying units		
            for (int i = 0; i < nToLoop; i++)
            {
                iDeathPhase++;

                if (iDeathPhase >= allUnits.Count)
                {
                    iDeathPhase = 0;
                }

                UnitPars deadPars = allUnits[iDeathPhase];

                if (deadPars.isDying)
                {
                    // If unit is dead long enough, prepare for rotting (sinking) phase and removing from the unitss list
                    if (deadPars.deathCalls > deadPars.maxDeathCalls)
                    {
                        deadPars.isDying = false;
                        deadPars.isSinking = true;

                        deadPars.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                        sinks.Add(deadPars);
                        allUnits.Remove(deadPars);

                        for (int j = 0; j < targetRefreshTimes.Count; j++)
                        {
                            targetRefreshTimes[j] = -1f;
                        }
                    }
                    // unsetting unit activity and keep it dying	
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
                        ManualControl manualControl = deadPars.GetComponent<ManualControl>();

                        if (manualControl != null)
                        {
                            manualControl.isSelected = false;
                            UnitControls.active.Refresh();
                        }

                        deadPars.transform.gameObject.tag = "Untagged";

                        UnityEngine.AI.NavMeshAgent nma = deadPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        nma.SetDestination(deadPars.transform.position);
                        nma.avoidancePriority = 0;

                        deadPars.deathCalls++;

                        if (deadPars.changeMaterial)
                        {
                            deadPars.GetComponent<Renderer>().material.color = Color.blue;
                        }
                    }
                }
            }
        }

        int iSinkPhase = 0;
        float fSinkPhase = 0f;

        // rotting or sink phase includes time before unit is destroyed: for example to perform rotting animation or sink object into the ground
        public void SinkPhase(float deltaTime)
        {
            float sinkSpeed = -0.2f;

            fSinkPhase += sinks.Count * sinkUpdateFraction;

            int nToLoop = (int)fSinkPhase;
            fSinkPhase -= nToLoop;

            // checking in sinks array, which is already different from main units array
            for (int i = 0; i < sinks.Count; i++)
            {
                iSinkPhase++;

                if (iSinkPhase >= sinks.Count)
                {
                    iSinkPhase = 0;
                }

                UnitPars sinkPars = sinks[iSinkPhase];

                if (sinkPars.isSinking)
                {
                    if (sinkPars.changeMaterial)
                    {
                        sinkPars.GetComponent<Renderer>().material.color = new Color((148.0f / 255.0f), (0.0f / 255.0f), (211.0f / 255.0f), 1.0f);
                    }

                    // moving sinking object down into the ground	
                    if (sinkPars.transform.position.y > -1.0f)
                    {
                        sinkPars.transform.position += new Vector3(0f, sinkSpeed * deltaTime / sinkUpdateFraction, 0f);
                    }
                    // destroy object if it has sinked enough
                    else
                    {
                        sinks.Remove(sinkPars);
                        Destroy(sinkPars.gameObject);
                    }
                }
            }
        }

        int iManualMover = 0;
        float fManualMover = 0f;

        // ManualMover controls unit if it is selected and target is defined by player
        public void ManualMover()
        {
            fManualMover += allUnits.Count * 0.1f;

            int nToLoop = (int)fManualMover;
            fManualMover -= nToLoop;

            for (int i = 0; i < nToLoop; i++)
            {
                iManualMover++;

                if (iManualMover >= allUnits.Count)
                {
                    iManualMover = 0;
                }

                UnitPars up = allUnits[iManualMover];
                ManualControl manualControl = up.GetComponent<ManualControl>();

                if (manualControl.isMoving)
                {
                    float r = (up.transform.position - manualControl.manualDestination).magnitude;

                    if (r >= manualControl.prevDist)
                    {
                        manualControl.failedDist++;
                        if (manualControl.failedDist > manualControl.critFailedDist)
                        {
                            manualControl.failedDist = 0;
                            manualControl.isMoving = false;
                            ResetSearching(up);
                        }
                    }

                    manualControl.prevDist = r;
                }

                if (manualControl.prepareMoving)
                {
                    if (up.isMovable)
                    {
                        if (up.target == null)
                        {
                            UnSetSearching(up);
                        }

                        manualControl.prepareMoving = false;
                        manualControl.isMoving = true;

                        up.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(manualControl.manualDestination);
                    }
                }
            }
        }

        public void ResetSearching(UnitPars up)
        {
            up.isApproaching = false;
            up.isAttacking = false;
            up.target = null;

            up.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(up.transform.position);

            if (up.changeMaterial)
            {
                up.GetComponent<Renderer>().material.color = Color.yellow;
            }

            up.isReady = true;
        }

        public void UnSetSearching(UnitPars up)
        {
            up.isReady = false;
            up.isApproaching = false;
            up.isAttacking = false;
            up.target = null;

            up.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(up.transform.position);

            if (up.changeMaterial)
            {
                up.GetComponent<Renderer>().material.color = Color.grey;
            }
        }

        public void AddNation()
        {
            targets.Add(new List<UnitPars>());
            targetRefreshTimes.Add(-1f);
            targetKD.Add(null);
        }
    }
}
