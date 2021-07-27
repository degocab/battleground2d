using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager active;

        public List<CursorTexture> cursorTextures = new List<CursorTexture>();
        [HideInInspector] public int cursorMode = 0;

        Texture2D selectionHighlight = null;
        public static Rect selection = new Rect(0, 0, 0, 0);
        Vector3 startClick = -Vector3.one;

        static private Vector3 destinationClick = -Vector3.one;

        private static Vector3 moveToDestination = Vector3.zero;

        [HideInInspector] public List<UnitPars> selectablesPars = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> allUnits = null;
        [HideInInspector] public List<UnitPars> selectedGoPars = new List<UnitPars>();

        bool lockRectangle = false;

        UnitPars tGo = null;

        RTSMaster rtsm;

        BattleSystem battleSystem;
        Diplomacy diplomacy;

        [HideInInspector] public bool isMouseOnActiveScreen = true;
        [HideInInspector] public int movementButtonMode = 0;

        Vector2 lastMousePosition = Vector2.zero;

        [HideInInspector] public float remainingSelectedHealth = 0;
        [HideInInspector] public float totalSelectedHealth = 0;

        public int pointerRegularMode = 0;
        public int pointerMoveToMode = 1;
        public int pointerRightClickAttackMode = 2;
        public int pointerButtonClickAttackInactiveMode = 3;
        public int pointerButtonClickAttackActiveMode = 4;
        public int pointerMiningPointInactiveMode = 5;
        public int pointerMiningPointActiveMode = 6;
        public int pointerChopWoodInactiveMode = 7;
        public int pointerChopWoodActiveMode = 8;

        public bool useDoubleSelect = true;
        bool isDoubleSelectRunning = false;
        public float doubleSelectTime = 0.5f;
        public float doubleSelectDistance = 100f;

        public AudioClip selectSound;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            rtsm = RTSMaster.active;

            battleSystem = BattleSystem.active;
            diplomacy = Diplomacy.active;

            selectionHighlight = new Texture2D(2, 2);
            selectionHighlight = Texture2D.whiteTexture;

            allUnits = rtsm.allUnits;

            InstanciateCursor();
        }

        void InstanciateCursor()
        {
            Cursor.visible = false;
            cursorMode = pointerRegularMode;
        }

        void Update()
        {
            UpdateUnitsHealth();

            if (isMouseOnActiveScreen == true)
            {
                if (movementButtonMode == pointerRegularMode)
                {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
                    CheckCamera();
#endif
#if UNITY_IPHONE || UNITY_ANDROID
				    CheckCamera_Mobile();
#endif
                }

                if (movementButtonMode == pointerMoveToMode)
                {
                    CheckMovementButtonCamera();
                }

                if (movementButtonMode == pointerButtonClickAttackActiveMode)
                {
                    CheckAttackButtonCamera();
                }

                if (movementButtonMode == pointerMiningPointActiveMode)
                {
                    CheckMiningPointButtonCamera();
                }

                if (movementButtonMode == pointerChopWoodActiveMode)
                {
                    CheckWoodButtonCamera();
                }
            }
            else
            {
                if (Input.GetMouseButtonUp(0))
                {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
                    CheckCamera();
#endif
#if UNITY_IPHONE || UNITY_ANDROID
				    CheckCamera_Mobile();
#endif
                }
            }
#if UNITY_IPHONE || UNITY_ANDROID
            if (isMouseOnActiveScreen == false)
            {
                if (IsOnActiveScreenRect() == true)
                {
                    if (movementButtonMode == pointerMoveToMode)
                    {
                        CheckMovementButtonCamera();
                    }

                    if (movementButtonMode == pointerButtonClickAttackActiveMode)
                    {
                        CheckAttackButtonCamera();
                    }

                    if (movementButtonMode == pointerMiningPointActiveMode)
                    {
                        CheckMiningPointButtonCamera();
                    }

                    if (movementButtonMode == pointerChopWoodActiveMode)
                    {
                        CheckWoodButtonCamera();
                    }
                }
            }
#endif
        }

        void UpdateUnitsHealth()
        {
            remainingSelectedHealth = 0f;
            totalSelectedHealth = 0f;

            for (int i = 0; i < selectedGoPars.Count; i++)
            {
                UnitPars up = selectedGoPars[i];

                remainingSelectedHealth = remainingSelectedHealth + up.health;
                totalSelectedHealth = totalSelectedHealth + up.maxHealth;
            }

            if (SelectedIconUI.active != null)
            {
                if (SelectedIconUI.active.healthBar.gameObject.activeSelf == true)
                {
                    SelectedIconUI.active.SetHealth(remainingSelectedHealth / totalSelectedHealth);
                    SelectedUnitsInfo();
                }
            }
        }

        private void CheckCamera()
        {
            if (Input.GetMouseButtonDown(0))
            {
                startClick = Input.mousePosition;

                if (BuildMark.active.projector.activeSelf == false)
                {
                    SelectByClick(startClick, 1);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (lockRectangle == false)
                {
                    if (selection.width != 0)
                    {
                        if (selection.height != 0)
                        {
                            SelectByRect();
                        }
                    }

                    startClick = -Vector3.one;
                    selection = new Rect(0f, 0f, 0f, 0f);
                }

                lockRectangle = false;
            }

            if (Input.GetMouseButton(0))
            {
                if (lockRectangle == false)
                {
                    selection = new Rect(startClick.x, InvertMouseY(startClick.y), Input.mousePosition.x - startClick.x, InvertMouseY(Input.mousePosition.y) - InvertMouseY(startClick.y));

                    if (selection.width < 0)
                    {
                        selection.x += selection.width;
                        selection.width = -selection.width;
                    }

                    if (selection.height < 0)
                    {
                        selection.y += selection.height;
                        selection.height = -selection.height;
                    }
                }
            }

            if (selectedGoPars.Count > 0)
            {
                if (selectedGoPars[0].unitParsType.isBuilding == false)
                {
                    if (selectedGoPars[0].nation == diplomacy.playerNation)
                    {
                        if (Input.GetMouseButton(1))
                        {
                            cursorMode = pointerMoveToMode;
                            tGo = null;
                            destinationClick = Input.mousePosition;
                            RightClick(destinationClick);

                            if (tGo != null)
                            {
                                cursorMode = pointerRightClickAttackMode;

                                if (AreAllSelectedUnitsWorkers())
                                {
                                    if (tGo.unitParsType.isBuilding)
                                    {
                                        cursorMode = pointerMoveToMode;

                                        if (tGo.IsResourceCollectionBuilding())
                                        {
                                            cursorMode = pointerMiningPointActiveMode;
                                        }
                                        else
                                        {
                                            if (rtsm.nationPars[diplomacy.playerNation].resourcesCollection.GetTreeFromMouse() != null)
                                            {
                                                cursorMode = pointerChopWoodActiveMode;
                                            }

                                            if (rtsm.nationPars[diplomacy.playerNation].resourcesCollection.GetRPOMouse() != null)
                                            {
                                                cursorMode = pointerMiningPointActiveMode;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (AreAllSelectedUnitsWorkers())
                                {
                                    if (rtsm.nationPars[diplomacy.playerNation].resourcesCollection.GetTreeFromMouse() != null)
                                    {
                                        cursorMode = pointerChopWoodActiveMode;
                                    }

                                    if (rtsm.nationPars[diplomacy.playerNation].resourcesCollection.GetRPOMouse() != null)
                                    {
                                        cursorMode = pointerMiningPointActiveMode;
                                    }
                                }
                            }
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            cursorMode = pointerRegularMode;
                            tGo = null;
                            destinationClick = Input.mousePosition;

                            RightClick(destinationClick);
                            GetDestination();
                            SetDestinations();
                            PlaySelectSound();
                        }
                    }
                }
            }

            if (Input.GetMouseButton(1))
            {
                NationListUI.active.CloseAllDiplomacy();
            }
        }

        private void CheckCamera_Mobile()
        {
            if (RTSCamera.active.mobileCameraMode == 4)
            {
                CheckCamera();
            }
        }

        private void CheckMovementButtonCamera()
        {
            cursorMode = pointerMoveToMode;

            if (IsPointerUp())
            {
                cursorMode = pointerRegularMode;
                destinationClick = Input.mousePosition;

                RightClick(destinationClick);
                GetDestination();

                tGo = null;

                SetDestinations();
                PlaySelectSound();

                movementButtonMode = pointerRegularMode;
            }

            if (Input.GetMouseButtonUp(1))
            {
                cursorMode = pointerRegularMode;
                movementButtonMode = pointerRegularMode;
            }
        }

        private void CheckAttackButtonCamera()
        {
            cursorMode = pointerButtonClickAttackInactiveMode;
            tGo = null;
            destinationClick = Input.mousePosition;
            RightClick(destinationClick);

            if (tGo != null)
            {
                bool workerPass = true;

                if (AreAllSelectedUnitsWorkers())
                {
                    if (tGo.unitParsType.isBuilding)
                    {
                        for (int i = 0; i < selectedGoPars.Count; i++)
                        {
                            if (selectedGoPars[i].isSelected)
                            {
                                workerPass = false;
                            }
                        }
                    }
                }

                if (workerPass)
                {
                    cursorMode = pointerButtonClickAttackActiveMode;
                }
            }

            if (IsPointerUp())
            {
                cursorMode = pointerRegularMode;

                tGo = null;
                destinationClick = Input.mousePosition;
                RightClick(destinationClick);
                GetDestination();

                if (tGo != null)
                {
                    SetDestinations();
                    PlaySelectSound();
                }

                movementButtonMode = pointerRegularMode;
            }

            if (Input.GetMouseButtonUp(1))
            {
                cursorMode = pointerRegularMode;
                movementButtonMode = pointerRegularMode;
            }
        }

        void CheckMiningPointButtonCamera()
        {
            cursorMode = pointerMiningPointInactiveMode;
            UnitPars miningPoint = rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection.GetMiningPointFromMouse();
            ResourcePointObject rpo2 = rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection.GetRPOMouse();

            if (miningPoint != null || rpo2 != null)
            {
                cursorMode = pointerMiningPointActiveMode;
            }

            if (IsPointerUp())
            {
                cursorMode = pointerRegularMode;
                movementButtonMode = pointerRegularMode;

                if (miningPoint != null)
                {
                    rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection.MineResource(miningPoint.resourceType, miningPoint, miningPoint.resourcePointObject);
                    PlaySelectSound();
                }
                else if (rpo2 != null)
                {
                    rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection.MineResource(rpo2.resourceType, null, rpo2);
                    PlaySelectSound();
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                cursorMode = pointerRegularMode;
                movementButtonMode = pointerRegularMode;
            }
        }

        void CheckWoodButtonCamera()
        {
            cursorMode = pointerChopWoodInactiveMode;
            ResourcePointObject rpo = rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection.GetTreeFromMouse();

            if (rpo != null)
            {
                cursorMode = pointerChopWoodActiveMode;
            }

            if (IsPointerUp())
            {
                cursorMode = pointerRegularMode;
                movementButtonMode = pointerRegularMode;

                if (rpo != null)
                {
                    rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection.MineResource(rpo.resourceType, null, rpo);
                    PlaySelectSound();
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                cursorMode = pointerRegularMode;
                movementButtonMode = pointerRegularMode;
            }
        }

        public bool IsPointerUp()
        {
            bool pUp = false;
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
            if (Input.GetMouseButtonUp(0))
            {
                pUp = true;
            }
#endif
#if UNITY_IPHONE || UNITY_ANDROID
        if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled))
        {
            pUp = true;
        }
#if UNITY_EDITOR
        if (Input.GetMouseButtonUp(0))
        {
            pUp = true;
        }
#endif
#endif
            return pUp;
        }

        private void OnGUI()
        {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
            lastMousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
#endif

#if UNITY_IPHONE || UNITY_ANDROID
        if (Input.touchCount > 0)
        {
            lastMousePosition = new Vector2(Input.GetTouch(0).position.x, Screen.height - Input.GetTouch(0).position.y);
        }
#if UNITY_EDITOR
        lastMousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
#endif
#endif
            bool fullScreenPass = true;

            if (ActiveScreenPEC.active != null)
            {
                if (ActiveScreenPEC.active.isActive == false)
                {
                    fullScreenPass = false;
                }
            }

            if (fullScreenPass)
            {
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUI.DrawTexture(selection, selectionHighlight);
                Cursor.visible = false;
            }

            if ((cursorMode >= 0) && (cursorMode < cursorTextures.Count))
            {
                GUI.color = cursorTextures[cursorMode].GetColor();
                GUI.DrawTexture(cursorTextures[cursorMode].GetRect(lastMousePosition), cursorTextures[cursorMode].activeTexture);
            }
            else
            {
                Debug.Log(
                    "corsorMode refering out of bounds cursorTextures element. cursorMode value is: " +
                    cursorMode.ToString() +
                    ". Size of cursorTextures: " +
                    cursorTextures.Count.ToString()
                );
            }
        }

        public static float InvertMouseY(float y)
        {
            return Screen.height - y;
        }

        public void GetDestination()
        {
            bool hitted = false;

            RaycastHit hit;
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(r, out hit))
            {
                hitted = true;
            }

            if (TerrainProperties.HasNavigation(hit.point) == false)
            {
                hitted = false;
            }

            if (hitted == true)
            {
                moveToDestination = TerrainProperties.TerrainVectorProc(hit.point);
            }
            else
            {
                moveToDestination = TerrainProperties.TerrainVectorProc(new Vector3(1f, 0f, 1f));
            }
        }

        public void StopDestinationsF()
        {
            LoadSelectables(1);

            UnitPars goPars;
            UnityEngine.AI.NavMeshAgent goNav;

            for (int i = 0; i < selectablesPars.Count; i++)
            {

                goPars = selectablesPars[i];
                goNav = goPars.thisNMA;

                if (goPars.isSelected)
                {
                    if (goPars.targetUP != null)
                    {
                        goPars.targetUP.attackers.Remove(goPars);
                        goPars.targetUP = null;
                    }

                    goPars.strictApproachMode = false;
                    goPars.onManualControl = true;

                    goPars.isMovingMC = true;
                    battleSystem.UnSetSearching(goPars, true);
                    goPars.manualDestination = goPars.transform.position;

                    if (rtsm.useAStar)
                    {
                        goPars.agentPars.manualAgent.StopMoving();
                    }
                    else
                    {
                        if (goNav.enabled == true)
                        {
                            goNav.ResetPath();
                        }
                    }

                    if (goPars.thisUA != null)
                    {
                        goPars.thisUA.PlayAnimation(goPars.GetIdleAnimation());
                    }

                    if (goPars.unitParsType.isWorker)
                    {
                        ResourcesCollection.ResetWorker(goPars);
                    }
                }
            }
        }

        public void SetDestinations()
        {
            LoadSelectables(1);

            bool proceedToWar = false;

            if (tGo != null)
            {
                if (selectedGoPars.Count > 0)
                {
                    if (tGo.nation != diplomacy.playerNation)
                    {
                        NationPars np = RTSMaster.active.GetNationPars(tGo.nationName);

                        if ((np.isWarWarningIssued == false) && (diplomacy.relations[diplomacy.playerNation][tGo.nation] != 1) && (diplomacy.useWarNoticeWarning))
                        {
                            DiplomacyReportsUI.active.MakeTextReport("Attacking the object will lead to War!");
                            np.isWarWarningIssued = true;
                            StartCoroutine(ResetWarNotice(np));
                        }
                        else
                        {
                            StopCoroutine("ResetWarNotice");
                            proceedToWar = true;
                            diplomacy.SetRelation(diplomacy.GetPlayerNationName(), tGo.nationName, 1);
                        }
                    }
                    else
                    {
                        proceedToWar = true;
                    }
                }
            }

            UnitPars goPars;
            UnityEngine.AI.NavMeshAgent goNav;

            List<Vector3> formationDest = LoadFormationDestinations();
            int iManual = 0;

            for (int i = 0; i < selectablesPars.Count; i++)
            {
                goPars = selectablesPars[i];
                goNav = goPars.thisNMA;

                if ((goNav != null) || (rtsm.useAStar))
                {
                    if (goPars.isSelected)
                    {
                        goPars.failedDist = 0;
                        UnitPars tGo2 = tGo;

                        if (goPars.unitParsType.isWorker)
                        {
                            if (tGo != null)
                            {
                                if (tGo.unitParsType.isBuilding)
                                {
                                    tGo2 = null;
                                }
                            }
                        }

                        if (tGo2 != null)
                        {
                            if (proceedToWar == true)
                            {
                                if (goPars != tGo)
                                {
                                    if (goPars.unitParsType.isWorker == false)
                                    {
                                        goPars.strictApproachMode = true;
                                        goPars.targetUP = tGo;
                                        tGo.attackers.Remove(goPars);
                                        tGo.attackers.Add(goPars);
                                        goPars.militaryMode = 20;
                                        goPars.attackDelayCorPass = false;

                                        UnitsMover.active.AddMilitaryAvoider(goPars, BattleSystem.GetClosestBuildingPoint(goPars, tGo), 0);
                                    }
                                    else if (goPars.unitParsType.isWorker)
                                    {
                                        if (goPars.resourceAmount <= 0)
                                        {
                                            goPars.strictApproachMode = true;
                                            goPars.targetUP = tGo;
                                            tGo.attackers.Remove(goPars);
                                            tGo.attackers.Add(goPars);
                                            goPars.militaryMode = 20;
                                            goPars.attackDelayCorPass = false;
                                            UnitsMover.active.AddMilitaryAvoider(goPars, BattleSystem.GetClosestBuildingPoint(goPars, tGo), 0);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (goPars.unitParsType.isWorker == false)
                            {
                                if (goPars.targetUP != null)
                                {
                                    goPars.targetUP.attackers.Remove(goPars);
                                    goPars.targetUP = null;
                                }

                                goPars.strictApproachMode = false;
                                goPars.onManualControl = true;

                                goPars.isMovingMC = true;
                                battleSystem.UnSetSearching(goPars, false);

                                Vector3 manDest = formationDest[iManual];
                                goPars.manualDestination = manDest;

                                UnitsMover.active.AddMilitaryAvoider(goPars, manDest, 0);

                                iManual = iManual + 1;
                            }
                            else if (goPars.unitParsType.isWorker)
                            {
                                if (goPars.targetUP != null)
                                {
                                    goPars.targetUP.attackers.Remove(goPars);
                                    goPars.targetUP = null;
                                }

                                goPars.strictApproachMode = false;
                                battleSystem.UnSetSearching(goPars, false);

                                ResourcesCollection rc = rtsm.nationPars[Diplomacy.active.playerNation].resourcesCollection;

                                UnitPars miningPoint = rc.GetMiningPointFromMouse();
                                ResourcePointObject rpo = rc.GetTreeFromMouse();
                                ResourcePointObject rpo2 = rc.GetRPOMouse();

                                if ((miningPoint != null) && (goPars.resourceAmount <= 0))
                                {
                                    rc.MineResource(miningPoint.resourceType, miningPoint, miningPoint.resourcePointObject);
                                }
                                else if ((rpo != null) && (goPars.resourceAmount <= 0))
                                {
                                    rc.MineResource(rpo.resourceType, null, rpo);
                                }
                                else if ((rpo2 != null) && (goPars.resourceAmount <= 0))
                                {
                                    rc.MineResource(rpo2.resourceType, null, rpo2);
                                }
                                else if ((tGo != null) && (tGo.IsResourceCollectionBuilding()) && (goPars.resourceAmount <= 0))
                                {
                                    rc.MineResource(tGo.resourceType, tGo, tGo.resourcePointObject);
                                }
                                else
                                {
                                    ResourcesCollection.WalkTo(goPars, moveToDestination);
                                }
                            }
                        }
                    }
                }
            }

            tGo = null;
        }

        public List<Vector3> LoadFormationDestinations()
        {
            UnitPars goPars;
            UnityEngine.AI.NavMeshAgent goNav;

            List<Vector3> destinations = null;
            List<UnitPars> manualUP = new List<UnitPars>();

            for (int i = 0; i < selectablesPars.Count; i++)
            {
                goPars = selectablesPars[i];
                goNav = goPars.thisNMA;

                if (rtsm.useAStar)
                {
                    if (goPars.isSelected)
                    {
                        if (tGo == null)
                        {
                            manualUP.Add(goPars);
                        }
                    }
                }
                else
                {
                    if (goNav != null)
                    {
                        if (goPars.isSelected)
                        {
                            if (tGo == null)
                            {
                                manualUP.Add(goPars);
                            }
                        }
                    }
                }
            }

            if (manualUP.Count > 0)
            {
                Formation form = null;
                bool pass = false;

                if (manualUP[0].formation != null)
                {
                    if (manualUP[0].formation.strictMode == 1)
                    {
                        if (manualUP[0].unitsGroup != null)
                        {
                            if (manualUP[0].unitsGroup.members.Count > 0)
                            {
                                if (manualUP[0].unitsGroup.members[0].formation == manualUP[0].formation)
                                {
                                    pass = true;
                                }
                            }
                            else if (manualUP[0].formation.strictMode == 1)
                            {
                                pass = true;
                            }
                        }
                    }
                }

                if (pass == true)
                {
                    form = manualUP[0].formation;
                }
                else
                {
                    form = new Formation();

                    Formations.active.AddUnitsToFormations(manualUP, form);
                }

                destinations = form.GetDestinations(manualUP, moveToDestination);
            }

            return destinations;
        }

        public void SelectByClick(Vector3 clickPos, int clickMode)
        {
            Camera cam = Camera.main;

#if URTS_UNET
        if (rtsm.isMultiplayer)
        {
            Camera[] allObjects = UnityEngine.Object.FindObjectsOfType<Camera>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                Camera cm1 = allObjects[i];

                if (cm1.gameObject.GetComponent<NetworkIdentity>() != null)
                {
                    if (cm1.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer == true)
                    {
                        cam = cm1;
                    }
                }
            }
        }
#endif

            Ray r = cam.ScreenPointToRay(clickPos);
            Vector3 camVec = cam.transform.position;

            LoadSelectables(clickMode);

            Vector3 rayDirection = r.direction;
            float distFromRay = 0f;

            UnitPars goPars;
            Vector3 goVec;

            UnitPars selCandidate_Pars = null;

            float minDist = float.MaxValue;

            if (BuildMark.active.projector.activeSelf == false)
            {
                if (isLockLeftClickSelectionOneFrameRunning == false)
                {
                    DeselectAll();
                }
            }

            for (int i = 0; i < selectablesPars.Count; i++)
            {
                goPars = selectablesPars[i];
                goVec = goPars.transform.position;

                float distFromCamera = (goVec - camVec).magnitude;

                distFromRay = Vector3.Distance(rayDirection * distFromCamera, goVec - camVec);

                if (distFromRay < goPars.rEnclosed)
                {
                    if (distFromCamera < minDist)
                    {
                        if (goPars.health > 0f)
                        {
                            minDist = distFromCamera;
                            selCandidate_Pars = goPars;
                        }
                    }
                }
            }

            if (selCandidate_Pars != null)
            {
                SelectObject(selCandidate_Pars);
                lockRectangle = true;

                if (selCandidate_Pars.nation == Diplomacy.active.playerNation)
                {
                    if (selCandidate_Pars.unitParsType.isBuilding)
                    {
                        ActivateBuildingsMenu(selCandidate_Pars.rtsUnitId);

                        if (selCandidate_Pars.rtsUnitId == 5)
                        {

                            MiningPointLabelUI.active.Activate(selCandidate_Pars.resourceType, selCandidate_Pars.resourceAmount);
                        }

                        // activating buildProgressNum
                        if (
                            (selCandidate_Pars.rtsUnitId == 0) ||
                            (selCandidate_Pars.rtsUnitId == 1) ||
                            (selCandidate_Pars.rtsUnitId == 6) ||
                            (selCandidate_Pars.rtsUnitId == 7)
                        )
                        {
                            if (selCandidate_Pars.gameObject.GetComponent<SpawnPoint>() != null)
                            {
                                SpawnPoint selCandidate_sp = selCandidate_Pars.gameObject.GetComponent<SpawnPoint>();

                                if (selCandidate_sp.isSpawning == true)
                                {
                                    CancelSpawnUI.active.Activate();
                                    ProgressCounterUI.active.Activate();
                                    DeActivateBuildingsMenu();
                                }
                                else
                                {
                                    CancelSpawnUI.active.DeActivate();
                                    ActivateBuildingsMenu(selCandidate_Pars.rtsUnitId);
                                }
                            }
                        }
                        else
                        {
                            CancelSpawnUI.active.DeActivate();
                            ActivateBuildingsMenu(selCandidate_Pars.rtsUnitId);
                        }
                    }
                    else if (selCandidate_Pars.unitParsType.isBuilding == false)
                    {
                        if (useDoubleSelect)
                        {
                            if (isDoubleSelectRunning == false)
                            {
                                StartCoroutine(DoubleClickSelect());
                            }
                            else
                            {
                                for (int i = 0; i < rtsm.allUnits.Count; i++)
                                {
                                    if (rtsm.allUnits[i] != selCandidate_Pars)
                                    {
                                        if (rtsm.allUnits[i].rtsUnitId == selCandidate_Pars.rtsUnitId)
                                        {
                                            if (rtsm.allUnits[i].nation == selCandidate_Pars.nation)
                                            {
                                                if ((rtsm.allUnits[i].transform.position - selCandidate_Pars.transform.position).magnitude < doubleSelectDistance)
                                                {
                                                    SelectObject(rtsm.allUnits[i]);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        ActivateUnitsMenu();
                    }
                }

                SelectedUnitsInfo();
                PlaySelectSound();
            }
        }

        public static bool isLockLeftClickSelectionOneFrameRunning = false;

        public void LockLeftClickSelectionOneFrame()
        {
            if (isLockLeftClickSelectionOneFrameRunning == false)
            {
                StartCoroutine(LockLeftClickSelectionOneFrameCor());
            }
        }

        IEnumerator LockLeftClickSelectionOneFrameCor()
        {
            isLockLeftClickSelectionOneFrameRunning = true;
            yield return new WaitForEndOfFrame();
            isLockLeftClickSelectionOneFrameRunning = false;
        }

        public void RightClick(Vector3 clickPos)
        {
            Ray r = Camera.main.ScreenPointToRay(clickPos);
            Vector3 camVec = Camera.main.transform.position;

            LoadSelectables(1);

            Vector3 rayDirection = r.direction;
            float distFromRay = 0.0f;

            UnitPars goPars;
            Vector3 goVec;

            for (int i = 0; i < selectablesPars.Count; i++)
            {
                goPars = selectablesPars[i];

                goVec = goPars.transform.position;
                float distFromCamera = (goVec - camVec).magnitude;

                distFromRay = Vector3.Distance(rayDirection * distFromCamera, goVec - camVec);

                if (distFromRay < goPars.rEnclosed)
                {
                    tGo = goPars;
                }
            }
        }

        public void SelectByRect()
        {
            LoadSelectables(0);

            Camera camera = Camera.main;

            UnitPars goPars;
            Vector3 goPos;
            Vector3 camPos;

            for (int i = 0; i < selectablesPars.Count; i++)
            {
                goPars = selectablesPars[i];
                goPos = goPars.transform.position;

                // if ManualControl is attached to gameobject
                camPos = camera.WorldToScreenPoint(goPos);
                camPos.y = SelectionManager.InvertMouseY(camPos.y);

                if (selection.Contains(camPos))
                {
                    if (goPars.unitParsType.isBuilding == false)
                    {
                        if (goPars.health > 0f)
                        {
                            SelectObject(goPars);

                            ActivateUnitsMenu();
                        }
                    }
                }
            }

            if (selectedGoPars.Count > 0)
            {
                SelectedUnitsInfo();
                PlaySelectSound();
            }
        }

        public void LoadSelectables(int mode)
        {
            selectablesPars.Clear();

            for (int i = 0; i < allUnits.Count; i++)
            {
                UnitPars goPars = allUnits[i];

                if (mode == 0)
                {
                    if (goPars.nation == Diplomacy.active.playerNation)
                    {
                        selectablesPars.Add(goPars);
                    }
                }
                else if (mode == 1)
                {
                    selectablesPars.Add(goPars);
                }
            }
        }

        public void SelectedUnitsInfo()
        {
            if (selectedGoPars.Count == 1)
            {
                UnitPars up = selectedGoPars[0];
                SelectedIconUI.active.Activate(up.rtsUnitId);
            }
            else if (selectedGoPars.Count > 1)
            {
                SelectedIconUI.active.Activate(-1);
            }
        }

        public IEnumerator ResetWarNotice(NationPars np)
        {
            yield return new WaitForSeconds(8.0f);

            if (np != null)
            {
                np.isWarWarningIssued = false;
            }
        }

        public void ActiveScreenTrue()
        {
            isMouseOnActiveScreen = true;
        }

        public void ActiveScreenFalse()
        {
            isMouseOnActiveScreen = false;
        }

        public bool IsOnActiveScreenRect()
        {
            bool isOnScreen = false;

            if (
                lastMousePosition.x / Screen.width > 0.02f &&
                lastMousePosition.x / Screen.width < 0.85f &&
                lastMousePosition.y / Screen.height > 0.03f &&
                lastMousePosition.y / Screen.height < 0.95f
            )
            {
                isOnScreen = true;
            }

            return isOnScreen;
        }

        public void StopSelectedSpawning()
        {
            if (selectedGoPars.Count == 1)
            {
                SpawnPoint sp = selectedGoPars[0].gameObject.GetComponent<SpawnPoint>();
                sp.StopSpawning();
            }
        }

        public void ActivateUnitsMenu()
        {
            UnitUI.active.ActivateWorker();
        }

        public void DeActivateUnitsMenu()
        {
            SpawnGridUI.active.CloseBuildingMenu();
            UnitUI.active.DeActivate();
        }

        public void ActivateBuildingsMenu(int id)
        {
            if (selectedGoPars.Count == 1)
            {
                if (selectedGoPars[0].isBuildFinished)
                {
                    SpawnGridUI.active.OpenBuildingMenu(HeroCheckId(id));
                }
            }
        }

        public void RefreshCentralBuildingMenuOnHeroPresence(UnitPars up)
        {
            if (up.nation == Diplomacy.active.playerNation)
            {
                if (up.rtsUnitId == 20)
                {
                    if (selectedGoPars.Count == 1)
                    {
                        UnitPars selectedUnit = selectedGoPars[0];

                        if (selectedUnit.nation == up.nation)
                        {
                            if (selectedUnit.rtsUnitId == 0)
                            {
                                SpawnGridUI.active.CloseBuildingMenu();
                                SpawnGridUI.active.ToggleGrid(HeroCheckId(selectedUnit.rtsUnitId));
                            }
                        }
                    }
                }
            }
        }

        public int HeroCheckId(int id)
        {
            if (id == 0)
            {
                if (RTSMaster.active.numberOfUnitTypes[Diplomacy.active.playerNation][20] <= 0)
                {
                    return 20;
                }
            }

            return id;
        }

        public void DeActivateBuildingsMenu()
        {
            SpawnGridUI.active.CloseBuildingMenu();
        }

        public void DeselectAll()
        {
            for (int i = 0; i < selectedGoPars.Count; i++)
            {
                DeselectObject(selectedGoPars[i]);
            }

            for (int i = 0; i < allUnits.Count; i++)
            {
                DeselectObject(allUnits[i]);
            }

            selectedGoPars.Clear();
        }

        public int SingleUnitsTypeTroop()
        {
            // returns rtsUnitId if troop is made from single type units and -1 otherwise
            int sTroop = -1;

            if (selectedGoPars.Count > 0)
            {
                List<int> typesCount = rtsm.GetTypesCount(selectedGoPars);

                for (int i = 0; i < typesCount.Count; i++)
                {
                    if (typesCount[i] == selectedGoPars.Count)
                    {
                        sTroop = i;
                    }
                }
            }

            return sTroop;
        }

        public int SelectedUnitsCount(int id)
        {
            // returns number of units in selection of chosen id
            int nUnits = 0;

            for (int i = 0; i < selectedGoPars.Count; i++)
            {
                if (selectedGoPars[i].rtsUnitId == id)
                {
                    nUnits = nUnits + 1;
                }
            }

            return nUnits;
        }

        public bool AreAllSelectedUnitsWorkers()
        {
            if (selectedGoPars.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < selectedGoPars.Count; i++)
            {
                if (selectedGoPars[i].unitParsType.isWorker == false)
                {
                    return false;
                }
            }

            return true;
        }

        public void DeselectObject(UnitPars goPars)
        {
            if (goPars.isSelected == true)
            {
                goPars.isSelected = false;
                selectedGoPars.Remove(goPars);

                if (UnitSelectionMark.active != null)
                {
                    UnitSelectionMark.active.RemoveUnit(goPars);
                }

                if (selectedGoPars.Count < 1)
                {
                    SelectedIconUI.active.DeActivate();
                }

                totalSelectedHealth = totalSelectedHealth - goPars.maxHealth;

                if (selectedGoPars.Count < 1)
                {
                    SelectedIconUI.active.DeActivate();
                    CancelSpawnUI.active.DeActivate();
                }
                else
                {
                    SelectedIconUI.active.SetHealth(remainingSelectedHealth / totalSelectedHealth);
                }

                if (selectedGoPars.Count < 1)
                {
                    DeActivateUnitsMenu();
                }

                if (goPars.unitParsType.isBuilding == true)
                {
                    DeActivateBuildingsMenu();
                }

                if (selectedGoPars.Count == 0)
                {
                    UnitUI.active.DeActivate();
                }

                if (ProgressCounterUI.active.isActive)
                {
                    ProgressCounterUI.active.DeActivate();
                }

                if (MiningPointLabelUI.active != null)
                {
                    MiningPointLabelUI.active.DeActivate();
                }

                if (SpawnNumberUI.active != null)
                {
                    SpawnNumberUI.active.DisableScrollMode();
                }

                if (LevelElementsManager.active != null)
                {
                    LevelElementsManager.active.DeActivate();
                }
            }
        }

        public void SelectObject(UnitPars goPars)
        {
            DeselectObject(goPars);
            goPars.isSelected = true;
            selectedGoPars.Add(goPars);

            if (UnitSelectionMark.active != null)
            {
                UnitSelectionMark.active.AddUnit(goPars);
            }

            CheckFormation(goPars);
        }

        public void SelectObjectS(UnitPars goPars)
        {
            goPars.isSelected = true;
            selectedGoPars.Add(goPars);

            if (UnitSelectionMark.active != null)
            {
                UnitSelectionMark.active.AddUnit(goPars);
            }
        }

        public void CheckFormation(UnitPars goPars)
        {
            if (goPars.formation != null)
            {
                Formation form = goPars.formation;

                if (form.strictMode == 1)
                {
                    for (int i = 0; i < form.units.Count; i++)
                    {
                        UnitPars up = form.units[i];

                        if (up != goPars)
                        {
                            if (up.isDying == false)
                            {
                                if (up.isSinking == false)
                                {
                                    selectedGoPars.Remove(up);
                                    SelectObjectS(up);
                                }
                            }
                        }
                    }
                }
            }
        }

        IEnumerator DoubleClickSelect()
        {
            isDoubleSelectRunning = true;
            yield return new WaitForSeconds(doubleSelectTime);
            isDoubleSelectRunning = false;
        }

        public void PlaySelectSound()
        {
            if (selectSound != null)
            {
                AudioSource.PlayClipAtPoint(selectSound, RTSCamera.active.transform.position);
            }
        }

        [System.Serializable]
        public class CursorTexture
        {
            public Texture2D activeTexture;
            public Gradient colorGradient;
            public float gradientTransitionPeriod = 1f;

            public int sizeMin = 16;
            public int sizeMax = 16;
            public float sizeVariationPeriod = 1f;

            public Color GetColor()
            {
                return colorGradient.Evaluate(Mathf.PingPong(Time.time / gradientTransitionPeriod, 1f));
            }

            public Rect GetRect(Vector2 mousePos)
            {
                int size = (int)(Mathf.PingPong(Time.time / sizeVariationPeriod, 1f) * (sizeMax - sizeMin) + sizeMin);
                return (new Rect(mousePos.x - size, mousePos.y - size, 2 * size, 2 * size));
            }
        }
    }
}
