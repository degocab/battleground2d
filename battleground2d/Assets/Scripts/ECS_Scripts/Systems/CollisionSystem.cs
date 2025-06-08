//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Unity.Burst;
//using Unity.Collections;
using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Physics;
//using Unity.Rendering;
//using Unity.Transforms;
//using UnityEngine;

public struct Unit : IComponentData
{
    public bool isMounted;  // Flag to indicate if this unit is mounted (e.g., cavalry)
    /// <summary>
    /// rank 1: soldier/hoplite
    /// rank 2: elite soldier - leads the group formations/is the commander's personal guard
    /// rank 3: officer - commands 15 soldiers
    /// rank 4: captain - commands phalanx/~256 units
    /// rank 5: general - commands whole sections or unit types
    /// rank 6: commander's personal guard
    /// rank 7: commander - player or AI commander
    /// </summary>
    public int rank;

}
