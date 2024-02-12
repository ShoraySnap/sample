using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeCeiling : TrudeModel
    {
        private List<XYZ> faceVertices = new List<XYZ>();
        ElementId existingCeilingTypeId = null;
        private float thickness;
        private double height;
        private TrudeLayer[] Layers;
        private static CeilingTypeStore TypeStore = new CeilingTypeStore();
        private Ceiling ceiling { get; set; }
        private XYZ centerPosition;
        private string baseType = null;
        private string materialName = null;
        /// <summary>
        /// Imports floors into revit from snaptrude json data
        /// </summary>
        /// <param name="ceilingProps"></param>
        /// <param name="levelId"></param>
        public TrudeCeiling(FloorProperties ceilingProps, ElementId levelId)
        {
            // add backward compatibility for ceiling, use create floor for 2021 or older instead of ceiling.create
            thickness = ceilingProps.Thickness;
            baseType = ceilingProps.BaseType;
            height = ceilingProps.FaceVertices[0].Z;
            centerPosition = ceilingProps.CenterPosition;
            materialName = ceilingProps.MaterialName;
            // To fix height offset issue, this can fixed from snaptude side by sending top face vertices instead but that might or might not introduce further issues
            foreach (var v in ceilingProps.FaceVertices)
            {
                faceVertices.Add(v + new XYZ(0, 0, thickness));
            }
            
            // get existing ceiling id from revit meta data if already exists else set it to null
            if (!GlobalVariables.ForForge && ceilingProps.ExistingElementId != null)
            {
                bool isExistingCeiling = GlobalVariables.idToElement.TryGetValue(ceilingProps.ExistingElementId.ToString(), out Element e);
                if (isExistingCeiling)
                {
                    Ceiling existingCeiling = (Ceiling)e;
                    existingCeilingTypeId = existingCeiling.GetTypeId();
                }
            }
            var _layers = new List<TrudeLayer>();
            //you can improve this section 
            // --------------------------------------------
            if (ceilingProps.Layers != null)
            {
                foreach (var layer in ceilingProps.Layers)
                {
                    _layers.Add(layer.ToTrudeLayer(ceilingProps.BaseType));
                }
            }
            else
            {
                _layers.Add(new TrudeLayer("Default Base Type", "Default Snaptrude Ceiling", ceilingProps.Thickness, true));
            }
            Layers = _layers.ToArray();
            //setCoreLayerIfNotExist(Math.Abs(thickness));
            // --------------------------------------------
            CreateCeiling(levelId, int.Parse(GlobalVariables.RvtApp.VersionNumber) < 2022);
            CreateHoles(ceilingProps.Holes);



            try
            {
                if (ceilingProps.SubMeshes.Count == 1)
                {
                    int _materialIndex = ceilingProps.SubMeshes.First().MaterialIndex;
                    String snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                        ceilingProps.MaterialName,
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
                        this.ApplyMaterialByObject(GlobalVariables.Document, this.ceiling, _materialElement);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Multiple submeshes detected.");
                    this.ApplyMaterialByFace(GlobalVariables.Document, ceilingProps.MaterialName, ceilingProps.SubMeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, this.ceiling);
                }
            }
            catch
            {
                Utils.LogTrace("Failed to set Slab material");
            }
        }

        //private void setCoreLayerIfNotExist(double fallbackThickness)
        //{
        //    if (Layers.Length == 0)
        //    {
        //        Layers = new TrudeLayer[] { new TrudeLayer("Ceiling", "screed" + Utils.RandomString(4), UnitsAdapter.FeetToMM(fallbackThickness), true) };

        //        return;
        //    }

        //    TrudeLayer coreLayer = Layers.FirstOrDefault(layer => layer.IsCore);

        //    if (coreLayer != null)
        //    {
        //        if (fallbackThickness != 0)
        //        {
        //            coreLayer.ThicknessInMm = UnitsAdapter.FeetToMM(fallbackThickness, 1);
        //        }

        //        return;
        //    }

        //    foreach (TrudeLayer layer in Layers)
        //    {
        //        if (layer.Name.ToLower() == "screed")
        //        {
        //            layer.IsCore = true;
        //            return;
        //        }
        //    }

        //    int coreIndex = Layers.Count() / 2;
        //    Layers[coreIndex].IsCore = true;
        //}

        private void CreateCeiling(ElementId levelId, bool depricated = false)
        {
            double levelElevation = (GlobalVariables.Document.GetElement(levelId) as Level).Elevation;
            height = faceVertices[0].Z - levelElevation;
            List<XYZ> verticesInLevelElevation = faceVertices.Select(v => new XYZ(v.X, v.Y, levelElevation)).ToList();
            CurveLoop profile = getProfileLoop(verticesInLevelElevation);
            CeilingType defaultCeilingType = null;
            var Doc = GlobalVariables.Document;

            if (existingCeilingTypeId is null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(Doc).OfClass(typeof(CeilingType));
                defaultCeilingType = collector.Where(type => ((CeilingType)type).FamilyName == "Compound Ceiling" && ((CeilingType)type).Name == "Plain").First() as CeilingType;
            }

            try
            {
                var ceilingType = TypeStore.GetType(Layers, Doc, defaultCeilingType);
#if !(REVIT2019 || REVIT2020 || REVIT2021)
                ceiling = Ceiling.Create(Doc, new List<CurveLoop> { profile }, ceilingType.Id ?? ElementId.InvalidElementId, levelId);
                ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).Set(height);
#endif
            }
            catch
            {
                //Could not create ceiling
            }
            Doc.Regenerate();
        }

        private void CreateHoles(List<List<XYZ>> holes)
        {
            foreach (var hole in holes)
            {
                var holeProfile = getProfileArray(hole);
                try
                {
                    GlobalVariables.Document.Create.NewOpening(ceiling, holeProfile, true);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Could not create hole with error: ", e);
                }
            }
        }

        private CurveLoop getProfileLoop(List<XYZ> vertices)
        {
            CurveLoop curves = new CurveLoop();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    i++;
                    if (i > vertices.Count() + 3) break;
                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }

        private CurveArray getProfileArray(List<XYZ> vertices)
        {
            CurveArray curves = new CurveArray();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];
                bool samePoint = false;

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    //This can be potentially handled on snaptrude side by sending correct vertices. Currently, some points are duplicate.
                    if (pt1.X == pt2.X && pt1.Y == pt2.Y && pt1.Z == pt2.Z)
                    {
                        samePoint = true;
                        break;
                    }

                    i++;
                    if (i > vertices.Count() + 3) break;

                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                if (samePoint) continue;
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }

        public GeometryElement GetGeometryElement()
        {
            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;
            geoOptions.ComputeReferences = true;

            return ceiling.get_Geometry(geoOptions);
        }


        public void ApplyMaterialByFace(Document document, String materialNameWithId, List<SubMeshProperties> subMeshes, JArray materials, JArray multiMaterials, Ceiling ceiling)
        {
            //Dictionary that stores Revit Face And Its Normal
            IDictionary<String, Face> normalToRevitFace = new Dictionary<String, Face>();

            List<XYZ> revitFaceNormals = new List<XYZ>();

            IEnumerator<GeometryObject> geoObjectItor = GetGeometryElement().GetEnumerator();
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
                String _materialName = GlobalVariables.sanitizeString(Utils.getMaterialNameFromMaterialId(materialNameWithId, materials, multiMaterials, face.Value)) + "_snaptrude";
                Autodesk.Revit.DB.Material _materialElement = null;
                foreach (var materialElement in materialsEnum)
                {
                    String matName = GlobalVariables.sanitizeString(materialElement.Name);
                    if (matName.Replace("_", "") == _materialName.Replace("_", ""))
                    {
                        _materialElement = materialElement;
                    }
                }
                if (_materialElement != null)
                {
                    document.Paint(ceiling.Id, face.Key, _materialElement.Id);
                }
            }

        }

        public void ApplyMaterialByObject(Document document, Ceiling ceiling, Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine
            };

            // Obtain geometry for the given ceiling element
            GeometryElement geoElem = ceiling.get_Geometry(geoOptions);

            // Find a face on the ceiling
            //Face ceilingFace = null;
            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> ceilingFaces = new List<Face>();

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
                        ceilingFaces.Add(face);
                    }
                }
            }

            //loop through all the faces and paint them


            foreach (Face face in ceilingFaces)
            {
                document.Paint(ceiling.Id, face, material.Id);
            }
        }

        //private void rotate()
        //{
        //    Location position = this.ceiling.Location;
        //    if (roofData["meshes"][0] != null)
        //    {
        //        Line localXAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisX);
        //        Line localYAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisY);
        //        Line localZAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisZ);

        //        // Why am i rotating them in this particular order? I wish i knew.
        //        if (!TrudeRepository.HasRotationQuaternion(roofData))
        //        {
        //            position.Rotate(localZAxis, -double.Parse(roofData["meshes"][0]["rotation"][1].ToString()));
        //            position.Rotate(localYAxis, -double.Parse(roofData["meshes"][0]["rotation"][2].ToString()));
        //            position.Rotate(localXAxis, -double.Parse(roofData["meshes"][0]["rotation"][0].ToString()));
        //        }
        //        else
        //        {
        //            EulerAngles rotation = TrudeRepository.GetEulerAnglesFromRotationQuaternion(roofData);

        //            // Y and Z axis are swapped moving from snaptrude to revit.
        //            position.Rotate(localXAxis, -rotation.bank);
        //            position.Rotate(localZAxis, -rotation.heading);
        //            position.Rotate(localYAxis, -rotation.attitude);
        //        }
        //    }
        //}

        //private void setHeight(Level level)
        //{
        //    double bottomZ = faceVertices[0].Z/* * Scaling.Z*/;
        //    double slabHeightAboveLevel = centerPosition.Z + bottomZ - level.ProjectElevation + thickness;

        //    ceiling
        //        .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
        //        .Set(slabHeightAboveLevel);
        //}

        //private List<TrudeLayer> createLayers(double fallbackThickness = 25)
        //{

        //    // TODO: handle existing revit data

        //    List<TrudeLayer> stLayers = new List<TrudeLayer>();

        //    return null;
        //}
    }


}