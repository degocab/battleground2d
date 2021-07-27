using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class RandomScattering
    {
        public List<Vector2> points = new List<Vector2>();

        public void CreateScatter(float x, float y, float w, float h, int rows, int cols)
        {
            float xd = w / cols;
            float yd = h / rows;
            float p = 0.4f;

            for (int ix = 0; ix < (cols + 1); ix++)
            {
                for (int iy = 0; iy < (rows + 1); iy++)
                {
                    Vector2 v2 = new Vector2(
                        x + xd * ix + Random.Range(-p, p) * xd,
                        y + yd * iy + Random.Range(-p, p) * yd
                    );

                    points.Add(v2);
                }
            }
        }

        public void CleanScatter(float x, float y, float w, float h)
        {
            List<int> mask = new List<int>();

            for (int i = 0; i < points.Count; i++)
            {
                mask.Add(0);
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 pt = points[i];

                if (pt.x < x - 0.5f * w)
                {
                    mask[i] = 1;
                }
                else if (pt.x > x + 0.5f * w)
                {
                    mask[i] = 1;
                }
                else if (pt.y < y - 0.5f * h)
                {
                    mask[i] = 1;
                }
                else if (pt.y > y + 0.5f * h)
                {
                    mask[i] = 1;
                }
            }

            List<Vector2> cpoints = new List<Vector2>();

            for (int i = 0; i < points.Count; i++)
            {
                if (mask[i] == 0)
                {
                    cpoints.Add(points[i]);
                }
            }

            points = cpoints;
        }
    }
}
