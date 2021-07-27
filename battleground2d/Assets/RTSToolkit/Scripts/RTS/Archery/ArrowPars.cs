using UnityEngine;
using System.Collections;

namespace RTSToolkit
{
    public class ArrowPars : MonoBehaviour
    {
        [HideInInspector] public UnitPars attPars = null;
        [HideInInspector] public UnitPars targPars = null;
        UnitPars origTargPars;

        float carriedDamage = 0.0f;
        public float maxDamage = 10;
        public float buildingDamageModifier = 1f;

        [HideInInspector] public bool damageApplied = false;
        [HideInInspector] public bool destrStarted = false;

        [HideInInspector] public float oldVelocity = 1.0f;

        [HideInInspector] public bool isCosmetic = false;

        RTSMaster rtsm;
        Rigidbody rigidBody;
        Scores scores;

        public bool fireArrow = false;
        public float chanceOfFire = 0.1f;

        public GameObject arrowFragment;
        public int numberOfFragments = 0;
        public float fragmentsFallRadius = 10f;

        [HideInInspector] public bool searchForTraget = false;

        public Light fireLight;

        public bool lookToFlyDirection = true;
        bool searchForAllTargets = true;
        public float searchForAllTargetsProbability = 0f;

        public float arrowRemovalDelay = 10f;

        public float attackAreaDamageRadius = 10f;

        public float failureErrorScale = 0.5f;
        public float failureErrorLevelOfset = 10f;

        bool hasColider = false;

        void Start()
        {
            rtsm = RTSMaster.active;
            scores = Scores.active;
            rigidBody = GetComponent<Rigidbody>();
            oldVelocity = rigidBody.velocity.sqrMagnitude;

            searchForAllTargets = false;
            if (Random.value < searchForAllTargetsProbability)
            {
                searchForAllTargets = true;
            }

            float def = 1f;
            if (targPars != null)
            {
                def = (5f / (5f + targPars.defence));
            }

            carriedDamage = Random.Range(0f, maxDamage) * def;
            if (targPars != null)
            {
                if (targPars.unitParsType.isBuilding)
                {
                    carriedDamage = buildingDamageModifier * carriedDamage;
                }
            }

            BoxCollider bcol = GetComponent<BoxCollider>();
            if (bcol != null)
            {
                float minDist = 0f;
                float maxDist = 400f;

                float curDistSq = (transform.position - RTSCamera.active.transform.position).sqrMagnitude;
                float randomDistance = Random.Range(minDist, maxDist);

                if (curDistSq > randomDistance * randomDistance)
                {
                    bcol.enabled = false;
                }
                else
                {
                    hasColider = true;
                }
            }

            StartCoroutine(LookToFlyDirectionCor());
            origTargPars = targPars;
            timeSinceLaunch = 0f;

            ArrowSystem.active.AddArrowPars(this);
        }

        float timeSinceLaunch = 0f;
        public void Updater()
        {
            timeSinceLaunch = timeSinceLaunch + Time.deltaTime;

            if (lookToFlyDirection)
            {
                transform.LookAt((rigidBody.velocity).normalized + transform.position);
            }

            if (destrStarted == false)
            {
                if (damageApplied == false)
                {
                    if (targPars != null && attPars != null && targPars != attPars)
                    {
                        float goDist = (targPars.transform.position - transform.position).sqrMagnitude;

                        if (isCosmetic == false)
                        {
                            if (goDist < targPars.rEnclosed * targPars.rEnclosed)
                            {
                                ApplyDamage();
                            }
                        }
                    }
                }
            }

            if (damageApplied == false)
            {
                if (searchForTraget && fireArrow)
                {

                    FireScaler.SearchAndIgniteFires(
                        transform.position,
                        false,
                        0.1f,
                        0.1f,
                        1,
                        1f
                    );

                }
            }

            if (transform.position.y < -1.0f)
            {
                if (hasColider == false)
                {
                    Destroy(this.gameObject);
                }
            }

            if (hasColider)
            {
                if (timeSinceLaunch > arrowRemovalDelay)
                {
                    DestrPhase();
                }
            }
        }

        public void UpdateSearcheableTargets()
        {
            if (destrStarted == false)
            {
                if (damageApplied == false)
                {
                    if (searchForAllTargets)
                    {
                        SearchForAllTargets();
                    }
                }
            }
        }

