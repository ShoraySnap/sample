using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System;
using System.Collections.Generic;
using System.Text;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeColumn : TrudeComponent
    {
        private static readonly string[] categoryList = new string[]
        {
            "Structural Columns",
            "Columns"
        };
        public double[][] bottomFace;
        public double[] normal;
        public double[] center;
        public double height;
        public string type;


        private TrudeColumn(string elementId,
            string level, string family, string type,
            bool isInstance, bool isParametric,
            double[][] bottomFace,
            double[] normal,
            double[] center,
            double height 
            ) : base(elementId, "Columns", family, level)
        {
            this.type = type;
            this.isInstance = isInstance;
            this.isParametric = isParametric;

            this.bottomFace = bottomFace;
            this.normal = normal;
            this.center = center;
            this.height = height;
        }


        public static bool IsColumnCategory(Element element)
        {
            var category = element.Category.Name;
            return Array.Exists(categoryList, element.Category.Name.Contains);
        }

        private static (List<XYZ>, int, XYZ) HandleSolid(Solid solid)
        {
            List<XYZ> points = new List<XYZ>();
            int numberOfFaces = 0;
            XYZ normal = new XYZ();
            foreach(var face in solid.Faces)
            {
                using(var planarFace = face as PlanarFace)
                {
                    if (planarFace == null) continue;
                    if(planarFace.ComputeNormal(new UV(0.5, 0.5)).Z < 0)
                    {
                        normal = planarFace.ComputeNormal(new UV(0.5, 0.5));
                        numberOfFaces += 1;

                        IList<CurveLoop> edges = planarFace.GetEdgesAsCurveLoops();
                        IList<IList<CurveLoop>> sortedLoops = ExporterIFCUtils.SortCurveLoops(edges);

                        if (sortedLoops.Count > 1) numberOfFaces += 1;
                        foreach(var e in edges)
                        {
                            var pointList = CurveUtils.GetPointsListFromCurveLoop(e, out bool isDifferentCurve);
                            if(!isDifferentCurve)
                            {
                                points.AddRange(pointList);
                            }
                        }
                        break;
                    }
                }
            }

            return (points, numberOfFaces, normal);
        }

        private static (double[][], double[], bool) GetBottomFaceAndNormal(Element element)
        {
            bool tooManyFaces = false;
            var options = new Options();
            options.View = GlobalVariables.Document.ActiveView;
            GeometryElement geometry = element.get_Geometry(options);

            List<XYZ> bottomFace = new List<XYZ>();
            XYZ normal = new XYZ();

            int totalFacesBottom = 0;

            foreach(var geo in geometry)
            {
                if (geo is Solid solid)
                {
                    var (bottomFacePoints, numberOfFaces, faceNormal) = HandleSolid(solid);
                    totalFacesBottom += numberOfFaces;
                    if(bottomFacePoints.Count > 0)
                    {
                        bottomFace.AddRange(bottomFacePoints);
                        normal = faceNormal;
                    }
                }
                else if(geo is GeometryInstance instance)
                {
                    foreach(var g in instance.GetInstanceGeometry())
                    {
                        if(g is Solid instanceSolid)
                        {
                            var (bottomFacePoints, numberOfFaces, faceNormal) = HandleSolid(instanceSolid);
                            totalFacesBottom += numberOfFaces;
                            if(bottomFacePoints.Count > 0)
                            {
                                bottomFace.AddRange(bottomFacePoints);
                                normal = faceNormal;
                            }
                        }
                    }
                }
            }

            tooManyFaces = totalFacesBottom > 1;

            var unitProperBottomFace = UnitConversion.ConvertToMilimeterUnits(bottomFace);
            return (CurveUtils.TransformPointsListToDoubleArray(unitProperBottomFace), new double[] {normal.X, normal.Y, normal.Z }, tooManyFaces);
        }

        private static double GetHeight(Element element)
        {
            double height = 0;
            if(element.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM) != null)
            {
                height = element.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble() * 304.8;
            }
            else if(element.LookupParameter("Height") != null)
            {
                height = element.LookupParameter("Height").AsDouble() * 304.8;
            }

            if(height == 0)
            {
                height = GetHeightFromBoundingBox(element);
            }

            return height;
        }

        private static double[] GetCenter(Element element)
        {
            var center = InstanceUtility.GetCenterFromBoundingBox(element);


            return UnitConversion.ConvertToMilimeterUnits(center).ToArray();
        }

        internal static TrudeColumn GetSerializedComponent(SerializedTrudeData serializedData, Element element)
        {
            FamilyInstance column = element as FamilyInstance;

            string elementId = element.Id.ToString();
            string levelName = TrudeLevel.GetLevelName(element);
            string family = InstanceUtility.GetFamily(element);
            bool isInstance = false;

            double[] cne = new double[3] { 0, 0, 0, };
            cne = GetCenter(element);

            var (bottomFace, normal, tooManyFaces) = GetBottomFaceAndNormal(element);
            double height = GetHeight(element);

            bool isParametric = !tooManyFaces && !(bottomFace.Length > 4);
            string ty = element.Name.ToString();

            if(!isParametric) { bottomFace = new double[][] {}; }

            

            TrudeColumn serializedColumn = new TrudeColumn(elementId, levelName, family, ty, isInstance, isParametric, bottomFace, normal, cne, height);
            SetColumnType(serializedData, column);
            return serializedColumn;

        }


        static public void SetColumnType(SerializedTrudeData importData, Element column)
        {
            var elemType = GlobalVariables.Document.GetElement(column.GetTypeId()) as FamilySymbol;
            string name = elemType.Name;
            if (importData.FamilyTypes.HasCeilingType(name)) return;

            TrudeColumnType snaptrudeColumnType = TrudeColumnType.GetLayersData(column);
            importData.FamilyTypes.AddColumnType(name, snaptrudeColumnType);
        }

    }
}
