using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIControl : MonoBehaviour
{

    public string CurrentCommand { get; set; }
    public string PreviousCommand { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        CurrentCommand = "Attack";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