        void SearchForAllTargets()
        {
            targPars = RTSMaster.active.GetNearestUnit(transform.position);

            if ((origTargPars != null) && (origTargPars != targPars) && (targPars != null))
            {
                float distSq = (origTargPars.transform.position - targPars.transform.position).sqrMagnitude;

                if (distSq > attackAreaDamageRadius * attackAreaDamageRadius)
                {
                    targPars = origTargPars;
                }
            }
        }

        void ApplyDamage()
        {
            float damageCoolDownTime = targPars.unitParsType.damageCoolDownTime;
            float damageCoolDownMin = targPars.unitParsType.damageCoolDownMin * carriedDamage;
            float damageCoolDownMax = targPars.unitParsType.damageCoolDownMax * carriedDamage;

            carriedDamage = GenericMath.InterpolateClamped((Time.time - targPars.lastDamageTakenTime), 0, damageCoolDownTime, damageCoolDownMax, damageCoolDownMin);
            targPars.lastDamageTakenTime = Time.time;

            targPars.UpdateHealth(targPars.health - carriedDamage);

            float damageDefended = maxDamage - carriedDamage;

            if (attPars.levelExp != null)
            {
                attPars.AddExp(0, 0.5f * carriedDamage);
                attPars.AddExp(1, carriedDamage);
            }

            float lowRat = 1f;

            if (carriedDamage > 0 && damageDefended > 0 && maxDamage > 0)
            {
                if ((carriedDamage / maxDamage) < lowRat)
                {
                    lowRat = carriedDamage / maxDamage;
                }
                if ((damageDefended / maxDamage) < lowRat)
                {
                    lowRat = damageDefended / maxDamage;
                }
            }
            else
            {
                lowRat = 0f;
            }

            targPars.AddExp(0, 0.5f * damageDefended * lowRat);
            targPars.AddExp(2, damageDefended * lowRat);


            damageApplied = true;

            if ((attPars.nation >= 0) && (attPars.nation < scores.damageObtained.Count))
            {
                scores.damageMade[attPars.nation] = scores.damageMade[attPars.nation] + carriedDamage;
            }

            if ((targPars.nation >= 0) && (targPars.nation < scores.damageObtained.Count))
            {
                scores.damageObtained[targPars.nation] = scores.damageObtained[targPars.nation] + carriedDamage;
            }

            if (targPars.health < 0)
            {
                if (targPars.health + carriedDamage > 0)
                {
                    if (targPars.nation != attPars.nation)
                    {
                        rtsm.nationPars[targPars.nation].nationAI.beatenUnits[attPars.nation] = rtsm.nationPars[targPars.nation].nationAI.beatenUnits[attPars.nation] + 1;
                    }
                }
            }

            float fireProb = Random.Range(0f, 1f);

            if (fireArrow && (fireProb < chanceOfFire))
            {
                if (targPars.rtsUnitId != 19)
                {
                    FireScaler.SetUpOnFireCheck(targPars, transform.position, transform.rotation);
                }
            }

            DecayIntoFragments();
        }

        void DecayIntoFragments()
        {
            if (numberOfFragments > 0)
            {
                if (arrowFragment != null)
                {
                    for (int i = 0; i < numberOfFragments; i++)
                    {
                        Vector3 fragmentFallPos = TerrainProperties.RandomTerrainVectorCircleProc(transform.position, fragmentsFallRadius);
                        if (BattleSystem.active.CanHitCoordinate(transform.position, fragmentFallPos, Vector3.zero, attPars.unitParsType.velArrow, 0f))
                        {
                            BattleSystem.active.LaunchArrowInner(arrowFragment, transform.position, fragmentFallPos, attPars.unitParsType.velArrow);
                        }
                    }
                }
            }
        }

        void DestrPhase()
        {
            if (this.gameObject != null)
            {
                Destroy(this.gameObject);
            }
        }

        void OnDestroy()
        {
            ArrowSystem.active.RemoveArrowPars(this);
        }

        IEnumerator LookToFlyDirectionCor()
        {
            bool lookToFlyDirection1 = lookToFlyDirection;
            lookToFlyDirection = true;
            yield return new WaitForSeconds(0.1f);
            lookToFlyDirection = lookToFlyDirection1;
        }
    }
}
