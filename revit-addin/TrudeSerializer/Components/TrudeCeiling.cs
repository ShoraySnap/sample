using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using System.Collections.Generic;
using TrudeImporter;
using System;
using System.Text;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeCeiling : TrudeComponent
    {
        public Dictionary<string, double[][]> outline;
        public double area;
        public Dictionary<string, Dictionary<string, double[][]>> voids;
        public string type;

        public double heightOffsetFromLevel = 0;

        private TrudeCeiling(string elementId,
            string level, string family, string type,
            bool isInstance, bool isParametric,
            Dictionary<string, double[][]> outline,
            Dictionary<string, Dictionary<string, double[][]>> voids,
            double heightOffset
            ) : base(elementId, "Ceilings", family, level)
        {
            this.elementId = elementId;
            this.level = level;
            this.family = family;
            this.type = type;
            this.isInstance = isInstance;
            this.isParametric = isParametric;
            this.voids = voids;
            this.outline = outline;
            this.heightOffsetFromLevel = heightOffset;
        }

        static public TrudeCeiling GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            Ceiling ceiling = element as Ceiling;

            string elementId = element.Id.ToString();
            string levelName = TrudeLevel.GetLevelName(element);
            var elemType = GlobalVariables.Document.GetElement(element.GetTypeId()) as CeilingType;
            string family = elemType.FamilyName;
            string floorType = element.Name;

            Parameter heightOffsetParam = ceiling.LookupParameter("Height Offset From Level");
            double heightOffset = 0;
            if(heightOffsetParam.HasValue)
            {
                heightOffset = heightOffsetParam.AsDouble();
                heightOffset = UnitConversion.ConvertToSnaptrudeUnits(heightOffset, heightOffsetParam.GetUnitTypeId());
            }
            var (outline, voids, isDifferentCurve) = GetOutline(element);
            SetCeilingType(importData, ceiling);
            TrudeCeiling serializedCeiling = new TrudeCeiling(elementId, levelName, family, floorType, false, true, outline, voids, heightOffset);
            serializedCeiling.SetIsParametric(isDifferentCurve);


            return serializedCeiling;
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


        static public void SetCeilingType(SerializedTrudeData importData, Ceiling ceiling)
        {
            var elemType = GlobalVariables.Document.GetElement(ceiling.GetTypeId()) as CeilingType;
            string name = elemType.Name;
            if (importData.FamilyTypes.HasCeilingType(name)) return;

            TrudeCeilingType snaptrudeFloorType = TrudeCeilingType.GetLayersData(ceiling);
            importData.FamilyTypes.AddCeilingType(name, snaptrudeFloorType);
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
