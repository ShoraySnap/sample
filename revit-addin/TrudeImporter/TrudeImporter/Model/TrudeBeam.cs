using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeBeam : TrudeModel
    {
        private XYZ CenterPosition;
        private Plane countoursPlane;
        private ElementId levelId;
        private double length;
        private double height;
        private List<XYZ> vertices;
        private Transform rotationTransform;
        private List<XYZ> GlobalTopFaceVertices = new List<XYZ>();
        private List<XYZ> LocalTopFaceVertices = new List<XYZ>();
        XYZ topFaceCentroid;
        XYZ bottomFaceCentroid;
        private bool inverseDirection = false;

        private string familyName;

        private BeamRfaGenerator beamRfaGenerator = new BeamRfaGenerator();

        public static Dictionary<double, Level> NewLevelsByElevation = new Dictionary<double, Level>();
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();
        public static TrudeBeam FromMassData(JToken massData)
        {
            TrudeBeam st_beam = new TrudeBeam();

            st_beam.Name                = TrudeRepository.GetName(massData);
            st_beam.Position            = TrudeRepository.GetPosition(massData);
            st_beam.CenterPosition      = TrudeRepository.GetCenterPosition(massData);
            st_beam.levelNumber         = TrudeRepository.GetLevelNumber(massData);
            st_beam.vertices            = TrudeRepository.GetVertices(massData, 6);

            // Get global face vertices
            foreach (var point in massData["faceVertices"])
            {
                st_beam.GlobalTopFaceVertices.Add( new XYZ(UnitsAdapter.convertToRevit(Double.Parse(point["_x"].ToString())),
                                                        UnitsAdapter.convertToRevit(Double.Parse(point["_z"].ToString())),
                                                        UnitsAdapter.convertToRevit(Double.Parse(point["_y"].ToString()))) );
            }

            st_beam.countoursPlane = Plane.CreateByThreePoints(Extensions.Round(st_beam.GlobalTopFaceVertices[0]),
                                                               Extensions.Round(st_beam.GlobalTopFaceVertices[1]),
                                                               Extensions.Round(st_beam.GlobalTopFaceVertices[2]));

            // Get rotation angle required to align face plane with the YZ plane. (The faces are parallel to the YZ plane in rfa file)
            XYZ YZPlaneNormal = new XYZ(-1, 0, 0);

            XYZ axisOfRotation = XYZ.BasisZ;
            double rotationAngle = st_beam.countoursPlane.Normal.AngleTo(YZPlaneNormal);

            if (st_beam.countoursPlane.Normal.Z == 1 || st_beam.countoursPlane.Normal.Z == -1)
            {
                axisOfRotation = XYZ.BasisY;
                rotationAngle = st_beam.countoursPlane.Normal.AngleTo(YZPlaneNormal);
            }

            var globalRotationTransform = Transform.CreateRotationAtPoint(axisOfRotation, rotationAngle, st_beam.Position);
            List<XYZ> rotatedTopFaceVertices = new List<XYZ>();
            double topFaceRotatedX = -1;
            foreach(XYZ v in st_beam.GlobalTopFaceVertices)
            {
                XYZ rotatedPoint = globalRotationTransform.OfPoint(v);
                rotatedTopFaceVertices.Add(rotatedPoint);
                topFaceRotatedX = rotatedPoint.X;
            }

            // Find length
            List<XYZ> rotatedVertices = new List<XYZ>();
            double bottomFaceRotatedX = -1;
            foreach (XYZ v in st_beam.vertices)
            {
                XYZ globalVertix = new XYZ(v.X + st_beam.Position.X,
                                           v.Y + st_beam.Position.Y,
                                           v.Z + st_beam.Position.Z);
                XYZ rotatedPoint = globalRotationTransform.OfPoint(globalVertix);
                rotatedVertices.Add(rotatedPoint);

                if (!rotatedPoint.X.RoundedEquals(topFaceRotatedX))
                {
                    bottomFaceRotatedX = rotatedPoint.X;
                }
            }
            st_beam.CalculateLength(rotatedVertices);

            if (bottomFaceRotatedX > topFaceRotatedX) st_beam.inverseDirection = true;


            st_beam.rotationTransform = Transform.CreateRotation(axisOfRotation, rotationAngle);

            // Find centroid of face
            Transform undoRotationTransform = Transform.CreateRotationAtPoint(axisOfRotation, -rotationAngle, st_beam.CenterPosition);

            XYZ rotatedTopFaceCentroid = new XYZ(st_beam.CenterPosition.X - st_beam.length / 2,
                                              st_beam.CenterPosition.Y,
                                              st_beam.CenterPosition.Z);

            st_beam.topFaceCentroid = undoRotationTransform.OfPoint(rotatedTopFaceCentroid);

            XYZ rotatedBottomFaceCentroid = new XYZ(st_beam.CenterPosition.X + st_beam.length / 2,
                                              st_beam.CenterPosition.Y,
                                              st_beam.CenterPosition.Z);

            st_beam.bottomFaceCentroid = undoRotationTransform.OfPoint(rotatedBottomFaceCentroid);

            // Find local face vertices
            foreach (var point in st_beam.GlobalTopFaceVertices)
            {
                st_beam.LocalTopFaceVertices.Add( new XYZ(point.X - st_beam.topFaceCentroid.X,
                                                          point.Y - st_beam.topFaceCentroid.Y,
                                                          point.Z - st_beam.topFaceCentroid.Z) );
            }

            return st_beam;
        }

        public void CreateBeam(Document doc, ElementId levelId, bool forForge = false)
        {
            List<XYZ> rotatedFaceVertices = RotateCountoursParallelToMemberRightPlane();

            ShapeIdentifier shapeIdentifier = new ShapeIdentifier(ShapeIdentifier.YZ);
            ShapeProperties shapeProperties = shapeIdentifier.GetShapeProperties(rotatedFaceVertices, inverseDirection);

            familyName = shapeProperties is null ? $"beam_custom_{Utils.RandomString(5)}" : $"beam_{shapeProperties.ToFamilyName()}";

            string baseDir = forForge
                ? "."
                : $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}";

            CreateFamilyTypeIfNotExist(GlobalVariables.RvtApp, doc, familyName, shapeProperties, rotatedFaceVertices, length, baseDir, forForge);
            CreateFamilyInstance(doc, familyName, levelId, shapeProperties);

            BeamRfaGenerator.DeleteAll();
        }

        private List<XYZ> RotateCountoursParallelToMemberRightPlane()
        {
            const double REF_PLANE_MEMBER_LEFT_X = -4.101049869;

            List<XYZ> rotatedCountours = new List<XYZ>();
            foreach (XYZ point in LocalTopFaceVertices)
            {
                XYZ rotatedPoint = rotationTransform.OfPoint(point);
                rotatedCountours.Add(new XYZ(REF_PLANE_MEMBER_LEFT_X, rotatedPoint.Y, rotatedPoint.Z));
            }

            CalculateHeight(rotatedCountours);

            return rotatedCountours;
        }

        private void CalculateHeight(List<XYZ> vertices)
        {
            double z0 = vertices[0].Z;
            double zLeast = z0; 
            double zHighest = z0; 
            foreach(XYZ v in vertices)
            {
                if (v.Z < zLeast) zLeast = v.Z;
                if (v.Z > zHighest) zHighest = v.Z;
            }

            height = Math.Abs(zHighest - zLeast);
            beamRfaGenerator.height = height;
        }
        private void CalculateLength(List<XYZ> vertices)
        {
            double x0 = vertices[0].X;
            double xLeast = x0; 
            double xHighest = x0; 
            foreach(XYZ v in vertices)
            {
                if (v.X < xLeast) xLeast = v.X;
                if (v.X > xHighest) xHighest = v.X;
            }

            length = Math.Abs(xHighest - xLeast);
        }

        private void CreateFamilyInstance(Document doc, string familyName, ElementId levelId, ShapeProperties props)
        {
                FamilySymbol familySymbol;
                if (types.ContainsKey(familyName)) { familySymbol = types[familyName]; }
                else
                {
                    doc.LoadFamily(beamRfaGenerator.fileName(familyName), out Family beamFamily);
                    familySymbol = TrudeModel.GetFamilySymbolByName(doc, familyName);
                    types.Add(familyName, familySymbol);
                }

                Curve curve = GetPositionCurve(props);

                Level level = doc.GetElement(levelId) as Level;

                FamilyInstance beam = doc.Create.NewFamilyInstance(curve, familySymbol, level, StructuralType.Beam);
                beam.GetParameters("Cross-Section Rotation")[0].Set(props?.rotation ?? 0);
                beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set((int) ZJustification.Center);
        }

        private Curve GetPositionCurve(ShapeProperties props)
        {
            if (props is null)
            {
                return Line.CreateBound(topFaceCentroid, bottomFaceCentroid);
            }
            else
            {
                return Line.CreateBound(bottomFaceCentroid, topFaceCentroid);
            }

        }

        private void CreateFamilyTypeIfNotExist(Application app, Document doc, string familyName, ShapeProperties shapeProperties,
            List<XYZ> rotatedCountours, double height, string baseDir, bool forForge)
        {
            if (!types.ContainsKey(familyName))
            {

                if (shapeProperties is null)
                {
                    beamRfaGenerator.CreateRFAFile(app, familyName, height, rotatedCountours, forForge);
                }
                else if (shapeProperties.GetType() == typeof(RectangularProperties))
                {
                        string defaultRfaPath = $"{baseDir}/resourceFile/beams/Rectangular.rfa";
                        doc.LoadFamily(defaultRfaPath, out Family family);
                        FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "Rectangular");
                        FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                        newFamilyType.GetParameters("b")[0].Set((shapeProperties as RectangularProperties).width);
                        newFamilyType.GetParameters("d")[0].Set((shapeProperties as RectangularProperties).depth);

                        types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(LShapeProperties))
                {
                        string defaultRfaPath = $"{baseDir}resourceFile/beams/L-Shaped.rfa";
                        doc.LoadFamily(defaultRfaPath, out Family family);
                        FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "L-Shaped");
                        FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                        newFamilyType.GetParameters("d")[0].Set((shapeProperties as LShapeProperties).depth);
                        newFamilyType.GetParameters("b")[0].Set((shapeProperties as LShapeProperties).breadth);
                        newFamilyType.GetParameters("t")[0].Set((shapeProperties as LShapeProperties).thickness);

                        types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(HShapeProperties))
                {

                        string defaultRfaPath = $"{baseDir}resourceFile/beams/I-Shaped.rfa";
                        doc.LoadFamily(defaultRfaPath, out Family family);
                        FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "I-Shaped");
                        FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                        newFamilyType.GetParameters("d")[0].Set((shapeProperties as HShapeProperties).depth);
                        newFamilyType.GetParameters("bf")[0].Set((shapeProperties as HShapeProperties).flangeBreadth);
                        newFamilyType.GetParameters("tf")[0].Set((shapeProperties as HShapeProperties).flangeThickness);
                        newFamilyType.GetParameters("tw")[0].Set((shapeProperties as HShapeProperties).webThickness);

                        types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(CShapeProperties))
                {
                        string defaultRfaPath = $"{baseDir}resourceFile/beams/C-Shaped.rfa";
                        doc.LoadFamily(defaultRfaPath, out Family family);
                        FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "C-Shaped");
                        FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                        newFamilyType.GetParameters("d")[0].Set((shapeProperties as CShapeProperties).depth);
                        newFamilyType.GetParameters("bf")[0].Set((shapeProperties as CShapeProperties).flangeBreadth);
                        newFamilyType.GetParameters("tf")[0].Set((shapeProperties as CShapeProperties).flangeThickness);
                        newFamilyType.GetParameters("tw")[0].Set((shapeProperties as CShapeProperties).webThickness);

                        types.Add(familyName, newFamilyType);
                }
            }
        }
    }
}
