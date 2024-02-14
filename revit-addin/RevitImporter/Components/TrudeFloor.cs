using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using TrudeSerializer.Components;
using TrudeSerializer;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using System.Collections.Generic;
using TrudeImporter;
using System;
using System.Linq;
using System.Text;
using Amazon.Runtime.Internal.Util;

namespace RevitImporter.Components
{
    internal class TrudeFloor : TrudeComponent
    {
        public string type;
        public string subType;
        public Dictionary<string, double[][]> outline;
        public double area;
        public double[][] voids;
        private TrudeFloor(string elementId, string level, string family, string type, string subType, bool isInstance, bool isParametric, Dictionary<string, double[][]> outline) : base(elementId, "Floors", family, level)
        {
            this.elementId = elementId;
            this.level = level;
            this.family = family;
            this.type = type;
            this.subType = subType; 
            this.isInstance = isInstance;
            this.isParametric = isParametric;

            this.outline = outline;
        }

        static public TrudeFloor GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            Floor floor = element as Floor;

            string elementId = element.Id.ToString();
            string levelName = TrudeLevel.GetLevelName(element);
            string family = floor.FloorType.FamilyName;
            string floorType = element.Name;

            // TODO: Subtype
            // TODO: IsInstance
            // TODO: isParametric

            var outline = GetOutline(element);

            SetFloorType(importData, floor);
            TrudeFloor serializedFloor = new TrudeFloor(elementId, levelName, family, floorType, "", false, true, outline);
            serializedFloor.SetIsParametric();
            return serializedFloor;
        }

        static Dictionary<string, double[][]> GetOutline(Element element)
        {
            const double FOOT_TO_MM = 304.8;
            var bottomProfileRef = HostObjectUtils.GetBottomFaces(element as HostObject);

            var bottomProfile = element.GetGeometryObjectFromReference(bottomProfileRef[0]);

            var faceNormal = new XYZ();
            using(var bottomProfilePlanarFace = bottomProfile as PlanarFace)
            {
                faceNormal = bottomProfilePlanarFace.FaceNormal;
            }

            var options = new Options();
            options.View = TrudeSerializer.GlobalVariables.Document.ActiveView;
            var geometry = element.get_Geometry(options);

            List<string> curveKeys = new List<string>();
            List<List<XYZ>> curveData = new List<List<XYZ>>();
            foreach(var geo in geometry)
            {
                XYZ currentFaceNormal = new XYZ();
                if (geo is Solid)
                {
                    Solid solidGeometry = geo as Solid;
                    foreach(var face in solidGeometry.Faces)
                    {
                        using(var planarFace = face as PlanarFace)
                        {
                            if(!planarFace.FaceNormal.IsNull())
                                currentFaceNormal = planarFace.FaceNormal;
                            else
                            {
                                var bbox = planarFace.GetBoundingBox();
                                var faceCenter = (bbox.Max - bbox.Min) / 2;
                                currentFaceNormal = planarFace.ComputeNormal(faceCenter).Normalize();
                            }
                            bool areSameNormal = currentFaceNormal.IsAlmostEqualTo(faceNormal);

                            if(areSameNormal)
                            {
                                var curveLoops = planarFace.GetEdgesAsCurveLoops();
                                var sortedLoops = ExporterIFCUtils.SortCurveLoops(curveLoops);

                                //return new Dictionary<string, double[][]>();

                                foreach(var sortedLists in sortedLoops)
                                {
                                    StringBuilder curveKey = new StringBuilder();
                                    curveKey.Append("[");
                                    List<XYZ> curveDataPoint = new List<XYZ>();
                                    foreach(var curveLoop in sortedLists)
                                    {
                                        var itr = curveLoop.GetCurveLoopIterator();
                                        if(!itr.IsValidObject) continue;
                                        do
                                        {
                                            var curve = itr.Current;
                                            if (curve is Arc || curve is NurbSpline)
                                            {
                                                // is different curve
                                                var points = curve.Tessellate();
                                                foreach (var p in points)
                                                {
                                                    var pconv = p.Multiply(FOOT_TO_MM);
                                                    curveDataPoint.Add(pconv);
                                                    curveKey.Append("[" + p.X + ", " + p.Y + ", " + p.Z + "]");
                                                }

                                            }
                                            else if (curve is Line)
                                            {
                                                var endPoint = curve.GetEndPoint(0);
                                                curveDataPoint.Add(endPoint.Multiply(FOOT_TO_MM));
                                                curveKey.Append("[" + endPoint.X + ", " + endPoint.Y + "]");
                                            }
                                            else
                                            {

                                            }
                                        } while (itr.MoveNext());
                                    }
                                    curveKey.Append("]");
                                    curveKey.Append(element.Id.ToString());
                                    curveKeys.Add(curveKey.ToString());
                                    curveData.Add(curveDataPoint);
                                }
                            }
                        }
                    }
                }
            }

            List<string> keys = new List<string>();
            List<double[][]> data = new List<double[][]>();
            foreach(var ck in curveKeys)
            {
                keys.Add(ck);
            }

            foreach(var plist in curveData)
            {
                double[][] arrofarr = new double[plist.Count][];
                int idx = 0;
                foreach(var p in plist)
                {
                    double[] arr = new double[3] { p.X, p.Y, p.Z };
                    arrofarr[idx] = arr;
                    idx++;
                }

                data.Add(arrofarr);
            }
            
            var dic = new Dictionary<string, double[][]>();

            for(int i = 0; i < keys.Count; i++)
            {
                dic.Add(keys[i], data[i]);
            }
            return dic;
        }


        static public void SetFloorType(SerializedTrudeData importData, Floor floor)
        {
            string name = floor.FloorType.Name;
            if(importData.FamilyTypes.HasFloorType(name))  return;

            TrudeFloorType snaptrudeFloorType = TrudeFloorType.GetLayersData(floor);
            importData.FamilyTypes.AddFloorType(name, snaptrudeFloorType);
        }

        private void SetIsParametric()
        {
            // TODO: Logic for parametric
        }

    }
}
