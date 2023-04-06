using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace TrudeImporter
{
    public static class Extensions
    {

        public const int XY = 0;
        public const int YZ = 1;


        public static bool IsNull(this object obj)
        {
            return obj is null;
        }

        public static CompoundStructureLayer GetCoreLayer(this CompoundStructure compoundStructure)
        {
            CompoundStructureLayer coreLayer = null;
            foreach (CompoundStructureLayer layer in compoundStructure.GetLayers())
            {
                if (layer.Function == MaterialFunctionAssignment.Structure)
                {
                    coreLayer = layer;
                }
            }

            return coreLayer;
        }


        public static XYZ ToSnaptrudeUnits(this XYZ p)
        {
            return new XYZ(
                Math.Round((double)(p.X * 12 / 10), 6),
                Math.Round((double)(p.Z * 12 / 10), 6),
                Math.Round((double)(p.Y * 12 / 10), 6));
        }

        public static XYZ ProjectOnto( this Plane plane, XYZ p)
        {
            double d = plane.SignedDistanceTo(p);

            XYZ q = p - d * plane.Normal;

            return q;
        }

        public static XYZ SetX(this XYZ p, double value)
        {
            return new XYZ(value, p.Y, p.Z);
        }

        public static XYZ SetY(this XYZ p, double value)
        {
            return new XYZ(p.X, value, p.Z);
        }

        public static XYZ SetZ(this XYZ p, double value)
        {
            return new XYZ(p.X, p.Y, value);
        }

        public static bool InFirstQuadrant(this XYZ p, int planeId = XY)
        {
            if(planeId == XY) return p.X >= 0 && p.Y >= 0;
            if(planeId == YZ) return p.Y >= 0 && p.Z >= 0;

            return false;
        }
        public static bool InSecondQuadrant(this XYZ p, int planeId = XY)
        {
            if (planeId == XY) return p.X <= 0 && p.Y >= 0;
            if (planeId == YZ) return p.Y <= 0 && p.Z >= 0;

            return false;
        }
        public static bool InThirdQuadrant(this XYZ p, int planeId = XY)
        {
            if (planeId == XY) return p.X <= 0 && p.Y <= 0;
            if (planeId == YZ) return p.Y <= 0 && p.Z <= 0;

            return false;
        }
        public static bool InFourthQuadrant(this XYZ p, int planeId = XY)
        {
            if (planeId == XY) return p.X >= 0 && p.Y <= 0;
            if (planeId == YZ) return p.Y >= 0 && p.Z <= 0;

            return false;
        }
        public static double SignedDistanceTo(this Plane plane, XYZ p)
        {
            XYZ v = p - plane.Origin;

            return plane.Normal.DotProduct(v);
        }

        public static XYZ MultiplyEach(this XYZ p, XYZ q)
        {
            return new XYZ(p.X * q.X, p.Y * q.Y, p.Z * q.Z);
        }

        public static bool IsNullOrEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                   (token.Type == JTokenType.Null);
        }

        public static XYZ ComputeNormal(this Curve line)
        {
            XYZ startPoint = line.GetEndPoint(0);
            XYZ endPoint = line.GetEndPoint(1);

            XYZ direction = (endPoint - startPoint).Normalize();
            return direction.Round();
        }

        public static XYZ ToXYZ(this Point3D point)
        {
            return new XYZ(point.X, point.Y, point.Z);
        }

        public static Point3D ToPoint3D(this XYZ xyz)
        {
            return new Point3D(xyz.X, xyz.Y, xyz.Z);
        }

        public static int Mod(this int x, int m)
        {
            return (x % m + m) % m;
        }
        public static string GenerateCurveId(this CurveArray curves, String revitId)
        {
            string ret = "[";
            List<String> key = new List<string>();

            foreach (Curve curve in curves)
            {
                key.Add("[" + ((int)curve.GetEndPoint(0).X) + ", " + ((int)curve.GetEndPoint(0).Y) + "]");
            }
            key.Sort();

            foreach(string k in key)
            {
                ret += k + ", ";
            }

            ret = ret.Remove(ret.Length - 2, 2);
            ret += "]" + revitId;
            return ret;
        }

        public static string ToJson(this CurveArray curves)
        {
            string ret = "[";
            foreach (Curve curve in curves)
            {
                ret += curve.Stringify() + " ,";
            }
            ret += "]";
            return ret;
        }

        public static string Stringify(this IList<Curve> curves)
        {
            return curves.Aggregate("", (acc, c) => acc + " " + c.Stringify());
        }
        public static string Stringify(this Curve c)
        {
            return $"[{c.GetEndPoint(0).Stringify()}, {c.GetEndPoint(1).Stringify()}]";
        }

        public static string Stringify(this XYZ p)
        {
            return $"[{p.X},{p.Y},{p.Z}]";
        }

        public static string RemoveIns(this string s)
        {
            return Regex.Replace(s, @"Ins[0-9]+", "").Replace('_', ' ').Trim();
        }

        public static bool IsZero(this XYZ xyz)
        {
            return xyz.X == 0 && xyz.Y == 0 && xyz.Z == 0;
        }

        public static XYZ Round(this XYZ xyz, int precision = 3)
        {
            return new XYZ(Math.Round(xyz.X, precision), Math.Round(xyz.Y, precision), Math.Round(xyz.Z, precision));
        }

        public static bool RoundedEquals(this double x, double y)
        {
            return Math.Round(x, 2) == Math.Round(y, 2);
        }

        public static bool AlmostEquals(this double x, double y, double tolerance  = 0.005)
        {
            return Math.Abs(x - y) <= tolerance;
        }

        public static bool RoundedEquals(this XYZ a, XYZ b, int precision = 3)
        {
            return (Math.Round(a.X, precision) == Math.Round(b.X, precision))
                && (Math.Round(a.Y, precision) == Math.Round(b.Y, precision))
                && (Math.Round(a.Z, precision) == Math.Round(b.Z, precision));
        }

        public static bool Contains(this Line line, XYZ point)
        {
            return line.Distance(point).AlmostEquals(0); 
        }
        public static bool ContainsLine(this Line outerLine, Line innerLine)
        {
            return outerLine.Contains(innerLine.GetEndPoint(0)) && outerLine.Contains(innerLine.GetEndPoint(1));
        }
    }
}
