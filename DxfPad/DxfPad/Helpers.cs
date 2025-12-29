using System;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Globalization;
using Vector2d = OpenTK.Mathematics.Vector2d;
using Vector3d = OpenTK.Mathematics.Vector3d;

namespace DxfPad
{
    public static class Helpers
    {
        public static double ParseDouble(string v)
        {
            return double.Parse(v.Replace(",", "."), CultureInfo.InvariantCulture);
        }
        public static decimal ParseDecimal(string v)
        {
            return decimal.Parse(v.Replace(",", "."), CultureInfo.InvariantCulture);
        }


        public static PointF ToPointF(this Vector2d v) => new PointF((float)v.X, (float)v.Y);
        public static PointF Offset(this PointF v, float x, float y) => new PointF((float)v.X + x, (float)v.Y + y);
        public static Vector2d ToVector2d(this PointF v) => new Vector2d(v.X, v.Y);

        public static Vector3d ParseVector(string value)
        {
            Vector3d ret = new ();
            var spl = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseDouble).ToArray();
            for (int i = 0; i < 3; i++)
            {
                ret[i] = spl[i];
            }
            return ret;
        }

        public static Vector2d ParseVector2(string value)
        {
            Vector2d ret = new Vector2d();
            var spl = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseDouble).ToArray();
            for (int i = 0; i < 2; i++)
            {
                ret[i] = spl[i];
            }
            return ret;
        }
    }
}
