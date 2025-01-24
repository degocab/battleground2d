using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAnimationController : MonoBehaviour
{
    public Material material; // Reference to the material with the sprite sheet

    void Start()
    {
        material.SetFloat("_AnimationFrame", 0); // Start with the first frame (manually set)
    }

    void Update()
    {
        float frame = Mathf.PingPong(Time.time * 10, 16); // Animate by incrementing frame over time
        material.SetFloat("_AnimationFrame", frame); // Update the frame
    }
}
