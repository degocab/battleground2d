using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class Formations : MonoBehaviour
    {
        public static Formations active;

        public Texture2D formationMask;
        [HideInInspector] public List<Formation> unitFormations = new List<Formation>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void CreateNewStrictFormation(List<UnitPars> ups)
        {
            Formation form = new Formation();
            AddUnitsToFormations(ups, form, true);

            form.GetDestinations(ups, form.CurrentMassCentre(ups));
        }

        public void AddUnitsToFormationsSC(List<UnitPars> ups, Formation form)
        {
            List<UnitPars> ups1 = new List<UnitPars>();

            for (int i = 0; i < ups.Count; i++)
            {
                UnitPars up = ups[i];

                if (up.formation != null)
                {
                    if (up.formation.strictMode != 1)
                    {
                        ups1.Add(up);
                    }
                }
                else
                {
                    ups1.Add(up);
                }
            }

            AddUnitsToFormations(ups1, form);
        }

        public void AddUnitsToFormations(List<UnitPars> ups, Formation form)
        {
            RemoveUnitsFromAnyFormation(ups);

            if (form.units == null)
            {
                form.units = ups;
                for (int i = 0; i < ups.Count; i++)
                {
                    ups[i].formation = form;
                }
            }
            else
            {
                for (int i = 0; i < ups.Count; i++)
                {
                    AddUnitToFormations(ups[i], form);
                }
            }

            if (!unitFormations.Contains(form))
            {
                unitFormations.Add(form);
            }
        }

        public void AddUnitsToFormations(List<UnitPars> ups, Formation form, bool strictMode)
        {
            AddUnitsToFormations(ups, form);

            if (strictMode)
            {
                form.strictMode = 1;
            }
        }

        public void AddUnitToFormations(UnitPars up, Formation form)
        {
            form.units.Add(up);
            up.formation = form;
        }

        public void RemoveUnitsFromAnyFormation(List<UnitPars> unitsToRemove)
        {
            for (int i = 0; i < unitsToRemove.Count; i++)
            {
                RemoveUnitFromFormation(unitsToRemove[i]);
            }
        }

        public void RemoveUnitFromFormation(UnitPars up)
        {
            if (up.formation != null)
            {
                if (up.formation.units != null)
                {
                    up.formation.units.Remove(up);

                    if (up.formation.units.Count == 0)
                    {
                        unitFormations.Remove(up.formation);
                    }

                    up.formation = null;
                }
            }
        }

        public void AddToFormations(Formation form)
        {
            unitFormations.Add(form);
        }

        public void RemoveFromFormations(Formation form)
        {
            unitFormations.Remove(form);
        }
    }

    [System.Serializable]
    public class Formation
    {
        public List<UnitPars> units;
        public float size;

        public List<Vector3> positions;

        Vector3 currentCentre;
        Vector3 destination;

        Vector3 formationDirection;

        public Texture2D formationMask;

        public int strictMode = 0;

        public void LoadFormation(List<UnitPars> units1)
        {
            if (Formations.active.formationMask != null)
            {
                formationMask = Formations.active.formationMask;
            }
            else
            {
                formationMask = CirclePatern(64);
            }

            size = GetMaxUnitSize();
        }

        public float GetMaxUnitSize()
        {
            float maxSize = 0f;

            for (int i = 0; i < units.Count; i++)
            {
                if (RTSMaster.active.useAStar)
                {
                    maxSize = units[i].rEnclosed;
                }
                else
                {
                    if (units[i].thisNMA.radius > maxSize)
                    {
                        maxSize = units[i].thisNMA.radius;
                    }
                }
            }

            return maxSize;
        }

        public void MoveMassCentreOnly()
        {
            destination = CurrentMassCentre();
            CalculateSquad(destination);
        }

        public List<Vector3> GetDestinations(List<UnitPars> units1, Vector3 dest)
        {
            destination = dest;
            LoadFormation(units1);
            currentCentre = CurrentMassCentre();
            formationDirection = (destination - currentCentre).normalized;
            CalculateSquad(dest);
            return positions;
        }

        public void CalculateSquad(Vector3 dest)
        {
            int nX = (int)(Mathf.Sqrt(1f * units.Count) + 0.99f);

            int nAcceptedPositions = 0;
            int whileControl = 0;

            if (units.Count == 1)
            {
                positions = new List<Vector3>();
                positions.Add(dest);
            }
            else
            {
                while ((nAcceptedPositions < units.Count) && (whileControl < 1000))
                {
                    whileControl = whileControl + 1;
                    nAcceptedPositions = 0;

                    List<Vector2> positions2d = new List<Vector2>();
                    positions = new List<Vector3>();

                    Vector2 dest2d = new Vector2(dest.x, dest.z);

                    float destAngle = AngleFromXAxis(formationDirection);

                    for (int i = 0; i < nX; i++)
                    {
                        for (int j = 0; j < nX; j++)
                        {
                            Vector2 pos = 2.5f * size * (new Vector2(1f * i - 0.5f * nX, 1f * j - 0.5f * nX));
                            pos = RotAround2d(destAngle, pos);
                            pos = pos + dest2d;

                            Vector3 v3d = TerrainVector(new Vector3(pos.x, 0f, pos.y));

                            if ((MinimumReachDistance(currentCentre, v3d) < 2.5f * size) || RTSMaster.active.useAStar)
                            {
                                float rr5 = GetPointBrightness(i, j, nX);
                                if (rr5 > 0.5f)
                                {
                                    nAcceptedPositions = nAcceptedPositions + 1;
                                    positions2d.Add(pos);
                                }
                            }
                        }
                    }

                    for (int i = 0; i < positions2d.Count; i++)
                    {
                        Vector3 v3d = TerrainVector(new Vector3(positions2d[i].x, 0f, positions2d[i].y));
                        positions.Add(v3d);
                    }

                    nX = nX + 1;
                }
            }
        }

        public Vector3 CurrentMassCentre()
        {
            Vector3 massCentre = Vector3.zero;

            for (int i = 0; i < units.Count; i++)
            {
                massCentre = massCentre + units[i].transform.position;
            }

            massCentre = massCentre / units.Count;
            return massCentre;
        }

        public Vector3 CurrentMassCentre(List<UnitPars> ups)
        {
            Vector3 massCentre = Vector3.zero;

            for (int i = 0; i < ups.Count; i++)
            {
                massCentre = massCentre + ups[i].transform.position;
            }

            massCentre = massCentre / units.Count;
            return massCentre;
        }


        float MinimumReachDistance(Vector3 source, Vector3 dest)
        {
            // calculates minimum distance which unit can reach by walking on NavMesh

            UnityEngine.AI.NavMeshHit hit;
            UnityEngine.AI.NavMesh.SamplePosition(dest, out hit, 2.5f * size, UnityEngine.AI.NavMesh.AllAreas);

            Vector3 lastPoint = hit.position;
            float minDistance = (dest - lastPoint).magnitude;

            return minDistance;
        }

        public Texture2D CirclePatern(int resol)
        {
            Texture2D textur = new Texture2D(resol, resol);
            float resolSqr = 0.25f * resol * resol;

            for (int x = 0; x < textur.height; x++)
            {
                for (int y = 0; y < textur.width; y++)
                {
                    if ((x - 0.5f * resol) * (x - 0.5f * resol) + (y - 0.5f * resol) * (y - 0.5f * resol) < resolSqr)
                    {
                        textur.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        textur.SetPixel(x, y, Color.black);
                    }

                }
            }

            textur.Apply();
            return textur;
        }

        public float GetPointBrightness(int ptX, int ptY, int resol)
        {
            float ratMask = 1f * formationMask.width / resol;

            int ptXmin = (int)(ratMask * (ptX - 0.5f));
            int ptXmax = (int)(ratMask * (ptX + 0.5f));

            if (ptXmin < 0)
            {
                ptXmin = 0;
            }
            if (ptXmax > (formationMask.width))
            {
                ptXmax = formationMask.width;
            }

            int ptYmin = (int)(ratMask * (ptY - 0.5f));
            int ptYmax = (int)(ratMask * (ptY + 0.5f));

            if (ptYmin < 0)
            {
                ptYmin = 0;
            }
            if (ptYmax > (formationMask.width))
            {
                ptYmax = formationMask.width;
            }

            float avBrightness = 0f;
            int countBrightness = 0;

            for (int x = ptXmin; x < ptXmax; x++)
            {
                for (int y = ptYmin; y < ptYmax; y++)
                {
                    Color col = formationMask.GetPixel(x, y);
                    float brightness = (col.r + col.g + col.b) / 3f;
                    avBrightness = avBrightness + brightness;
                    countBrightness = countBrightness + 1;
                }
            }

            avBrightness = avBrightness / countBrightness;

            return avBrightness;
        }

        float AngleFromXAxis(Vector3 origin)
        {
            return (-AngleBetween2d360(new Vector2(1f, 0f), new Vector2(origin.x, origin.z)));
        }

        Vector2 RotAround2d(float angle, Vector2 v)
        {
            return Quaternion.Euler(0f, 0f, angle) * v;
        }

        public Vector3 TerrainVector(Vector3 origin)
        {
            return TerrainProperties.TerrainVectorProc(origin);
        }

        float AngleBetween2d360(Vector2 a, Vector2 b)
        {
            float angle = Vector2.Angle(a, b);
            float sign = Mathf.Sign(a.y * b.x - a.x * b.y);

            // angle in [-179,180]
            float signed_angle = angle * sign;

            // angle in [0,360] (not used but included here for completeness)
            float angle360 = (signed_angle + 180) % 360;

            return angle360;
        }
    }
}
