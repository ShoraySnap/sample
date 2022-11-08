using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Snaptrude
{
    public class ST_Column : ST_Abstract
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

        public static ST_Column FromMassData(JToken massData)
        {
            ST_Column st_column = new ST_Column();

            st_column.Name = STDataConverter.GetName(massData);
            st_column.Position = STDataConverter.GetPosition(massData);
            st_column.CenterPosition = STDataConverter.GetCenterPosition(massData);
            st_column.levelNumber = STDataConverter.GetLevelNumber(massData);
            st_column.levelId = TrudeImporter.LevelIdByNumber[st_column.levelNumber];

            // Find face vertices calculate depth, width and height
            List<XYZ> vertices = STDataConverter.GetVertices(massData, 6);

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

        public void CreateColumn(Document doc)
        {
            ShapeProperties shapeProperties = (new ShapeIdentifier(ShapeIdentifier.XY)).GetShapeProperties(faceVertices);

            string familyName = shapeProperties is null
                ? $"column_custom_{TrudeImporter.RandomString(5)}"
                : $"column_{shapeProperties.ToFamilyName()}";

            CreateFamilyTypeIfNotExist(GlobalVariables.RvtApp, doc, familyName, shapeProperties);
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
                familySymbol = ST_Abstract.GetFamilySymbolByName(doc, familyName);
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
                // TODO: use  ST_Storey to create levels

                double topElevation = level.Elevation + height;
                if (!NewLevelsByElevation.ContainsKey(topElevation))
                {
                    ST_Storey storey = new ST_Storey()
                    {
                        basePosition = topElevation
                    };

                    Level newLevel = storey.CreateLevel(doc);

                    NewLevelsByElevation.Add(topElevation, newLevel);
                }

                column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(NewLevelsByElevation[topElevation].Id);
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

        private void CreateFamilyTypeIfNotExist(Application app, Document doc, string familyName, ShapeProperties shapeProperties)
        {
            if (!types.ContainsKey(familyName))
            {
                if (shapeProperties is null)
                {
                    columnRfaGenerator.CreateRFAFile(app, familyName, faceVertices, width, depth);
                }
                else if (shapeProperties.GetType() == typeof(RectangularProperties))
                {

                    //string defaultRfaPath = "resourceFile/columns/rectangular.rfa";
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string defaultRfaPath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Columns/rectangular.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "rectangular");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("Width")[0].Set((shapeProperties as RectangularProperties).width);
                    newFamilyType.GetParameters("Depth")[0].Set((shapeProperties as RectangularProperties).depth);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(LShapeProperties))
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string defaultRfaPath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Columns/L Shaped.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "L Shaped");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as LShapeProperties).depth);
                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as LShapeProperties).breadth);
                    newFamilyType.GetParameters("t")[0].Set((shapeProperties as LShapeProperties).thickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(HShapeProperties))
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string defaultRfaPath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Columns/H Shaped.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "H Shaped");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as HShapeProperties).depth);
                    newFamilyType.GetParameters("bf")[0].Set((shapeProperties as HShapeProperties).flangeBreadth);
                    newFamilyType.GetParameters("tf")[0].Set((shapeProperties as HShapeProperties).flangeThickness);
                    newFamilyType.GetParameters("tw")[0].Set((shapeProperties as HShapeProperties).webThickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(CShapeProperties))
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string defaultRfaPath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Columns/C Shaped.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "C Shaped");
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
