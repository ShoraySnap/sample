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
        private List<XYZ> faceVertices = new List<XYZ>();
        private XYZ centerPosition = new XYZ();
        private float columnHeight;
        //private float columnWidth;
        //private float columnDepth;
        public static Dictionary<double, Level> NewLevelsByElevation = new Dictionary<double, Level>();
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();
        private ColumnRfaGenerator columnRfaGenerator= new ColumnRfaGenerator();
        public TrudeColumn(ColumnProperties column, ElementId levelId, bool forForge = false)
        {
            faceVertices = column.FaceVertices;
            centerPosition = column.CenterPosition;
            columnHeight = column.Height;
            //columnWidth = column.Width;
            //columnDepth = column.Depth;
            CreateColumn(levelId);
        }

        public void CreateColumn(ElementId levelId, bool forForge = false)
        {
            ShapeProperties shapeProperties = (new ShapeIdentifier(ShapeIdentifier.XY)).GetShapeProperties(faceVertices);

            string familyName = shapeProperties is null
                ? $"column_custom_{Utils.RandomString(5)}"
                : $"column_{shapeProperties.ToFamilyName()}";

            string baseDir = forForge
                ? "."
                : $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}";

            CreateFamilyTypeIfNotExist(familyName, shapeProperties, baseDir, forForge);
            CreateFamilyInstance(familyName, levelId, shapeProperties);

            ColumnRfaGenerator.DeleteAll();
        }

        private void CreateFamilyTypeIfNotExist(string familyName, ShapeProperties shapeProperties, string baseDir, bool forForge)
        {
            var app = GlobalVariables.RvtApp;
            var doc = GlobalVariables.Document;
            if (!types.ContainsKey(familyName))
            {
                if (shapeProperties is null)
                {
                    columnRfaGenerator.CreateRFAFile(app, familyName, faceVertices, /*columnWidth, columnDepth, */forForge);
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

        private void CreateFamilyInstance(string familyName, ElementId levelId, ShapeProperties props)
        {
            var doc = GlobalVariables.Document;
            FamilySymbol familySymbol;
            if (types.ContainsKey(familyName)) { familySymbol = types[familyName]; }
            else
            {
                doc.LoadFamily(columnRfaGenerator.fileName(familyName), out Family columnFamily);
                familySymbol = TrudeModel.GetFamilySymbolByName(doc, familyName);
                types.Add(familyName, familySymbol);
            }

            Curve curve = GetPositionCurve(props);

            Level level = doc.GetElement(levelId) as Level;

            FamilyInstance column = doc.Create.NewFamilyInstance(curve, familySymbol, level, StructuralType.Column);
            column.Location.Rotate(curve as Line, props?.rotation ?? 0);
            column.Location.Move(centerPosition);

            double zBase = centerPosition.Z - (columnHeight / 2d);
            //double zBase = 0;
            column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(zBase - level.ProjectElevation);
            ElementId baseLevelId = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId();
            ElementId topLevelId = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId();
            if (baseLevelId == topLevelId)
            {
                try
                {
                    double topElevation = level.ProjectElevation + columnHeight;
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

        private Curve GetPositionCurve(ShapeProperties props)
        {
            XYZ columnBasePoint = new XYZ(0, 0, centerPosition.Z - columnHeight / 2);
            XYZ columnTopPoint = new XYZ(0, 0, centerPosition.Z + columnHeight / 2);
            return Line.CreateBound(columnBasePoint, columnTopPoint) as Curve;
        }
    }
}