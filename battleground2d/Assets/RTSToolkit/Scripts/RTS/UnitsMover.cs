using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class UnitsMover : MonoBehaviour
    {
        public static UnitsMover active;

        RTSMaster rtsm;

        [HideInInspector] public List<UnitPars> militaryAvoiders = new List<UnitPars>();
        [HideInInspector] public List<UnitPars> cursedWalkers = new List<UnitPars>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
        }

        float tMoveCursedWalkers = 0f;
        void Update()
        {
            float dt = Time.deltaTime;
            MoveMilitaryAvoiders();
            tMoveCursedWalkers = tMoveCursedWalkers + dt;

            if (tMoveCursedWalkers > 0.1f)
            {
                tMoveCursedWalkers = 0f;
                MoveCursedWalkers();
            }
        }

        public void AddMilitaryAvoider(UnitPars up, Vector3 pos, int completionMark)
        {
            float rad = up.rEnclosed;
            if (up.thisNMA != null)
            {
                rad = up.thisNMA.radius;
            }

            string idle = "";
            if (up.thisUA != null)
            {
                idle = up.GetIdleAnimation();
            }

            string walk = "";
            if (up.thisUA != null)
            {
                walk = up.GetWalkAnimation();
            }

            AddMilitaryAvoider(up, pos, walk, idle, rad, completionMark);
        }

        public void AddCursedWalker(UnitPars up)
        {
            if (cursedWalkers.Contains(up) == false)
            {
                if (up.unitParsType.isBuilding == false)
                {
                    up.AssignTarget(null);
                    up.militaryMode = 500;
                    cursedWalkers.Add(up);
                    Vector3 randPos = TerrainProperties.RandomTerrainVectorOnCircleProc(up.transform.position, 40f);
                    AddMilitaryAvoider(up, randPos, 0);
                }
            }
        }

        public void AddMilitaryAvoider(UnitPars up, Vector3 pos, string animation, string animationOnComplete, float stopDistance, int completionMark)
        {
            if (rtsm.useAStar)
            {
                up.agentPars.stopDistance = stopDistance;
            }
            else
            {
                up.thisNMA.stoppingDistance = stopDistance;
            }

            if (up.thisUA != null)
            {
                if (up.thisUA.animName == up.thisUA.GetAttackAnimation())
                {
                    up.thisUA.PlayAnimationCheck(up.thisUA.GetIdleAnimation());
                }
            }

            up.MoveUnit(pos);
            up.um_staticPosition = pos;
            up.um_previousPosition = pos;
            up.um_animationOnMove = animation;
            up.um_animationOnComplete = animationOnComplete;
            up.um_stopDistance = stopDistance;
            up.um_completionMark = completionMark;
            up.hasPath = true;

            if (militaryAvoiders.Contains(up) == false)
            {
                militaryAvoiders.Add(up);
            }

            up.um_isOnMilitaryAvoiders = true;
        }

        public void CompleteMovement(UnitPars up)
        {
            CompleteMovement(up, true);
        }

        public bool CompleteMovement(UnitPars up, bool stopAnimAndMotion)
        {
            bool pass = false;

            if (up.um_isOnMilitaryAvoiders)
            {
                FinishMilitaryAvoider(up, stopAnimAndMotion);
                pass = true;
            }

            up.hasPath = false;
            return pass;
        }

        public void FinishMilitaryAvoider(UnitPars up, bool stopAnimAndMotion)
        {
            militaryAvoiders.Remove(up);
            up.um_isOnMilitaryAvoiders = false;

            if (stopAnimAndMotion)
            {
                if (up.unitParsType.isWorker)
                {
                    if (up.thisUA != null)
                    {
                        if (up.um_animationOnComplete == "Idle" || up.um_animationOnComplete == "IdlePouch" || up.um_animationOnComplete == "IdleLog")
                        {
                            up.um_animationOnComplete = up.GetIdleAnimation();
                        }
                    }
                }

                up.StopUnit(up.um_animationOnComplete);
            }

            up.um_complete = up.um_completionMark;
            up.um_completionMark = 0;
            up.um_staticPosition = Vector3.zero;
            up.um_stopDistance = 0f;

            if (stopAnimAndMotion)
            {
                up.um_animationOnComplete = "";
            }
        }

        public void RemoveCursedWalker(UnitPars up)
        {
            up.militaryMode = 10;
            cursedWalkers.Remove(up);
        }

        int iInnerMoveMilitaryAvoiders = 0;
        public void MoveMilitaryAvoiders()
        {
            int nToLoop = GenericMath.FloatToIntRandScaled((1f * militaryAvoiders.Count) / 10f);

            if (militaryAvoiders.Count < nToLoop)
            {
                nToLoop = militaryAvoiders.Count;
            }

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerMoveMilitaryAvoiders >= militaryAvoiders.Count)
                {
                    iInnerMoveMilitaryAvoiders = 0;
                }

                UnitPars up = militaryAvoiders[iInnerMoveMilitaryAvoiders];

                if (up != null)
                {
                    CheckMovementAnimations(up, up.um_animationOnMove, up.um_animationOnComplete);
                    float rStop = up.um_stopDistance;

                    if (up.militaryMode == 10)
                    {
                        if ((up.transform.position - up.um_staticPosition).sqrMagnitude < rStop * rStop)
                        {
                            FinishMilitaryAvoider(up, true);
                        }
                    }

                    if (up.unitParsType.isWorker == false)
                    {
                        float dist_to_dest = (up.transform.position - up.um_staticPosition).magnitude;
                        Vector3 deltaMoved = up.transform.position - up.um_previousPosition;
                        Vector2 deltaMoved2d = new Vector2(deltaMoved.x, deltaMoved.z);

                        if (deltaMoved2d.magnitude <= 0)
                        {
                            up.failPath = up.failPath + 1;

                            if (up.failPath > up.unitParsType.maxFailPath)
                            {
                                up.failPath = 0;
                                up.MoveUnit(up.um_staticPosition, up.um_animationOnMove);
                            }
                        }

                        up.remainingPathDistance = dist_to_dest;
                        up.um_previousPosition = up.transform.position;
                    }
                }

                iInnerMoveMilitaryAvoiders++;
            }
        }

        int iInnerMoveCursedWalkers = 0;
        void MoveCursedWalkers()
        {
            int nToLoop = GenericMath.FloatToIntRandScaled((1f * cursedWalkers.Count) / 20f);

            if (cursedWalkers.Count < nToLoop)
            {
                nToLoop = cursedWalkers.Count;
            }

            for (int i = 0; i < nToLoop; i++)
            {
                if (iInnerMoveCursedWalkers >= cursedWalkers.Count)
                {
                    iInnerMoveCursedWalkers = 0;
                }

                UnitPars up = cursedWalkers[iInnerMoveCursedWalkers];

                if (up != null)
                {
                    Vector3 randPos = TerrainProperties.RandomTerrainVectorOnCircleProc(up.transform.position, 40f);
                    AddMilitaryAvoider(up, randPos, 0);

                    up.AssignTarget(null);
                    up.militaryMode = 500;
                }

                iInnerMoveCursedWalkers++;
            }
        }

        public void CheckMovementAnimations(UnitPars up, string movingAnim, string idleAnim)
        {
            float navSpeed = 1f;

            if (up.thisNMA != null)
            {
                navSpeed = up.thisNMA.speed;
            }

            if ((up.velocityVector.magnitude > 0.05f * navSpeed) && (up.velocityVector.magnitude <= 8f))
            {
                if (up.unitParsType.isWorker)
                {
                    if ((up.thisUA.animName == up.GetIdleAnimation()) || (up.thisUA.animName == up.thisUA.GetIdleAnimation()))
                    {
                        up.thisUA.PlayAnimationCheck(up.GetWalkAnimation());
                    }
                }
                else
                {
                    if (up.thisUA.animName == idleAnim)
                    {
                        up.thisUA.PlayAnimationCheck(movingAnim);
                    }

                    if ((up.rtsUnitId == 18) && (up.thisUA.animName == up.thisUA.GetRunAnimation()))
                    {
                        up.thisUA.PlayAnimationCheck(up.GetWalkAnimation());
                    }
                }
            }

            if (up.velocityVector.magnitude > 8f)
            {
                if (up.rtsUnitId == 18)
                {
                    if (up.thisUA.animName != up.thisUA.GetRunAnimation())
                    {
                        up.thisUA.PlayAnimationCheck(up.thisUA.GetRunAnimation());
                    }
                }
            }

            if (up.velocityVector.magnitude < 0.05f * navSpeed)
            {
                if (up.unitParsType.isWorker == false)
                {
                    if (up.thisUA.animName == movingAnim)
                    {
                        up.thisUA.PlayAnimationCheck(idleAnim);
                    }

                    if (up.rtsUnitId == 18)
                    {
                        if (up.thisUA.animName == up.thisUA.GetRunAnimation())
                        {
                            up.thisUA.PlayAnimationCheck(idleAnim);
                        }
                    }
                }
                else
                {
                    if ((up.thisUA.animName == up.GetWalkAnimation()) || (up.thisUA.animName == up.thisUA.GetWalkAnimation()))
                    {
                        up.thisUA.PlayAnimationCheck(up.GetIdleAnimation());
                    }
                }
            }
        }
    }
}
