using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIControl : MonoBehaviour
{

    public string CurrentCommand { get; set; }

    private GameObject battleManager;
    private BattleSystemCust battleManagerScript;

    public string PreviousCommand { get; set; }

    public List<UnitParsCust> selectedUnits;

    // Start is called before the first frame update
    void Start()
    {
        CurrentCommand = "Hold";
        //selectedUnits = new List<UnitParsCust>();

        battleManager = GameObject.Find("BattleManager");
        battleManagerScript = battleManager.GetComponent<BattleSystemCust>();

    }

    //testing for now
    public List<UnitParsCust> selectedCommanders = new List<UnitParsCust>();

    public bool firstRun = true;
    // Update is called once per frame
    void Update()
    {
        if (selectedCommanders.Count == 0 && battleManager != null && battleManagerScript.allUnits.Count > 0)
        {
            for (int i = 0; i < battleManagerScript.allUnits.Count; i++)
            {
                UnitParsCust unit = battleManagerScript.allUnits[i];

                if (unit.IsEnemy && unit.UnitRank == 1)
                {
                    selectedCommanders.Add(unit);  
                }
            }
        }

        if (selectedCommanders.Count > 0 && firstRun)
        {
            for (int i = 0; i < selectedCommanders.Count; i++)
            {
                selectedCommanders[i].CurrentCommand = CurrentCommand;
            }
            firstRun = false;
        }
    }
}
