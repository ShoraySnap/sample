using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Material = Autodesk.Revit.DB.Material;

namespace TrudeImporter
{
    public class TrudeWall : TrudeModel
    {
        public TrudeLayer[] Layers;

        public Wall wall { get; set; }
        public static WallTypeStore TypeStore = new WallTypeStore();

        public TrudeWall(WallProperties wallProps)
        {
            try
            {
                Wall existingWall = null;
                ElementId existingLevelId = null;
                WallType existingWallType = null;
                if (!GlobalVariables.ForForge && wallProps.ExistingElementId != null)
                {
                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                    {
                        try
                        {
                            t.Start();

                            Element e;
                            bool isExistingWall = GlobalVariables.idToElement.TryGetValue(wallProps.ExistingElementId.ToString(), out e);
                            if (isExistingWall)
                            {
                                existingWall = (Wall)e;
                                existingLevelId = existingWall.LevelId;
                                existingWallType = existingWall.WallType;

                                t.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            Utils.LogTrace(e.Message);
                        }
                    }
                }

                using (SubTransaction trans = new SubTransaction(GlobalVariables.Document))
                {
                    trans.Start();
                    try
                    {
                        double baseHeight = wallProps.BaseHeight;
                        double height = wallProps.Height;

                        bool useOriginalMesh = false;

                        this.Layers = wallProps.Layers.Select(layer => layer.ToTrudeLayer(wallProps.Type)).ToArray();

                        this.levelNumber = (int)wallProps.Storey;

                        IList<Curve> profile = TrudeWall.GetProfile(wallProps.ProfilePoints);

                        //TODO remove this loop after wall core layer thickness is fixed after doing freemove
                        if (!wallProps.ThicknessInMm.IsNull())
                        {
                            bool coreIsFound = false;
                            for (int i = 0; i < this.Layers.Length; i++)
                            {
                                if (this.Layers[i].IsCore)
                                {
                                    coreIsFound = true;
                                    this.Layers[i].ThicknessInMm = (double)wallProps.ThicknessInMm;
                                }
                            }

                            if (!coreIsFound)
                            {
                                int index = (int)(this.Layers.Length / 2);

                                this.Layers[index].IsCore = true;
                                this.Layers[index].ThicknessInMm = (double)wallProps.ThicknessInMm;
                            }
                        }

                        ElementId levelIdForWall;
                        levelIdForWall = GlobalVariables.LevelIdByNumber[this.levelNumber];
                        Level level = (Level)GlobalVariables.Document.GetElement(levelIdForWall);

                        if (existingWall == null)
                        {
                            string familyName = wallProps.RevitFamily;

                            FilteredElementCollector collector = new FilteredElementCollector(GlobalVariables.Document).OfClass(typeof(WallType));
                            WallType wallType = collector.Where(wt => ((WallType)wt).Name == familyName) as WallType;

                            foreach (WallType wt in collector.ToElements())
                            {
                                if (wt.Name == familyName)
                                {
                                    wallType = wt;
                                    break;
                                }
                            }

                            if (wallType is null)
                            {
                                wallType = TrudeWall.GetWallTypeByWallLayers(this.Layers, GlobalVariables.Document);
                            }
                            else if (!AreLayersSame(this.Layers, wallType) && !wallProps.IsStackedWallParent)
                            {
                                wallType = TrudeWall.GetWallTypeByWallLayers(this.Layers, GlobalVariables.Document);
                            }

                            if (wallProps.IsStackedWallParent)
                            {
                                this.wall = this.CreateWall(GlobalVariables.Document, profile, wallType.Id, level);
                            }
                            else
                            {
                                this.wall = this.CreateWall(GlobalVariables.Document, profile, wallType.Id, level, height, baseHeight);
                            }
                        }
                        else
                        {
                            bool areLayersSame = AreLayersSame(this.Layers, existingWallType);

                            if (areLayersSame || wallProps.IsStackedWallParent)
                            {
                                this.wall = this.CreateWall(GlobalVariables.Document, profile, existingWallType.Id, level, height, baseHeight);
                                if (wallProps.IsStackedWallParent)
                                {
                                    var existingHeightParam = existingWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                                    var newHeightParam = this.wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                                    if (!newHeightParam.IsReadOnly) newHeightParam.SetValueString(existingHeightParam.AsValueString());
                                }
                            }
                            else
                            {
                                WallType wallType = TrudeWall.GetWallTypeByWallLayers(this.Layers, GlobalVariables.Document, existingWallType);

                                this.wall = this.CreateWall(GlobalVariables.Document, profile, wallType.Id, level, height, baseHeight);
                            }
                        }
                        ElementId wallId = this.wall.Id;

                        // Create holes
                        GlobalVariables.Document.Regenerate();

                        foreach (List<XYZ> hole in wallProps.Holes)
                        {
                            try
                            {
                                // Create cutting family
                                VoidRfaGenerator voidRfaGenerator = new VoidRfaGenerator();
                                string familyName = "snaptrudeVoidFamily" + Utils.RandomString();
                                Plane plane = Plane.CreateByThreePoints(wallProps.ProfilePoints[0], wallProps.ProfilePoints[1], wallProps.ProfilePoints[2]);

                                // Project points on to the plane to make sure all the points are co-planar.
                                // In some cases, the points coming in from snaptrude are not co-planar due to reasons unknown, 
                                // this is especially true for walls that are rotated.
                                List<XYZ> projectedPoints = new List<XYZ>();
                                projectedPoints = hole.Select(p => plane.ProjectOnto(p)).ToList();

                                voidRfaGenerator.CreateRFAFile(GlobalVariables.RvtApp, familyName, projectedPoints, this.wall.WallType.Width, plane);
                                GlobalVariables.Document.LoadFamily(voidRfaGenerator.fileName(familyName), out Family beamFamily);

                                FamilySymbol cuttingFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName);

                                if (!cuttingFamilySymbol.IsActive) cuttingFamilySymbol.Activate();

                                FamilyInstance cuttingFamilyInstance = GlobalVariables.Document.Create.NewFamilyInstance(
                                    XYZ.Zero,
                                    cuttingFamilySymbol,
                                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                                InstanceVoidCutUtils.AddInstanceVoidCut(GlobalVariables.Document, this.wall, cuttingFamilyInstance);
                            }
                            catch
                            {

                            }
                        }

                        GlobalVariables.Document.Regenerate();
                        try
                        {
                            if (wallProps.SubMeshes.Count == 1)
                            {
                                int _materialIndex = wallProps.SubMeshes.First().MaterialIndex;
                                String snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                                    wallProps.MaterialName,
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
                                    this.ApplyMaterialByObject(GlobalVariables.Document, this.wall, _materialElement);
                                }
                            }
                            else
                            {
                                this.ApplyMaterialByFace(GlobalVariables.Document, wallProps.MaterialName, wallProps.SubMeshes, GlobalVariables.materials, GlobalVariables.multiMaterials, this.wall);
                            }
                        }
                        catch
                        {
                            Utils.LogTrace("Failed to set wall material");
                        }

                        WallType _wallType = this.wall.WallType;

                        // Uncomment if you dont want to join walls
                        //WallUtils.DisallowWallJoinAtEnd(this.wall, 0);
                        //WallUtils.DisallowWallJoinAtEnd(this.wall, 1);

                        TransactionStatus transactionStatus = trans.Commit();

                        // For some reason in a few rare cases, some transactions rolledback when walls are joined.
                        // This handles those cases to create the wall without being joined.
                        // This is not a perfect solution, ideally wall should be joined.
                        if (transactionStatus == TransactionStatus.RolledBack)
                        {
                            trans.Start();
                            this.CreateWall(GlobalVariables.Document, profile, _wallType.Id, level, height, baseHeight);
                            wallId = this.wall.Id;

                            WallUtils.DisallowWallJoinAtEnd(this.wall, 0);
                            WallUtils.DisallowWallJoinAtEnd(this.wall, 1);

                            transactionStatus = trans.Commit();
                        }

                        Utils.LogTrace("wall created");

                        foreach (JToken childUID in wallProps.ChildrenUniqueIds)
                        {
                            GlobalVariables.childUniqueIdToWallElementId.Add((int)childUID, wallId);
                        }

                        using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                        {
                            t.Start();

                            if (!GlobalVariables.ForForge && wallProps.SourceElementId != null)
                            {
                                Element element = GlobalVariables.Document.GetElement(wallProps.SourceElementId);
                                if (element != null)
                                {
                                    GlobalVariables.Document.Delete(new ElementId(int.Parse(wallProps.SourceElementId)));
                                }
                            }

                            if (existingWall != null)
                            {
                                try
                                {
                                    var val = GlobalVariables.Document.Delete(existingWall.Id);
                                }
                                catch { }
                            }

                            var transstatus = t.Commit();

                            Utils.LogTrace(transstatus.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Utils.LogTrace("Error in creating wall", e.ToString());
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogTrace(e.Message);
                throw e;

            }
        }

        public static IList<Curve> GetProfile(List<XYZ> points)
        {
            IList<Curve> profile = new List<Curve>();

            for (int i = 0; i < points.Count(); i++)
            {
                XYZ point0 = points[i];
                XYZ point1 = points[(i + 1).Mod(points.Count())];

                if (point1.IsAlmostEqualTo(point0))
                {
                    point1 = points[(i + 2).Mod(points.Count())];
                }
                profile.Add(Line.CreateBound(point0, point1));
            }

            return profile;
        }

        public Wall CreateWall(Document newDoc, IList<Curve> profile, ElementId wallTypeId, Level level, double height = -1, double baseOffset = 0)
        {
            wall = Wall.Create(newDoc, profile, wallTypeId, level.Id, false);

            if (height > 0)
            {
                Parameter top_constraint_param = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                top_constraint_param.Set(ElementId.InvalidElementId);

                Parameter heightParam = wall.LookupParameter("Unconnected Height");
                heightParam.Set(height);

                Parameter baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                baseOffsetParam.Set(baseOffset - level.ProjectElevation);
            }
            else
            {
                Parameter top_constraint_param = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                //top_constraint_param.Set(Command.GlobalVariables.GlobalVariables.LevelIdByNumber[levelNumber + 1]); // TODO: fix this
            }

            return wall;
        }

        public static void JoinAllWalls(FilteredElementCollector walls, Wall wall, Document doc)
        {
            BoundingBoxXYZ bb = wall.get_BoundingBox(doc.ActiveView);
            Outline outline = new Outline(bb.Min, bb.Max);
            BoundingBoxIntersectsFilter bbfilter = new BoundingBoxIntersectsFilter(outline);
            walls.WherePasses(bbfilter);
            foreach (Wall w in walls)
            {
                if (!(JoinGeometryUtils.AreElementsJoined(doc, wall, w)))
                {
                    JoinGeometryUtils.JoinGeometry(doc, wall, w);
                }
            }
        }
        /// <summary>
        /// Paint wall faces.
        /// </summary>
        /// <param name="wall">Wall to be painted.</param>
        /// <param name="matId">ElementId of the material to paint on the wall.</param>
        /// <param name="doc">Revit document under process.</param>
        public void PaintWallFaces(Wall wall, ElementId matId, Document doc)
        {
            GeometryElement geometryElement = wall.get_Geometry(new Options());
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (geometryObject is Solid)
                {
                    Solid solid = geometryObject as Solid;
                    foreach (Face face in solid.Faces)
                    {
                        if (doc.IsPainted(wall.Id, face) == false)
                        {
                            doc.Paint(wall.Id, face, matId);
                        }
                    }
                }
            }
        }

        public GeometryElement GetGeometryElement()
        {
            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;
            geoOptions.ComputeReferences = true;

            return wall.get_Geometry(geoOptions);
        }

        public FaceArray GetFaces()
        {
            IEnumerator<GeometryObject> geoObjectItor = GetGeometryElement().GetEnumerator();
            while (geoObjectItor.MoveNext())
            {
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid) return theSolid.Faces;
            }

            return null;
        }

        public void ApplyMaterialByFace(Document document, String materialNameWithId, List<SubMeshProperties> subMeshes, JArray materials, JArray multiMaterials, Wall wall)
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
                    if (matName == _materialName)
                    {
                        _materialElement = materialElement;
                    }
                }
                if (_materialElement != null)
                {
                    document.Paint(wall.Id, face.Key, _materialElement.Id);
                }
            }

        }

