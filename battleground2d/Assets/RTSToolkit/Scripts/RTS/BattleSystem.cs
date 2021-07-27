using UnityEngine;
using System.Collections.Generic;

// BSystem is core component for simulating RTS battles
// It has 6 phases for attack and gets all different game objects parameters inside.
// Attack phases are: Search, Approach target, Attack, Self-Heal, Die, Rot (Sink to ground).
// All 6 phases are running all the time and checking if object is matching criteria, then performing actions
// Movements between different phases are also described

namespace RTSToolkit
{
    public class BattleSystem : MonoBehaviour
    {
        public static BattleSystem active;

        [HideInInspector] public string message1 = " ";
        [HideInInspector] public string message2 = " ";
        [HideInInspector] public string message3 = " ";
        [HideInInspector] public string message4 = " ";
        [HideInInspector] public string message5 = " ";
        [HideInInspector] public string message6 = " ";

        [HideInInspector] public List<UnitPars> unitssUP;

        [HideInInspector] public List<UnitPars> selfHealers = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> deads = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> sinks = new List<UnitPars>();

        Scores scores;
        RTSMaster rtsm;
        Diplomacy diplomacy;

        [HideInInspector] public bool lockUpdate = true;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            scores = Scores.active;
            diplomacy = Diplomacy.active;

            unitssUP = rtsm.allUnits;
            lockUpdate = false;

            UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;
        }

        void Update()
        {
            if (lockUpdate == false)
            {
                ApproachTargetPhase();
                AttackPhase();
                UpdateAttackDelays();
                SelfHealingPhase();
                DeathPhase();
                SinkPhase();
                TowerAttack();
                ManualRestorer();
                PathResetter();
                UnitsVelocities();
                UnitForestVelocities();
            }
        }

        int iInnerApproach = 0;
        void ApproachTargetPhase()
        {
            float Rtarget;
            float stoppDistance;

            int noApproachers = 0;

            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 10f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerApproach >= unitssUP.Count)
                {
                    iInnerApproach = 0;
                }

                UnitPars apprPars = unitssUP[iInnerApproach];

