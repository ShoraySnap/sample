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
        private Plane countoursPlane;
        private bool inverseDirection = false;
        private float beamHeight;
        private Transform rotationTransform;
        private XYZ topFaceCentroid;
        private XYZ bottomFaceCentroid;
        private List<XYZ> LocalTopFaceVertices = new List<XYZ>();
        private List<BeamInstanceProperties> instances;
        private string familyName;
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();
        private BeamRfaGenerator beamRfaGenerator = new BeamRfaGenerator();
        private string materialName = null;
        private List<SubMeshProperties> submeshes;

        public TrudeBeam(BeamProperties beam, ElementId levelId, bool forForge = false)
        {
            this.countoursPlane = Plane.CreateByThreePoints(Extensions.Round(beam.FaceVertices[0]), Extensions.Round(beam.FaceVertices[1]), Extensions.Round(beam.FaceVertices[2]));

            // Get rotation angle required to align face plane with the YZ plane. (The faces are parallel to the YZ plane in rfa file)
            XYZ YZPlaneNormal = new XYZ(-1, 0, 0);

            XYZ axisOfRotation = XYZ.BasisZ;
            double rotationAngle = this.countoursPlane.Normal.AngleTo(YZPlaneNormal);

            if (this.countoursPlane.Normal.Z == 1 || this.countoursPlane.Normal.Z == -1)
            {
                axisOfRotation = XYZ.BasisY;
                rotationAngle = this.countoursPlane.Normal.AngleTo(YZPlaneNormal);
            }

            var globalRotationTransform = Transform.CreateRotationAtPoint(axisOfRotation, rotationAngle, beam.CenterPosition);
            List<XYZ> rotatedTopFaceVertices = new List<XYZ>();
            double topFaceRotatedX = -1;
            foreach (XYZ v in beam.FaceVertices)
            {
                XYZ rotatedPoint = globalRotationTransform.OfPoint(v);
                rotatedTopFaceVertices.Add(rotatedPoint);
                topFaceRotatedX = rotatedPoint.X;
            }

            List<XYZ> rotatedVertices = new List<XYZ>();
            double bottomFaceRotatedX = -1;
            foreach (XYZ v in beam.FaceVertices)
            {
                XYZ globalVertix = new XYZ(v.X + beam.CenterPosition.X,
                                           v.Y + beam.CenterPosition.Y,
                                           v.Z + beam.CenterPosition.Z);
                XYZ rotatedPoint = globalRotationTransform.OfPoint(globalVertix);
                rotatedVertices.Add(rotatedPoint);

                if (!rotatedPoint.X.RoundedEquals(topFaceRotatedX))
                {
                    bottomFaceRotatedX = rotatedPoint.X;
                }
            }

            if (bottomFaceRotatedX > topFaceRotatedX) this.inverseDirection = true;

            this.rotationTransform = Transform.CreateRotation(axisOfRotation, rotationAngle);

            // Find centroid of face
            Transform undoRotationTransform = Transform.CreateRotationAtPoint(axisOfRotation, -rotationAngle, beam.CenterPosition);

            XYZ rotatedTopFaceCentroid = new XYZ(beam.CenterPosition.X - beam.Length / 2,
                                              beam.CenterPosition.Y,
                                              beam.CenterPosition.Z);

            this.topFaceCentroid = undoRotationTransform.OfPoint(rotatedTopFaceCentroid);

            XYZ rotatedBottomFaceCentroid = new XYZ(beam.CenterPosition.X + beam.Length / 2,
                                              beam.CenterPosition.Y,
                                              beam.CenterPosition.Z);

            this.bottomFaceCentroid = undoRotationTransform.OfPoint(rotatedBottomFaceCentroid);

            // Find local face vertices
            foreach (var point in beam.FaceVertices)
            {
                this.LocalTopFaceVertices.Add(new XYZ(point.X - this.topFaceCentroid.X,
                                                          point.Y - this.topFaceCentroid.Y,
                                                          point.Z - this.topFaceCentroid.Z));
            }
            submeshes = beam.SubMeshes;
            materialName = beam.MaterialName;
            instances = beam.Instances;
            beamHeight = beam.Height;
            CreateBeam(levelId);
        }

        private void CreateBeam(ElementId levelId, bool forForge = false)
        {
            List<XYZ> rotatedFaceVertices = RotateCountoursParallelToMemberRightPlane();

            ShapeIdentifier shapeIdentifier = new ShapeIdentifier(ShapeIdentifier.YZ);
            ShapeProperties shapeProperties = shapeIdentifier.GetShapeProperties(rotatedFaceVertices, inverseDirection);

            familyName = shapeProperties is null ? $"beam_custom_{Utils.RandomString(5)}" : $"beam_{shapeProperties.ToFamilyName()}";

            string baseDir = forForge
                ? "."
                : $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}";

            CreateFamilyTypeIfNotExist(GlobalVariables.RvtApp, GlobalVariables.Document, familyName, shapeProperties, rotatedFaceVertices, baseDir, forForge);
            CreateFamilyInstance(GlobalVariables.Document, familyName, levelId, shapeProperties);

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

            return rotatedCountours;
        }

        private void CreateFamilyTypeIfNotExist(Application app, Document doc, string familyName, ShapeProperties shapeProperties,
            List<XYZ> rotatedCountours, string baseDir, bool forForge)
        {
            if (!types.ContainsKey(familyName))
            {

                if (shapeProperties is null)
                {
                    beamRfaGenerator.CreateRFAFile(app, familyName, rotatedCountours, forForge);
                }
                else if (shapeProperties.GetType() == typeof(RectangularProperties))
                {
                    string defaultRfaPath = $"{baseDir}/resourceFile/beams/rectangular_beam.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "rectangular_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as RectangularProperties).width);
                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as RectangularProperties).depth);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(LShapeProperties))
                {
                    string defaultRfaPath = $"{baseDir}resourceFile/beams/l_shaped_beam.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "l_shaped_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as LShapeProperties).depth);
                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as LShapeProperties).breadth);
                    newFamilyType.GetParameters("t")[0].Set((shapeProperties as LShapeProperties).thickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(HShapeProperties))
                {

                    string defaultRfaPath = $"{baseDir}resourceFile/beams/i_shaped_beam.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "i_shaped_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as HShapeProperties).depth);
                    newFamilyType.GetParameters("bf")[0].Set((shapeProperties as HShapeProperties).flangeBreadth);
                    newFamilyType.GetParameters("tf")[0].Set((shapeProperties as HShapeProperties).flangeThickness);
                    newFamilyType.GetParameters("tw")[0].Set((shapeProperties as HShapeProperties).webThickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(CShapeProperties))
                {
                    string defaultRfaPath = $"{baseDir}resourceFile/beams/c_shaped_beam.rfa";
                    doc.LoadFamily(defaultRfaPath, out Family family);
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "c_shaped_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as CShapeProperties).depth);
                    newFamilyType.GetParameters("bf")[0].Set((shapeProperties as CShapeProperties).flangeBreadth);
                    newFamilyType.GetParameters("tf")[0].Set((shapeProperties as CShapeProperties).flangeThickness);
                    newFamilyType.GetParameters("tw")[0].Set((shapeProperties as CShapeProperties).webThickness);

                    types.Add(familyName, newFamilyType);
                }
            }
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
            foreach (var instance in instances)
            {
                Curve curve = GetPositionCurve(instance.CenterPosition);
                Level level = doc.GetElement(levelId) as Level;

                FamilyInstance beam = doc.Create.NewFamilyInstance(curve, familySymbol, level, StructuralType.Beam);
                beam.GetParameters("Cross-Section Rotation")[0].Set(props?.rotation ?? 0);
                beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set((int)ZJustification.Center);
                if (instance.Rotation != null)
                {
                    // Only rotating around one Axis (i.e Z Axis on Revit Side and Y Axis on Snaptrude Side)
                    // Because the getBottomFaceVertices() function on Snaptrude React side gives wrong face's local vertices -
                    // - if column is rotated around X or Z Axis because internally to identify bottom face it uses global vertices -
                    // - which it convertes to local vertices before returning
                    LocationCurve curveForRotation = beam.Location as LocationCurve;
                    Curve line = curveForRotation.Curve;
                    XYZ rotationAxisEndpoint1 = line.GetEndPoint(0);
                    XYZ rotationAxisEndpoint2 = new XYZ(rotationAxisEndpoint1.X, rotationAxisEndpoint1.Y, rotationAxisEndpoint1.Z + 10);
                    Line axis = Line.CreateBound(rotationAxisEndpoint1, rotationAxisEndpoint2);
                    beam.Location.Rotate(axis, -instance.Rotation.Z);
                }
                try
                {
                    if (submeshes.Count == 1 && !string.IsNullOrEmpty(materialName))
                    {
                        int _materialIndex = submeshes.First().MaterialIndex;
                        String snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                            materialName,
                            GlobalVariables.materials,
                            GlobalVariables.multiMaterials,
                            _materialIndex).Replace(" ", "").Replace("_", "");
                        FilteredElementCollector materialCollector =
                            new FilteredElementCollector(GlobalVariables.Document)
                            .OfClass(typeof(Material));

                        IEnumerable<Material> materialsEnum = materialCollector.ToElements().Cast<Material>();
                        Material _materialElement = null;

                        foreach (var materialElement in materialsEnum)
                        {
                            String matName = materialElement.Name.Replace(" ", "").Replace("_", "");
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

                        if (_materialElement == null)
                        {
                            System.Diagnostics.Debug.WriteLine("Material not found, creating new column mat");
                            string path = "C:\\Users\\shory\\OneDrive\\Documents\\snaptrudemanager\\revit-addin\\TrudeImporter\\TrudeImporter\\Model\\metal.jpg";
                            Material newmat = GlobalVariables.CreateMaterial(GlobalVariables.Document, "newMetal", path);
                            newmat.Transparency = 30;
                            _materialElement = newmat;
                        }

                        this.ApplyMaterialByObject(GlobalVariables.Document, beam, _materialElement);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Multiple submeshes detected. ");
                        //this.ApplyMaterialByFace(GlobalVariables.Document, materialName, submeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, beam);
                    }

                }
                catch
                {
                    Utils.LogTrace("Failed to set Slab material");
                }
            }
        }

        private Curve GetPositionCurve(XYZ centerPosition)
        {
            XYZ beamBasePoint = new XYZ(centerPosition.X, centerPosition.Y, centerPosition.Z - beamHeight / 2);
            XYZ beamTopPoint = new XYZ(centerPosition.X, centerPosition.Y, centerPosition.Z + beamHeight / 2);
            return Line.CreateBound(beamBasePoint, beamTopPoint) as Curve;
        }

        public void ApplyMaterialByObject(Document document, FamilyInstance beam, Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine
            };

            // Obtain geometry for the given beam element
            GeometryElement geoElem = beam.GetOriginalGeometry(geoOptions);

            // Find a face on the beam
            //Face slabFace = null;
            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> slabFaces = new List<Face>();

            while (geoObjectItor.MoveNext())
            {
                // need to find a solid first
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid)
                {
                    // Examine faces of the solid to find one with at least
                    // one region. Then take the geometric face of that region.
                    foreach (Face face in theSolid.Faces)
                    {
                        PlanarFace p = (PlanarFace)face;
                        var normal = p.FaceNormal;
                        slabFaces.Add(face);
                    }
                }
            }

            //loop through all the faces and paint them


            foreach (Face face in slabFaces)
            {
                document.Paint(beam.Id, face, material.Id);
            }
        }

        public void ApplyMaterialByFace(Document document, String materialNameWithId, List<SubMeshProperties> subMeshes, JArray materials, JArray multiMaterials, FamilyInstance beam)
        {
            //Dictionary that stores Revit Face And Its Normal
            IDictionary<String, Face> normalToRevitFace = new Dictionary<String, Face>();

            List<XYZ> revitFaceNormals = new List<XYZ>();

            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;
            geoOptions.ComputeReferences = true;

            IEnumerator<GeometryObject> geoObjectItor = beam.get_Geometry(geoOptions).GetEnumerator();
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
                String _materialName = Utils.getMaterialNameFromMaterialId(materialNameWithId, materials, multiMaterials, face.Value);
                Autodesk.Revit.DB.Material _materialElement = null;
                foreach (var materialElement in materialsEnum)
                {
                    String matName = materialElement.Name;
                    if (matName.Replace("_", "") == _materialName.Replace("_", ""))
                    {
                        _materialElement = materialElement;
                    }
                }
                if (_materialElement != null)
                {
                    document.Paint(beam.Id, face.Key, _materialElement.Id);
                }
            }

        }


    }
}