        public void ApplyMaterialByObject(Document document, Wall wall, Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine
            };

            // Obtain geometry for the given Wall element
            GeometryElement geoElem = wall.get_Geometry(geoOptions);

            // Find a face on the wall
            //Face wallFace = null;
            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> wallFaces = new List<Face>();

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
                        wallFaces.Add(face);
                    }
                }
            }

            //loop through all the faces and paint them


            foreach (Face face in wallFaces)
            {
                document.Paint(wall.Id, face, material.Id);
            }
        }

        public static WallType GetWallTypeByWallLayers(TrudeLayer[] layers, Document doc, WallType existingWallType = null)
        {
            WallType defaultType = null;
            if (!(existingWallType is null)) defaultType = existingWallType;

            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(WallType));
            if (defaultType is null) defaultType = collector.Where(wallType => ((WallType)wallType).Kind == WallKind.Basic) as WallType;
            if (defaultType is null) defaultType = collector.Where(wallType => ((WallType)wallType).Kind == WallKind.Stacked) as WallType;
            if (defaultType is null)
                foreach (WallType wt in collector.ToElements())
                {
                    if (wt.Kind == WallKind.Basic)
                    {
                        defaultType = wt;
                        break;
                    }
                }
            if (defaultType is null) defaultType = collector.First() as WallType;

            try
            {
                return TypeStore.GetType(layers, defaultType);
            }
            catch (Exception e)
            {
                return defaultType;
            }
        }
        public bool AreLayersSame(TrudeLayer[] stLayers, WallType wallType)
        {
            try
            {
                CompoundStructure compoundStructure = wallType.GetCompoundStructure();
                if (compoundStructure == null) return true; // TODO: find a way to handle walls without compoundStructure

                CompoundStructureLayer coreLayer = compoundStructure.GetCoreLayer();

                if (coreLayer is null)
                {
                    int coreLayerIndex = compoundStructure.GetFirstCoreLayerIndex();
                    coreLayer = compoundStructure.GetLayers()[coreLayerIndex];
                }
                if (coreLayer is null)
                {
                    int coreLayerIndex = compoundStructure.GetLastCoreLayerIndex();
                    coreLayer = compoundStructure.GetLayers()[coreLayerIndex];
                }
                if (coreLayer is null)
                {
                    return false;
                }

                TrudeLayer stCoreLayer = null;
                for (int i = 0; i < stLayers.Length; i++)
                {
                    TrudeLayer stLayer = stLayers[i];

                    if (stLayer.IsCore)
                    {
                        stCoreLayer = stLayer;
                        break;
                    }

                }

                if (stCoreLayer is null) return false;

                double coreLayerThicknessInMm = UnitsAdapter.FeetToMM(coreLayer.Width);
                if (stCoreLayer.ThicknessInMm.AlmostEquals(coreLayerThicknessInMm, 0.5)) return true;

                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
