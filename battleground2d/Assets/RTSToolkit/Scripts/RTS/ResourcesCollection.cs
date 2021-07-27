using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class ResourcesCollection : MonoBehaviour
    {
        [HideInInspector] public List<UnitPars> up_workers = new List<UnitPars>();

        public List<CollectionDeliveryPoint> collectionPoints = new List<CollectionDeliveryPoint>();
        public List<CollectionDeliveryPoint> deliveryPoints = new List<CollectionDeliveryPoint>();

        [HideInInspector] public bool coroutinesLocked = false;

        [HideInInspector] public NationPars nationPars;

        float phasesTimeStep = 0.1f;

        void Start()
        {
            SetCollectionAndDeliveryPoints();
            GetNationPars();
        }

        float tWorkerMiningPointPhases = 0f;
        void Update()
        {
            float dt = Time.deltaTime;
            tWorkerMiningPointPhases = tWorkerMiningPointPhases + dt;

            if (tWorkerMiningPointPhases > phasesTimeStep)
            {
                WorkerMiningPointPhases();
                tWorkerMiningPointPhases = 0;
            }
        }

        public void SetCollectionAndDeliveryPoints()
        {
            if ((collectionPoints.Count == 0) || (deliveryPoints.Count == 0))
            {
                collectionPoints.Clear();
                deliveryPoints.Clear();

                for (int i = 0; i < Economy.active.resources.Count; i++)
                {
                    CollectionDeliveryPoint cp = new CollectionDeliveryPoint();
                    cp.rtsUnitId = Economy.active.resources[i].collectionRtsUnitId;
                    collectionPoints.Add(cp);

                    CollectionDeliveryPoint dp = new CollectionDeliveryPoint();
                    dp.rtsUnitId = Economy.active.resources[i].deliveryRtsUnitId;
                    deliveryPoints.Add(dp);
                }
            }
        }

        public NationPars GetNationPars()
        {
            if (nationPars == null)
            {
                nationPars = GetComponent<NationPars>();
            }
            return nationPars;
        }

        public void StopAllCoroutinesHere()
        {
            coroutinesLocked = true;
        }

        public static void WalkTo(UnitPars worker, Vector3 dest)
        {
            UnitsMover.active.AddMilitaryAvoider(worker, dest, 0);
            ResetWorker(worker);
        }

        public static void ResetWorker(UnitPars worker)
        {
            worker.collectionUnit = null;
            worker.resourcePointObject = null;
            worker.chopTreePhase = -1;
        }

        public void MineResource(int resourceType, UnitPars miningPoint, ResourcePointObject rpo)
        {
            for (int i = 0; i < up_workers.Count; i++)
            {
                if (up_workers[i].isSelected == true)
                {
                    SendWorkerToCollectionPoint(up_workers[i], resourceType, miningPoint, rpo);
                }
            }
        }

        public void SendWorkerToCollectionPoint(UnitPars worker, int resourceType, UnitPars miningPoint, ResourcePointObject rpo)
        {
            Vector3 pos = Vector3.zero;

            if (miningPoint != null)
            {
                pos = miningPoint.collectionOrDeliveryPoint.position;
            }
            else if (rpo != null)
            {
                pos = rpo.position;
            }

            UnitsMover.active.AddMilitaryAvoider(worker, pos, 0);
            worker.collectionUnit = miningPoint;
            worker.resourcePointObject = rpo;
            worker.chopTreePhase = 11;
            worker.resourceType = resourceType;
        }

        public void SendWorkerToDeliveryPoint(UnitPars worker)
        {
            if (deliveryPoints[worker.resourceType].ups.Count > 0)
            {
                if (worker.resourceAmount > 0)
                {
                    int neighId = deliveryPoints[worker.resourceType].kd.FindNearest(worker.transform.position);

                    UnitsMover.active.CompleteMovement(worker);
                    worker.MoveUnit(deliveryPoints[worker.resourceType].pos[neighId], worker.GetWalkAnimation());

                    worker.deliveryPointId = neighId;
                    worker.chopTreePhase = 13;
                    worker.collectionTimeSpend = 0;

                    worker.ResetFakePath();
                }
                else
                {
                    worker.thisUA.PlayAnimation(worker.GetIdleAnimation());
                    worker.chopTreePhase = 15;

                    worker.ResetFakePath();
                }
            }
            else
            {
                UnitsMover.active.CompleteMovement(worker);
                worker.StopUnit(worker.GetIdleAnimation());
                worker.chopTreePhase = 14;
                worker.collectionTimeSpend = 0;

                worker.ResetFakePath();
            }
        }

        public void SetAutoCollection(UnitPars worker, int resourceType)
        {
            if (up_workers.Contains(worker))
            {
                worker.resourceType = resourceType;
                worker.chopTreePhase = 16;
            }
        }

        void WorkerMiningPointPhases()
        {
            if (this == null)
            {
                Debug.Log("ResourcesCollection is null");
            }

            if (coroutinesLocked == false)
            {

                for (int i = 0; i < up_workers.Count; i++)
                {
                    UnitPars worker = up_workers[i];

                    // moving towards collection point
                    if (worker.chopTreePhase == 11)
                    {
                        if (worker.resourcePointObject != null)
                        {
                            if (
                                (GenericMath.ProjectionXZ(worker.resourcePointObject.GetPosition(worker.collectionUnit) - worker.transform.position)).sqrMagnitude
                                <
                                worker.resourcePointObject.GetEffectiveDistance(worker.collectionUnit)
                            )
                            {
                                UnitsMover.active.CompleteMovement(worker);
                                string anim = Economy.active.resources[worker.resourceType].collectionAnimation;

                                if (string.IsNullOrEmpty(anim))
                                {
                                    anim = worker.thisUA.GetIdleAnimation();
                                }

                                worker.StopUnit(anim);
                                worker.chopTreePhase = 12;
                                worker.collectionTimeSpend = 0f;
                                worker.ResetFakePath();
                            }
                            else
                            {
                                worker.MoveUnit(worker.resourcePointObject.GetPosition(worker.collectionUnit), worker.GetWalkAnimation());
                            }
                        }
                        else
                        {
                            worker.chopTreePhase = 16;
                        }
                    }
                    // gathering resource	
                    else if (worker.chopTreePhase == 12)
                    {
                        if ((int)(worker.collectionTimeSpend) < (int)(worker.collectionTimeSpend + phasesTimeStep))
                        {
                            if (Economy.active.resources[worker.resourceType].collectionSound != null)
                            {
                                if ((worker.transform.position - RTSCamera.active.transform.position).magnitude < 200f)
                                {
                                    Vector3 pos1 = 0.03f * worker.transform.position + 0.97f * RTSCamera.active.transform.position;
                                    AudioSource.PlayClipAtPoint(Economy.active.resources[worker.resourceType].collectionSound, pos1, 1f);
                                }
                            }
                        }

                        worker.collectionTimeSpend = worker.collectionTimeSpend + phasesTimeStep;

                        if (worker.collectionTimeSpend > Economy.active.resources[worker.resourceType].collectionTime)
                        {
                            if (Economy.active.resources[worker.resourceType].collectionRtsUnitId >= 0)
                            {
                                UnitPars collectionUnit = worker.collectionUnit;
                                int resourceType = worker.resourceType;

                                if ((collectionUnit != null) && (collectionUnit.resourcePointObject != null))
                                {
                                    int resToTake = Economy.active.resources[worker.resourceType].collectionAmount;

                                    if (collectionUnit.resourceAmount < resToTake)
                                    {
                                        resToTake = collectionUnit.resourceAmount;
                                    }
                                    if (resToTake < 0)
                                    {
                                        resToTake = 0;
                                    }

                                    worker.resourceAmount = resToTake;
                                    collectionUnit.TakeResourcesFromMiningPoint(resToTake);

                                    if (collectionUnit.isSelected == true)
                                    {
                                        MiningPointLabelUI.active.UpdateAmount(collectionUnit.resourceAmount);
                                    }

                                    OnResourceChangeAdjustUI(worker);
                                    ResourcePointObject rpo = collectionUnit.resourcePointObject;

                                    if (collectionUnit.resourceAmount <= 0)
                                    {
                                        if (collectionUnit.selfHealFactor >= 0)
                                        {
                                            collectionUnit.selfHealFactor = -(int)(0.1f * collectionUnit.maxHealth) - 1;
                                            collectionUnit.health = collectionUnit.health + 2 * collectionUnit.selfHealFactor;
                                        }

                                        int id_1 = collectionPoints[resourceType].ups.IndexOf(collectionUnit);

                                        if (id_1 > -1)
                                        {
                                            collectionPoints[resourceType].ups.RemoveAt(id_1);
                                            collectionPoints[resourceType].pos.RemoveAt(id_1);
                                            collectionPoints[resourceType].RefreshKDTree();
                                        }

                                        RemoveFromResourcesCollection(collectionUnit);

                                        // removal of resource point from map	
                                        if (rpo.resourceAmount <= 0)
                                        {
                                            if (rpo != null)
                                            {
                                                ResourcePoint.active.UnsetResourcePoint(rpo);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                int resToTake = Economy.active.resources[worker.resourceType].collectionAmount;

                                worker.resourceAmount = resToTake;

                                if (worker.resourcePointObject.resourceAmount - resToTake < 0)
                                {
                                    worker.resourceAmount = worker.resourcePointObject.resourceAmount;
                                }

                                worker.resourcePointObject.resourceAmount = worker.resourcePointObject.resourceAmount - resToTake;
                                OnResourceChangeAdjustUI(worker);

                                if (worker.resourcePointObject.resourceAmount <= 0)
                                {
                                    if (Economy.active.resources[worker.resourceType].isTerrainTree)
                                    {
                                        ForestPlacer fp = worker.resourcePointObject.FindForestPlacer();
                                        fp.RemoveTree(worker.resourcePointObject);
                                    }
                                    else
                                    {
                                        ResourcePoint.active.UnsetResourcePoint(worker.resourcePointObject);
                                    }
                                }
                            }

                            SendWorkerToDeliveryPoint(worker);
                        }

                        if (worker.resourcePointObject != null)
                        {
                            if (worker.resourcePointObject.resourceAmount <= 0)
                            {
                                worker.resourcePointObject = null;
                                worker.collectionUnit = null;
                                worker.collectionTimeSpend = 0f;

                                if (worker.chopTreePhase == 12)
                                {
                                    worker.chopTreePhase = 15;
                                }
                            }
                        }
                    }

                    // moving towards delivery point	
                    else if (worker.chopTreePhase == 13)
                    {
                        if (deliveryPoints[worker.resourceType].ups.Count > 0)
                        {
                            if (worker.deliveryPointId < deliveryPoints[worker.resourceType].ups.Count)
                            {
                                Vector3 cp = deliveryPoints[worker.resourceType].pos[worker.deliveryPointId];
                                Vector3 vp = worker.transform.position;

                                Vector3 diff2d = GenericMath.ProjectionXZ(cp - vp);

                                if (diff2d.sqrMagnitude < 25f)
                                {
                                    int nationId = deliveryPoints[worker.resourceType].ups[worker.deliveryPointId].nation;
                                    Economy.active.AddResource(nationId, worker.resourceType, worker.resourceAmount);
                                    worker.resourceAmount = 0;
                                    OnResourceChangeAdjustUI(worker);

                                    UnitsMover.active.CompleteMovement(worker);

                                    if (worker.resourcePointObject != null)
                                    {
                                        worker.MoveUnit(worker.resourcePointObject.GetPosition(worker.collectionUnit), worker.GetWalkAnimation());
                                        worker.chopTreePhase = 11;
                                        worker.ResetFakePath();
                                    }
                                    else
                                    {
                                        worker.StopUnit(worker.GetIdleAnimation());
                                        worker.chopTreePhase = 15;
                                    }

                                    worker.ResetFakePath();
                                }
                            }
                        }
                    }
                    else if (worker.chopTreePhase == 14)
                    {
                        if (deliveryPoints[worker.resourceType].ups.Count > 0)
                        {
                            int neighId = deliveryPoints[worker.resourceType].kd.FindNearest(worker.transform.position);

                            UnitsMover.active.CompleteMovement(worker);
                            worker.MoveUnit(deliveryPoints[worker.resourceType].pos[neighId], worker.GetWalkAnimation());

                            worker.deliveryPointId = neighId;
                            worker.chopTreePhase = 13;
                            worker.collectionTimeSpend = 0;

                            worker.ResetFakePath();
                        }
                    }
                    else if ((worker.chopTreePhase == 15) || (worker.chopTreePhase == 16))
                    {
                        int resourceType = -1;
                        UnitPars collectionPoint = null;
                        ResourcePointObject rpo = null;

                        if (Economy.active.resources[worker.resourceType].collectionRtsUnitId >= 0)
                        {
                            int totCount = 0;

                            for (int i1 = 0; i1 < collectionPoints.Count; i1++)
                            {
                                if (collectionPoints[i1].rtsUnitId > -1)
                                {
                                    if (collectionPoints[i1].ups != null)
                                    {
                                        totCount = totCount + collectionPoints[i1].ups.Count;
                                    }
                                }
                            }

                            if (totCount > 0)
                            {
                                UnitPars neighUp = null;

                                if (worker.chopTreePhase == 15)
                                {
                                    neighUp = GetMiningPoint(worker.transform.position, false);
                                }
                                else if (worker.chopTreePhase == 16)
                                {
                                    neighUp = GetRandomMiningPoint();
                                }

                                if (neighUp.resourceAmount > 0)
                                {
                                    collectionPoint = neighUp;
                                    rpo = collectionPoint.resourcePointObject;
                                    resourceType = rpo.resourceType;
                                }
                            }
                        }
                        else
                        {
                            rpo = ResourcePointObject.FindNearestTerrainTreeProc(worker.transform.position);
                            resourceType = rpo.resourceType;
                        }

                        if (resourceType > -1)
                        {
                            SendWorkerToCollectionPoint(worker, resourceType, collectionPoint, rpo);
                        }
                    }
                }
            }
        }

        void OnResourceChangeAdjustUI(UnitPars up)
        {
            if (up.nation == Diplomacy.active.playerNation)
            {
                if (up.isSelected)
                {
                    if (up.unitParsType.isWorker)
                    {
                        if (UnitUI.active != null)
                        {
                            UnitUI.active.ActivateWorker();
                        }
                    }
                }
            }
        }

        public void AddToResourcesCollection(UnitPars goPars)
        {
            if (goPars.unitParsType.isWorker)
            {
                if (!up_workers.Contains(goPars))
                {
                    up_workers.Add(goPars);
                }
            }

            if (goPars.unitParsType.isWorker == false)
            {
                for (int i = 0; i < collectionPoints.Count; i++)
                {
                    if (goPars.rtsUnitId == collectionPoints[i].rtsUnitId)
                    {
                        if (!collectionPoints[i].ups.Contains(goPars))
                        {
                            int neigh = ResourcePoint.active.kd_allResLocations.FindNearest(goPars.transform.position);
                            float r = (ResourcePoint.active.resourcePoints[neigh].position - goPars.transform.position).magnitude;
                            int resAmount = ResourcePoint.active.resourcePoints[neigh].resourceAmount;
                            int resToAdd = (int)((1f - 0.6f * (r / 7f)) * resAmount);

                            if (resToAdd < 500)
                            {
                                resToAdd = 500;
                                if (resToAdd > resAmount)
                                {
                                    resToAdd = resAmount;
                                }
                            }

                            goPars.resourceType = ResourcePoint.active.resourcePoints[neigh].resourceType;
                            goPars.resourceAmount = resToAdd;
                            OnResourceChangeAdjustUI(goPars);
                            goPars.resourcePointObject = ResourcePoint.active.resourcePoints[neigh];
                            goPars.resourcePointObject.linkedUnits.Add(goPars);

                            collectionPoints[i].ups.Add(goPars);
                            collectionPoints[i].pos.Add(goPars.collectionOrDeliveryPoint.position);
                            collectionPoints[i].resourcePointObjects.Add(goPars.resourcePointObject);
                            collectionPoints[i].RefreshKDTree();
                        }
                    }
                }

                for (int i = 0; i < deliveryPoints.Count; i++)
                {
                    if (goPars.rtsUnitId == deliveryPoints[i].rtsUnitId)
                    {
                        if (!deliveryPoints[i].ups.Contains(goPars))
                        {
                            deliveryPoints[i].ups.Add(goPars);
                            deliveryPoints[i].pos.Add(goPars.collectionOrDeliveryPoint.position);
                            deliveryPoints[i].resourcePointObjects.Add(null);
                            deliveryPoints[i].RefreshKDTree();
                        }
                    }
                }
            }
        }

        public void RemoveFromResourcesCollection(UnitPars goPars)
        {
            if (goPars.unitParsType.isWorker)
            {
                if (up_workers.Contains(goPars))
                {
                    int iii = up_workers.IndexOf(goPars);
                    up_workers.RemoveAt(iii);
                }
            }

            if (goPars.unitParsType.isWorker == false)
            {
                for (int i = 0; i < collectionPoints.Count; i++)
                {
                    if (goPars.rtsUnitId == collectionPoints[i].rtsUnitId)
                    {
                        if (collectionPoints[i].ups.Contains(goPars))
                        {
                            int iii = collectionPoints[i].ups.IndexOf(goPars);
                            collectionPoints[i].ups.RemoveAt(iii);

                            if (iii < collectionPoints[i].pos.Count)
                            {
                                collectionPoints[i].pos.RemoveAt(iii);

                                if (iii > -1 && iii < collectionPoints[i].resourcePointObjects.Count)
                                {
                                    collectionPoints[i].resourcePointObjects.RemoveAt(iii);
                                }
                            }

                            collectionPoints[i].RefreshKDTree();
                        }
                    }
                }

                for (int i = 0; i < deliveryPoints.Count; i++)
                {
                    if (goPars.rtsUnitId == deliveryPoints[i].rtsUnitId)
                    {
                        if (deliveryPoints[i].ups.Contains(goPars))
                        {
                            int iii = deliveryPoints[i].ups.IndexOf(goPars);

                            deliveryPoints[i].ups.RemoveAt(iii);
                            deliveryPoints[i].pos.RemoveAt(iii);

                            if (iii > -1 && iii < deliveryPoints[i].resourcePointObjects.Count)
                            {
                                deliveryPoints[i].resourcePointObjects.RemoveAt(iii);
                            }

                            deliveryPoints[i].RefreshKDTree();

                            for (int iw = 0; iw < up_workers.Count; iw++)
                            {
                                if (up_workers[iw].resourceType == i)
                                {
                                    if (up_workers[iw].deliveryPointId > iii)
                                    {
                                        up_workers[iw].deliveryPointId = up_workers[iw].deliveryPointId - 1;
                                    }
                                }
                            }
                        }
                    }
                }

                if (goPars.resourcePointObject != null)
                {
                    goPars.resourcePointObject.linkedUnits.Remove(goPars);
                    goPars.resourcePointObject = null;
                }
            }
        }

        public ResourcePointObject GetTreeFromMouse()
        {
            ResourcePointObject thisTree = null;

            bool hitted = false;

            RaycastHit hit;
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(r, out hit))
            {
                hitted = true;
            }

            if (hitted == true)
            {
                ResourcePointObject neigh = ResourcePointObject.FindNearestTerrainTreeProc(hit.point);

                if (neigh != null)
                {
                    if (GenericMath.ProjectionXZ(neigh.position - hit.point).magnitude < 5f)
                    {
                        thisTree = neigh;
                    }
                }
            }

            return thisTree;
        }

        public ResourcePointObject GetRPOMouse()
        {
            ResourcePointObject rpo = null;

            bool hitted = false;

            RaycastHit hit;
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(r, out hit))
            {
                hitted = true;
            }

            if (hitted == true)
            {
                for (int i = 0; i < ResourcePoint.active.resourcePointTypes.Count; i++)
                {
                    if (rpo == null)
                    {
                        int resourceType = ResourcePoint.active.resourcePointTypes[i].resourceType;

                        if (resourceType > -1)
                        {
                            if (Economy.active.resources[resourceType].collectionRtsUnitId < 0)
                            {
                                if (ResourcePoint.active.resourcePointTypes[i].kd_catLocations != null)
                                {
                                    int neigh = ResourcePoint.active.resourcePointTypes[i].kd_catLocations.FindNearest(hit.point);

                                    if (neigh > -1)
                                    {
                                        if (GenericMath.ProjectionXZ(ResourcePoint.active.resourcePointTypes[i].categorizedResourcePoints[neigh].position - hit.point).magnitude < 5f)
                                        {
                                            rpo = ResourcePoint.active.resourcePointTypes[i].categorizedResourcePoints[neigh];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return rpo;
        }

        public UnitPars GetMiningPointFromMouse()
        {
            bool hitted = false;

            RaycastHit hit;
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(r, out hit))
            {
                hitted = true;
            }

            UnitPars miningPoint = null;

            if (hitted)
            {
                miningPoint = GetMiningPoint(hit.point, true);
            }

            return miningPoint;
        }

        public UnitPars GetMiningPoint(Vector3 point, bool useRsmall)
        {
            float minDist = float.MaxValue;
            UnitPars miningPoint = null;

            for (int i = 0; i < collectionPoints.Count; i++)
            {
                if (collectionPoints[i].rtsUnitId > -1)
                {
                    if (collectionPoints[i].pos.Count > 0)
                    {
                        int id1 = collectionPoints[i].kd.FindNearest(point);
                        float r1 = (collectionPoints[i].pos[id1] - point).magnitude;

                        if (r1 < minDist)
                        {
                            minDist = r1;

                            if (r1 < 1.15f * collectionPoints[i].ups[id1].rEnclosed)
                            {
                                miningPoint = collectionPoints[i].ups[id1];
                            }
                            if (useRsmall == false)
                            {
                                miningPoint = collectionPoints[i].ups[id1];
                            }
                        }
                    }
                }
            }

            return miningPoint;
        }

        public UnitPars GetRandomMiningPoint()
        {
            UnitPars miningPoint = null;
            List<UnitPars> randMiningPoints = new List<UnitPars>();

            for (int i = 0; i < collectionPoints.Count; i++)
            {
                if (collectionPoints[i].rtsUnitId > -1)
                {
                    if (collectionPoints[i].ups.Count > 0)
                    {
                        for (int j = 0; j < collectionPoints[i].ups.Count; j++)
                        {
                            randMiningPoints.Add(collectionPoints[i].ups[j]);
                        }
                    }
                }
            }

            if (randMiningPoints.Count > 0)
            {
                int i1 = Random.Range(0, randMiningPoints.Count);
                miningPoint = randMiningPoints[i1];
            }

            return miningPoint;
        }

        public class CollectionDeliveryPoint
        {
            public int rtsUnitId;

            public List<UnitPars> ups = new List<UnitPars>();
            public List<Vector3> pos = new List<Vector3>();
            public List<ResourcePointObject> resourcePointObjects = new List<ResourcePointObject>();
            public KDTree kd;

            public void RefreshKDTree()
            {
                if (ups.Count > 0)
                {
                    kd = KDTree.MakeFromPoints(pos.ToArray());
                }
                else
                {
                    kd = null;
                }
            }
        }
    }
}
