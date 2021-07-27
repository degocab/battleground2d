using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class BulletShooter : MonoBehaviour
    {
        public float viewingAngle = 1f;
        public float randomizeFromTarget = 0.01f;

        UnitPars thisUP;

        public Vector3 shootingTrailStartPoint;
        public float trailLength = 2f;
        public int numberOfTrailParticles = 10;

        void Start()
        {
            thisUP = gameObject.GetComponent<UnitPars>();
        }

        public void Launch()
        {
            Vector3 dir = thisUP.targetUP.transform.position - thisUP.transform.position;
            dir = dir + dir.magnitude * randomizeFromTarget * Random.onUnitSphere;
            List<UnitPars> allUnits = RTSMaster.active.allUnits;

            BulletTrailsRenderer.active.EmitBetween(
                transform.position + shootingTrailStartPoint,
                transform.position + shootingTrailStartPoint + dir.normalized * trailLength,
                numberOfTrailParticles
            );

            List<UnitPars> acceptableUnits = new List<UnitPars>();

            for (int i = 0; i < allUnits.Count; i++)
            {
                Vector3 unitDir = allUnits[i].transform.position - transform.position;
                float angle = GenericMath.Angle360quat(unitDir, dir);

                if (angle < viewingAngle)
                {
                    if (allUnits[i] != thisUP)
                    {
                        acceptableUnits.Add(allUnits[i]);
                    }
                }
            }

            if (acceptableUnits.Count > 0)
            {
                int i1 = Random.Range(0, acceptableUnits.Count);
                acceptableUnits[i1].UpdateHealth(acceptableUnits[i1].health - Random.Range(0f, 20f));
            }
        }
    }
}
