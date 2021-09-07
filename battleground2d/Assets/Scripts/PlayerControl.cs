using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public string PlayerCommand { get; set; }
    public string PreviousCommand { get; set; }
    public float movementSpeed = 0.1f;
    private UnitParsCust apprPars { get; set; }
    public Vector2 lastDirection { get; private set; }

    public PlayAnimationCust playAnimationCust;


    // Start is called before the first frame update
    void Start()
    {
        apprPars = GetComponent<UnitParsCust>();


        apprPars.isReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayerCommand = "Hold";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayerCommand = "Move";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayerCommand = "Attack";
        }

        HandleMovement();
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
}
