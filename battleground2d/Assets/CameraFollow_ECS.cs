using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow_ECS : MonoBehaviour
{
    public float moveSpeed = 5f;  // Movement speed for the camera
    public Vector2 offset = new Vector2(0, 0);  // Optional offset to adjust the camera's starting position relative to the player

    private Vector3 targetPosition;  // The target position the camera should move to

    // Start is called before the first frame update
    void Start()
    {
        targetPosition = transform.position;  // Set the initial target position to the current camera position
    }

    // Update is called once per frame
    void Update()
    {
        // Get player input (WASD, Arrow Keys, or joystick)
        float horizontal = Input.GetAxis("Horizontal");  // Left/Right input
        float vertical = Input.GetAxis("Vertical");      // Up/Down input

        // Calculate the direction to move the camera based on input
        Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;  // Normalize to avoid faster diagonal movement

        // Update target position based on input
        targetPosition += moveDirection * moveSpeed * Time.deltaTime;

        // Optionally add an offset to the camera position (adjust if needed)
        //targetPosition += new Vector3(offset.x, offset.y, -13);

        // Smoothly move the camera to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, -13f);
    }
}
