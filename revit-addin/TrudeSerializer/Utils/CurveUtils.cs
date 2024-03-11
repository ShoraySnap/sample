using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeSerializer.Utils
{
    internal static class CurveUtils
    {
        public static List<XYZ> GetPointsListFromCurveLoop(CurveLoop curveLoop, out bool isDifferentCurve, bool voidLoop = false)
        {
            isDifferentCurve = false;
            CurveLoopIterator itr = curveLoop.GetCurveLoopIterator();
            List<XYZ> curvePoints = new List<XYZ>();
            if (!itr.IsValidObject) return curvePoints;
            while (itr.MoveNext())
            {
                var curve = itr.Current;
                if (curve is Arc || curve is NurbSpline)
                {
                    IList<XYZ> points = curve.Tessellate();
                    foreach (var p in points)
                    {
                        curvePoints.Add(p);
                    }
                }
                else if (curve is Line)
                {
                    XYZ endPoint = curve.GetEndPoint(0);
                    curvePoints.Add(endPoint);
                }
                else
                {
                    isDifferentCurve = true;
                }
            }

            return curvePoints;
        }

        public static string GetCurveKeyFromPointsList(List<XYZ> curvePoints, string elementID)
        {
            if (curvePoints.Count <= 0) return "";
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.Append("[");
            foreach (XYZ curvePoint in curvePoints)
            {
                keyBuilder.Append("[" + Math.Round(curvePoint.X).ToString() + ", " + Math.Round(curvePoint.Y) + "]");
                if (curvePoints.IndexOf(curvePoint) != curvePoints.Count - 1) keyBuilder.Append(", ");
            }

            keyBuilder.Append("]");
            keyBuilder.Append(elementID);
            return keyBuilder.ToString();
        }
        public static double[][] TransformPointsListToDoubleArray(List<XYZ> pointList)
        {
            double[][] dataArrays = new double[pointList.Count][];
            foreach (var point in pointList)
            {
                dataArrays[pointList.IndexOf(point)] = new double[] { point.X, point.Y, point.Z };
            }
            return dataArrays;
        }
        public static List<XYZ> GetPointsInProperUnits(List<XYZ> dataList)
        {
            var convertedPoints = new List<XYZ>();
            foreach (XYZ point in dataList)
            {
#if REVIT2019 || REVIT2020
                float multiplier = (float)UnitConversion.ConvertToMillimeter(1.0, DisplayUnitType.DUT_DECIMAL_FEET);
#else
                float multiplier = (float)UnitConversion.ConvertToMillimeter(1.0, UnitTypeId.Feet);
#endif
                convertedPoints.Add(point.Multiply(multiplier));
            }

            return convertedPoints;
        }

        public static Dictionary<string, double[][]> GetPointsDataDict(List<string> keys, List<List<XYZ>> values, out bool isDifferentCurve)
        {
            isDifferentCurve = false;
            if (keys.Count != values.Count)
            {
                throw new Exception("Points data invalid!");
            }
            var dataDict = new Dictionary<string, double[][]>();
            List<double[][]> data = new List<double[][]>();
            foreach (var plist in values)
            {
                data.Add(TransformPointsListToDoubleArray(GetPointsInProperUnits(plist)));
            }
            for (int i = 0; i < keys.Count; i++)
            {
                dataDict.Add(keys[i], data[i]);
            }

            if (values.Count > 600)
            {
                isDifferentCurve = true;
            }
            return dataDict;
        }

    }
}
