using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class DLANode
    {
        public DLANode prevNode;
        public DLANode nextNode;
        public Vector3 position;

        public List<DLANode> childs = new List<DLANode>();

        public List<Vector3> subchilds_pos = new List<Vector3>();

        public int treeIndex = -1;
        public int node_global = -1;
        public int branch_global = -1;

        public DLANode(DLANode prevNode1, Vector3 position1)
        {
            prevNode = prevNode1;
            position = position1;

            if (prevNode != null)
            {
                prevNode.childs.Add(this);
                treeIndex = prevNode.treeIndex;
            }
        }

        public void ScalePositions(float factor)
        {
            position = position * factor;
            for (int i = 0; i < childs.Count; i++)
            {
                childs[i].ScalePositions(factor);
            }
        }

        public void InsertSubChilds(int nSub, DLASystem dlaSystem)
        {
            for (int i = 0; i < childs.Count; i++)
            {
                InsertHalfWay(childs[i], position, childs[i].position, nSub, dlaSystem);
                childs[i].InsertSubChilds(nSub, dlaSystem);
            }
        }

        void InsertHalfWay(DLANode child, Vector3 beg, Vector3 end, int remainder, DLASystem dlaSystem)
        {
            Vector3 newPos = 0.5f * (beg + end);
            Vector3 diff = 0.5f * (end - beg);
            Vector3 randPos = 0.7f * Random.Range(-1f, 1f) * GenericMath.RotAround(-90f, diff, new Vector3(0, 0, 1));

            newPos = newPos + randPos;
            subchilds_pos.Add(newPos);
            int remainder2 = remainder / 2;

            if (remainder2 > 1)
            {
                InsertHalfWay(child, beg, newPos, remainder2, dlaSystem);
                InsertHalfWay(child, newPos, end, remainder2, dlaSystem);
            }
            else
            {
                float rot1 = dlaSystem.worldRotation;

                dlaSystem.allPositionsBeg.Add(GenericMath.RotAround(-rot1, beg, new Vector3(0, 0, 1)));
                dlaSystem.allPositionsEnd.Add(GenericMath.RotAround(-rot1, newPos, new Vector3(0, 0, 1)));
                dlaSystem.allPositionsBeg.Add(GenericMath.RotAround(-rot1, newPos, new Vector3(0, 0, 1)));
                dlaSystem.allPositionsEnd.Add(GenericMath.RotAround(-rot1, end, new Vector3(0, 0, 1)));

                dlaSystem.branchIds.Add(child.branch_global);
                dlaSystem.branchIds.Add(child.branch_global);

                dlaSystem.posBegRandoms.Add(Random.value);
                dlaSystem.posBegRandoms.Add(Random.value);
            }
        }
    }
}