                if (apprPars.militaryMode == 20)
                {
                    UnitPars targPars = apprPars.targetUP;
                    bool relationCont = true;

                    if (targPars != null)
                    {
                        if (diplomacy.relations[apprPars.nation][targPars.nation] != 1)
                        {
                            if (apprPars.nation != targPars.nation)
                            {
                                if (apprPars.strictApproachMode == false)
                                {
                                    relationCont = false;
                                    ResetSearching(apprPars);
                                }
                            }
                        }
                    }

                    if ((relationCont) && (apprPars.militaryMode == 20))
                    {
                        if (targPars != null)
                        {
                            if (targPars.isDying == false)
                            {
                                // stopping condition for NavMesh

                                float aRadius = 0f;
                                float tRadius = 0f;
                                stoppDistance = 0f;

                                if (rtsm.useAStar)
                                {
                                    aRadius = apprPars.agentPars.radius;

                                    if (targPars.unitParsType.isBuilding == false)
                                    {
                                        tRadius = targPars.agentPars.radius;
                                        apprPars.agentPars.stopDistance = 0f;
                                        stoppDistance = apprPars.unitParsType.stopDistOut + aRadius + tRadius;
                                    }
                                }
                                else
                                {
                                    aRadius = apprPars.thisNMA.radius;

                                    if (targPars.unitParsType.isBuilding == false)
                                    {
                                        tRadius = apprPars.targetUP.thisNMA.radius;
                                        apprPars.thisNMA.stoppingDistance = 0f;
                                        stoppDistance = apprPars.unitParsType.stopDistOut + aRadius + tRadius;
                                    }
                                }

                                Rtarget = (targPars.transform.position - apprPars.transform.position).magnitude;

                                if (targPars.unitParsType.isBuilding)
                                {
                                    stoppDistance = 0f;

                                    if (apprPars.unitParsType.isArcher == false)
                                    {
                                        if ((GetClosestBuildingPoint(apprPars, targPars) - apprPars.transform.position).magnitude < (aRadius + 0.5f))
                                        {
                                            stoppDistance = 1.5f * Rtarget;
                                            apprPars.thisNMA.stoppingDistance = 0f;
                                        }
                                    }
                                }

                                // counting increased distances (failure to approach) between attacker and target;
                                // if counter failedR becomes bigger than critFailedR, preparing for new target search.

                                if (apprPars.strictApproachMode)
                                {
                                    // for manual approachers

                                    float sD = 0.0f;

                                    if (apprPars.unitParsType.isArcher == false)
                                    {
                                        sD = stoppDistance;
                                    }
                                    else if (apprPars.unitParsType.isArcher)
                                    {
                                        if (CanHitCoordinate(apprPars.transform.position, targPars.transform.position, targPars.velocityVector, apprPars.unitParsType.velArrow, 0.4f))
                                        {
                                            sD = 1.5f * Rtarget;
                                        }
                                        else
                                        {
                                            sD = 0f;
                                        }
                                    }

                                    if (Rtarget < sD)
                                    {
                                        UnitsMover.active.CompleteMovement(apprPars);

                                        // pre-setting for attacking
                                        apprPars.militaryMode = 30;
                                    }
                                    else
                                    {
                                        // starting to move

                                        if (apprPars.isMovable)
                                        {
                                            noApproachers = noApproachers + 1;
                                            Vector3 closesPos = GetClosestBuildingPoint(apprPars, targPars);

                                            if ((closesPos - apprPars.targetPos).sqrMagnitude > 0.04f)
                                            {
                                                UnitsMover.active.AddMilitaryAvoider(apprPars, closesPos, 0);
                                                apprPars.targetPos = closesPos;
                                            }
                                        }
                                    }
                                }

                                if (apprPars.prevR < Rtarget)
                                {
                                    apprPars.failedR = apprPars.failedR + 1;

                                    if (apprPars.failedR > apprPars.unitParsType.critFailedR)
                                    {
                                        apprPars.AssignTarget(null);

                                        if (targPars.isAttackable == false)
                                        {
                                            if (targPars.attackers.Count < targPars.unitParsType.maxAttackers)
                                            {
                                                targPars.isAttackable = true;
                                            }
                                        }

                                        apprPars.militaryMode = 10;
                                        apprPars.failedR = 0;

                                        UnitsMover.active.CompleteMovement(apprPars);
                                    }
                                }
                                else
                                {
                                    // if approachers already close to their targets

                                    float sD = 0.0f;

                                    if (apprPars.unitParsType.isArcher)
                                    {
                                        if (CanHitCoordinate(apprPars.transform.position, targPars.transform.position, targPars.velocityVector, apprPars.unitParsType.velArrow, 0.4f) == true)
                                        {
                                            sD = 1.5f * Rtarget;
                                        }
                                        else
                                        {
                                            sD = 0f;
                                        }
                                    }
                                    else
                                    {
                                        sD = stoppDistance;
                                    }

                                    if (Rtarget < sD)
                                    {
                                        UnitsMover.active.CompleteMovement(apprPars);

                                        // pre-setting for attacking
                                        apprPars.militaryMode = 30;
                                    }
                                    else
                                    {
                                        // starting to move
                                        if (apprPars.isMovable)
                                        {
                                            noApproachers = noApproachers + 1;
                                            Vector3 closesPos = GetClosestBuildingPoint(apprPars, targPars);

                                            if ((closesPos - apprPars.targetPos).sqrMagnitude > 0.04f)
                                            {
                                                UnitsMover.active.AddMilitaryAvoider(apprPars, closesPos, 0);
                                                apprPars.targetPos = closesPos;
                                            }
                                        }
                                    }
                                }

                                // saving previous R
                                apprPars.prevR = Rtarget;
                            }
                            // condition for non approachable targets	
                            else
                            {
                                apprPars.AssignTarget(null);

                                if (targPars.isAttackable == false)
                                {
                                    if (targPars.attackers.Count < targPars.unitParsType.maxAttackers)
                                    {
                                        targPars.isAttackable = true;
                                    }
                                }

                                UnitsMover.active.CompleteMovement(apprPars);
                                apprPars.militaryMode = 10;
                            }

                            // finding targets from attackers if they are closer and more reasonable to approach
                            if (targPars.targetUP != apprPars)
                            {
                                if (apprPars.attackers != null)
                                {
                                    if (apprPars.attackers.Count > 0)
                                    {
                                        int randTargId = Random.Range(0, apprPars.attackers.Count);
                                        UnitPars target2 = apprPars.attackers[randTargId];

                                        if (target2 != null)
                                        {
                                            Vector3 apprPos = apprPars.transform.position;
                                            if (
                                                (target2.transform.position - apprPos).sqrMagnitude
                                                <
                                                (targPars.transform.position - apprPos).sqrMagnitude
                                            )
                                            {
                                                apprPars.AssignTarget(target2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                iInnerApproach++;
            }
        }

        public static Vector3 GetClosestBuildingPoint(UnitPars apprPars, UnitPars targPars)
        {
            if (targPars.thisNMO == null)
            {
                return targPars.transform.position;
            }

            if (targPars.unitParsType.isBuilding == false)
            {
                return targPars.transform.position;
            }

            float apprSize = 2f;

            if (RTSMaster.active.useAStar)
            {
                apprSize = apprPars.agentPars.radius;
            }
            else
            {
                apprSize = apprPars.thisNMA.radius * 2f;
            }

            Vector3 boundsCenter = targPars.thisNMO.center;
            Vector3 boundsSize = targPars.thisNMO.size;
            Vector3 upScale = targPars.transform.localScale;

            boundsCenter = new Vector3(boundsCenter.x * upScale.x, boundsCenter.y * upScale.y, boundsCenter.z * upScale.z);
            boundsCenter = RTSMaster.active.rtsUnitTypePrefabs[targPars.rtsUnitId].transform.rotation * boundsCenter;
            boundsCenter = boundsCenter + targPars.transform.position;

            boundsSize = new Vector3(boundsSize.x * upScale.x, boundsSize.y * upScale.y, boundsSize.z * upScale.z);
            boundsSize = RTSMaster.active.rtsUnitTypePrefabs[targPars.rtsUnitId].transform.rotation * boundsSize;
            boundsSize = new Vector3(Mathf.Abs(boundsSize.x), Mathf.Abs(boundsSize.y), Mathf.Abs(boundsSize.z));

            Bounds bounds = new Bounds(boundsCenter, boundsSize + new Vector3(apprSize, apprSize, apprSize));
            Vector3 aPos = apprPars.transform.position;

            aPos = aPos - targPars.transform.position;
            aPos = GenericMath.RotAround(targPars.transform.eulerAngles.y, aPos, Vector3.up);
            aPos = aPos + targPars.transform.position;

            aPos = bounds.ClosestPoint(aPos);

            aPos = aPos - targPars.transform.position;
            aPos = GenericMath.RotAround(-targPars.transform.eulerAngles.y, aPos, Vector3.up);
            aPos = aPos + targPars.transform.position;

            return aPos;
        }

        int iInnerAttack = 0;

        void AttackPhase()
        {
            // Attacking phase set attackers to attack their targets and cause damage when they already approached their targets

            float Rtarget;
            float stoppDistance;

            UnitPars attPars = null;
            UnitPars targPars = null;

            Vector3 attPos = Vector3.zero;
            Vector3 targPos = Vector3.zero;

            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 8f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            int noAttackers = 0;

            // checking through main unitss array which units are set to approach (isAttacking)

            for (int iu = 0; iu < nToLoop; iu++)
            {
                if (iInnerAttack >= unitssUP.Count)
                {
                    iInnerAttack = 0;
                }

                attPars = unitssUP[iInnerAttack];

                if (attPars.militaryMode == 30)
                {
                    attPos = attPars.transform.position;

                    if (attPars.hasPath)
                    {
                        UnitsMover.active.CompleteMovement(attPars);
                    }

                    targPars = attPars.targetUP;
                    targPos = targPars.transform.position;

                    bool relationCont = true;

                    if (diplomacy.relations[attPars.nation][targPars.nation] != 1)
                    {
                        if (attPars.nation != targPars.nation)
                        {
                            if (attPars.strictApproachMode == false)
                            {
                                relationCont = false;
                                ResetSearching(attPars);
                            }
                        }
                    }

                    if (relationCont == true)
                    {

                        float aRadius = 0f;
                        float tRadius = 0f;
                        stoppDistance = 0f;

                        if (rtsm.useAStar)
                        {
                            aRadius = attPars.agentPars.radius;

                            if (targPars.unitParsType.isBuilding == false)
                            {
                                tRadius = targPars.agentPars.radius;
                                attPars.agentPars.stopDistance = 0;
                                stoppDistance = attPars.unitParsType.stopDistOut + aRadius + tRadius;
                            }
                        }
                        else
                        {
                            aRadius = attPars.thisNMA.radius;

                            if (targPars.unitParsType.isBuilding == false)
                            {
                                tRadius = attPars.targetUP.thisNMA.radius;
                                attPars.thisNMA.stoppingDistance = 0;
                                stoppDistance = attPars.unitParsType.stopDistOut + aRadius + tRadius;
                            }
                        }

                        // distance between attacker and target

                        Rtarget = (targPos - attPos).magnitude;

                        if (targPars.unitParsType.isBuilding)
                        {
                            stoppDistance = targPars.rEnclosed + attPars.unitParsType.stopDistOut + aRadius;

                            if (attPars.unitParsType.isArcher == false)
                            {
                                if ((GetClosestBuildingPoint(attPars, targPars) - attPars.transform.position).magnitude < (aRadius + 0.6f))
                                {
                                    stoppDistance = 1.5f * Rtarget;
                                    attPars.thisNMA.stoppingDistance = 0f;
                                }
                            }
                        }

                        // auto-correction for archers, who can't reach their targets in large enough distance

                        float sD = 0.0f;

                        if (attPars.unitParsType.isArcher == false)
                        {
                            sD = stoppDistance;
                        }
                        else if (attPars.unitParsType.isArcher)
                        {
                            if (CanHitCoordinate(attPos, targPos, targPars.velocityVector, attPars.unitParsType.velArrow, 0.1f) == true)
                            {
                                sD = 1.5f * Rtarget;
                            }
                            else
                            {
                                sD = 0f;
                            }
                        }

                        // if targets becomes immune, attacker is reset to start searching for new target

                        if ((targPars.isDying) || (targPars.isSinking) || (targPars.health < 0))
                        {
                            targPars.CleanAttackers();
                            attPars.AssignTarget(null);
                        }
                        else if (Rtarget > sD)
                        {
                            attPars.militaryMode = 20;
                            UnitsMover.active.AddMilitaryAvoider(attPars, targPars.transform.position, 0);
                        }
                        // attacker starts attacking their target	
                        else
                        {
                            noAttackers = noAttackers + 1;

                            // if attack passes target through target defence, cause damage to target

                            if ((attPars.isDying == false) && (attPars.isSinking == false) && (attPars.health > 0))
                            {
                                if (Time.time - attPars.timeMark > attPars.unitParsType.attackWaiter)
                                {
                                    attPars.timeMark = Time.time;

                                    if (attPars.unitParsType.isBuilding == false)
                                    {
                                        attPars.transform.LookAt(targPars.transform);
                                    }

                                    if (attPars.unitParsType.isArcher)
                                    {
                                        attPars.thisUA.PlayAnimation(attPars.thisUA.GetAttackAnimation());
                                        attPars.LaunchArrowDelay(targPars, attPars.transform.position);
                                    }
                                    else if (attPars.unitParsType.hasBullets)
                                    {
                                        BulletShooter bshoot = attPars.gameObject.GetComponent<BulletShooter>();
                                        if (bshoot != null)
                                        {
                                            bshoot.Launch();
                                        }
                                    }
                                    else
                                    {
                                        attPars.thisUA.PlayAnimation(attPars.thisUA.GetAttackAnimation());
                                        attPars.isAttackDelayRunning = true;
                                        attPars.attackDelayStartTime = Time.time;
                                        attPars.attackDelayCorPass = true;
                                    }
                                }
                            }
                        }
                    }
                }

                iInnerAttack++;
            }
        }

        int iInnerAttackDelays = 0;

        void UpdateAttackDelays()
        {
            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 8f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            float time = Time.time;

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerAttackDelays >= unitssUP.Count)
                {
                    iInnerAttackDelays = 0;
                }

                UnitPars up = unitssUP[iInnerAttackDelays];

                if (up.isAttackDelayRunning)
                {
                    if (time - up.attackDelayStartTime > up.unitParsType.attackDelay)
                    {
                        up.AttackDelay();
                    }
                }

                iInnerAttackDelays++;
            }
        }

        public void UpdateBeatenUnitScores(UnitPars attPars, UnitPars targPars)
        {
            if (targPars.unitParsType.isBuilding)
            {
                scores.AddToMasterScoreDiff(0.5f * targPars.totalLevel, attPars.nation);
            }
            else if (targPars.unitParsType.isBuilding == false)
            {
                scores.AddToMasterScoreDiff(0.05f * targPars.totalLevel, attPars.nation);
            }

            if (targPars.unitParsType.isBuilding)
            {
                scores.AddToMasterScoreDiff(-1f, targPars.nation);
            }
            else if (targPars.unitParsType.isBuilding == false)
            {
                scores.AddToMasterScoreDiff(-0.1f, targPars.nation);
            }
        }

        int iInnerSHeal = 0;
        void SelfHealingPhase()
        {
            // Self-Healing phase heals damaged units over time

            int nToLoop = GenericMath.FloatToIntRandScaled((1f * selfHealers.Count) / 40f);

            if (selfHealers.Count < nToLoop)
            {
                nToLoop = selfHealers.Count;
            }

            // checking which units are damaged	

            float dt = Time.deltaTime;

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerSHeal >= selfHealers.Count)
                {
                    iInnerSHeal = 0;
                }

                UnitPars sheal = selfHealers[iInnerSHeal];

                if (sheal.health < sheal.maxHealth)
                {
                    // if unit has less health than 0, preparing it to die
                    if (sheal.health < 0.0f)
                    {
                        MakeDead(sheal);
                    }
                    // healing unit	
                    else
                    {
                        if ((sheal.unitParsType.isBuilding == false) || (sheal.selfHealFactor < 0))
                        {
                            if (sheal.health < sheal.maxHealth)
                            {
                                float newHealth = sheal.health + sheal.selfHealFactor * dt * selfHealers.Count / nToLoop;

                                if (newHealth > sheal.maxHealth)
                                {
                                    newHealth = sheal.maxHealth;
                                }

                                sheal.UpdateHealth(newHealth);
                            }
                        }
                    }
                }

                iInnerSHeal++;
            }
        }

        public void MakeDead(UnitPars up)
        {
            if (up.isDying == false)
            {
                up.PlayDeathSound();

                if (up.thisUA != null)
                {
                    up.thisUA.PlayAnimation(up.thisUA.GetDeathAnimation());
                }

                if (up.unitParsType.isWizzard)
                {
                    WizzardLightning wizzardLightning = up.GetComponent<WizzardLightning>();

                    if (wizzardLightning != null)
                    {
                        wizzardLightning.TriggerAreaRandomStrikes();
                    }
                }
            }

            up.isDying = true;
            selfHealers.Remove(up);

            SelectionManager.active.DeselectObject(up);

            if ((up.nation >= 0) && (up.nation < rtsm.nationPars.Count))
            {
                rtsm.nationPars[up.nation].nationAI.UnsetUnit(up);
                rtsm.nationPars[up.nation].wandererAI.RemoveUnit(up);
                rtsm.nationPars[up.nation].resourcesCollection.RemoveFromResourcesCollection(up);
            }

            rtsm.unitsListByType[up.rtsUnitId].Remove(up);
            UnitsMover.active.CompleteMovement(up);

            if (up.nation > -1 && up.nation < rtsm.numberOfUnitTypes.Count)
            {
                if (up.rtsUnitId > -1 && up.rtsUnitId < rtsm.numberOfUnitTypes[up.nation].Count)
                {
                    rtsm.numberOfUnitTypes[up.nation][up.rtsUnitId] = rtsm.numberOfUnitTypes[up.nation][up.rtsUnitId] - 1;
                }
            }

            UnitsGrouping.active.RemoveUnitFromGroup(up);
            Formations.active.RemoveUnitFromFormation(up);

            RemoveUnitFromBS(up);

            if (!(deads.Contains(up)))
            {
                deads.Add(up);
            }

            up.CleanAttackers();

            for (int i = 0; i < rtsm.nationPars.Count; i++)
            {
                if (rtsm.nationPars[i] != null)
                {
                    if (rtsm.nationPars[i].battleAI != null)
                    {
                        rtsm.nationPars[i].battleAI.RemoveTarget(up);
                    }
                }
            }

            SelectionManager.active.RefreshCentralBuildingMenuOnHeroPresence(up);
            GameOver.active.CheckIfHeroAndCentralBuildingAreDestroyed();
        }


        int iInnerDeath = 0;

        void DeathPhase()
        {
            // Death phase unset all unit activity and prepare to die

            int nToLoop = GenericMath.FloatToIntRandScaled((1f * deads.Count) / 3f);

            if (deads.Count < nToLoop)
            {
                nToLoop = deads.Count;
            }

            // Getting dying units		

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerDeath >= deads.Count)
                {
                    iInnerDeath = 0;
                }

                UnitPars dead = deads[iInnerDeath];

                if (dead.isDying)
                {
                    // If unit is dead long enough, prepare for rotting (sinking) phase and removing from the unitss list
                    if (dead.deathCalls > dead.unitParsType.maxDeathCalls)
                    {
                        int nation = dead.nation;

                        if ((nation > -1) && (nation < rtsm.nationPars.Count))
                        {
                            rtsm.nationPars[nation].nationAI.UnsetUnit(dead);
                        }

                        dead.isDying = false;
                        dead.isSinking = true;

                        deads.Remove(dead);

                        if (dead.isSelected == true)
                        {
                            CameraSwitcher.active.ResetFromUnit(dead);
                            SelectionManager.active.DeselectObject(dead);
                        }

                        if (rtsm.useAStar)
                        {
                            if (dead.agentPars != null)
                            {
                                dead.agentPars.enabled = false;
                            }
                        }
                        else
                        {
                            if (dead.unitParsType.isBuilding == false)
                            {
                                dead.thisNMA.enabled = false;
                            }
                        }

                        sinks.Add(dead);
                        unitssUP.Remove(dead);
                    }
                    // unsetting unit activity and keep it dying	
                    else
                    {
                        dead.isMovable = false;
                        dead.isAttackable = false;
                        dead.militaryMode = -1;

                        if (dead.deathCalls == 0)
                        {
                            if (dead.targetUP != null)
                            {
                                dead.AssignTarget(null);
                            }

                            if ((dead.nation > -1) && (dead.nation < rtsm.nationPars.Count))
                            {
                                rtsm.nationPars[dead.nation].resourcesCollection.RemoveFromResourcesCollection(dead);
                            }
                        }

                        // unsetting attackers			
                        dead.AssignTarget(null);
                        dead.CleanAttackers();

                        // unsetting movement	
                        UnitsMover.active.CompleteMovement(dead);

                        if (rtsm.useAStar)
                        {
                            if (dead.agentPars != null)
                            {
                                dead.agentPars.maxSpeed = 0f;
                                dead.agentPars.RemoveFromManualRVOs();
                            }
                        }
                        else
                        {
                            if (dead.unitParsType.isBuilding == false)
                            {
                                UnityEngine.AI.NavMeshAgent navM = dead.thisNMA;

                                if (navM.enabled == true)
                                {
                                    navM.speed = 0f;
                                }
                            }
                        }

                        dead.deathCalls = dead.deathCalls + 1;

                        // unselecting deads							
                        if (dead.isSelected)
                        {
                            CameraSwitcher.active.ResetFromUnit(dead);
                            SelectionManager.active.DeselectObject(dead);
                        }
                    }
                }

                iInnerDeath++;
            }
        }

        int iInnerSink = 0;
        void SinkPhase()
        {
            // rotting or sink phase includes time before unit is destroyed: for example to perform rotting animation or sink object into the ground

            int nToLoop = GenericMath.FloatToIntRandScaled((1f * sinks.Count) / 5f);

            if (sinks.Count < nToLoop)
            {
                nToLoop = sinks.Count;
            }

            float dt = Time.deltaTime;
            float dv = -0.01f;
            Vector3 dpos = new Vector3(0, (dt * dv * sinks.Count) / nToLoop, 0);

            // checking in sinks array, which is already different from main units array
            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerSink >= sinks.Count)
                {
                    iInnerSink = 0;
                }

                UnitPars sink = sinks[iInnerSink];

                if (sink.isSinking)
                {
                    // moving sinking object down into the ground	
                    if (TerrainProperties.HeightFromTerrain(sink.transform.position) > -2.0f)
                    {
                        sink.transform.position = sink.transform.position + dpos;
                    }
                    // destroy object if it has sinked enough	
                    else
                    {
                        sinks.Remove(sink);

                        if (sink.unitParsType.isBuilding == false)
                        {
                            scores.nUnits[sink.nation] = scores.nUnits[sink.nation] - 1;
                            scores.unitsLost[sink.nation] = scores.unitsLost[sink.nation] + 1;
                        }
                        else if (sink.unitParsType.isBuilding == true)
                        {
                            scores.nBuildings[sink.nation] = scores.nBuildings[sink.nation] - 1;
                            scores.buildingsLost[sink.nation] = scores.buildingsLost[sink.nation] + 1;
                        }

                        rtsm.DestroyUnit(sink);
                    }
                }

                iInnerSink++;
            }
        }

        int iInnerTowerAttack = 0;
        void TowerAttack()
        {
            int nToLoop = 20;
            int nTot = rtsm.unitsListByType[10].Count;

            if (iInnerTowerAttack >= nToLoop)
            {
                iInnerTowerAttack = 0;
            }

            for (int i = 0; i < nTot; i++)
            {
                int iModified = i + iInnerTowerAttack;

                if (iModified % nToLoop == 0)
                {
                    UnitPars up = rtsm.unitsListByType[10][i];

                    if (up.isBuildFinished)
                    {
                        if (up.isDying == false)
                        {
                            UnitPars targ = rtsm.nationPars[up.nation].battleAI.GetTarget(up.transform.position);

                            if (targ != null)
                            {
                                Vector3 launchPoint = new Vector3(up.transform.position.x, up.transform.position.y + 3f * Random.Range(1, 4), up.transform.position.z);

                                if (CanHitCoordinate(launchPoint, targ.transform.position, targ.velocityVector, up.unitParsType.velArrow, 0.1f))
                                {
                                    LaunchArrow(up, targ, launchPoint);
                                }
                            }
                        }
                    }
                }
            }

            iInnerTowerAttack++;
        }

        public void LaunchArrow(UnitPars attPars, UnitPars targPars, Vector3 launchPoint)
        {
            if ((attPars != null) && (targPars != null))
            {
                LaunchArrowInner(attPars, targPars, launchPoint, false);

                if (rtsm.isMultiplayer)
                {
                    rtsm.rtsCameraNetwork.Cmd_LaunchArrow(attPars.gameObject, targPars.gameObject, launchPoint, diplomacy.GetPlayerNationName());
                }
            }
        }

        public void LaunchArrowInner(UnitPars attPars, UnitPars targPars, Vector3 launchPoint1, bool isCosmetic)
        {
            Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
            Vector3 launchPoint = launchPoint1 + attPars.unitParsType.arrowOffset;

            if (attPars != null && targPars != null)
            {
                Vector3 arrForce2 = LaunchDirection(launchPoint, targPars.transform.position, targPars.velocityVector, attPars.unitParsType.velArrow);
                float failureError = 0f;

                if (attPars.unitParsType.arrow != null)
                {
                    ArrowPars arp = attPars.unitParsType.arrow.GetComponent<ArrowPars>();

                    if (arp != null)
                    {
                        if (1 < attPars.levelValues.Length)
                        {
                            failureError = arp.failureErrorScale / (arp.failureErrorLevelOfset + attPars.levelValues[1]);
                        }
                    }
                }

                float magBeforeError = arrForce2.magnitude;
                arrForce2 = arrForce2 + Random.insideUnitSphere * arrForce2.magnitude * failureError;
                arrForce2 = arrForce2.normalized * magBeforeError;

                if ((arrForce2.sqrMagnitude > 0.0f) && (arrForce2.y != -Mathf.Infinity) && (arrForce2.y != Mathf.Infinity))
                {
                    if (attPars.unitParsType.arrow != null)
                    {
                        GameObject arroww = (GameObject)Instantiate(attPars.unitParsType.arrow, launchPoint, rot);
                        arroww.GetComponent<Rigidbody>().AddRelativeForce(arrForce2, ForceMode.VelocityChange);

                        ArrowPars arrPars = arroww.GetComponent<ArrowPars>();

                        if (arrPars != null)
                        {
                            arrPars.attPars = attPars;
                            arrPars.targPars = targPars;
                            arrPars.isCosmetic = isCosmetic;
                        }
                    }
                }
            }
        }

        public void LaunchArrowInner(GameObject arrow, Vector3 launchPoint, Vector3 targetPoint, float velocity)
        {
            Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
            GameObject arroww = (GameObject)Instantiate(arrow, launchPoint, rot);

            Vector3 arrForce2 = LaunchDirection(launchPoint, targetPoint, Vector3.zero, velocity);

            if ((arrForce2.sqrMagnitude > 0.0f) && (arrForce2.y != -Mathf.Infinity) && (arrForce2.y != Mathf.Infinity))
            {
                arroww.GetComponent<Rigidbody>().AddRelativeForce(arrForce2, ForceMode.VelocityChange);
            }
            else
            {
                Destroy(arroww.gameObject);
            }
        }

        public void AddFromBuffer(UnitPars goPars)
        {
            if (goPars.onManualControl == false)
            {
                goPars.militaryMode = 10;
            }

            goPars.isAttackable = true;
            unitssUP.Add(goPars);
        }

        // ManualMover controls unit if it is selected and target is defined by player
        int iInnerManualRestorer = 0;

        void ManualRestorer()
        {
            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 30f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerManualRestorer >= unitssUP.Count)
                {
                    iInnerManualRestorer = 0;
                }

                UnitPars goPars = unitssUP[iInnerManualRestorer];

                if (goPars.strictApproachMode == false)
                {
                    if (goPars.isMovingMC)
                    {
                        float r = (goPars.transform.position - goPars.manualDestination).magnitude;

                        if (r >= goPars.prevDist)
                        {
                            if (r < 50f)
                            {
                                goPars.failedDist = goPars.failedDist + 1;

                                if (goPars.failedDist > (r * 0.3f))
                                {
                                    goPars.failedDist = 0;
                                    goPars.onManualControl = false;
                                    goPars.isMovingMC = false;
                                    ResetSearching(goPars);
                                }
                            }
                        }

                        goPars.prevDist = r;
                    }
                }

                iInnerManualRestorer++;
            }
        }

        // single action functions	
        public void AddSelfHealer(UnitPars up)
        {
            selfHealers.Add(up);
        }

        public void ResetSearching(UnitPars goPars)
        {
            if (goPars.targetUP != null)
            {
                goPars.AssignTarget(null);
            }

            UnitsMover.active.CompleteMovement(goPars);
            goPars.militaryMode = 10;
        }

        public void RemoveUnitFromBS(UnitPars up)
        {
            unitssUP.Remove(up);

            for (int i = 0; i < unitssUP.Count; i++)
            {
                if (unitssUP[i].targetUP == up)
                {
                    ResetSearching(unitssUP[i]);
                }
            }

            selfHealers.Remove(up);
            deads.Remove(up);
            sinks.Remove(up);
        }

        public void UnSetSearching(UnitPars goPars, bool completeMovement)
        {
            goPars.militaryMode = -1;

            if (goPars.targetUP != null)
            {
                goPars.AssignTarget(null);
            }

            if (completeMovement)
            {
                UnitsMover.active.CompleteMovement(goPars);
            }
        }

        int iInnerPathResetter = 0;
        void PathResetter()
        {
            // resets paths for stuck units
            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 50f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerPathResetter >= unitssUP.Count)
                {
                    iInnerPathResetter = 0;
                }

                UnitPars up_go = unitssUP[iInnerPathResetter];
                UnityEngine.AI.NavMeshAgent nav_go = up_go.thisNMA;

                if (up_go.unitParsType.isWorker)
                {
                    // workers walking phases
                    if ((up_go.chopTreePhase == 1) || (up_go.chopTreePhase == 3) || (up_go.chopTreePhase == 11) || (up_go.chopTreePhase == 3))
                    {
                        if (up_go.fakePathMode == 1)
                        {
                            up_go.fakePathCount = up_go.fakePathCount + 1;

                            if (up_go.fakePathCount > up_go.unitParsType.maxFakePathCount)
                            {
                                up_go.fakePathMode = 0;
                                up_go.fakePathCount = 0;

                                if (!rtsm.useAStar)
                                {
                                    nav_go.ResetPath();
                                }

                                UnitsMover.active.AddMilitaryAvoider(up_go, up_go.restoreTruePath, 0);
                            }
                        }
                        else if (up_go.fakePathMode == 0)
                        {
                            Vector3 dest1 = Vector3.zero;

                            if (rtsm.useAStar)
                            {
                                dest1 = up_go.agentPars.GetTargetPosition();
                            }
                            else
                            {
                                dest1 = nav_go.destination;
                            }

                            float rSq = (up_go.transform.position - dest1).sqrMagnitude;

                            if (rSq >= up_go.remainingPathDistance)
                            {
                                up_go.failPath = up_go.failPath + 1;

                                if ((up_go.failPath > up_go.unitParsType.maxFailPath) && (up_go.chopTreePhase != 3))
                                {
                                    if (up_go.chopTreePhase == 1)
                                    {
                                        up_go.chopTreePhase = 6;
                                        up_go.fakePathMode = 0;
                                        up_go.fakePathCount = 0;
                                        up_go.remainingPathDistance = 1000000000000f;
                                    }
                                    else
                                    {
                                        up_go.failPath = 0;
                                        Vector3 dest = dest1;
                                        up_go.restoreTruePath = dest;

                                        if (!rtsm.useAStar)
                                        {
                                            nav_go.ResetPath();
                                        }

                                        Vector3 curPos = up_go.transform.position;
                                        float x = curPos.x + Random.Range(-7f, 7f);
                                        float z = curPos.z + Random.Range(-7f, 7f);
                                        Vector3 fakePos = new Vector3(x, 0f, z);

                                        up_go.MoveUnit(fakePos);
                                        up_go.fakePathMode = 1;
                                        up_go.remainingPathDistance = 1000000000000f;
                                    }
                                }
                                else if ((up_go.failPath > 80) && (up_go.chopTreePhase == 3))
                                {
                                    up_go.chopTreePhase = 6;
                                    up_go.fakePathMode = 0;
                                    up_go.fakePathCount = 0;
                                    up_go.remainingPathDistance = 1000000000000f;
                                }
                            }

                            up_go.remainingPathDistance = rSq;
                        }
                    }
                    else
                    {
                        if (up_go.fakePathMode == 1)
                        {
                            up_go.fakePathMode = 0;
                            up_go.fakePathCount = 0;
                            up_go.remainingPathDistance = 1000000000000f;
                        }
                    }
                }

                iInnerPathResetter++;
            }
        }

        int iInnerUnitsVelocities = 0;
        void UnitsVelocities()
        {
            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 10f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            float dt = Time.deltaTime;
            float velocityMultiplier = 1f / ((dt * unitssUP.Count) / nToLoop);

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerUnitsVelocities >= unitssUP.Count)
                {
                    iInnerUnitsVelocities = 0;
                }

                UnitPars up = unitssUP[iInnerUnitsVelocities];

                if (up.isSinking == false)
                {
                    if (up.isDying == false)
                    {
                        up.velocityVector = (up.transform.position - up.lastPosition) * velocityMultiplier;
                        up.lastPosition = up.transform.position;
                    }
                }

                iInnerUnitsVelocities++;
            }
        }

        int iInnerUnitForestVelocities = 0;

        void UnitForestVelocities()
        {
            // slowing down velocities when units are walking through the forest

            int nToLoop = GenericMath.FloatToIntRandScaled((1f * unitssUP.Count) / 30f);

            if (unitssUP.Count < nToLoop)
            {
                nToLoop = unitssUP.Count;
            }

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerUnitForestVelocities >= unitssUP.Count)
                {
                    iInnerUnitForestVelocities = 0;
                }

                UnitPars up = unitssUP[iInnerUnitForestVelocities];

                if (up.isSinking == false)
                {
                    if (up.isDying == false)
                    {
                        if (up.thisNMA != null)
                        {
                            if (up.lockForestSpeedChanges == false)
                            {
                                float forestCoeff = 1f;

                                if (Forest.active.IsPointInsideForest(up.transform.position))
                                {
                                    forestCoeff = 0.5f;
                                }

                                up.thisNMA.speed = forestCoeff * RTSMaster.active.rtsUnitTypePrefabsUp[up.rtsUnitId].thisNMA.speed;
                            }
                        }
                    }
                }

                iInnerUnitForestVelocities++;
            }
        }

        public Vector3 LaunchDirection(Vector3 shooterPosition, Vector3 targetPosition, Vector3 targetVolocity, float launchSpeed)
        {
            float vini = launchSpeed;

            // horizontal plane projections	
            Vector3 shooterPosition2d = new Vector3(shooterPosition.x, 0f, shooterPosition.z);
            Vector3 targetPosition2d = new Vector3(targetPosition.x, 0f, targetPosition.z);

            float Rtarget2d = (targetPosition2d - shooterPosition2d).magnitude;

            // shooter and target coordinates		
            float ax = shooterPosition.x;
            float ay = shooterPosition.y;
            float az = shooterPosition.z;

            float tx = targetPosition.x;
            float ty = targetPosition.y;
            float tz = targetPosition.z;

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

            Vector3 finalDirection = vini * arrForce + 0.5f * shTime * targetVolocity;
            return finalDirection;
        }

        public bool CanHitCoordinate(Vector3 shooterPosition, Vector3 targetPosition, Vector3 targetVolocity, float launchSpeed, float distanceIncrement)
        {
            bool canHit = false;

            float vini = launchSpeed;
            float g = 9.81f;

            Vector3 shooterPosition2d = new Vector3(shooterPosition.x, 0f, shooterPosition.z);
            Vector3 targetPosition2d = new Vector3(targetPosition.x, 0f, targetPosition.z);

            float rTarget2d = (targetPosition2d - shooterPosition2d).magnitude;
            rTarget2d = rTarget2d + distanceIncrement * rTarget2d;
            float sqrt = (vini * vini * vini * vini) - (g * (g * (rTarget2d * rTarget2d) + 2 * (targetPosition.y - shooterPosition.y) * (vini * vini)));

            if (sqrt >= 0)
            {
                canHit = true;
            }

            return canHit;
        }

        // Gets an axis aligned bound box around an array of game objects
        public static Bounds GetBounds(GameObject go)
        {
            float minX = Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxY = -Mathf.Infinity;
            float minZ = Mathf.Infinity;
            float maxZ = -Mathf.Infinity;

            Vector3[] points = new Vector3[8];

            GetBoundsPointsNoAlloc(go, points);

            foreach (Vector3 v in points)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }

            float sizeX = maxX - minX;
            float sizeY = maxY - minY;
            float sizeZ = maxZ - minZ;

            Vector3 center = new Vector3(minX + sizeX / 2.0f, minY + sizeY / 2.0f, minZ + sizeZ / 2.0f);

            return new Bounds(center, new Vector3(sizeX, sizeY, sizeZ));
        }

        // Pass in a game object and a Vector3[8], and the corners of the mesh.bounds in 
        // world space are returned in the passed array;
        public static void GetBoundsPointsNoAlloc(GameObject go, Vector3[] points)
        {
            if (points == null || points.Length < 8)
            {
                Debug.Log("Bad Array");
                return;
            }

            MeshFilter mf = go.GetComponent<MeshFilter>();

            if (mf == null)
            {
                Debug.Log("No MeshFilter on object");

                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = go.transform.position;
                }

                return;
            }

            Transform tr = go.transform;

            Vector3 v3Center = mf.mesh.bounds.center;
            Vector3 v3ext = mf.mesh.bounds.extents;

            points[0] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y + v3ext.y, v3Center.z - v3ext.z));  // Front top left corner
            points[1] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y + v3ext.y, v3Center.z - v3ext.z));  // Front top right corner
            points[2] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y - v3ext.y, v3Center.z - v3ext.z));  // Front bottom left corner
            points[3] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y - v3ext.y, v3Center.z - v3ext.z));  // Front bottom right corner
            points[4] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y + v3ext.y, v3Center.z + v3ext.z));  // Back top left corner
            points[5] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y + v3ext.y, v3Center.z + v3ext.z));  // Back top right corner
            points[6] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y - v3ext.y, v3Center.z + v3ext.z));  // Back bottom left corner
            points[7] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y - v3ext.y, v3Center.z + v3ext.z));  // Back bottom right corner
        }
    }
}
