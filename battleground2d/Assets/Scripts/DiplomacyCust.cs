using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiplomacyCust : MonoBehaviour
{
    public static DiplomacyCust active;

    [HideInInspector] public int numberNations;
    public List<List<int>> relations = new List<List<int>>();
    public int playerNation = 0;

    public int defaultRelation = 1;

    void Awake()
    {
        active = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        SetAllWar();
    }

    private void SetAllWar()
    {
        for (int i = 0; i < numberNations; i++)
        {
            for (int j = 0; j < numberNations; j++)
            {
                if (i != j)
                {
                    relations[i][j] = 1;
                }
            }
        }
    }


    public void AddNation()
    {
        for (int i = 0; i < numberNations; i++)
        {
            relations[i].Add(defaultRelation);
        }

        relations.Add(new List<int>());

        for (int i = 0; i < numberNations+1; i++)
        {
            relations[numberNations].Add(defaultRelation);
        }

        numberNations++;

        BattleSystemCust.active.AddNation();
    }
}
