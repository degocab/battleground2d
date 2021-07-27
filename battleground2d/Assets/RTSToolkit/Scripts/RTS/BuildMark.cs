using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class BuildMark : MonoBehaviour
    {
        public static BuildMark active;

        public GameObject projector;

        public MeshFilter mFilter;
        public MeshRenderer mRenderer;

        List<MeshRenderer> renderers = new List<MeshRenderer>();

        float markRadius = 10f;

        [HideInInspector] public bool buildingAllowed = false;
        [HideInInspector] public Vector3 buildPosition;
        [HideInInspector] public bool lockedProjector = false;
        Vector3 curPos = Vector3.zero;

        RTSMaster rtsm;
        BattleSystem battleSystem;

        [HideInInspector] public GameObject objectToSpawn;
        [HideInInspector] public UnitPars up_objectToSpawn;
        [HideInInspector] public int objectToSpawnId = 0;

        float rEnclosed = 0f;

        int nLeftClicks = 0;

        List<GameObject> projectorFixed = new List<GameObject>();
        List<Vector3> spawnLocations = new List<Vector3>();
        List<Quaternion> spawnRotations = new List<Quaternion>();
        List<bool> fenceAllowed = new List<bool>();

        float projRot = 0f;

        public void ActivateProjector()
        {
            rEnclosed = objectToSpawn.GetComponent<MeshRenderer>().GetComponent<Renderer>().bounds.extents.magnitude;

            MeshFilter mf1 = objectToSpawn.GetComponent<MeshFilter>();
            MeshRenderer mr1 = objectToSpawn.GetComponent<MeshRenderer>();

            mFilter.mesh = mf1.sharedMesh;
            mRenderer.materials = mr1.sharedMaterials;
            mFilter.gameObject.transform.localScale = objectToSpawn.transform.localScale;

            for (int i = 0; i < mRenderer.materials.Length; i++)
            {
                Material mat = mRenderer.materials[i];
                mat.color = new Color(0.25f, 1f, 0.25f, 1f);
            }

            projRot = Random.Range(0f, 360f);
            projector.transform.rotation = Quaternion.Euler(0f, projRot, 0f) * objectToSpawn.transform.rotation;
            projector.SetActive(true);

            Transform[] allChildren = objectToSpawn.GetComponentsInChildren<Transform>(true);
            int i12 = 0;

            for (int i = 0; i < allChildren.Length; i++)
            {
                Transform child = allChildren[i];

                if (child.gameObject != objectToSpawn)
                {
                    MeshFilter mf2 = child.gameObject.GetComponent<MeshFilter>();
                    MeshRenderer mr2 = child.gameObject.GetComponent<MeshRenderer>();

                    if ((mf2 != null) && (mr2 != null))
                    {
                        GameObject go3 = new GameObject("child");
                        go3.transform.SetParent(mFilter.gameObject.transform);

                        go3.transform.localScale = child.localScale;
                        go3.transform.localPosition = child.localPosition;
                        go3.transform.localRotation = child.localRotation;

                        MeshFilter mf3 = go3.AddComponent<MeshFilter>();
                        MeshRenderer mr3 = go3.AddComponent<MeshRenderer>();

                        mf3.mesh = mf2.sharedMesh;
                        mr3.materials = mr2.sharedMaterials;
                        renderers.Add(mr3);
                        i12++;

                        for (int j = 0; j < mr3.materials.Length; j++)
                        {
                            mr3.materials[j].color = new Color(0.25f, 1f, 0.25f, 1f);
                        }
                    }
                }
            }
        }

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            battleSystem = BattleSystem.active;
            projector.SetActive(false);
        }

        void LateUpdate()
        {
            if (projector.activeSelf == true)
            {
                // 	    

#if UNITY_IPHONE || UNITY_ANDROID
			lockedProjector = false;

			if(Input.mousePosition.y < 0.17f * Screen.height)
			{
				lockedProjector = true;
			}
#if UNITY_EDITOR
			if(Input.GetMouseButton(0) == false)
			{
				lockedProjector = true;
			}
#endif
#endif
                RaycastHit hit;
                Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (!Physics.Raycast(r, out hit))
                {
                    return;
                }

                if (lockedProjector == false)
                {
                    curPos = TerrainProperties.TerrainVectorProc(hit.point);
                    projector.transform.position = curPos;

                    if (up_objectToSpawn.rtsUnitId != 9)
                    {
                        if (Input.GetKey(KeyCode.LeftArrow))
                        {
                            projRot = projRot - 1f;
                            Quaternion origRot = Quaternion.identity;

                            if (objectToSpawn != null)
                            {
                                origRot = objectToSpawn.transform.rotation;
                            }

                            projector.transform.rotation = Quaternion.Euler(0f, projRot, 0f) * origRot;
                        }

                        if (Input.GetKey(KeyCode.RightArrow))
                        {
                            projRot = projRot + 1f;
                            Quaternion origRot = Quaternion.identity;

                            if (objectToSpawn != null)
                            {
                                origRot = objectToSpawn.transform.rotation;
                            }

                            projector.transform.rotation = Quaternion.Euler(0f, projRot, 0f) * origRot;
                        }
                    }
                }

                ResourcePointObject rpo = ResourcePointObject.FindNearestTerrainTreeProc(curPos);

                if (TerrainProperties.active.TerrainSteepness(curPos, markRadius) > 30f)
                {
                    buildingAllowed = false;
                }
                else if (rpo != null && (rpo.position - curPos).magnitude < rEnclosed)
                {
                    buildingAllowed = false;
                }
                else if (up_objectToSpawn.IsEnoughResources(Diplomacy.active.playerNation) == false)
                {
                    buildingAllowed = false;
                }
                else
                {
                    buildingAllowed = true;
                }

                List<int> resCol = new List<int>();

                for (int i = 0; i < Economy.GetActive().resources.Count; i++)
                {
                    if (up_objectToSpawn.rtsUnitId == Economy.GetActive().resources[i].collectionRtsUnitId)
                    {
                        resCol.Add(i);
                    }
                }

                if (resCol.Count > 0)
                {
                    int neigh = ResourcePoint.active.kd_allResLocations.FindNearest(curPos);

                    if (ResourcePoint.active.resourcePoints[neigh].collectionRtsUnitId == up_objectToSpawn.rtsUnitId)
                    {
                        if (GenericMath.ProjectionXZ(ResourcePoint.active.resourcePoints[neigh].position - curPos).magnitude > 7f)
                        {
                            buildingAllowed = false;
                        }
                    }
                    else
                    {
                        buildingAllowed = false;
                    }
                }

                if (battleSystem != null)
                {
                    int ucount = battleSystem.unitssUP.Count;
                    float smallestDist2 = 10000f;

                    for (int ii = 0; ii < ucount; ii++)
                    {
                        if (battleSystem.unitssUP[ii].unitParsType.isBuilding == true)
                        {
                            float stopDistOut = (battleSystem.unitssUP[ii].rEnclosed + rEnclosed) * (battleSystem.unitssUP[ii].rEnclosed + rEnclosed);
                            float dist2 = (curPos - battleSystem.unitssUP[ii].transform.position).sqrMagnitude;

                            if (dist2 < smallestDist2)
                            {
                                if (battleSystem.unitssUP[ii].nation == Diplomacy.active.playerNation)
                                {
                                    smallestDist2 = dist2;
                                }
                            }

                            if (dist2 < stopDistOut)
                            {
                                buildingAllowed = false;
                            }
                        }
                    }

                    if (
                        (smallestDist2 > 1600f) &&
                          (
                            (up_objectToSpawn.rtsUnitId != 0) &&
                            (up_objectToSpawn.rtsUnitId != 2) &&
                            (up_objectToSpawn.rtsUnitId != 5)
                        )
                    )
                    {
                        buildingAllowed = false;
                    }
                }

                if (TerrainProperties.HasNavigation(curPos) == false)
                {
                    buildingAllowed = false;
                }

                if (SelectionManager.active != null)
                {
                    if (SelectionManager.active.isMouseOnActiveScreen == false)
                    {
                        buildingAllowed = false;
                    }
                }

                if (buildingAllowed == true)
                {
                    if (lockedProjector == false)
                    {
                        buildPosition = curPos;
                    }

                    for (int i = 0; i < mRenderer.materials.Length; i++)
                    {
                        Material mat = mRenderer.materials[i];
                        mat.color = new Color(0.25f, 1f, 0.25f, 1f);
                    }

                    for (int i = 0; i < renderers.Count; i++)
                    {
                        MeshRenderer mr3s = renderers[i];

                        for (int j = 0; j < mr3s.materials.Length; j++)
                        {
                            Material mat = mr3s.materials[j];
                            mat.color = new Color(0.25f, 1f, 0.25f, 1f);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < mRenderer.materials.Length; i++)
                    {
                        Material mat = mRenderer.materials[i];
                        mat.color = new Color(1f, 0.25f, 0.25f, 1f);
                    }
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        MeshRenderer mr3s = renderers[i];

                        for (int j = 0; j < mr3s.materials.Length; j++)
                        {
                            Material mat = mr3s.materials[j];
                            mat.color = new Color(1f, 0.25f, 0.25f, 1f);
                        }
                    }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (buildingAllowed == true)
                    {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
                        SetBuilding(buildPosition);
#endif
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
                    DisableProjector();
#endif
                }

                if (nLeftClicks == 1)
                {
                    int nProjectorFixed = projectorFixed.Count;

                    if (nProjectorFixed > 0)
                    {
                        bool isAllowed = false;
                        bool isPrevChanged = false;

                        KDTree kd = KDTree.MakeFromPointsGo(projectorFixed);
                        float rNearest = kd.FindNearest_R(projector.transform.position);

                        if (rNearest > rEnclosed)
                        {
                            isAllowed = true;
                        }

                        float rPrev = (projectorFixed[nProjectorFixed - 1].transform.position - projector.transform.position).magnitude;

                        if (rPrev > rEnclosed)
                        {
                            isPrevChanged = true;
                        }

                        if (spawnRotations.Count > 1)
                        {
                            Quaternion f_rot = Quaternion.LookRotation(projectorFixed[nProjectorFixed - 1].transform.position - projector.transform.position);
                            projectorFixed[nProjectorFixed - 1].transform.rotation = f_rot;
                        }

                        if (isPrevChanged)
                        {
                            Quaternion f_rot = Quaternion.LookRotation(projectorFixed[nProjectorFixed - 1].transform.position - projector.transform.position);
                            bool passAngle = true;

                            if (spawnRotations.Count > 2)
                            {
                                float angle1 = f_rot.eulerAngles.y;
                                float angle2 = projector.transform.rotation.eulerAngles.y;
                                if (Mathf.Abs(angle2 - angle1) > 90f)
                                {
                                    passAngle = false;
                                }
                            }

                            if (passAngle)
                            {
                                spawnRotations.Add(f_rot);
                                projectorFixed[nProjectorFixed - 1].transform.rotation = f_rot;

                                Vector3 diff = projectorFixed[nProjectorFixed - 1].transform.position + ((projector.transform.position - projectorFixed[nProjectorFixed - 1].transform.position).normalized) * rEnclosed;
                                spawnLocations.Add(diff);

                                projectorFixed.Add(Instantiate(projector, diff, projector.transform.rotation));
                                projector.transform.rotation = f_rot;

                                if (isAllowed == true)
                                {
                                    fenceAllowed.Add(true);
                                }
                                else
                                {
                                    fenceAllowed.Add(false);
                                }

                                if (nProjectorFixed > 1)
                                {
                                    for (int i = 1; i <= nProjectorFixed; i++)
                                    {
                                        Quaternion q1 = Quaternion.LookRotation(projectorFixed[i - 1].transform.position - projectorFixed[i].transform.position);
                                        projectorFixed[i].transform.rotation = Quaternion.Euler(0, q1.eulerAngles.y, 0);
                                        spawnRotations[i] = Quaternion.Euler(0, q1.eulerAngles.y, 0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DisableProjector()
        {
            projector.SetActive(false);
            BottomBarUI.active.DisableAll();

            if (nLeftClicks == 1)
            {
                for (int i = 0; i < projectorFixed.Count; i++)
                {
                    Destroy(projectorFixed[i].gameObject);
                }
            }
            if (up_objectToSpawn != null)
            {
                if (up_objectToSpawn.rtsUnitId == 0)
                {
                    GameOver.active.isActive = false;
                }
            }

            if (SelectionManager.active.selectedGoPars.Count == 1)
            {
                SelectionManager sm = SelectionManager.active;
                SpawnGridUI.active.ToggleGrid(sm.HeroCheckId(sm.selectedGoPars[0].rtsUnitId));
            }

            if (SelectionManager.active.selectedGoPars.Count == 0)
            {
                SpawnGridUI.active.ToggleGrid(-1);
            }

            spawnLocations.Clear();
            spawnRotations.Clear();
            projectorFixed.Clear();
            fenceAllowed.Clear();

            Transform[] allChildren = mFilter.gameObject.GetComponentsInChildren<Transform>();

            for (int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i].gameObject != mFilter.gameObject)
                {
                    Destroy(allChildren[i].gameObject);
                }
            }

            renderers.Clear();
        }

        public void SetBuilding(Vector3 pos)
        {
            UnitPars objectToSpawnPars = objectToSpawn.GetComponent<UnitPars>();
            if ((Diplomacy.active.playerNation > -1) && (Diplomacy.active.playerNation < rtsm.nationPars.Count))
            {
                SpawnPoint sp = rtsm.nationPars[Diplomacy.active.playerNation].spawnPoint;

                // If continuous building such as fence
                if (objectToSpawnPars.rtsUnitId == 9)
                {
                    nLeftClicks = nLeftClicks + 1;

                    if (nLeftClicks == 1)
                    {
                        projectorFixed.Add((GameObject)Instantiate(projector, projector.transform.position, projector.transform.rotation));
                        spawnLocations.Add(pos);
                        spawnRotations.Add(projector.transform.rotation);
                        fenceAllowed.Add(true);
                    }
                    else if (nLeftClicks == 2)
                    {

                        nLeftClicks = 0;
                        sp.model = objectToSpawn;
                        sp.numberOfObjects = 0;

                        if (projectorFixed.Count > 1)
                        {
                            spawnRotations[0] = spawnRotations[1];
                        }

                        for (int i = 0; i < projectorFixed.Count; i++)
                        {
                            sp.isManualPosition = true;

                            if (fenceAllowed[i] == true)
                            {
                                sp.manualPosition.Add(spawnLocations[i]);
                                sp.manualRotation.Add(spawnRotations[i]);
                                sp.numberOfObjects = sp.numberOfObjects + 1;
                            }
                        }

                        sp.StartSpawning();

                        for (int i = 0; i < projectorFixed.Count; i++)
                        {
                            Destroy(projectorFixed[i].gameObject);
                        }

                        projectorFixed.Clear();
                        spawnLocations.Clear();
                        spawnRotations.Clear();
                        fenceAllowed.Clear();

                        projector.SetActive(false);

                        if (SelectionManager.active.selectedGoPars.Count == 1)
                        {
                            SpawnGridUI.active.ToggleGrid(SelectionManager.active.selectedGoPars[0].rtsUnitId);
                        }

                        this.enabled = false;
                        BottomBarUI.active.DisableAll();
                    }
                }
                // if regular building made in single click	
                else
                {
                    sp.manualPosition.Clear();
                    sp.manualRotation.Clear();
                    sp.numberOfObjects = 0;
                    BuildBuilding(pos, projRot);
                }
            }
            else
            {
                Debug.Log(Diplomacy.active.playerNation + " " + rtsm.nationPars.Count);
            }
        }

        void BuildBuilding(Vector3 pos, float rot)
        {
            SpawnPoint sp = rtsm.nationPars[Diplomacy.active.playerNation].spawnPoint;

            sp.isManualPosition = true;
            sp.manualPosition.Add(pos);
            sp.manualRotation.Add(Quaternion.Euler(0f, rot, 0f) * objectToSpawn.transform.rotation);
            sp.numberOfObjects = 1;
            sp.model = objectToSpawn;

            sp.nation = Diplomacy.active.playerNation;
            sp.nationName = rtsm.nationPars[Diplomacy.active.playerNation].GetNationName();

            sp.StartSpawning();

            projector.SetActive(false);

            if (SelectionManager.active.selectedGoPars.Count == 1)
            {
                if (SelectionManager.active.selectedGoPars[0].rtsUnitId != 20)
                {
                    SpawnGridUI.active.ToggleGrid(SelectionManager.active.selectedGoPars[0].rtsUnitId);
                }

                UnitUI.active.unit_buildCentralBuildingButton.SetActive(false);
            }

            this.enabled = false;
            BottomBarUI.active.DisableAll();

            Transform[] allChildren = mFilter.gameObject.GetComponentsInChildren<Transform>();

            for (int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i].gameObject != mFilter.gameObject)
                {
                    Destroy(allChildren[i].gameObject);
                }
            }

            renderers.Clear();
        }
    }
}
