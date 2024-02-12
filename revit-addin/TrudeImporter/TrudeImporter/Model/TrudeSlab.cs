using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeSlab : TrudeModel
    {
        private List<XYZ> faceVertices = new List<XYZ>();
        FloorType existingFloorType = null;
        private float thickness;
        private TrudeLayer[] Layers;
        public static FloorTypeStore TypeStore = new FloorTypeStore();
        private Floor slab { get; set; }
        private XYZ centerPosition;
        private string baseType = null;
        private string materialName = null;

        ElementId levelId = null;

        /// <summary>
        /// Imports floors into revit from snaptrude json data
        /// </summary>
        /// <param name="slabProps"></param>
        /// <param name="levelId"></param>
        public TrudeSlab(SlabProperties slabProps)
        {
            thickness = slabProps.Thickness;
            baseType = slabProps.BaseType;
            materialName = slabProps.MaterialName;
            levelId = GlobalVariables.LevelIdByNumber[slabProps.Storey];
            centerPosition = slabProps.CenterPosition;
            // To fix height offset issue, this can fixed from snaptude side by sending top face vertices instead but that might or might not introduce further issues
            foreach (var v in slabProps.FaceVertices)
            {
                faceVertices.Add(v + new XYZ(0, 0, thickness));
            }

            // get existing slab id from revit meta data if already exists else set it to null
            if (!GlobalVariables.ForForge && slabProps.ExistingElementId != null)
            {
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                ElementId existingElementId = new ElementId((int)slabProps.ExistingElementId);
#else
                ElementId existingElementId = new ElementId((Int64)slabProps.ExistingElementId);
#endif
                Floor existingFloor = GlobalVariables.Document.GetElement(existingElementId) as Floor;
                existingFloorType = existingFloor.FloorType;
            }
            var _layers = new List<TrudeLayer>();
            //you can improve this section 
            // --------------------------------------------
            if (slabProps.Layers != null)
            {
                foreach (var layer in slabProps.Layers)
                {
                    _layers.Add(new TrudeLayer(slabProps.BaseType, layer.Name, layer.ThicknessInMm, layer.IsCore));
                }
            }
            Layers = _layers.ToArray();
            setCoreLayerIfNotExist(Math.Abs(thickness));
            // --------------------------------------------
            CreateSlab(levelId, int.Parse(GlobalVariables.RvtApp.VersionNumber) >= 2023);
            GlobalVariables.Document.Regenerate();
            CreateHoles(slabProps.Holes);
            GlobalVariables.Document.Regenerate();

            try
            {
                if (slabProps.SubMeshes.Count == 1)
                {
                    int _materialIndex = slabProps.SubMeshes.First().MaterialIndex;
                    String snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                        slabProps.MaterialName,
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
                        this.ApplyMaterialByObject(GlobalVariables.Document, this.slab, _materialElement);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Multiple submeshes detected. Material application by face is currently disabled.");
                    this.ApplyMaterialByFace(GlobalVariables.Document, slabProps.MaterialName, slabProps.SubMeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, this.slab);
                }
            }
            catch
            {
                Utils.LogTrace("Failed to set Slab material");
            }
        }

        private void setCoreLayerIfNotExist(double fallbackThickness)
        {
            if (Layers.Length == 0)
            {
                Layers = new TrudeLayer[] { new TrudeLayer("Floor", "screed" + Utils.RandomString(4), UnitsAdapter.FeetToMM(fallbackThickness), true) };

                return;
            }

            TrudeLayer coreLayer = Layers.FirstOrDefault(layer => layer.IsCore);

            if (coreLayer != null)
            {
                if (fallbackThickness != 0)
                {
                    coreLayer.ThicknessInMm = UnitsAdapter.FeetToMM(fallbackThickness, 1);
                }

                return;
            }

            foreach (TrudeLayer layer in Layers)
            {
                if (layer.Name.ToLower() == "screed")
                {
                    layer.IsCore = true;
                    return;
                }
            }

            int coreIndex = Layers.Count() / 2;
            Layers[coreIndex].IsCore = true;
        }

        private void CreateSlab(ElementId levelId, bool depricated = false)
        {
            CurveArray profile = getProfile(faceVertices);
            CurveLoop profileLoop = Utils.GetProfileLoop(verticesInLevelElevation);
            FloorType floorType = existingFloorType;

            var Doc = GlobalVariables.Document;
            if (floorType is null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(Doc).OfClass(typeof(FloorType));
                FloorType defaultFloorType = collector.Where(type => ((FloorType)type).FamilyName == "Foundation Slab").First() as FloorType;
                floorType = defaultFloorType;
            }
            var newFloorType = TypeStore.GetType(Layers, Doc, floorType);
            try
            {
                //throws exception 
#if REVIT2019 || REVIT2020 || REVIT2021
                slab = Doc.Create.NewFoundationSlab(profile, newFloorType, Doc.GetElement(levelId) as Level, true, new XYZ(0, 0, -1));
#else
                IList<CurveLoop> curveLoops = new List<CurveLoop> { profileLoop };
                slab = Floor.Create(GlobalVariables.Document, curveLoops, newFloorType.Id, levelId);
#endif          
            }
            catch
            {
#if REVIT2019 || REVIT2020 || REVIT2021
                slab = Doc.Create.NewFloor(profile, floorType, Doc.GetElement(levelId) as Level, true);
#else
                IList<CurveLoop> curveLoops = new List<CurveLoop> { profileLoop };
                slab = Floor.Create(GlobalVariables.Document, curveLoops, floorType.Id, levelId);
#endif
            }

            // Rotate and move the slab
            //rotate();

            //bool result = slab.Location.Move(centerPosition);

            //if (!result) throw new Exception("Move slab location failed.");

            //this.setType(floorType);

            Level level = Doc.GetElement(levelId) as Level;
            //setHeight(level);

            //Doc.Regenerate();
        }

        private void CreateHoles(List<List<XYZ>> holes)
        {
            foreach (var hole in holes)
            {
                var holeProfile = getProfile(hole);
                try
                {
                    GlobalVariables.Document.Create.NewOpening(slab, holeProfile, true);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Could not create hole with error: ", e);
                }
            }
        }

        private CurveArray getProfile(List<XYZ> vertices)
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
                    // This can be potentially handled on snaptrude side by sending correct vertices.Currently, some points are duplicate.
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

        //private void rotate()
        //{
        //    Location position = this.slab.Location;
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

        private void setHeight(Level level)
        {
            double bottomZ = faceVertices[0].Z/* * Scaling.Z*/;
            double slabHeightAboveLevel = centerPosition.Z + bottomZ - level.Elevation + thickness;

            slab
                .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
                .Set(slabHeightAboveLevel);
        }

        private List<TrudeLayer> createLayers(double fallbackThickness = 25)
        {

            // TODO: handle existing revit data

            List<TrudeLayer> stLayers = new List<TrudeLayer>();

            return null;
        }

        public void ApplyMaterialByFace(Document document, String materialNameWithId, List<SubMeshProperties> subMeshes, JArray materials, JArray multiMaterials, Floor slab)
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
                    if (matName.Replace("_", "") == _materialName.Replace("_", ""))
                    {
                        _materialElement = materialElement;
                    }
                }
                if (_materialElement != null)
                {
                    document.Paint(slab.Id, face.Key, _materialElement.Id);
                }
            }

        }

        public void ApplyMaterialByObject(Document document, Floor slab, Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine
            };

            // Obtain geometry for the given slab element
            GeometryElement geoElem = slab.get_Geometry(geoOptions);

            // Find a face on the slab
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
                document.Paint(slab.Id, face, material.Id);
            }
        }


        public GeometryElement GetGeometryElement()
        {
            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;
            geoOptions.ComputeReferences = true;

            return slab.get_Geometry(geoOptions);
        }

    }
}