using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class TrudeColumn : TrudeModel
    {
        private XYZ CenterPosition;
        private List<XYZ> faceVertices = new List<XYZ>();
        private ElementId levelId;
        private double height;

        private ColumnRfaGenerator columnRfaGenerator = new ColumnRfaGenerator();

        public static Dictionary<double, Level> NewLevelsByElevation = new Dictionary<double, Level>();
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();

        double depth = 0;
        double width = 0;


        double zLeast;
        double zHighest;

        public static TrudeColumn FromMassData(JToken massData)
        {
            TrudeColumn st_column = new TrudeColumn();

            st_column.Name = TrudeRepository.GetName(massData);
            st_column.Position = TrudeRepository.GetPosition(massData);
            st_column.CenterPosition = TrudeRepository.GetCenterPosition(massData);
            st_column.levelNumber = TrudeRepository.GetLevelNumber(massData);

            // Find face vertices calculate depth, width and height
            List<XYZ> vertices = TrudeRepository.GetVertices(massData, 6);

            double xLeast = vertices[0].X;
            double xHighest = vertices[0].X;

            double yLeast = vertices[0].Y;
            double yHighest = vertices[0].Y;

            st_column.zLeast = vertices[0].Z;
            st_column.zHighest = vertices[0].Z;

            foreach (XYZ v in vertices)
            {
                if (v.Z == vertices[0].Z) st_column.faceVertices.Add(new XYZ(v.X, v.Y, 0));

                xLeast = v.X < xLeast ? v.X : xLeast;
                yLeast = v.Y < yLeast ? v.Y : yLeast;
                st_column.zLeast = v.Z < st_column.zLeast ? v.Z : st_column.zLeast;

                xHighest = v.X > xHighest ? v.X : xHighest;
                yHighest = v.Y > yHighest ? v.Y : yHighest;
                st_column.zHighest = v.Z > st_column.zHighest ? v.Z : st_column.zHighest;
            }

            st_column.width = Math.Abs(xHighest - xLeast);
            st_column.depth = Math.Abs(yHighest - yLeast);
            st_column.height = Math.Abs(st_column.zHighest - st_column.zLeast);

            return st_column;
        }

        public void CreateColumn(Document doc, ElementId levelId, bool forForge = false)
        {
            ShapeProperties shapeProperties = (new ShapeIdentifier(ShapeIdentifier.XY)).GetShapeProperties(faceVertices);

            string familyName = shapeProperties is null
                ? $"column_custom_{Utils.RandomString(5)}"
                : $"column_{shapeProperties.ToFamilyName()}";

            string baseDir = forForge
                ? "."
                : $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}";

            CreateFamilyTypeIfNotExist(GlobalVariables.RvtApp, doc, familyName, shapeProperties, baseDir, forForge);
            CreateFamilyInstance(doc, familyName, levelId, height, shapeProperties);

            ColumnRfaGenerator.DeleteAll();
        }

        private void CreateFamilyInstance(Document doc, string familyName, ElementId levelId, double height, ShapeProperties props)
        {
            FamilySymbol familySymbol;
            if (types.ContainsKey(familyName)) { familySymbol = types[familyName]; }
            else
            {
                doc.LoadFamily(columnRfaGenerator.fileName(familyName), out Family columnFamily);
                familySymbol = TrudeModel.GetFamilySymbolByName(doc, familyName);
                types.Add(familyName, familySymbol);
            }

            Curve curve = GetPositionCurve(props, height);

            Level level = doc.GetElement(levelId) as Level;

            FamilyInstance column = doc.Create.NewFamilyInstance(curve, familySymbol, level, StructuralType.Column);
            column.Location.Rotate(curve as Line, props?.rotation ?? 0);

            //double zBase = Position.Z - (height / 2d);
            double zBase = Position.Z + zLeast;
            column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(zBase - level.Elevation);
            ElementId baseLevelId = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId();
            ElementId topLevelId = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId();
            if (baseLevelId == topLevelId)
            {
                try
                {
                    double topElevation = level.Elevation + height;
                    if (!NewLevelsByElevation.ContainsKey(topElevation))
                    {
                        TrudeStorey storey = new TrudeStorey()
                        {
                            Elevation = topElevation
                        };

                        Level newLevel = storey.CreateLevel(doc);

                        NewLevelsByElevation.Add(topElevation, newLevel);
                    }

                    column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(NewLevelsByElevation[topElevation].Id);
                }
                catch { }
            }
        }

        private Curve GetPositionCurve(ShapeProperties props, double height)
        {
            double zBase = Position.Z + zLeast;
            if (props is null)
            {
                XYZ columnBasePoint = new XYZ(Position.X, Position.Y, zBase);
                XYZ columnTopPoint = new XYZ(Position.X, Position.Y, zBase + height);

                return Line.CreateBound(columnBasePoint, columnTopPoint) as Curve;
            }
            else
            {
                XYZ columnBasePoint = new XYZ(CenterPosition.X, CenterPosition.Y, zBase);
                XYZ columnTopPoint = new XYZ(CenterPosition.X, CenterPosition.Y, zBase + height);

                return Line.CreateBound(columnBasePoint, columnTopPoint) as Curve;
            }
        }

        private void CreateFamilyTypeIfNotExist(Application app, Document doc, string familyName, ShapeProperties shapeProperties, string baseDir, bool forForge)
        {
            if (!types.ContainsKey(familyName))
            {
                if (shapeProperties is null)
                {
                    columnRfaGenerator.CreateRFAFile(app, familyName, faceVertices, /*width, depth,*/ forForge);
                }
                else if (shapeProperties.GetType() == typeof(RectangularProperties))
                {
                    string defaultRfaPath = $"{baseDir}/resourceFile/Columns/rectangular_column.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "rectangular_column");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("Width")[0].Set((shapeProperties as RectangularProperties).width);
                    newFamilyType.GetParameters("Depth")[0].Set((shapeProperties as RectangularProperties).depth);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(LShapeProperties))
                {
                    string defaultRfaPath = $"{baseDir}/resourceFile/Columns/l_shaped_column.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "l_shaped_column");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as LShapeProperties).depth);
                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as LShapeProperties).breadth);
                    newFamilyType.GetParameters("t")[0].Set((shapeProperties as LShapeProperties).thickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(HShapeProperties))
                {
                    string defaultRfaPath = $"{baseDir}/resourceFile/Columns/h_shaped_column.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "h_shaped_column");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as HShapeProperties).depth);
                    newFamilyType.GetParameters("bf")[0].Set((shapeProperties as HShapeProperties).flangeBreadth);
                    newFamilyType.GetParameters("tf")[0].Set((shapeProperties as HShapeProperties).flangeThickness);
                    newFamilyType.GetParameters("tw")[0].Set((shapeProperties as HShapeProperties).webThickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(CShapeProperties))
                {
                    string defaultRfaPath = $"{baseDir}/resourceFile/Columns/c_shaped_column.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "c_shaped_column");
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
