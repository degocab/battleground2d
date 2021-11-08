using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitParsCust : MonoBehaviour
{
    [SerializeField]
    public int UniqueID = 0;
    public bool IsEnemy { get; set; }


    [HideInInspector]public UnitParsTypeCust unitParsTypeCust;
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

    private Transform childTransform;
    private Animator childAnimator;
    private SpriteRenderer childSpriteRenderer;
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

    public float attackRate { get; set; }

    public UnityEngine.AI.NavMeshAgent nma;
    public Vector3 velocityVector = Vector3.zero;
    internal bool randomFrame;
    internal Vector3 lastDirection;

    public string CurrentCommand { get; set; }
    public string PreviousCommand { get; set; }
    
    void Start()
    {

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
        attackRate = 6;
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

}





