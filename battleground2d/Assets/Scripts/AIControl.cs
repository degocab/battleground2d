using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIControl : MonoBehaviour
{

    public string CurrentCommand { get; set; }
    public string PreviousCommand { get; set; }

    public List<UnitParsCust> selectedUnits;

    // Start is called before the first frame update
    void Start()
    {
        CurrentCommand = "Attack";
        //selectedUnits = new List<UnitParsCust>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
