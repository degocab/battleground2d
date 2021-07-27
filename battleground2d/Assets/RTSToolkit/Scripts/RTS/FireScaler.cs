using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class FireScaler : MonoBehaviour
    {
        ParticleSystem ps;
        public float scaleFactor = 1.1f;
        float sfaleFactor2;

        public GameObject lightGo;
        Light lght;

        [HideInInspector] public float currentSize = 1f;
        [HideInInspector] public UnitPars objectOnFire;
        [HideInInspector] public ResourcePointObject rpoOnFire;

        bool spreadRunning = false;

        public FireScalersPool pool;
        [HideInInspector] public GameObject buildingFirePrefab;

        public float burstRate = 0.15f;
        public float burstTime = 40f;
        public float spreadStartTime = 20f;

        public float spreadTime = 100f;
        public float spreadProbability = 0.15f;

        public float newNodeDistanceMin = 5f;
        public float newNodeDistanceMax = 6.5f;

        public float damagePerFireSize = 0.3f;

        public float burnOutProbability = 0.005f;
        public static List<FireScaler> allFireScalers = new List<FireScaler>();

        void Start()
        {
            allFireScalers.Add(this);
        }

        public void Ignite()
        {
            currentSize = 1f;

            if (pool == null)
            {
                pool = new FireScalersPool();
            }

            pool.fireScalers.Add(this);
            pool.firePositions.Add(this.transform.position);
            pool.firesKD = KDTree.MakeFromPoints(pool.firePositions.ToArray());

            ps = GetComponent<ParticleSystem>();

            ParticleSystem.MainModule psMain = ps.main;
            psMain.startLifetime = Mathf.Pow(scaleFactor, 0.4f) * psMain.startLifetime.constant;
            psMain.startSize = Mathf.Pow(scaleFactor, 0.3f) * psMain.startSize.constant;

            ParticleSystem.EmissionModule emission = ps.emission;
            ParticleSystem.MinMaxCurve rate = emission.rateOverTime;
            rate.curveMultiplier = scaleFactor * rate.curveMultiplier;
            rate.constantMin = scaleFactor * rate.constantMin;
            rate.constantMax = scaleFactor * rate.constantMax;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.radius = scaleFactor * sh.radius;

            lght = lightGo.GetComponent<Light>();
            lght.range = scaleFactor * lght.range;

            sfaleFactor2 = scaleFactor;

            if (this.gameObject.activeSelf)
            {
                StartCoroutine(SpreadFire());
                StartCoroutine(HitObject());
            }
        }

        public static void SearchAndIgniteFires(
            Vector3 ignitionSourcePosition,
            bool setOnIgnitionSourceNotUnit,
            float fireProbabilityUnitsPerUnitTime,
            float fireProbabilityForestPerUnitTime,
            float deltaTime,
            float distanceTolerance
        )
        {
            float fireProbabilityUnits = fireProbabilityUnitsPerUnitTime * deltaTime;
            float fireProbabilityForest = fireProbabilityForestPerUnitTime * deltaTime;

            bool ignitionApplied = false;

            if (Random.value < fireProbabilityUnits)
            {
                UnitPars target = KDHelper.FindNearestUPExcept(ignitionSourcePosition, RTSMaster.active.allUnits, RTSMaster.active.allUnitsKD, 19);

                if (target != null)
                {
                    if ((target.transform.position - ignitionSourcePosition).sqrMagnitude < distanceTolerance * distanceTolerance)
                    {
                        Vector3 positionToSet = target.transform.position;

                        if (setOnIgnitionSourceNotUnit)
                        {
                            positionToSet = ignitionSourcePosition;
                        }

                        FireScaler.SetUpOnFire(target, positionToSet, Quaternion.identity);
                        ignitionApplied = true;
                    }
                }
            }

            if (ignitionApplied == false)
            {
                if (Random.value < fireProbabilityForest)
                {
                    ResourcePointObject rpo = ResourcePointObject.FindNearestTerrainTreeProc(ignitionSourcePosition);

                    if (rpo != null)
                    {
                        Vector3 rpoxz = new Vector3(rpo.position.x, 0, rpo.position.z);
                        Vector3 ignitionSourcePositionXZ = new Vector3(ignitionSourcePosition.x, 0, ignitionSourcePosition.z);

                        if ((rpoxz - ignitionSourcePositionXZ).sqrMagnitude < distanceTolerance * distanceTolerance)
                        {
                            Vector3 terPosThis = TerrainProperties.TerrainVectorProc(ignitionSourcePosition);
                            float yAboveTerrain = Mathf.Abs(ignitionSourcePosition.y - terPosThis.y);

                            if (yAboveTerrain > 18)
                            {
                                yAboveTerrain = 18;
                            }

                            Vector3 posToSetFire = TerrainProperties.TerrainVectorProc(rpo.position) + new Vector3(0, yAboveTerrain, 0);

                            FireScaler.SetTreeOnFire(rpo, posToSetFire, Quaternion.identity);
                            ignitionApplied = true;
                        }
                    }
                }
            }
        }

        public static void SetUpOnFireCheck(UnitPars up, Vector3 posOnFire, Quaternion rotOnFire)
        {
            bool pass = false;

            if (up.unitParsType.isBuilding)
            {
                pass = true;
            }
            else
            {
                if (up.primaryFire == null)
                {
                    pass = true;
                }
            }

            if (pass)
            {
                SetUpOnFire(up, posOnFire, rotOnFire);
            }
        }

        public static void SetUpOnFire(UnitPars up, Vector3 posOnFire, Quaternion rotOnFire)
        {
            if (up.primaryFire == null)
            {
                Vector3 pos1 = posOnFire;

                if (up.unitParsType.isBuilding == false)
                {
                    pos1 = up.transform.position;
                }

                GameObject fire = Instantiate(RTSMaster.active.buildingFirePrefab, pos1, rotOnFire);
                FireScaler fs = fire.GetComponent<FireScaler>();
                up.primaryFire = fs;
                fs.objectOnFire = up;
                fs.rpoOnFire = null;
                fs.buildingFirePrefab = RTSMaster.active.buildingFirePrefab;

                if (up.unitParsType.isBuilding == false)
                {
                    fs.burstTime = 10f;
                    fs.damagePerFireSize = 1.0f;
                    fire.transform.parent = up.gameObject.transform;

                    if (UnitsMover.active != null)
                    {
                        if ((up.rtsUnitId != 16) && (up.rtsUnitId != 17))
                        {
                            UnitsMover.active.AddCursedWalker(up);
                        }
                    }
                }

                fs.Ignite();
            }
            else
            {
                if (up.primaryFire.pool.firesKD.FindNearest_R(posOnFire) > 5f)
                {
                    up.primaryFire.AddNewFireSource(posOnFire);

                    if (UnitsMover.active != null)
                    {
                        if ((up.rtsUnitId != 16) && (up.rtsUnitId != 17))
                        {
                            UnitsMover.active.AddCursedWalker(up);
                        }
                    }
                }
            }
        }

        public static void SetTreeOnFire(ResourcePointObject rpo, Vector3 posOnFire, Quaternion rotOnFire)
        {
            if (rpo.primaryFire == null)
            {
                Vector3 pos1 = posOnFire;
                GameObject fire = Instantiate(RTSMaster.active.buildingFirePrefab, pos1, rotOnFire);

                FireScaler fs = fire.GetComponent<FireScaler>();
                rpo.primaryFire = fs;

                fs.objectOnFire = null;
                fs.rpoOnFire = rpo;
                fs.buildingFirePrefab = RTSMaster.active.buildingFirePrefab;
                fs.Ignite();
            }
            else
            {
                if (rpo.primaryFire.pool.firesKD.FindNearest_R(posOnFire) > 5f)
                {
                    rpo.primaryFire.AddNewFireSource(posOnFire);
                }
            }
        }

        IEnumerator SpreadFire()
        {
            float waitTime = 0.2f;
            int n1 = (int)(burstTime / waitTime);
            int n2 = (int)(spreadStartTime / waitTime);

            for (int i = 0; i < n1; i++)
            {
                sfaleFactor2 = FindAddMultiplier(currentSize, burstRate * waitTime);
                currentSize = currentSize + burstRate * waitTime;
                Scaler(sfaleFactor2);
                yield return new WaitForSeconds(waitTime);

                if (objectOnFire != null)
                {
                    if (n2 < n1)
                    {
                        if (i == n2)
                        {
                            StartCoroutine(SpreadFire2());
                        }
                    }
                    else
                    {
                        if (i == (n1 - 1))
                        {
                            StartCoroutine(SpreadFire2());
                        }
                    }
                }
                else if (rpoOnFire != null)
                {
                    int n3 = n1 / 8;

                    if (n2 < n3)
                    {
                        if (i == n2)
                        {
                            StartCoroutine(SpreadFire2());
                        }
                    }
                    else
                    {
                        if (i == (n3 - 1))
                        {
                            StartCoroutine(SpreadFire2());
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.05f * (allFireScalers.Count + 1));
        }

        IEnumerator SpreadFire2()
        {
            float waitTime = 0.2f;
            int n1 = (int)(spreadTime / waitTime);

            if (rpoOnFire != null)
            {
                n1 = n1 * 1000;
            }

            for (int i = 0; i < n1; i++)
            {
                float prob = Random.value;

                if (prob > 1f - spreadProbability * waitTime)
                {
                    if (spreadRunning == false)
                    {
                        spreadRunning = true;
                        StartCoroutine(SetNewFirePoint());
                    }
                }
                else
                {
                    float probOtherObjects = Random.value;

                    if (probOtherObjects > 1f - spreadProbability * waitTime * 7f)
                    {
                        if (spreadRunning == false)
                        {
                            spreadRunning = true;
                            StartCoroutine(SetNewFirePointOnOtherObjects());
                        }
                    }
                }

                yield return new WaitForSeconds(waitTime * (allFireScalers.Count + 1));
            }
        }

        IEnumerator SetNewFirePoint()
        {
            if (objectOnFire != null)
            {
                GameObject go = objectOnFire.gameObject;

                if (go != null && go.GetComponent<MeshFilter>() != null)
                {
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = go.GetComponent<MeshFilter>().mesh;

                    yield return new WaitForEndOfFrame();

                    bool pass = false;
                    Vector3 newFirePos = Vector3.zero;

                    for (int i = 0; i < 20; i++)
                    {
                        if (pass == false)
                        {
                            Vector3 randPt = newNodeDistanceMax * Random.onUnitSphere + transform.position;
                            Vector3 randPt2 = newNodeDistanceMax * Random.onUnitSphere + transform.position;

                            Ray ray = new Ray(randPt, randPt2);
                            RaycastHit hit;

                            if (mc.Raycast(ray, out hit, newNodeDistanceMax * 3f))
                            {
                                if (pool.firePositions.Count > 1)
                                {
                                    if (pool.firesKD.FindNearestK_R(hit.point, 2) > newNodeDistanceMin)
                                    {
                                        pass = true;
                                        newFirePos = hit.point;
                                    }
                                }
                                else
                                {
                                    pass = true;
                                    newFirePos = hit.point;
                                }
                            }
                        }
                    }

                    if (pass)
                    {
                        AddNewFireSource(newFirePos);
                    }

                    Destroy(mc);
                    spreadRunning = false;
                }
            }

            if (rpoOnFire != null)
            {
                bool pass = false;
                Vector3 newFirePos = Vector3.zero;

                for (int i = 0; i < 10; i++)
                {
                    if (pass == false)
                    {
                        float h = Random.Range(-newNodeDistanceMax, newNodeDistanceMax);
                        Vector2 cir = 0.1f * newNodeDistanceMax * Random.insideUnitCircle;

                        Vector3 randPt = new Vector3(cir.x, h, cir.y) + transform.position;

                        if (pool.firePositions.Count > 1)
                        {
                            if (pool.firesKD.FindNearestK_R(randPt, 2) > newNodeDistanceMin)
                            {
                                Vector3 terVect = TerrainProperties.TerrainVectorProc(randPt);

                                if (randPt.y > terVect.y)
                                {
                                    if (randPt.y < (terVect.y + 18f))
                                    {
                                        pass = true;
                                        newFirePos = randPt;
                                    }
                                }
                            }
                        }
                        else
                        {
                            pass = true;
                            newFirePos = randPt;
                        }
                    }
                }

                if (pass)
                {
                    AddNewFireSource(newFirePos);
                }
            }

            spreadRunning = false;

            yield return new WaitForEndOfFrame();
        }

        IEnumerator SetNewFirePointOnOtherObjects()
        {
            UnitPars cand = null;

            if (RTSMaster.active.allUnitsKD != null)
            {
                int[] nearestsId = RTSMaster.active.allUnitsKD.FindNearestsK(transform.position, 8);

                if (nearestsId != null)
                {
                    if (nearestsId.Length > 0)
                    {
                        WindChanger windChanger = WindChanger.active;

                        if (windChanger != null)
                        {
                            for (int i = 0; i < nearestsId.Length; i++)
                            {
                                int nearestId = nearestsId[i];

                                if ((nearestId > -1) && (nearestId < RTSMaster.active.allUnits.Count))
                                {
                                    UnitPars up = RTSMaster.active.allUnits[nearestId];
                                    float nearestR = (up.transform.position - transform.position).magnitude;
                                    float scaleCoef = 4f * windChanger.currentSpeed;

                                    if (up.GetComponent<MeshFilter>() == false)
                                    {
                                        scaleCoef = 3f * windChanger.currentSpeed;
                                    }

                                    if (nearestR < scaleCoef * currentSize)
                                    {
                                        float windRotationY = windChanger.transform.rotation.eulerAngles.y;
                                        Vector3 posxzThis = new Vector3(transform.position.x, 0, transform.position.z);
                                        Vector3 posxzUp = new Vector3(up.transform.position.x, 0, up.transform.position.z);

                                        Vector3 diffvect = posxzThis - posxzUp;
                                        float angl = GenericMath.Angle360(diffvect, Vector3.forward, Vector3.up);

                                        float diff = Mathf.Abs(Mathf.DeltaAngle(windRotationY, angl));

                                        if (diff < 20f)
                                        {
                                            if (cand == null)
                                            {
                                                if (up.gameObject.GetComponent<MeshFilter>() != null)
                                                {
                                                    cand = up;
                                                }
                                                else
                                                {
                                                    if (up.primaryFire == null)
                                                    {
                                                        cand = up;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (cand != null)
            {
                SetUpOnFire(cand, transform.position, transform.rotation);
            }

            ResourcePointObject rpo = null;

            if (cand == null)
            {
                // find 8 nearest trees
                Vector3 terPosThis = TerrainProperties.TerrainVectorProc(transform.position);
                ResourcePointObject[] rpos = ResourcePointObject.FindNearestsKTerrainTreeProc(terPosThis, 8);

                if (rpos != null)
                {
                    if (rpos.Length > 0)
                    {
                        WindChanger windChanger = WindChanger.active;

                        if (windChanger != null)
                        {
                            for (int i = 0; i < rpos.Length; i++)
                            {
                                if (rpos[i] != null)
                                {
                                    float nearestR = (rpos[i].position - terPosThis).magnitude;

                                    if (nearestR < 4f * windChanger.currentSpeed * currentSize)
                                    {
                                        float windRotationY = windChanger.transform.rotation.eulerAngles.y;
                                        Vector3 posxzThis = new Vector3(terPosThis.x, 0, terPosThis.z);
                                        Vector3 posxzUp = new Vector3(rpos[i].position.x, 0, rpos[i].position.z);

                                        Vector3 diffvect = posxzThis - posxzUp;
                                        float angl = GenericMath.Angle360(diffvect, Vector3.forward, Vector3.up);

                                        float diff = Mathf.Abs(Mathf.DeltaAngle(windRotationY, angl));

                                        if (diff < 20f)
                                        {
                                            if (rpo == null)
                                            {
                                                if (rpos[i].primaryFire == null)
                                                {
                                                    rpo = rpos[i];
                                                }
                                                else
                                                {
                                                    if (rpos[i].primaryFire.pool.firesKD.FindNearest_R(transform.position) > 5f)
                                                    {
                                                        rpo = rpos[i];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (rpo != null)
                {
                    float yAboveTerrain = Mathf.Abs(transform.position.y - terPosThis.y);

                    if (yAboveTerrain > 18)
                    {
                        yAboveTerrain = 18;
                    }

                    Vector3 posToSetFire = TerrainProperties.TerrainVectorProc(rpo.position) + new Vector3(0, yAboveTerrain, 0);
                    SetTreeOnFire(rpo, posToSetFire, Quaternion.identity);
                }
            }

            yield return new WaitForEndOfFrame();
            spreadRunning = false;
        }

        IEnumerator HitObject()
        {
            float waitTime = 0.2f;

            while (objectOnFire != null)
            {
                float nonBuildingModifier = 1f;

                if (objectOnFire.unitParsType.isBuilding == false)
                {
                    nonBuildingModifier = 7f / (objectOnFire.levelValues[2] + 1);
                }

                float damage = waitTime * damagePerFireSize * currentSize * nonBuildingModifier;

                float damageCoolDownTime = objectOnFire.unitParsType.damageCoolDownTime;
                float damageCoolDownMin = objectOnFire.unitParsType.damageCoolDownMin * damage;
                float damageCoolDownMax = objectOnFire.unitParsType.damageCoolDownMax * damage;

                damage = GenericMath.InterpolateClamped((Time.time - objectOnFire.lastDamageTakenTime), 0, damageCoolDownTime, damageCoolDownMax, damageCoolDownMin);

                objectOnFire.UpdateHealth(objectOnFire.health - damage);

                if (objectOnFire.health < 0f)
                {
                    Extinguish();
                }

                float burnOutPr = Random.Range(0f, 1f);
                float restore_coef = 1f;

                if (objectOnFire.isRestoring)
                {
                    restore_coef = 100f;
                }

                if (burnOutPr < restore_coef * burnOutProbability * waitTime)
                {
                    Extinguish();
                }

                yield return new WaitForSeconds(waitTime);
            }

            while ((rpoOnFire != null) && (rpoOnFire.isAlive))
            {
                int resToSubtract = GenericMath.FloatToIntRandScaled(waitTime * damagePerFireSize * currentSize);
                rpoOnFire.resourceAmount = rpoOnFire.resourceAmount - resToSubtract;

                if (rpoOnFire.resourceAmount <= 0)
                {
                    if (Economy.active.resources[rpoOnFire.resourceType].isTerrainTree)
                    {
                        ForestPlacer fp = rpoOnFire.FindForestPlacer();
                        fp.RemoveTree(rpoOnFire);
                        Extinguish();
                    }
                    else
                    {
                        ResourcePoint.active.UnsetResourcePoint(rpoOnFire);
                    }
                }

                float burnOutPr = Random.Range(0f, 1f);

                if (burnOutPr < burnOutProbability * waitTime)
                {
                    Extinguish();
                }

                yield return new WaitForSeconds(waitTime);
            }

            Extinguish();
        }

        public void Extinguish()
        {
            this.StopAllCoroutines();

            if (objectOnFire != null)
            {
                if (objectOnFire.health < 0f)
                {
                    MeshRenderer mr = objectOnFire.gameObject.GetComponent<MeshRenderer>();

                    if (mr != null)
                    {
                        for (int i = 0; i < mr.materials.Length; i++)
                        {
                            mr.materials[i].color = new Color(0.1f, 0.1f, 0.1f, 1f);
                        }
                    }
                }

                objectOnFire.fires.Remove(this);

                if ((objectOnFire.fires.Count == 0) && ((objectOnFire.primaryFire == null) || (objectOnFire.primaryFire == this)))
                {
                    if (UnitsMover.active != null)
                    {
                        UnitsMover.active.RemoveCursedWalker(objectOnFire);
                    }
                }
            }

            lightGo.SetActive(false);
            StartCoroutine(StopParticleSystem());
        }

        public IEnumerator StopParticleSystem()
        {
            var systems = GetComponentsInChildren<ParticleSystem>();
            float m_MaxLifetime = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                var mainModule = system.main;
                m_MaxLifetime = Mathf.Max(mainModule.startLifetime.constant, m_MaxLifetime);
            }

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                var emission = system.emission;
                emission.enabled = false;
            }

            yield return new WaitForSeconds(m_MaxLifetime);
            Destroy(gameObject);
        }

        public void AddNewFireSource(Vector3 pos)
        {
            GameObject insGo = Instantiate(buildingFirePrefab, pos, Quaternion.identity);
            FireScaler fs = insGo.GetComponent<FireScaler>();
            fs.buildingFirePrefab = buildingFirePrefab;

            if (objectOnFire != null)
            {
                fs.objectOnFire = objectOnFire;
                fs.rpoOnFire = null;
                objectOnFire.fires.Add(fs);
            }

            else if (rpoOnFire != null)
            {
                fs.rpoOnFire = rpoOnFire;
                fs.objectOnFire = null;
                rpoOnFire.fires.Add(fs);
            }

            fs.pool = pool;
            fs.Ignite();
        }

        void Scaler(float multiplier)
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                ParticleSystem.MainModule mainModule = system.main;

                if (system.gameObject.name == "Smoke")
                {
                    mainModule.startSize = Mathf.Pow(multiplier, 1.3f) * mainModule.startSize.constant;
                }
                else
                {
                    mainModule.startSize = multiplier * mainModule.startSize.constant;
                }

                mainModule.startSpeed = multiplier * mainModule.startSpeed.constant;

                if (system.gameObject.name == "Smoke")
                {
                    mainModule.startLifetime = Mathf.Lerp(multiplier * multiplier, 1, 0.5f) * mainModule.startLifetime.constant;
                }
                else
                {
                    mainModule.startLifetime = Mathf.Lerp(multiplier, 1, 0.5f) * mainModule.startLifetime.constant;
                }

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTime = (1f / (Mathf.Pow(multiplier, 0.8f))) * emission.rateOverTime.constant;
            }
        }

        float FindAddMultiplier(float orig, float add)
        {
            float nextPoint = orig + add;
            return nextPoint / orig;
        }

        void OnDestroy()
        {
            allFireScalers.Remove(this);
        }
    }

    public class FireScalersPool
    {
        public List<FireScaler> fireScalers = new List<FireScaler>();
        public List<Vector3> firePositions = new List<Vector3>();
        public KDTree firesKD;
    }
}
