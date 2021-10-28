using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// https://gamedev.stackexchange.com/questions/116195/displaying-trajectory-path
/// replicate logic to create arrow arc
/// </summary>
public class ArrowParsCust : MonoBehaviour
{
    [HideInInspector] public UnitParsCust attPars = null;
    [HideInInspector] public UnitParsCust targPars = null;
    //I forewent the transform.forward because that would only confuse things right now
    public Vector3 gravity = new Vector3(0.0f, -9.81f, 0.0f);//Vector3.up * -9.81f;
    //let's assume it's always down
    public float drag = 0.01f;

    // A coefficient for the amount of drag in the medium
    public Vector3 velocity;

    //The movement to make this contains direction and speed
    private bool moving = false;
    float lifteTime, force;
    public float gravDivider = 4;
    private Vector3 previousPos;



    //store target position
    public Vector3 targetPos;

    //store source unit position.y;
    public Vector3 sourcePosition;
    private float test_t;
    private MeshRenderer mesh;

    public bool HasDamaged { get; set; }

    public float gravity2 = 9.8f;


    private void Start()
    {

        test_t = GameObject.Find("BattleManager").GetComponent<BattleSystemCust>().t;
        mesh = this.GetComponent<MeshRenderer>();
    }

    public void Init(float _force, Vector3 _velocity)
    {

        previousPos = transform.position;

        moving = true;
        targetPos.y -= .4f;
        HasDamaged = false;



    }



    // Update is called once per frame
    void Update()
    {

        //store previous pos to rotate towards
        if (!previousPos.Equals(transform.position))
        {
            previousPos = transform.position; 
        }

        //doesnt hit but should stop
        if (((targetPos.x + .75f > transform.position.x) && (targetPos.x - .75f < transform.position.x)) 
            && (targetPos.y + .75f > transform.position.y && targetPos.y - .75f < transform.position.y))
        {

            if (((targetPos.x + .25f > transform.position.x) && (targetPos.x - .25f < transform.position.x))
                && (targetPos.y + .25f > transform.position.y && targetPos.y - .25f < transform.position.y))
            {

                if (targPars != null)
                {


                    if (!HasDamaged)
                    {
                        targPars.health = targPars.health - (10f + UnityEngine.Random.Range(0f, 15f));
                        HasDamaged = true; 
                    }
                }

                if (targetPos.y + .25f > transform.position.y && targetPos.y - .25f < transform.position.y)
                {
                    moving = false;
                    var currPos = transform.position;
                }
            }

        }


        if (moving)
        {


            //calc dist
            float target_distance = Vector3.Distance(transform.position, targetPos);

            float firingAngle = 30.0f;

            //calc velocity required needed to throw objc to the target at angle
            float projectile_velocity = target_distance / (Mathf.Sin(2 * firingAngle * Mathf.Deg2Rad) / gravity2);

            //extract the x y component of the velocity
            float Vx = Mathf.Sqrt(projectile_velocity) * Mathf.Cos(firingAngle * Mathf.Deg2Rad);
            float Vy = Mathf.Sqrt(projectile_velocity) * Mathf.Sin(firingAngle * Mathf.Deg2Rad);

            //calc flight time
            float flightDuration = target_distance / Vx;

            ////rotate project to face target 
            var rot = targetPos - transform.position;
            transform.rotation = Quaternion.LookRotation(rot);


            transform.Translate(0, (Vy - (gravity2 * 0)) * Time.deltaTime, Vx * Time.deltaTime);
            elapsed_time += Time.deltaTime;


            //rotate to direction
            //var deltaX = targetPos.x - attPars.transform.position.x;
            //var deltaY = targetPos.y - attPars.transform.position.y;
            var deltaX = transform.position.x - previousPos.x;
            var deltaY = transform.position.y - previousPos.y;
            var rad = math.atan2(deltaY, deltaX); // In radians

            var deg = rad * (180 / math.PI);
            transform.eulerAngles = new Vector3(0f, 0.0f, deg);

        }



    }
    float elapsed_time = 0;




    /// <summary>
    /// get popsition from a parabola defined by start and end, height, and time
    /// </summary>
    /// <param name="start">
    /// The start point of the parabola
    /// </param>
    /// <param name="end">
    /// The end point of the parabola
    /// </param>
    /// <param name="height">
    /// The height of the parabola at its maximum
    /// </param>
    /// <param name="t">
    /// Normalized time (0 >1)
    /// </param>
    /// <returns></returns>
    public Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
    {
        t = test_t;

        float parabolicT = t * 2 - 1;
        if (Mathf.Abs(start.y - end.y) < 0.5f)
        {
            //start and end are roughly level, pretend they are - simpler solution with less steps
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-parabolicT * parabolicT + 1) * height;
            result.z = 0;
            return result;
        }
        else
        {
            //start and end are not level, gets more complicated
            Vector3 travelDirection = end - start;
            Vector3 levelDirecteion = end - new Vector3(start.x, end.y, 0f);
            Vector3 right = Vector3.Cross(travelDirection, levelDirecteion);
            Vector3 up = Vector3.Cross(right, travelDirection);
            if (end.y > start.y)
                up = -up;
            Vector3 result = start + t * travelDirection;
            result += ((-parabolicT * parabolicT + 1) * height) * up.normalized;
            result.z = 0;
            return result;
        }
    }

}
