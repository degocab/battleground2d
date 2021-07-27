using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class KDHelper
    {
        public static KDTree TreeFromUnitPars(List<UnitPars> units)
        {
            int n = units.Count;

            if (n > 0)
            {
                Vector3[] positions = new Vector3[n];

                for (int i = 0; i < n; i++)
                {
                    positions[i] = units[i].transform.position;
                }

                return KDTree.MakeFromPoints(positions);
            }

            return null;
        }

        public static UnitPars FindNearestUP(Vector3 origin, List<UnitPars> allUnits, KDTree kd)
        {
            if (kd != null)
            {
                int i = kd.FindNearest(origin);

                if (i >= 0 && i < allUnits.Count)
                {
                    return allUnits[i];
                }
            }

            return null;
        }

        public static UnitPars FindNearestUPExcept(Vector3 origin, List<UnitPars> allUnits, KDTree kd, int ignoredType)
        {
            if (kd != null)
            {
                int iterations = 5;

                for (int j = 0; j < iterations; j++)
                {
                    int i = kd.FindNearestK(origin, j + 1);

                    if (i >= 0 && i < allUnits.Count)
                    {
                        if (allUnits[i].rtsUnitId != ignoredType)
                        {
                            return allUnits[i];
                        }
                    }
                }
            }

            return null;
        }
    }
}
