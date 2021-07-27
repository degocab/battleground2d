using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KDTreeCust
{

    public KDTreeCust[] lr;
    public Vector3 pivot;
    public int pivotIndex;
    public int axis;


    //	Change this value to 2 if you only need two-dimensional X,Y points. The search will
    //	be quicker in two dimensions.
    const int numDims = 2;

    public KDTreeCust()
    {
        lr = new KDTreeCust[2];
    }

    public static KDTreeCust MakeFromPoints(params Vector3[] pointss)
    {
        Vector3[] points = new Vector3[pointss.Length + 1];

        points[0] = new Vector3(-999999999999.99f, -999999999999.99f, -999999999999.99f);

        for (int i = 1; i < points.Length; i++)
        {
            points[i] = pointss[i - 1];
        }

        return MakeFromPointsC(points);
    }

    // make a new tree from a list of points
    public static KDTreeCust MakeFromPointsC(params Vector3[] points)
    {
        int[] indices = Iota(points.Length);
        return MakeFromPointsInner(0, 0, points.Length - 1, points, indices);
    }

    //recursively build a tree by separating points at plane boundaries
    // this might need simplified with my 2d game, i dont care about depth of plane
    static KDTreeCust MakeFromPointsInner(
            int depth,// -- do i need this?
            int stIndex,
            int enIndex,
            Vector3[] points,
            int[] inds
        )
    {
        KDTreeCust root = new KDTreeCust();
        root.axis = depth % numDims;

        //find middle point to split points by
        int splitPoint = FindPivotIndex(points, inds, stIndex, enIndex, root.axis);

        root.pivotIndex = inds[splitPoint];
        root.pivot = points[root.pivotIndex];

        int leftEndIndex = splitPoint - 1;

        if (leftEndIndex >= stIndex)
        {
            root.lr[0] = MakeFromPointsInner(depth + 1, stIndex, leftEndIndex, points, inds);
        }

        int rightStartIndex = splitPoint + 1;

        if (rightStartIndex <= enIndex)
        {
            try
            {
                root.lr[1] = MakeFromPointsInner(depth + 1, rightStartIndex, enIndex, points, inds);

            }
            catch (Exception e)
            {

                throw;
            }
        }

        return root;

    }

    // Find a new pivot index from the range by splitting the points that fall either side
    // of its plane -- split values by smaller than and greater than or equal to
    static int FindPivotIndex(Vector3[] points, int[] inds, int stIndex, int enIndex, int axis)
    {
        int splitPoint = FindSplitPoint(points, inds, stIndex, enIndex, axis);

        Vector3 pivot = points[inds[splitPoint]];
        SwapElements(inds, stIndex, splitPoint);

        int currPt = stIndex + 1;
        int endPt = enIndex;

        //swapping points until we have all the values we need on one side
        while (currPt <= endPt)
        {
            Vector3 curr = points[inds[currPt]];


            if ((curr[axis] > pivot[axis]))
            {
                SwapElements(inds, currPt, endPt);
                endPt--;
            }
            else
            {
                SwapElements(inds, currPt - 1, currPt);
                currPt++;
            }
        }
        return currPt - 1;
    }

    /// <summary>
    /// swapping values
    /// </summary>
    /// <param name="arr"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    static void SwapElements(int[] arr, int a, int b)
    {
        int temp = arr[a];
        arr[a] = arr[b];
        arr[b] = temp;
    }


    // simple "median of three" heuristic to find a reasonable splitting plane
    static int FindSplitPoint(Vector3[] points, int[] inds, int stIndex, int enIndex, int axis)
    {
        float a = points[inds[stIndex]][axis];
        float b = points[inds[enIndex]][axis];
        int midIndex = (stIndex + enIndex) / 2;
        float m = points[inds[midIndex]][axis];

        if (a > b)
        {
            if (m > a)
            {
                return stIndex;
            }

            if (b > m)
            {
                return enIndex;
            }
            return midIndex;

        }
        else
        {
            if (a > m)
            {
                return stIndex;
            }

            if (m > b)
            {
                return enIndex;
            }

            return midIndex;
        }
    }


    // find the nearest point in the set of the supplied point
    public int FindNearest(Vector3 pt)
    {
        float bestSqDist = float.MaxValue;
        int bestIndex = -1;

        Search(pt, ref bestSqDist, ref bestIndex);

        return bestIndex - 1;
    }

    // recursively search the tree
    void Search(Vector3 pt, ref float bestSqSoFar, ref int bestIndex)
    {
        float mySqDist = (pivot - pt).sqrMagnitude;

        if (mySqDist < bestSqSoFar)
        {
            bestSqSoFar = mySqDist;
            bestIndex = pivotIndex;
        }

        float planeDist = pt[axis] - pivot[axis];

        int selector = planeDist <= 0 ? 0 : 1;

        if (lr[selector] != null)
        {
            lr[selector].Search(pt, ref bestSqSoFar, ref bestIndex);
        }

        selector = (selector + 1) % 2;

        float sqPlaneDist = planeDist * planeDist;

        if ((lr[selector] != null) && (bestSqSoFar > sqPlaneDist))
        {
            lr[selector].Search(pt, ref bestSqSoFar, ref bestIndex);
        }
    }

    /// <summary>
    /// make an array of ints from lenght of points
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static int[] Iota(int num)
    {
        int[] result = new int[num];

        for (int i = 0; i < num; i++)
        {
            result[i] = i;
        }

        return result;
    }
}
