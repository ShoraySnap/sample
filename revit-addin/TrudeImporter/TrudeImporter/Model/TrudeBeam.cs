using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeBeam : TrudeModel
    {
        private Plane countoursPlane;
        private bool inverseDirection = false;
        private Transform rotationTransform;
        private XYZ topFaceCentroid;
        private XYZ bottomFaceCentroid;
        private List<XYZ> LocalTopFaceVertices = new List<XYZ>();
        private string familyName;
        private int submeshCount;
        private string materialName = null;
        private List<SubMeshProperties> submeshes;
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();
        private BeamRfaGenerator beamRfaGenerator = new BeamRfaGenerator();


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
            this.submeshes = beam.SubMeshes;
            this.submeshCount = beam.SubMeshes.Count;
            this.materialName = beam.MaterialName;

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

            Curve curve = GetPositionCurve(props);

            Level level = doc.GetElement(levelId) as Level;

            FamilyInstance beam = doc.Create.NewFamilyInstance(curve, familySymbol, level, StructuralType.Beam);
            beam.GetParameters("Cross-Section Rotation")[0].Set(props?.rotation ?? 0);
            beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set((int)ZJustification.Center);


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
                            System.Diagnostics.Debug.WriteLine("Material found " + snaptrudeMaterialName);
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
                        this.ApplyMaterialByObject(GlobalVariables.Document, beam, _materialElement);
                    }
                }
                //else
                //{
                //    System.Diagnostics.Debug.WriteLine("Multiple submeshes detected. ");
                //    this.ApplyMaterialByFace(GlobalVariables.Document, materialName, submeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, column);
                //}

            }
            catch
            {
                Utils.LogTrace("Failed to set Slab material");
            }

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

        public void ApplyMaterialByObject(Document document, FamilyInstance beam, Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine
            };
            document.Regenerate();
            // Obtain geometry for the given beam element
            GeometryElement geoElem = beam.get_Geometry(geoOptions);

            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> slabFaces = new List<Face>();

            while (geoObjectItor.MoveNext())
            {
                List<Solid> solids = GetSolids(geoObjectItor.Current);

                foreach (Solid solid in solids)
                {
                    System.Diagnostics.Debug.WriteLine("Number of faces " + solid.Faces.Size);
                    foreach (Face face in solid.Faces)
                    {
                        slabFaces.Add(face);
                    }
                }

            }

            foreach (Face face in slabFaces)
            {
                System.Diagnostics.Debug.WriteLine(face.Area);
                System.Diagnostics.Debug.WriteLine(beam.Id);
                System.Diagnostics.Debug.WriteLine(material.Id + "\n");
                // Paint the face with the material
                document.Paint(beam.Id, face, material.Id);
            }
        }

        static public List<Solid> GetSolids(GeometryObject gObj)
        {
            List<Solid> solids = new List<Solid>();
            if (gObj is Solid) // already solid
            {
                Solid solid = gObj as Solid;
                if (solid.Faces.Size > 0 && Math.Abs(solid.Volume) > 0) // skip invalid solid
                    solids.Add(gObj as Solid);
            }
            else if (gObj is GeometryInstance) // find solids from GeometryInstance
            {
                IEnumerator<GeometryObject> gIter2 = (gObj as GeometryInstance).GetInstanceGeometry().GetEnumerator();
                gIter2.Reset();
                while (gIter2.MoveNext())
                {
                    solids.AddRange(GetSolids(gIter2.Current));
                }
            }
            else if (gObj is GeometryElement) // find solids from GeometryElement
            {
                IEnumerator<GeometryObject> gIter2 = (gObj as GeometryElement).GetEnumerator();
                gIter2.Reset();
                while (gIter2.MoveNext())
                {
                    solids.AddRange(GetSolids(gIter2.Current));
                }
            }
            return solids;
        }

    }
}