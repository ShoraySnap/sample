using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System;
using System.Collections.Generic;
using System.Text;
using TrudeImporter;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeFloor : TrudeComponent
    {
        public Dictionary<string, double[][]> outline;
        public double area;
        public Dictionary<string, Dictionary<string, double[][]>> voids;
        public string type;

        private TrudeFloor(string elementId,
            string level, string family, string type,
            bool isInstance, bool isParametric,
            Dictionary<string, double[][]> outline,
            Dictionary<string, Dictionary<string, double[][]>> voids
            ) : base(elementId, "Floors", family, level)
        {
            this.elementId = elementId;
            this.level = level;
            this.family = family;
            this.type = type;
            this.isInstance = isInstance;
            this.isParametric = isParametric;
            this.voids = voids;

            this.outline = outline;
        }

        static public TrudeFloor GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            Floor floor = element as Floor;

            string elementId = element.Id.ToString();
            string levelName = TrudeLevel.GetLevelName(element);
            string family = floor.FloorType.FamilyName;
            string floorType = element.Name;

            var (outline, voids, isDifferentCurve) = GetOutline(element);
            SetFloorType(importData, floor);
            TrudeFloor serializedFloor = new TrudeFloor(elementId, levelName, family, floorType, false, true, outline, voids);
            serializedFloor.SetIsParametric(isDifferentCurve);

            return serializedFloor;
        }

        static private List<XYZ> GetPointsListFromCurveLoop(CurveLoop curveLoop, out bool isDifferentCurve, bool voidLoop = false)
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

        static List<XYZ> GetPointsInProperUnits(List<XYZ> dataList)
        {
            var convertedPoints = new List<XYZ>();
            foreach (XYZ point in dataList)
            {
                float multiplier = (float)UnitConversion.ConvertToMillimeterForRevit2021AndAbove(1.0, UnitTypeId.Feet);
                convertedPoints.Add(point.Multiply(multiplier));
            }

            return convertedPoints;
        }

        static private string GetCurveKeyFromPointsList(List<XYZ> curvePoints, string elementID)
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

        static private double[][] TransformPointsListToDoubleArray(List<XYZ> pointList)
        {
            double[][] dataArrays = new double[pointList.Count][];
            foreach (var point in pointList)
            {
                dataArrays[pointList.IndexOf(point)] = new double[] { point.X, point.Y, point.Z };
            }
            return dataArrays;
        }

        static private Dictionary<string, double[][]> GetPointsDataDict(List<string> keys, List<List<XYZ>> values, out bool isDifferentCurve)
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

        static (Dictionary<string, double[][]>, Dictionary<string, Dictionary<string, double[][]>>, bool) GetOutline(Element element)
        {
            var bottomProfileRef = HostObjectUtils.GetBottomFaces(element as HostObject);

            GeometryObject bottomProfile = element.GetGeometryObjectFromReference(bottomProfileRef[0]);

            var faceNormal = new XYZ();
            using (PlanarFace bottomProfilePlanarFace = bottomProfile as PlanarFace)
            {
                if (bottomProfilePlanarFace != null)
                    faceNormal = bottomProfilePlanarFace.FaceNormal;
            }

            var options = new Options();
            options.View = TrudeSerializer.GlobalVariables.Document.ActiveView;
            GeometryElement geometry = element.get_Geometry(options);

            List<string> curveKeys = new List<string>();
            List<List<XYZ>> curveData = new List<List<XYZ>>();
            Dictionary<string, Dictionary<string, double[][]>> outlinesToVoidsMap = new Dictionary<string, Dictionary<string, double[][]>>();


            bool isDifferentCurve = false;
            foreach (GeometryObject geo in geometry)
            {
                XYZ currentFaceNormal = new XYZ();
                if (geo is Solid)
                {
                    Solid solidGeometry = geo as Solid;
                    foreach (var face in solidGeometry.Faces)
                    {
                        using (var planarFace = face as PlanarFace)
                        {
                            if (planarFace == null) continue;
                            if (planarFace.FaceNormal.IsNull())
                                currentFaceNormal = planarFace.FaceNormal;
                            else
                            {
                                BoundingBoxUV bbox = planarFace.GetBoundingBox();
                                UV faceCenter = (bbox.Max - bbox.Min) / 2;
                                currentFaceNormal = planarFace.ComputeNormal(faceCenter).Normalize();
                            }
                            bool areSameNormal = currentFaceNormal.IsAlmostEqualTo(faceNormal);

                            if (areSameNormal)
                            {
                                IList<CurveLoop> curveLoops = planarFace.GetEdgesAsCurveLoops();
                                IList<IList<CurveLoop>> sortedLoops = ExporterIFCUtils.SortCurveLoops(curveLoops);

                                foreach (var sortedLists in sortedLoops)
                                {
                                    var curveLoop = sortedLists[0]; // Take the outer curve
                                    bool isOuterCurveDifferent = false;
                                    var curveDataPoint = GetPointsListFromCurveLoop(curveLoop, out isOuterCurveDifferent);
                                    var curveKey = GetCurveKeyFromPointsList(curveDataPoint, element.Id.ToString());
                                    curveKeys.Add(curveKey);
                                    curveData.Add(curveDataPoint);
                                    var voidKeys = new List<string>();
                                    var voidData = new List<List<XYZ>>();
                                    if (sortedLists.Count > 1) // Inner Curves mean voids
                                    {
                                        bool isInnerCurveDifferent = false;
                                        // STARTING FROM 1 means ignore the outer curve
                                        for (var i = 1; i < sortedLists.Count; i++)
                                        {
                                            var voidCurve = sortedLists[i];
                                            var voidDataPoint = GetPointsListFromCurveLoop(voidCurve, out isInnerCurveDifferent, true);
                                            voidKeys.Add(GetCurveKeyFromPointsList(voidDataPoint, element.Id.ToString()));
                                            voidData.Add(voidDataPoint);
                                        }

                                        Dictionary<string, double[][]> voidsDict = GetPointsDataDict(voidKeys, voidData, out isInnerCurveDifferent);

                                        outlinesToVoidsMap.Add(curveKey, voidsDict);

                                        isDifferentCurve |= isInnerCurveDifferent;
                                    }

                                    isDifferentCurve |= isOuterCurveDifferent;
                                }
                            }
                        }
                    }
                }
            }

            bool tooManyPoints = false;
            var outlineDict = GetPointsDataDict(curveKeys, curveData, out tooManyPoints);
            isDifferentCurve = isDifferentCurve || tooManyPoints;

            return (outlineDict, outlinesToVoidsMap, isDifferentCurve);
        }


        static public void SetFloorType(SerializedTrudeData importData, Floor floor)
        {
            string name = floor.FloorType.Name;
            if (importData.FamilyTypes.HasFloorType(name)) return;

            TrudeFloorType snaptrudeFloorType = TrudeFloorType.GetLayersData(floor);
            importData.FamilyTypes.AddFloorType(name, snaptrudeFloorType);
        }

        private void SetIsParametric(bool isDifferentCurve)
        {
            if (isDifferentCurve)
                this.isParametric = false;
            else
                this.isParametric = true;
        }

    }
}
