using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public string PlayerCommand { get; set; } = "Attack";
    public string PreviousCommand { get; set; }
    public float movementSpeed = 0.1f;
    private UnitParsCust apprPars { get; set; }
    public Vector2 lastDirection { get; private set; }

    public PlayAnimationCust playAnimationCust;
    public List<UnitParsCust> selectedUnits;

    [SerializeField]
    public List<Material> selectionRings;


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


    // Start is called before the first frame update
    void Start()
    {
        apprPars = GetComponent<UnitParsCust>();


        apprPars.isReady = true;

        selectedUnits = new List<UnitParsCust>();
        //selectionRings = new List<Material>();
    }

    // Update is called once per frame
    void Update()
    {

        //TODO: fix units just attacking instead of waiting on hold 
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            PlayerCommand = "Hold";
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            PlayerCommand = "Move";
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            PlayerCommand = "Attack";
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            PlayerCommand = "Follow";
        }
        if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Alpha4))
        {

            //draw selection

            if (BattleSystemCust.active != null && BattleSystemCust.active.allUnits != null && BattleSystemCust.active.allUnits.Count(x => !x.IsEnemy) > 0)
            {

                selectedUnits = new List<UnitParsCust>();

                UnitParsCust[] units = BattleSystemCust.active.allUnits.Where(x => !x.IsEnemy).ToArray();
                var curPos = this.transform.position;

                for (int i = 0; i < units.Count(); i++)
                {
                    UnitParsCust pos = units[i];
                    if ((pos.transform.position.x < curPos.x + 2 && pos.transform.position.x > curPos.x - 2)
                                                                               && (pos.transform.position.y < curPos.y + 2 && pos.transform.position.y > curPos.y - 2)

                                                                               )
                    {
                        Material selectionRing;
                        
                        //horizontal dir
                        if (new int[]{ 1,2 }.Contains( pos.playAnimationCust.animDir))
                        {
                            selectionRing = selectionRings[0];
                        }
                        else
                        {
                            selectionRing = selectionRings[1];
                        }
                        Material curMat = pos.springAttractScreenRend.material;

                        pos.springAttractScreenRend.materials = new Material[2] { curMat, selectionRing };

                        selectedUnits.Add(pos);

                    }
                }
            }

        }




        float deltaTime = Time.deltaTime;
        HandleMovement();
        //UpdateAnimation(deltaTime);
    }

    private void HandleMovement()
    {




        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");


        Vector2 movementDirection = new Vector2(horizontal, vertical);
        movementDirection.Normalize();

        float movementSpeedLocal = movementSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movementSpeedLocal = 1.0f;
        }
        else
        {
            movementSpeedLocal = 0.4f;
        }

        transform.position = transform.position + new Vector3(horizontal * movementSpeedLocal * Time.deltaTime, vertical * movementSpeedLocal * Time.deltaTime, 0);
        //var direction = transform.forward;
        //direction.y = 0;
        if (Math.Abs(vertical) > 0 || Math.Abs(horizontal) > 0)
        {
            lastDirection = movementDirection;

            apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Run, movementDirection, default);

        }
        else
        {
            apprPars.playAnimationCust.PlayAnim(UnitAnimDataCust.BaseAnimMaterialType.Idle, lastDirection, default);

        }


    }
    internal void UpdateAnimation(float deltaTime)
    {

        //play animations
        if (this.playAnimationCust.forced)
        {
            this.spriteSheetData = UnitAnimationCust.PlayAnimForced(/*ref prepSheetUnitPars, */this.playAnimationCust.baseAnimType, this.playAnimationCust.animDir, this.playAnimationCust.onComplete
                                                                                 , apprPars.UnitType, apprPars.IsEnemy);

        }
        else
        {
            SpriteSheetAnimationDataCust currSpriteSheetData = this.spriteSheetData;
            SpriteSheetAnimationDataCust? newSpriteSheetData = UnitAnimationCust.PlayAnim(/*ref prepSheetUnitPars, */this.playAnimationCust.baseAnimType, currSpriteSheetData, this.playAnimationCust.animDir, this.playAnimationCust.onComplete
                                                                                          , apprPars.UnitType, apprPars.IsEnemy);

            // if changes
            if (newSpriteSheetData != null)
            {
                this.spriteSheetData = newSpriteSheetData.Value;
            }
        }




        //if (IsDead) return; // Skip animation update if unit is dead

        //// Random frame logic
        //if (randomFrame)
        //{
        //    currentFrame = UnityEngine.Random.Range(0, spriteSheetData.frameCount);
        //    randomFrame = false;
        //}

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
}
