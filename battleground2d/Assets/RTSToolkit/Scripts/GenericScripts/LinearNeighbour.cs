using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class LinearNeighbour
    {
        public Vector3[] pts;

        public static LinearNeighbour MakeFromPoints(params Vector3[] points)
        {
            LinearNeighbour root = new LinearNeighbour();
            root.pts = points;
            return root;
        }

        public int FindNearest(Vector3 pt)
        {
            int bestIndex = -1;
            float bestSqDist = float.MaxValue;

            for (int i = 0; i < pts.Length; i++)
            {
                float R = (pts[i] - pt).sqrMagnitude;
                if (R < bestSqDist)
                {
                    bestIndex = i;
                    bestSqDist = R;
                }
            }

            return bestIndex;
        }

        public int[] FindNearestsR(Vector3 pt, float rcrit)
        {
            List<int> ind_pts = new List<int>();

            for (int i = 0; i < pts.Length; i++)
            {
                if ((pts[i] - pt).magnitude < rcrit)
                {
                    ind_pts.Add(i);
                }
            }

            int[] ind_pts2 = null;
            if (ind_pts.Count > 0)
            {
                ind_pts2 = ind_pts.ToArray();
            }

            return ind_pts2;
        }
    }
}
