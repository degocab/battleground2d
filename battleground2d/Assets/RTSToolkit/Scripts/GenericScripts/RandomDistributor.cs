using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class RandomDistributor
    {
        public List<Vector3> dataPoints = new List<Vector3>();

        public Vector3 minimumLimits;
        public Vector3 maximumLimits;

        public int nPoints;
        public int nIterations;

        public float minNeighDist;

        int iIter = 0;

        public static List<Vector3> CreateDistributionD(
            Vector3 minLim,
            Vector3 maxLim,
            int nPt,
            int nIt,
            float limFrac
        )
        {
            Vector3 dlim = maxLim - minLim;

            float vol = dlim.x * dlim.z;
            float celVol = vol / nPt;

            float celSize = Mathf.Pow(celVol, (1f / 2f));

            return RandomDistributor.CreateDistribution(
                minLim,
                maxLim,
                nPt,
                nIt,
                limFrac * celSize
            );
        }

        public static List<Vector3> CreateDistribution(
            Vector3 minLim,
            Vector3 maxLim,
            int nPt,
            int nIt,
            float minDist
        )
        {
            RandomDistributor rd = new RandomDistributor();

            rd.minimumLimits = minLim;
            rd.maximumLimits = maxLim;

            rd.nPoints = nPt;
            rd.nIterations = nIt;

            rd.minNeighDist = minDist;

            rd.DistributePoints(nPt);

            return rd.dataPoints;
        }

        public void DistributePoints(int n)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 p = new Vector3(
                    Random.Range(minimumLimits.x, maximumLimits.x),
                    Random.Range(minimumLimits.y, maximumLimits.y),
                    Random.Range(minimumLimits.z, maximumLimits.z)
                );

                dataPoints.Add(p);
            }

            RemoveUnwanted();
        }

        public void RemoveUnwanted()
        {
            List<int> mask = new List<int>();

            for (int i = 0; i < dataPoints.Count; i++)
            {
                mask.Add(0);
            }

            KDTree kd = KDTree.MakeFromPoints(dataPoints.ToArray());

            for (int i = dataPoints.Count - 1; i >= 0; i--)
            {
                int neigh = kd.FindNearestK(dataPoints[i], 2);
                float rNeigh = (dataPoints[i] - dataPoints[neigh]).magnitude;

                if (rNeigh < minNeighDist)
                {
                    if (mask[neigh] == 0)
                    {
                        mask[i] = 1;
                    }
                }
            }

            List<Vector3> cDataPoints = new List<Vector3>();

            for (int i = 0; i < dataPoints.Count; i++)
            {
                if (mask[i] == 0)
                {
                    cDataPoints.Add(dataPoints[i]);
                }
            }

            dataPoints = cDataPoints;
            iIter++;

            if (iIter < nIterations)
            {
                int n = mask.Count - dataPoints.Count;
                Debug.Log(n);
                if (n > 0)
                {
                    DistributePoints(n);
                }
            }
        }
    }
}
