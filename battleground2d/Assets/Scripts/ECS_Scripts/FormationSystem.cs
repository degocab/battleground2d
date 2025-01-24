using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FormationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref PositionComponent position, in FormationComponent formation ) => 
        {
            //exmaple of line formatoin
            if (formation.formationType == 0)
            {
                position.value.x += 2f;
            }
        }).Schedule();
    }
}
