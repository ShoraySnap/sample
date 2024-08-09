using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeColumn : TrudeModel
    {
        private List<XYZ> faceVertices = new List<XYZ>();
        private float columnHeight;
        private double Diameter;
        private bool IsCircular;
        private int submeshCount;
        private List<SubMeshProperties> submeshes;
        private List<ColumnInstanceProperties> instances;
        public static Dictionary<double, Level> NewLevelsByElevation = new Dictionary<double, Level>();
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();
        private ColumnRfaGenerator columnRfaGenerator = new ColumnRfaGenerator();
        private string materialName = null;
        public TrudeColumn(ColumnProperties columnProps)
        {
            faceVertices = columnProps.FaceVertices;
            columnHeight = columnProps.Height;
            instances = columnProps.Instances;
            materialName = columnProps.MaterialName;
            submeshCount = columnProps.SubMeshes.Count;
            submeshes = columnProps.SubMeshes;
            IsCircular = columnProps.IsCircular;
            Diameter = columnProps.Diameter;
            CreateColumn();
        }

        public void CreateColumn()
        {
            ShapeProperties shapeProperties = null;
            try
            {
                if (IsCircular)
                {
                    shapeProperties = (new ShapeIdentifier(ShapeIdentifier.XY)).GetShapeProperties(faceVertices, false, Diameter);
                }
                else
                {
                    shapeProperties = (new ShapeIdentifier(ShapeIdentifier.XY)).GetShapeProperties(faceVertices);
                }
            }
            catch (Exception e)
            {
            }

            string familyName = shapeProperties is null
                ? $"column_custom_{Utils.RandomString(5)}"
                : $"column_{shapeProperties.ToFamilyName()}";

            string baseDir = GlobalVariables.ForForge
                ? "."
                : $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}";

            CreateFamilyTypeIfNotExist(familyName, shapeProperties, baseDir);
            CreateFamilyInstances(familyName, shapeProperties);

            ColumnRfaGenerator.DeleteAll();
        }

        private void CreateFamilyTypeIfNotExist(string familyName, ShapeProperties shapeProperties, string baseDir)
        {
            var app = GlobalVariables.RvtApp;
            var doc = GlobalVariables.Document;
            if (!types.ContainsKey(familyName))
            {
                if (shapeProperties is null)
                {
                    //Calculate depth, width
                    double xLeast = faceVertices[0].X;
                    double xHighest = faceVertices[0].X;
                    double yLeast = faceVertices[0].Y;
                    double yHighest = faceVertices[0].Y;

                    foreach (XYZ v in faceVertices)
                    {
                        xLeast = v.X < xLeast ? v.X : xLeast;
                        yLeast = v.Y < yLeast ? v.Y : yLeast;
                        xHighest = v.X > xHighest ? v.X : xHighest;
                        yHighest = v.Y > yHighest ? v.Y : yHighest;
                    }

                    double depth = Math.Abs(xHighest - xLeast);
                    double width = Math.Abs(yHighest - yLeast);

                    columnRfaGenerator.CreateRFAFile(app, familyName, faceVertices, width, depth);
                }
                else if (shapeProperties.GetType() == typeof(CircularProperties))
                {
                    FamilyLoader.LoadCustomFamily("round_column", FamilyLoader.FamilyFolder.Columns);

                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "round_column");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.LookupParameter("Diameter").Set((shapeProperties as CircularProperties).diameter);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(RectangularProperties))
                {
                    FamilyLoader.LoadCustomFamily("rectangular_column", FamilyLoader.FamilyFolder.Columns);

                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "rectangular_column");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("Width")[0].Set((shapeProperties as RectangularProperties).width);
                    newFamilyType.GetParameters("Depth")[0].Set((shapeProperties as RectangularProperties).depth);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(LShapeProperties))
                {
                    FamilyLoader.LoadCustomFamily("l_shaped_column", FamilyLoader.FamilyFolder.Columns);

                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "l_shaped_column");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as LShapeProperties).depth);
                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as LShapeProperties).breadth);
                    newFamilyType.GetParameters("t")[0].Set((shapeProperties as LShapeProperties).thickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(HShapeProperties))
                {
                    FamilyLoader.LoadCustomFamily("h_shaped_column", FamilyLoader.FamilyFolder.Columns);

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
                    FamilyLoader.LoadCustomFamily("c_shaped_column", FamilyLoader.FamilyFolder.Columns);

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

        private void CreateFamilyInstances(string familyName, ShapeProperties props)
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

            foreach (var instance in instances)
            {
                Curve curve = GetPositionCurve(instance.CenterPosition);
                Level level = doc.GetElement(GlobalVariables.LevelIdByNumber[instance.Storey]) as Level;
                FamilyInstance column = doc.Create.NewFamilyInstance(curve, familySymbol, level, StructuralType.Column);
                // This is required for correct rotation of Beams of identified Shapes
                column.Location.Rotate(curve as Line, props?.rotation ?? 0);

                if (instance.Rotation != null)
                {
                    // Only rotating around one Axis (i.e Z Axis on Revit Side and Y Axis on Snaptrude Side)
                    // Because the getBottomFaceVertices() function on Snaptrude React side gives wrong face's local vertices -
                    // - if column is rotated around X or Z Axis because internally to identify bottom face it uses global vertices -
                    // - which it convertes to local vertices before returning
                    LocationCurve curveForRotation = column.Location as LocationCurve;
                    Curve line = curveForRotation.Curve;
                    XYZ rotationAxisEndpoint1 = line.GetEndPoint(0);
                    XYZ rotationAxisEndpoint2 = new XYZ(rotationAxisEndpoint1.X, rotationAxisEndpoint1.Y, rotationAxisEndpoint1.Z + 10);
                    Line axis = Line.CreateBound(rotationAxisEndpoint1, rotationAxisEndpoint2);
                    column.Location.Rotate(axis, -instance.Rotation.Z);
                }

                // Assuming the material name is available in the instance object
                try
                {
                    if (submeshCount == 1 && !string.IsNullOrEmpty(materialName))
                    {
                        int _materialIndex = submeshes.First().MaterialIndex;
                        String snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                            materialName,
                            GlobalVariables.materials,
                            GlobalVariables.multiMaterials,
                            _materialIndex);
                        snaptrudeMaterialName = GlobalVariables.sanitizeString(snaptrudeMaterialName) + "_snaptrude";
                        FilteredElementCollector materialCollector =
                            new FilteredElementCollector(GlobalVariables.Document)
                            .OfClass(typeof(Material));

                        IEnumerable<Material> materialsEnum = materialCollector.ToElements().Cast<Material>();
                        Material _materialElement = null;

                        foreach (var materialElement in materialsEnum)
                        {
                            string matName = GlobalVariables.sanitizeString(materialElement.Name);
                            if (matName == snaptrudeMaterialName)
                            {
                                _materialElement = materialElement;
                                break;
                            }
                        }
                        try/* (_materialElement is null && snaptrudeMaterialName.ToLower().Contains("glass"))*/
                        {
                            if (snaptrudeMaterialName != null)
                            {
                                if (_materialElement is null && snaptrudeMaterialName.ToLower().Contains("glass"))
                                {
                                    foreach (var materialElement in materialsEnum)
                                    {
                                        String matName = materialElement.Name;
                                        if (matName.ToLower().Contains("glass"))
                                        {
                                            _materialElement = materialElement;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {

                        }
                        if (_materialElement != null)
                        {
                            this.ApplyMaterialByObject(GlobalVariables.Document, column, _materialElement);
                        }
                    }
                    else
                    {
                        this.ApplyMaterialByFace(GlobalVariables.Document, materialName, submeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, column);
                    }

                }
                catch
                {
                    Utils.LogTrace("Failed to set Slab material");
                }

            }

            // -----

            //column.Location.Move(new XYZ(centerPosition.X, centerPosition.Y, centerPosition.Z - level.ProjectElevation));

            //double zBase = centerPosition.Z - (columnHeight / 2d);
            ////double zBase = 0;
            //column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(zBase - level.ProjectElevation);

            //ElementId baseLevelId = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId();
            //ElementId topLevelId = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId();
            //if (baseLevelId == topLevelId)
            //{
            //    try
            //    {
            //        double topElevation = level.ProjectElevation + columnHeight;
            //        if (!NewLevelsByElevation.ContainsKey(topElevation))
            //        {
            //            TrudeStorey storey = new TrudeStorey()
            //            {
            //                Elevation = topElevation
            //            };

            //            Level newLevel = storey.CreateLevel(doc);

            //            NewLevelsByElevation.Add(topElevation, newLevel);
            //        }

            //        column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(NewLevelsByElevation[topElevation].Id);
            //    }
            //    catch { }
            //}
        }

        private Curve GetPositionCurve(XYZ centerPosition)
        {
            XYZ columnBasePoint = new XYZ(centerPosition.X, centerPosition.Y, centerPosition.Z - columnHeight / 2);
            XYZ columnTopPoint = new XYZ(centerPosition.X, centerPosition.Y, centerPosition.Z + columnHeight / 2);
            return Line.CreateBound(columnBasePoint, columnTopPoint) as Curve;
        }


        public void ApplyMaterialByObject(Document document, FamilyInstance column, Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine
            };

            // Obtain geometry for the given column element
            GeometryElement geoElem = column.get_Geometry(geoOptions);

            // Find a face on the column
            //Face slabFace = null;
            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> slabFaces = new List<Face>();

            while (geoObjectItor.MoveNext())
            {
                // need to find a solid first
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid)
                {
                    foreach (Face face in theSolid.Faces)
                    {
                        PlanarFace p = (PlanarFace)face;
                        var normal = p.FaceNormal;
                        slabFaces.Add(face);
                    }
                }
            }


            foreach (Face face in slabFaces)
            {
                document.Paint(column.Id, face, material.Id);
            }
        }


        public void ApplyMaterialByFace(Document document, String materialNameWithId, List<SubMeshProperties> subMeshes, JArray materials, JArray multiMaterials, FamilyInstance column)
        {
            //Dictionary that stores Revit Face And Its Normal
            IDictionary<String, Face> normalToRevitFace = new Dictionary<String, Face>();

            List<XYZ> revitFaceNormals = new List<XYZ>();

            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;
            geoOptions.ComputeReferences = true;

            IEnumerator<GeometryObject> geoObjectItor = column.get_Geometry(geoOptions).GetEnumerator();
            while (geoObjectItor.MoveNext())
            {
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid)
                {
                    foreach (Face face in theSolid.Faces)
                    {
                        PlanarFace p = (PlanarFace)face;
                        var normal = p.FaceNormal;
                        revitFaceNormals.Add(normal.Round(3));
                        if (!normalToRevitFace.ContainsKey(normal.Round(3).Stringify()))
                        {
                            normalToRevitFace.Add(normal.Round(3).Stringify(), face);
                        }
                    }
                }
            }

            //Dictionary that has Revit Face And The Material Index to Be Applied For It.
            IDictionary<Face, int> revitFaceAndItsSubMeshIndex = new Dictionary<Face, int>();

            foreach (SubMeshProperties subMesh in subMeshes)
            {
                if (subMesh.Normal == null)
                {
                    //   System.Diagnostics.Debug.WriteLine(subMesh);
                    continue;
                }
                String key = subMesh.Normal.Stringify();
                if (normalToRevitFace.ContainsKey(key))
                {
                    Face revitFace = normalToRevitFace[key];
                    if (!revitFaceAndItsSubMeshIndex.ContainsKey(revitFace)) revitFaceAndItsSubMeshIndex.Add(revitFace, subMesh.MaterialIndex);
                }
                else
                {
                    // find the closest key
                    double leastDistance = Double.MaxValue;
                    foreach (XYZ normal in revitFaceNormals)
                    {
                        double distance = normal.DistanceTo(subMesh.Normal);
                        if (distance < leastDistance)
                        {
                            leastDistance = distance;
                            key = normal.Stringify();
                        }
                    }

                    Face revitFace = normalToRevitFace[key];

                    if (!revitFaceAndItsSubMeshIndex.ContainsKey(revitFace)) revitFaceAndItsSubMeshIndex.Add(revitFace, subMesh.MaterialIndex);
                }
            }

            FilteredElementCollector collector1 = new FilteredElementCollector(document).OfClass(typeof(Autodesk.Revit.DB.Material));
            IEnumerable<Autodesk.Revit.DB.Material> materialsEnum = collector1.ToElements().Cast<Autodesk.Revit.DB.Material>();


            foreach (var face in revitFaceAndItsSubMeshIndex)
            {
                String _materialName = GlobalVariables.sanitizeString(Utils.getMaterialNameFromMaterialId(materialNameWithId, materials, multiMaterials, face.Value)) + "_snaptrude";
                Autodesk.Revit.DB.Material _materialElement = null;
                foreach (var materialElement in materialsEnum)
                {
                    String matName = GlobalVariables.sanitizeString(materialElement.Name);
                    if (matName == _materialName)
                    {
                        _materialElement = materialElement;
                    }
                }
                if (_materialElement != null)
                {
                    document.Paint(column.Id, face.Key, _materialElement.Id);
                }
            }

        }

    }
}


