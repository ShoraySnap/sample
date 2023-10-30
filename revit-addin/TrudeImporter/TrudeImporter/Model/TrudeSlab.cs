﻿using Autodesk.Revit.DB;
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
        /// <param name="slabprops"></param>
        /// <param name="levelId"></param>
        /// <param name="forForge"></param>
        public TrudeSlab(SlabProperties slabprops, bool forForge = false)
        {
            thickness = slabprops.Thickness;
            baseType = slabprops.BaseType;
            materialName = slabprops.MaterialName;
            System.Diagnostics.Debug.WriteLine(materialName);
            levelId = GlobalVariables.LevelIdByNumber[slabprops.Storey];
            centerPosition = slabprops.CenterPosition;
            // To fix height offset issue, this can fixed from snaptude side by sending top face vertices instead but that might or might not introduce further issues
            foreach (var v in slabprops.FaceVertices)
            {
                faceVertices.Add(v + new XYZ(0, 0, thickness));
            }

            // get existing slab id from revit meta data if already exists else set it to null
            if (slabprops.ExistingElementId != null)
            {
                Floor existingFloor = GlobalVariables.Document.GetElement(new ElementId((int)slabprops.ExistingElementId)) as Floor;
                existingFloorType = existingFloor.FloorType;
            }
            var _layers = new List<TrudeLayer>();
            //you can improve this section 
            // --------------------------------------------
            if (slabprops.Layers != null)
            {
                foreach (var layer in slabprops.Layers)
                {
                    _layers.Add(new TrudeLayer(slabprops.BaseType, layer.Name, layer.ThicknessInMm, layer.IsCore));
                }
            }
            Layers = _layers.ToArray();
            setCoreLayerIfNotExist(Math.Abs(thickness));
            // --------------------------------------------
            CreateSlab(levelId, int.Parse(GlobalVariables.RvtApp.VersionNumber) >= 2023);
            GlobalVariables.Document.Regenerate();
            CreateHoles(slabprops.Holes);
            GlobalVariables.Document.Regenerate();

            try
            {
                if (slabprops.SubMeshes.Count == 1)
                {
                    int _materialIndex = slabprops.SubMeshes.First().MaterialIndex;
                    String snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                        slabprops.MaterialName,
                        GlobalVariables.materials,
                        GlobalVariables.multiMaterials,
                        _materialIndex);
                    snaptrudeMaterialName = snaptrudeMaterialName.Replace(" ", "");
                    snaptrudeMaterialName = snaptrudeMaterialName.Replace("_", "");

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
                            System.Diagnostics.Debug.WriteLine(matName);
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
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Material not found, creating new");
                        string path = "C:\\Users\\shory\\OneDrive\\Documents\\snaptrudemanager\\revit-addin\\TrudeImporter\\TrudeImporter\\Model\\metal.jpg";
                        Material newmat = GlobalVariables.CreateMaterial(GlobalVariables.Document, "newMetal", path);
                        newmat.Transparency = 30;
                        this.ApplyMaterialByObject(GlobalVariables.Document, this.slab, newmat);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Multiple submeshes detected. Material application by face is currently disabled.");
                    //this.ApplyMaterialByFace(GlobalVariables.Document, props.MaterialName, props.SubMeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, this.wall);
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
            FloorType floorType = existingFloorType;

            var Doc = GlobalVariables.Document;
            if (floorType is null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(Doc).OfClass(typeof(FloorType));
                FloorType defaultFloorType = collector.Where(type => ((FloorType)type).FamilyName == "Floor").First() as FloorType;
                floorType = defaultFloorType;
            }
            var newFloorType = TypeStore.GetType(Layers, Doc, floorType);
            try
            {
                slab = Doc.Create.NewFloor(profile, newFloorType, Doc.GetElement(levelId) as Level, true);
            }
            catch
            {
                slab = Doc.Create.NewFloor(profile, floorType, Doc.GetElement(levelId) as Level, true);
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
    }
}