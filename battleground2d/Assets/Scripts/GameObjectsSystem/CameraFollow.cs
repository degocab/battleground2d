using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    public Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            //transform.position = new Vector3(player.position.x, player.position.y, 0);
        }
    }

     void LateUpdate()
    {
        Vector3 newPos = player.position + offset;
        newPos.z = 0f;
        transform.position = newPos;
    }
}
