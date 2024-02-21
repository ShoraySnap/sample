using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

            var areaParam = element.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
            if(areaParam != null && areaParam.HasValue)
            {
                serializedFloor.area = UnitConversion.ConvertToSnaptrudeAreaUnits(areaParam.AsDouble());
            }
            else
            {
                serializedFloor.area = 0;
            }

            return serializedFloor;
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
                                    var curveDataPoint = CurveUtils.GetPointsListFromCurveLoop(curveLoop, out isOuterCurveDifferent);
                                    var curveKey = CurveUtils.GetCurveKeyFromPointsList(curveDataPoint, element.Id.ToString());
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
                                            var voidDataPoint = CurveUtils.GetPointsListFromCurveLoop(voidCurve, out isInnerCurveDifferent, true);
                                            voidKeys.Add(CurveUtils.GetCurveKeyFromPointsList(voidDataPoint, element.Id.ToString()));
                                            voidData.Add(voidDataPoint);
                                        }

                                        Dictionary<string, double[][]> voidsDict = CurveUtils.GetPointsDataDict(voidKeys, voidData, out isInnerCurveDifferent);

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
            var outlineDict = CurveUtils.GetPointsDataDict(curveKeys, curveData, out tooManyPoints);
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
