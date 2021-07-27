using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public static class GenericMath
    {
        public static float Interpolate(float x, float x0, float x1, float y0, float y1)
        {
            return (y0 + (y1 - y0) * (x - x0) / (x1 - x0));
        }

        public static float InterpolateClamped(float x, float x0, float x1, float y0, float y1)
        {
            float y = (y0 + (y1 - y0) * (x - x0) / (x1 - x0));

            if (y0 < y1)
            {
                if (y < y0)
                {
                    y = y0;
                }
                if (y > y1)
                {
                    y = y1;
                }
            }
            else if (y0 > y1)
            {
                if (y > y0)
                {
                    y = y0;
                }
                if (y < y1)
                {
                    y = y1;
                }
            }

            return y;
        }

        public static Vector3 RotAround(float rotAngle, Vector3 original, Vector3 direction)
        {
            Vector3 cross1 = Vector3.Cross(original, direction);

            Vector3 pr = Vector3.Project(original, direction);
            Vector3 pr2 = original - pr;
            Vector3 cross2 = Vector3.Cross(pr2, cross1);

            Vector3 rotatedVector = (Quaternion.AngleAxis(rotAngle, cross2) * pr2) + pr;
            return rotatedVector;
        }

        public static float Angle3d(Vector3 a, Vector3 b)
        {
            double ax = (double)a.x;
            double ay = (double)a.y;
            double az = (double)a.z;

            double bx = (double)b.x;
            double by = (double)b.y;
            double bz = (double)b.z;

            double dotd = ax * bx + ay * by + az * bz;

            double aMag = System.Math.Sqrt(ax * ax + ay * ay + az * az);
            double bMag = System.Math.Sqrt(bx * bx + by * by + bz * bz);

            double aCos = System.Math.Acos(dotd / (aMag * bMag));

            return (float)(aCos * 180 / 3.14159265359);
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            double ax = (double)a.x;
            double ay = (double)a.y;
            double az = (double)a.z;

            double bx = (double)b.x;
            double by = (double)b.y;
            double bz = (double)b.z;

            double dotd = ax * bx + ay * by + az * bz;

            return (float)(dotd);
        }

        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 n)
        {
            //  Acute angle [0,180]
            float angle = Vector3.Angle(v1, v2);

            //  -Acute angle [180,-179]
            float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(v1, v2)));
            float signed_angle = angle * sign;

            return signed_angle;
        }

        public static float Angle360(Vector3 v1, Vector3 v2, Vector3 n)
        {
            //  Acute angle [0,180]
            float angle = Vector3.Angle(v1, v2);

            //  -Acute angle [180,-179]
            float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(v1, v2)));
            float signed_angle = angle * sign;

            //  360 angle
            return (signed_angle + 180) % 360;
        }

        public static float Angle360quat(Vector3 from, Vector3 to)
        {
            return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;
        }

        public static Color ColorFromPalette(float value, List<Color> palette)
        {
            int n = palette.Count;

            int ilow = (int)(value * (n - 1));
            int ihigh = (int)(value * (n - 1) + 1f);

            Color clow = palette[ilow];
            Color chigh = palette[ihigh];

            float value1 = Mathf.Repeat(value * (n - 1), 1f);

            float r = Interpolate(value1, 0f, 1f, clow.r, chigh.r);
            float g = Interpolate(value1, 0f, 1f, clow.g, chigh.g);
            float b = Interpolate(value1, 0f, 1f, clow.b, chigh.b);

            return (new Color(r, g, b, 1f));
        }

        public static string FirstLetterToUpper(string str)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length > 1)
            {
                return char.ToUpper(str[0]) + str.Substring(1);
            }

            return str.ToUpper();
        }

        public static Vector2 ProjectionXZ(Vector3 origin)
        {
            return (new Vector2(origin.x, origin.z));
        }

        public static Vector3 GetLOSPerpendicular(Transform tr, float angle)
        {
            Vector3 fwd = tr.TransformDirection(Vector3.forward);
            Vector3 dir = (GenericMath.RotAround(angle, new Vector3(fwd.x, 0f, fwd.z), new Vector3(0f, 1f, 0f))).normalized;
            return dir;
        }

        public static float PointToLineSegmentDistance(float vx, float vy, float v1x, float v1y, float v2x, float v2y)
        {
            float A = vx - v1x;
            float B = vy - v1y;
            float C = v2x - v1x;
            float D = v2y - v1y;

            float dot = A * C + B * D;
            float len_sq = C * C + D * D;
            float param = -1f;

            if (len_sq != 0)
            {
                param = dot / len_sq;
            }

            float xx = 0f;
            float yy = 0f;

            if (param < 0)
            {
                xx = v1x;
                yy = v1y;
            }
            else if (param > 1)
            {
                xx = v2x;
                yy = v2y;
            }
            else
            {
                xx = v1x + param * C;
                yy = v1y + param * D;
            }

            float dx = vx - xx;
            float dy = vy - yy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static int FloatToIntRandScaled(float f)
        {
            int i = (int)f;

            if (Random.value < f - i)
            {
                i = i + 1;
            }

            return i;
        }

        public static Color NormalizeColor(Color inputColor)
        {
            return inputColor / (inputColor.r + inputColor.g + inputColor.b);
        }

        public static float RandomFloatMinMax(System.Random rnd, float minimum, float maximum)
        {
            return (float)(rnd.NextDouble() * (maximum - minimum) + minimum);
        }

        public static Quaternion FlipQuaternion(Quaternion q)
        {
            return Quaternion.Euler(-q.eulerAngles);
        }

        public static float RandomPow(float from, float to, float exponent)
        {
            float randomLinear = Random.Range(Mathf.Pow(from, 1f / exponent), Mathf.Pow(to, 1f / exponent));
            return Mathf.Pow(randomLinear, exponent);
        }
    }

    public static class Vector2Extension
    {
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
        }
    }
}
