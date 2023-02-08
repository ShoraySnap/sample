﻿using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Media.Media3D;

namespace Snaptrude
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TrudeImporter : IExternalCommand
    {
        public static IDictionary<int, ElementId> LevelIdByNumber = new Dictionary<int, ElementId>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            uiapp.Application.FailuresProcessing += Application_FailuresProcessing;
            try
            {
                bool status = false;
                using(Transaction t = new Transaction(doc, "Parse Trude"))
                {
                    t.Start();
                    status = ParseTrude(uiapp, doc);
                    t.Commit();
                }

                if (status) ShowSuccessDialogue();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("catch", ex.ToString());
                return Result.Failed;
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
            }
        }

        void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor fa = e.GetFailuresAccessor();

            fa.DeleteAllWarnings();
        }

        private bool ParseTrude(UIApplication rvtApp, Document newDoc)
        {
            FileOpenDialog trudeFileOpenDialog = new FileOpenDialog("Trude (*.trude)|*.trude");

            trudeFileOpenDialog.Show();

            if (trudeFileOpenDialog.GetSelectedModelPath() == null)
            {
                ShowTrudeFileNotSelectedDialogue();

                return false;
            }

            String path = ModelPathUtils.ConvertModelPathToUserVisiblePath(trudeFileOpenDialog.GetSelectedModelPath());

            JObject structureCollection = JObject.Parse(File.ReadAllText(path));

            UnitsAdapter.metricSystem = (int)structureCollection.GetValue("userSettings")["unitsType"];

            GlobalVariables.RvtApp = rvtApp.Application;
            GlobalVariables.Document = newDoc;

            ImportSnaptrude(structureCollection, newDoc);

            LoadedFamilies.Clear();

            return true;
        }

        private static int countTotalElement(JObject jObject)
        {
            int totalElements = 0;

            if (jObject["metadata"] != null && jObject["metadata"]["revitDeleteIds"] != null)
            {

                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    foreach (JToken elementId in jObject["metadata"]["revitDeleteIds"])
                    {
                        try
                        {
                            ElementId id = new ElementId((int)elementId);
                            GlobalVariables.Document.Delete(id);
                        }
                        catch (Exception e)
                        {
                            LogTrace(e.Message);
                        }
                    }
                    t.Commit();
                }
            }

            foreach (JToken structure in jObject.GetValue("structures"))
            {
                JToken structureData = structure.First;

                // STOREYS
                JToken storeys = structureData["storeys"];
                if (!storeys.HasValues) continue;
                totalElements += storeys.Count();

                JToken geometryParent = structureData["01"];
                if (geometryParent is null) continue;

                //WALLS
                JToken walls = geometryParent["walls"];
                totalElements += walls.Count();

                //FLOORS
                JToken floors = geometryParent["floors"];
                totalElements += floors.Count();

                //BASE FLOORS AND INTERMEDIATE FLOORS
                JToken roofs = geometryParent["roofs"];
                totalElements += roofs.Count();

                // Columns and Beams
                JToken masses = geometryParent["masses"];
                totalElements += masses.Count();

                //DOORS
                JToken doors = geometryParent["doors"];
                totalElements += doors.Count();

                //WINDOWS
                JToken windows = geometryParent["windows"];
                totalElements += windows.Count();

                //STAIRCASES
                JToken stairs = geometryParent["staircases"];
                totalElements += stairs.Count();

                //FURNITURES
                JToken furnitures = geometryParent["furnitures"];
                totalElements += furnitures.Count();
            }

            return totalElements;
        }

        /// <summary>
        /// Create a data structure, attach it to a wall, 
        /// populate it with data, and retrieve the data 
        /// back from the wall
        /// </summary>
        public void StoreDataInElement(Element element, string dataToStore)
        {
            //Transaction createSchemaAndStoreData = new Transaction(wall.Document, "tCreateAndStore");

            //createSchemaAndStoreData.Start();
            SchemaBuilder schemaBuilder = new SchemaBuilder(new Guid("720080CB-DA99-40DC-9415-E53F280AA1F0"));

            // allow anyone to read the object
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

            // restrict writing to this vendor only
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);

            // required because of restricted write-access
            schemaBuilder.SetVendorId("ADSK");

            // create a field to store an XYZ
            FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("snaptrudeId", typeof(String));

            //fieldBuilder.SetUnitType(UnitType.UT_Length);

            fieldBuilder.SetDocumentation("A stored location value representing a wiring splice in a wall.");

            schemaBuilder.SetSchemaName("snaptrudeId");

            Schema schema = schemaBuilder.Finish(); // register the Schema object

            // create an entity (object) for this schema (class)
            Entity entity = new Entity(schema);

            // get the field from the schema
            Field fieldSpliceLocation = schema.GetField("snaptrudeId");

            entity.Set<string>(fieldSpliceLocation, dataToStore); // set the value for this entity

            element.SetEntity(entity); // store the entity in the element

            // get the data back from the wall
            Entity retrievedEntity = element.GetEntity(schema);

            string retrievedData = retrievedEntity.Get<string>(schema.GetField("snaptrudeId"));

            //createSchemaAndStoreData.Commit();
        }

        private string GetDataFromElement(Element element)
        {
            return null;

            SchemaBuilder schemaBuilder = new SchemaBuilder(new Guid("720080CB-DA99-40DC-9415-E53F280AA1F0"));

            // allow anyone to read the object
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

            // restrict writing to this vendor only
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);

            // required because of restricted write-access
            schemaBuilder.SetVendorId("ADSK");

            // create a field to store an XYZ
            FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("snaptrudeId", typeof(String));

            //fieldBuilder.SetUnitType(UnitType.UT_Length);

            fieldBuilder.SetDocumentation("A stored location value representing a wiring splice in a wall.");

            schemaBuilder.SetSchemaName("snaptrudeId");

            Schema schema = schemaBuilder.Finish(); // register the Schema object
            Entity retrievedEntity = element.GetEntity(schema);

            try
            {
                string retrievedData = retrievedEntity.Get<string>(schema.GetField("snaptrudeId"));

                return retrievedData;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException e)
            {
                LogTrace(e.Message);
                return null;
            }
        }
        public bool AreLayersSame(ST_Layer[] stLayers, FloorType wallType)
        {
            CompoundStructure compoundStructure = wallType.GetCompoundStructure();
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();

            if (stLayers.Length != layers.Count) return false;

            bool areSame = true;
            for (int i = 0; i < stLayers.Length; i++)
            {
                ST_Layer stLayer = stLayers[i];
                CompoundStructureLayer layer = layers[i];

                if (Math.Round(stLayer.ThicknessInMm) != UnitsAdapter.FeetToMM(layer.Width))
                {
                    //if (stLayer.IsCore && layer.Function == MaterialFunctionAssignment.Structure) continue; // TODO: remove this after core thickness is fixed on snaptrude end. 

                    areSame = false;
                    break;
                }
            }

            return areSame;
        }

        public bool AreLayersSame(ST_Layer[] stLayers, WallType wallType)
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

                ST_Layer stCoreLayer = null;
                for (int i = 0; i < stLayers.Length; i++)
                {
                    ST_Layer stLayer = stLayers[i];

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

        private void ImportSnaptrude(JObject jObject, Document newDoc)
        {

            int totalElements = countTotalElement(jObject);
            int processedElements = 0;

            Dictionary<String, Element> idToElement = new Dictionary<String, Element>();
            Dictionary<String, FamilySymbol> idToFamilySymbol = new Dictionary<String, FamilySymbol>();

            try
            {
                List<Element> existingElements = ST_Abstract.GetAllElements(newDoc);

                foreach (Element e in existingElements)
                {
                    string id = GetDataFromElement(e);
                    if (id is null)
                    {
                        id = e.Id.ToString();
                    }
                    idToElement.Add(id, e);

                    try
                    {
                        if (e.GetType().Name == "Wall") continue;
                        idToFamilySymbol.Add(id, ((FamilyInstance)e).Symbol);
                    } catch
                    {

                    }
                }
            }
            catch (Exception e)
            {
                LogTrace(e.Message);
            }

            foreach (JToken structure in jObject.GetValue("structures"))
            {
                JToken structureData = structure.First;

                // STOREYS
                Level baseLevel = new FilteredElementCollector(newDoc).OfClass(typeof(Level)).FirstElement() as Level;

                LevelIdByNumber.Clear();
                //LevelIdByNumber.Add(1, baseLevelId);

                JToken storeys = structureData["storeys"];
                if (!storeys.HasValues) continue;

                foreach (JToken storey in storeys)
                {
                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    JToken storeyData = storey.First;
                    ST_Storey newStorey = new ST_Storey(storeyData);

                    if (storeyData.IsNullOrEmpty())
                    {
                        try
                        {

                            using (SubTransaction t = new SubTransaction(newDoc))
                            {
                                t.Start();
                                newStorey.CreateLevel(newDoc);
                                LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);
                                t.Commit();
                            }

                        }
                        catch (Exception e)
                        {
                            LogTrace(e.Message);
                        }
                    }
                    else
                    {
                        if (storeyData["revitMetaData"].IsNullOrEmpty() || storeyData["revitMetaData"]["revitLowerLevel"].IsNullOrEmpty())
                        {
                            try
                            {

                                using (SubTransaction t = new SubTransaction(newDoc))
                                {
                                    t.Start();

                                    newStorey.CreateLevel(newDoc);
                                    LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);

                                    t.Commit();
                                }

                            }
                            catch (Exception e)
                            {
                                LogTrace(e.Message);
                            }
                        }
                        else
                        {
                            ElementId elementId = new ElementId((int)storeyData["revitMetaData"]["revitLowerLevel"]);
                            LevelIdByNumber.Add(newStorey.levelNumber, elementId);
                        }
                    }
                }
                LogTrace("storey created");

                JToken geometryParent = structureData["01"];
                if (geometryParent is null) continue;

                //WALLS
                JToken walls = geometryParent["walls"];
                int wallCount = 0;

                IDictionary<int, ElementId> childUniqueIdToWallElementId = new Dictionary<int, ElementId>();

                Dictionary<int, Exception> failedWalls = new Dictionary<int, Exception>();


                int wallsProcessed = 0;
                foreach (JToken wall in walls)
                {
                    wallsProcessed++;
                    if (!ShouldImport(wall)) continue;
                    if (IsStackedWall(wall) && !IsParentStackedWall(wall)) continue;
                    try
                    {

                        JToken wallData = wall.First;
                        int uniqueId = (int)wallData["dsProps"]["uniqueID"];

                        string revitId = (string)wallData["dsProps"]["revitMetaData"]["elementId"];

                        string sourceElementId = null;
                        try
                        {
                            if (!wallData["dsProps"]["revitMetaData"]["sourceElementId"].IsNullOrEmpty())
                                sourceElementId = (string)wallData["dsProps"]["revitMetaData"]["sourceElementId"];
                        }
                        catch { }

                        Wall existingWall = null;
                        ElementId existingLevelId = null;
                        WallType existingWallType = null;
                        if (revitId != null)
                        {
                            using (SubTransaction t = new SubTransaction(newDoc))
                            {
                                try
                                {
                                    t.Start();

                                    Element e;
                                    bool isExistingWall = idToElement.TryGetValue(revitId, out e);
                                    if (isExistingWall)
                                    {
                                        existingWall = (Wall)e;
                                        existingLevelId = existingWall.LevelId;
                                        existingWallType = existingWall.WallType;

                                        // delete original wall
                                        //var val = newDoc.Delete(existingWall.Id);
                                        t.Commit();
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogTrace(e.Message);
                                }
                            }
                        }

                        processedElements++;
                        LogProgress(processedElements, totalElements);

                        using (SubTransaction trans = new SubTransaction(newDoc))
                        {
                            ST_Wall st_wall = new ST_Wall();
                            trans.Start();
                            try
                            {
                                if (IsThrowAway(wallData))
                                {
                                    continue;
                                }

                                String _materialNameWithId = (String)wallData["meshes"][0]["materialId"];

                                if (_materialNameWithId == null || _materialNameWithId == String.Empty)
                                {
                                    _materialNameWithId = (String)wallData["materialName"];
                                }

                                JArray subMeshes = null;

                                if (wallData["meshes"][0]["subMeshes"].IsNullOrEmpty())
                                {
                                    subMeshes = wallData["subMeshes"].Value<JArray>();
                                }
                                else
                                {
                                    subMeshes = wallData["meshes"][0]["subMeshes"].Value<JArray>();
                                }

                                JArray _materials = jObject["materials"].Value<JArray>();
                                JArray _multiMaterials = jObject["multiMaterials"].Value<JArray>();

                                List<Point3D> profilePoints = new List<Point3D>();
                                
                                foreach(JToken pointArray in wallData["profile"])
                                {
                                    profilePoints.Add(STDataConverter.ArrayToXYZ(pointArray).ToPoint3D());
                                }

                                double baseHeight = UnitsAdapter.convertToRevit(wallData["baseHeight"]);
                                double height = UnitsAdapter.convertToRevit(wallData["height"]);

                                bool hasChildComponents = wallData["meshes"][0]["childrenComp"].HasValues;
                                bool useOriginalMesh = wallData["useOriginalMesh"] is null
                                                     ? hasChildComponents
                                                     : (bool)wallData["useOriginalMesh"];
                                useOriginalMesh = false;

                                JToken wallDsProps = wallData["dsProps"];

                                JToken wallMeshDataforLevel = wallData["meshes"].First;

                                JToken wallMeshData = useOriginalMesh
                                                    ? wallDsProps["originalWallMesh"]["meshes"].First
                                                    : wallData["meshes"].First;

                                st_wall.Name = wallMeshData["name"].ToString();

                                st_wall.Geom_ID = wallMeshData["geometryId"] is null
                                                ? "WallInstance"
                                                : wallMeshData["geometryId"].ToString();

                                st_wall.Position = useOriginalMesh
                                                 ? STDataConverter.ArrayToXYZ(wallMeshDataforLevel["position"])
                                                 : STDataConverter.ArrayToXYZ(wallMeshData["position"]);

                                float thickness = wallData["thickness"] is null ? -1 : (float) wallData["thickness"];

                                if (thickness > 0)
                                    st_wall.Layers = STDataConverter.GetLayers(wallData, thickness);
                                else
                                    st_wall.Layers = STDataConverter.GetLayers(wallData);

                                st_wall.Scaling = STDataConverter.GetScaling(wallData);

                                if (wallMeshDataforLevel["storey"] == null)
                                {
                                    continue;
                                }

                                st_wall.levelNumber = int.Parse(wallMeshDataforLevel["storey"].ToString());

                                List<XYZ> profilePointsXYZ = profilePoints.Select(p => p.ToXYZ()).ToList();
                                IList<Curve> profile = ST_Wall.GetProfile(profilePointsXYZ);

                                // Calculate and set thickness
                                string wallDirection = wallData["dsProps"]["direction"].Value<string>();

                                bool coreIsFound = false;
                                //TODO remove this loop after wall core layer thickness is fixed after doing freemove
                                for (int i = 0; i < st_wall.Layers.Length; i++)
                                {
                                    if (st_wall.Layers[i].IsCore)
                                    {
                                        coreIsFound = true;
                                        //st_wall.Layers[i].ThicknessInMm = UnitsAdapter.FeetToMM(thickness);
                                        if (thickness > 0) st_wall.Layers[i].ThicknessInMm = thickness;
                                    }
                                }

                                if (!coreIsFound)
                                {
                                    int index = (int)(st_wall.Layers.Length / 2);

                                    //double sumOfOtherThicknesses = 0;
                                    //for (int i = 0; i < st_wall.Layers.Length; i++)
                                    //{
                                    //    if (i == index) continue;

                                    //    sumOfOtherThicknesses += st_wall.Layers[i].ThicknessInMm;
                                    //}

                                    //st_wall.Layers[index].ThicknessInMm = UnitsAdapter.FeetToMM(thickness) - sumOfOtherThicknesses;
                                    st_wall.Layers[index].IsCore = true;
                                    if (thickness > 0) st_wall.Layers[index].ThicknessInMm = thickness;
                                }

                                ElementId levelIdForWall;
                                levelIdForWall = LevelIdByNumber[st_wall.levelNumber];
                                Level level = (Level) newDoc.GetElement(levelIdForWall);

                                if (existingWall == null)
                                {
                                    string familyName = (string)wallData["dsProps"]["revitMetaData"]["family"];

                                    FilteredElementCollector collector = new FilteredElementCollector(newDoc).OfClass(typeof(WallType));
                                    WallType wallType = collector.Where(wt => ((WallType)wt).Name == familyName) as WallType;

                                    foreach (WallType wt in collector.ToElements())
                                    {
                                        if (wt.Name == familyName)
                                        {
                                            wallType = wt;
                                            break;
                                        }
                                    }

                                    bool isWeworksMessedUpStackedWall = IsStackedWall(wall) && IsParentStackedWall(wall);

                                    if (wallType is null)
                                    {
                                        wallType = ST_Wall.GetWallTypeByWallLayers(st_wall.Layers, newDoc);
                                    }
                                    else if (!AreLayersSame(st_wall.Layers, wallType) && !isWeworksMessedUpStackedWall)
                                    {
                                        wallType = ST_Wall.GetWallTypeByWallLayers(st_wall.Layers, newDoc);
                                    }
                                    if (isWeworksMessedUpStackedWall)
                                    {
                                        st_wall.wall = st_wall.CreateWall(newDoc, profile, wallType.Id, level);
                                    }
                                    else
                                    {
                                        st_wall.wall = st_wall.CreateWall(newDoc, profile, wallType.Id, level, height, baseHeight);
                                    }

                                }
                                else
                                {
                                    bool areLayersSame = AreLayersSame(st_wall.Layers, existingWallType);
                                    bool isWeworksMessedUpStackedWall = IsStackedWall(wall) && IsParentStackedWall(wall);

                                    if (areLayersSame || isWeworksMessedUpStackedWall)
                                    {
                                        st_wall.wall = st_wall.CreateWall(newDoc, profile, existingWallType.Id, level, height, baseHeight);
                                        if (isWeworksMessedUpStackedWall)
                                        {
                                            var existingHeightParam = existingWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                                            var newHeightParam = st_wall.wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                                            if (!newHeightParam.IsReadOnly) newHeightParam.SetValueString(existingHeightParam.AsValueString());
                                        }
                                    }
                                    else
                                    {
                                        WallType wallType = ST_Wall.GetWallTypeByWallLayers(st_wall.Layers, newDoc, existingWallType);

                                        st_wall.wall = st_wall.CreateWall(newDoc, profile, wallType.Id, level, height, baseHeight);
                                    }
                                }
                                ElementId wallId = st_wall.wall.Id;

                                // Create holes
                                newDoc.Regenerate();

                                foreach (List<XYZ> hole in STDataConverter.GetHoles(wallData))
                                {
                                    try
                                    {
                                        // Create cutting family
                                        VoidRfaGenerator voidRfaGenerator = new VoidRfaGenerator();
                                        string familyName = "snaptrudeVoidFamily" + RandomString(4);
                                        Plane plane = Plane.CreateByThreePoints(profilePointsXYZ[0], profilePointsXYZ[1], profilePointsXYZ[2]);

                                        // Project points on to the plane to make sure all the points are co-planar.
                                        // In some cases, the points coming in from snaptrude are not co-planar due to reasons unknown, 
                                        // this is especially true for walls that are rotated.
                                        List<XYZ> projectedPoints = new List<XYZ>();
                                        projectedPoints = hole.Select(p => plane.ProjectOnto(p)).ToList();

                                        voidRfaGenerator.CreateRFAFile(GlobalVariables.RvtApp, familyName, projectedPoints, st_wall.wall.WallType.Width, plane);
                                        newDoc.LoadFamily(voidRfaGenerator.fileName(familyName), out Family beamFamily);

                                        FamilySymbol cuttingFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, familyName);

                                        if (!cuttingFamilySymbol.IsActive) cuttingFamilySymbol.Activate();

                                        FamilyInstance cuttingFamilyInstance = newDoc.Create.NewFamilyInstance(
                                            XYZ.Zero,
                                            cuttingFamilySymbol,
                                            Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                                        InstanceVoidCutUtils.AddInstanceVoidCut(newDoc, st_wall.wall, cuttingFamilyInstance);
                                    }
                                    catch
                                    {

                                    }
                                }

                                newDoc.Regenerate();
                                try
                                {
                                    if(subMeshes.Count == 1)
                                    {
                                        int _materialIndex = (int)subMeshes[0]["materialIndex"];
                                        String snaptrudeMaterialName = getMaterialNameFromMaterialId(
                                            _materialNameWithId,
                                            subMeshes,
                                            _materials,
                                            _multiMaterials,
                                            _materialIndex);

                                        FilteredElementCollector materialCollector =
                                            new FilteredElementCollector(newDoc)
                                            .OfClass(typeof(Autodesk.Revit.DB.Material));

                                        IEnumerable<Autodesk.Revit.DB.Material> materialsEnum = materialCollector.ToElements().Cast<Autodesk.Revit.DB.Material>();

                                        Autodesk.Revit.DB.Material _materialElement = null;

                                        foreach (var materialElement in materialsEnum)
                                        {
                                            String matName = materialElement.Name;
                                            if (matName.Replace("_", " ") == snaptrudeMaterialName)
                                            {
                                                _materialElement = materialElement;
                                                break;
                                            }
                                        }
                                        if (_materialElement is null && snaptrudeMaterialName.ToLower().Contains("glass")) {
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

                                        if (_materialElement != null)
                                        {
                                            st_wall.ApplyMaterialByObject(newDoc, st_wall.wall, _materialElement);
                                        }
                                       
                                    }
                                    else
                                    {
                                        st_wall.ApplyMaterialByFace(newDoc, _materialNameWithId, subMeshes, _materials, _multiMaterials, st_wall.wall);
                                    }
                                    
                                }
                                catch { }

                                WallType _wallType = st_wall.wall.WallType;

                                TransactionStatus transactionStatus = trans.Commit();

                                // For some reason in a few rare cases, some transactions rolledback when walls are joined.
                                // This handles those cases to create the wall without being joined.
                                // This is not a perfect solution, ideally wall should be joined.
                                if (transactionStatus == TransactionStatus.RolledBack)
                                {
                                    trans.Start();
                                    st_wall.CreateWall(newDoc, profile, _wallType.Id, level, height, baseHeight);
                                    wallId = st_wall.wall.Id;

                                    WallUtils.DisallowWallJoinAtEnd(st_wall.wall, 0);
                                    WallUtils.DisallowWallJoinAtEnd(st_wall.wall, 1);

                                    transactionStatus = trans.Commit();
                                }

                                LogTrace("wall created");
                                wallCount++;

                                foreach (JToken childUID in wallData["meshes"][0]["childrenComp"])
                                {
                                    childUniqueIdToWallElementId.Add((int)childUID, wallId);
                                }

                                using (SubTransaction t = new SubTransaction(newDoc))
                                {
                                    t.Start();

                                    if (sourceElementId != null)
                                    {
                                        newDoc.Delete(new ElementId(int.Parse(sourceElementId)));
                                    }

                                    if (existingWall != null)
                                    {
                                        try
                                        {
                                            var val = newDoc.Delete(existingWall.Id);
                                        }
                                        catch { }
                                    }

                                    var transstatus = t.Commit();

                                    LogTrace(transstatus.ToString());
                                }
                            }
                            catch (Exception e)
                            {
                                LogTrace("Error in creating wall", e.ToString());
                                failedWalls.Add(uniqueId, e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogTrace(e.Message);
                    }
                }
                ST_Wall.TypeStore.Clear();
                LogTrace("Walls Success");

                //FLOORS
                JToken floors = geometryParent["floors"];
                int count = 0;

                foreach (var floor in floors)
                {
                    if (!ShouldImport(floor)) continue;

                    var floorData = floor.First;

                    if (IsThrowAway(floorData)) { continue; }

                    String _materialNameWithId = (String)floorData["meshes"][0]["materialId"];

                    if (_materialNameWithId == null || _materialNameWithId == String.Empty)
                    {
                        _materialNameWithId = (String)floorData["materialName"];
                    }

                    JArray subMeshes = null;

                    if (floorData["meshes"][0]["subMeshes"].IsNullOrEmpty())
                    {
                        if (!floorData["subMeshes"].IsNullOrEmpty())
                        {
                            subMeshes = floorData["subMeshes"].Value<JArray>();
                        }
                    }
                    else
                    {
                        subMeshes = floorData["meshes"][0]["subMeshes"].Value<JArray>();
                    }

                    JArray _materials = jObject["materials"].Value<JArray>();
                    JArray _multiMaterials = jObject["multiMaterials"].Value<JArray>();

                    String _materialName = getMaterialNameFromMaterialId(_materialNameWithId, subMeshes, _materials, _multiMaterials, 0);

                    FilteredElementCollector collector1 = new FilteredElementCollector(newDoc).OfClass(typeof(Autodesk.Revit.DB.Material));

                    IEnumerable<Autodesk.Revit.DB.Material> materialsEnum = collector1.ToElements().Cast<Autodesk.Revit.DB.Material>();

                    /*
                                        Autodesk.Revit.DB.Material _materialElement = (from materialElement in materialsEnum
                                                                                       where materialElement.Name.Contains(_materialName)
                                                                                       select materialElement).FirstOrDefault();
                    */

                    Autodesk.Revit.DB.Material _materialElement = null;

                    foreach (var materialElement in materialsEnum)
                    {
                        String matName = materialElement.Name;

                        if (matName.Replace("_", " ") == _materialName)
                        {
                            _materialElement = materialElement;
                        }
                    }

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    using (SubTransaction transactionFloor = new SubTransaction(newDoc))
                    {
                        transactionFloor.Start();

                        FloorType existingFloorType = null;
                        int revitId;
                        ElementId existingElementId = null;

                        bool isExistingFloor = false;
                        try
                        {
                            if (!floorData["dsProps"]["revitMetaData"].IsNullOrEmpty())
                            {
                                isExistingFloor = true;
                                String _revitId = (String)floorData["dsProps"]["revitMetaData"]["elementId"];
                                revitId = (int)floorData["dsProps"]["revitMetaData"]["elementId"];
                                existingElementId = new ElementId(revitId);
                                Floor existingFloor = newDoc.GetElement(existingElementId) as Floor;
                                existingFloorType = existingFloor.FloorType;
                            }
                        }
                        catch
                        {

                        }

                        try
                        {
                            if (IsThrowAway(floorData))
                            {
                                continue;
                            }


                            ST_Floor st_floor = new ST_Floor(floorData, newDoc, existingFloorType);

                            try
                            {
                                List<List<XYZ>> holes = STDataConverter.GetHoles(floorData);

                                foreach (var hole in holes)
                                {
                                    var holeProfile = ST_Wall.GetProfile(hole);
                                    CurveArray curveArray1 = new CurveArray();
                                    foreach (Curve c in holeProfile)
                                    {
                                        curveArray1.Append(c);
                                    }
                                    newDoc.Create.NewOpening(st_floor.floor, curveArray1, true);
                                }
                            }
                            catch { }

                            if (_materialElement != null)
                            {
                                st_floor.ApplyPaintByMaterial(newDoc, st_floor.floor, _materialElement);
                            }

                            count++;

                            if (isExistingFloor)
                            {
                                try
                                {
                                    newDoc.Delete(existingElementId);
                                }
                                catch
                                {

                                }
                            }

                            TransactionStatus status = transactionFloor.Commit();
                        }
                        catch (Exception e)
                        {
                            LogTrace("Error in creating floorslab", e.ToString());
                        }
                    }
                }
                LogTrace("floors created");

                //BASE FLOORS AND INTERMEDIATE FLOORS
                JToken roofs = geometryParent["roofs"];
                count = 0;

                foreach (var roof in roofs)
                {
                    if (!ShouldImport(roof)) continue;

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    using (SubTransaction transactionRoofs = new SubTransaction(newDoc))
                    {
                        transactionRoofs.Start();

                        try
                        {
                            JToken roofData = roof.First;


                            FloorType existingFloorType = null;
                            int revitId;
                            ElementId existingElementId = null;

                            bool isExistingFloor = false;
                            try
                            {
                                if (!roofData["dsProps"]["revitMetaData"].IsNullOrEmpty())
                                {
                                    isExistingFloor = true;
                                    String _revitId = (String)roofData["dsProps"]["revitMetaData"]["elementId"];
                                    revitId = (int)roofData["dsProps"]["revitMetaData"]["elementId"];
                                    existingElementId = new ElementId(revitId);
                                    Floor existingFloor = newDoc.GetElement(existingElementId) as Floor;
                                    existingFloorType = existingFloor.FloorType;
                                }
                            }
                            catch
                            {

                            }

                            if (IsThrowAway(roofData))
                            {
                                continue;
                            }

                            ST_Roof st_roof = new ST_Roof(roofData, newDoc, existingFloorType);

                            try
                            {
                                List<List<XYZ>> holes = STDataConverter.GetHoles(roofData);

                                foreach (var hole in holes)
                                {
                                    var holeProfile = ST_Wall.GetProfile(hole);
                                    CurveArray curveArray1 = new CurveArray();
                                    foreach (Curve c in holeProfile)
                                    {
                                        curveArray1.Append(c);
                                    }
                                    newDoc.Create.NewOpening(st_roof.floor, curveArray1, true);
                                }
                            }
                            catch { }

                            count++;


                            if (isExistingFloor)
                            {
                                try
                                {
                                    newDoc.Delete(existingElementId);
                                }
                                catch
                                {

                                }
                            }

                            TransactionStatus status = transactionRoofs.Commit();
                        }
                        catch (Exception e)
                        {
                            LogTrace("Error in creating floorslab", e.ToString());
                        }
                    }
                }
                ST_Floor.TypeStore.Types.Clear();
                LogTrace("Roofs created");

                JToken ceilings = geometryParent["ceilings"];
                foreach (var ceiling in ceilings)
                {
                    if (!ShouldImport(ceiling)) continue;

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    try
                    {
                        JToken ceilingData = ceiling.First;

                        string revitId = (string)ceilingData["dsProps"]["revitMetaData"]["elementId"];

                        if (IsThrowAway(ceilingData)) continue;
                        if (ceilingData["dsProps"]["storey"].Value<String>() is null) continue;

                        if (ceilingData["dsProps"]["revitMetaData"]["curveId"] != null)
                        {
                            string curveId = (string)ceilingData["dsProps"]["revitMetaData"]["curveId"];

                            ElementId ceilingId = new ElementId(int.Parse(revitId));

                            Element ceilingElement = newDoc.GetElement(ceilingId);

                            //get ceiling sketch
                            ElementClassFilter filter = new ElementClassFilter(typeof(Sketch));

                            ElementId sketchId = ceilingElement.GetDependentElements(filter).First();

                            Sketch ceilingSketch = newDoc.GetElement(sketchId) as Sketch;

                            CurveArrArray ceilingProfile = ceilingSketch.Profile;

                            filter = new ElementClassFilter(typeof(CurveElement));

                            IEnumerable<Element> curves = ceilingElement.GetDependentElements(filter)
                                .Select(id => newDoc.GetElement(id));

                            IEnumerable<ModelLine> modelLines = curves.Where(e => e is ModelLine).Cast<ModelLine>();//target

                            if (curves.Count() != modelLines.Count())
                                throw new Exception("The ceiling contains non straight lines");



                            IList<IList<ModelLine>> editableSketch = new List<IList<ModelLine>>();

                            Dictionary<String, CurveArray> profiles = new Dictionary<string, CurveArray>();

                            foreach (CurveArray loop in ceilingProfile)
                            {
                                profiles.Add(loop.GenerateCurveId(revitId), loop);
                            }

                            List<XYZ> profilePoints = STDataConverter.ListToPoint3d(ceilingData["topVertices"])
                                .Distinct()
                                .Select((Point3D p) => p.ToXYZ())
                                .ToList();

                            Dictionary<String, List<XYZ>> allProfiles = new Dictionary<String, List<XYZ>>();

                            allProfiles.Add(curveId, profilePoints);

                            if (!ceilingData["voids"].IsNullOrEmpty())
                            {
                                foreach (var voidj in ceilingData["voids"])
                                {
                                    string key = (string)voidj["curveId"];
                                    List<XYZ> _profilePoints = STDataConverter.ListToPoint3d(voidj["profile"])
                                        .Select((Point3D p) => p.ToXYZ())
                                        .ToList();

                                    allProfiles.Add(key, _profilePoints);
                                }
                            }

                            foreach (CurveArray loop in ceilingProfile)
                            {
                                List<ModelLine> newLoop = new List<ModelLine>();

                                var currentLoopId = loop.GenerateCurveId(revitId);

                                if (!allProfiles.ContainsKey(currentLoopId)) continue;

                                var currentProfilePoints = allProfiles[currentLoopId];

                                foreach (Curve edge in loop)
                                {
                                    foreach (ModelLine modelLine in modelLines)
                                    {

                                        Curve currentLine = ((modelLine as ModelLine)
                                          .Location as LocationCurve).Curve;

                                        if (currentLine.Intersect(edge) == SetComparisonResult.Equal)
                                        {

                                            newLoop.Add(modelLine);
                                            break;
                                        }

                                        editableSketch.Add(newLoop);
                                    }
                                }

                                using (SubTransaction transaction = new SubTransaction(newDoc))
                                {
                                    transaction.Start();

                                    for (int i = 0; i < newLoop.Count(); i++)
                                    {
                                        LocationCurve lCurve = newLoop[i].Location as LocationCurve;

                                        XYZ pt1New = currentProfilePoints[i];
                                        XYZ pt2New = currentProfilePoints[(i + 1).Mod(newLoop.Count())];
                                        Line newLine = Line.CreateBound(pt1New, pt2New);
                                        lCurve.Curve = newLine;
                                    }

                                    transaction.Commit();
                                }
                            }

                            try
                            {
                                List<List<XYZ>> holes = STDataConverter.GetHoles(ceilingData);

                                foreach (var hole in holes)
                                {
                                    var holeProfile = ST_Wall.GetProfile(hole);
                                    CurveArray curveArray1 = new CurveArray();
                                    foreach (Curve c in holeProfile)
                                    {
                                        curveArray1.Append(c);
                                    }
                                    newDoc.Create.NewOpening(ceilingElement, curveArray1, true);
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            // TODO: Ceiling creation is not supported by revit 2019 API!!!! SMH FML
                        }

                        if (revitId != null)
                        {
                            try
                            {
                                Element e;
                                bool isExistingMass = idToElement.TryGetValue(revitId, out e);
                                if (isExistingMass)
                                {
                                    Element existingMass = e;
                                    ElementId existingLevelId = existingMass.LevelId;

                                    using (SubTransaction t = new SubTransaction(newDoc))
                                    {
                                        t.Start();
                                        var val = newDoc.Delete(existingMass.Id);
                                        t.Commit();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                LogTrace(e.Message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogTrace("Error Creating beam/column");
                    }
                }

                // Columns and Beams
                JToken masses = geometryParent["masses"];
                foreach (var mass in masses)
                {

                    if (!ShouldImport(mass)) continue;

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    try
                    {
                        JToken massData = mass.First;
                        JToken massMeshData = massData["meshes"].First;
                        JToken massGeometry = massData["geometries"];

                        string revitId = (string)massData["dsProps"]["revitMetaData"]["elementId"];

                        if (IsThrowAway(massData)) continue;
                        if (massGeometry is null) continue;
                        if (massData["dsProps"]["storey"].Value<String>() is null) continue;

                        string massType = massData["dsProps"]["massType"].Value<String>();
                        if (massType.Equals("Column"))
                        {
                            ST_Column.FromMassData(massData).CreateColumn(newDoc);
                        }
                        else if (massType.Equals("Beam"))
                        {
                            ST_Beam.FromMassData(massData).CreateBeam(newDoc);
                        }

                        if (revitId != null)
                        {
                            try
                            {
                                Element e;
                                bool isExistingMass = idToElement.TryGetValue(revitId, out e);
                                if (isExistingMass)
                                {
                                    Element existingMass = e;
                                    ElementId existingLevelId = existingMass.LevelId;

                                    using (SubTransaction t = new SubTransaction(newDoc))
                                    {
                                        t.Start();
                                        var val = newDoc.Delete(existingMass.Id);
                                        t.Commit();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                LogTrace(e.Message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogTrace("Error Creating beam/column");
                    }
                }
                ST_Column.NewLevelsByElevation.Clear();
                ST_Column.types.Clear();
                ST_Beam.types.Clear();

                LogTrace("beams and columns created");

                //DOORS
                JToken doors = geometryParent["doors"];
                foreach (var door in doors)
                {
                    if (!ShouldImport(door)) continue;

                    var doorData = door.First;
                    int uniqueId = (int)doorData["dsProps"]["uniqueID"];
                    string revitId = (string)doorData["dsProps"]["revitMetaData"]["elementId"];
                    string revitFamilyName = (string)doorData["dsProps"]["revitMetaData"]["family"];

                    if (IsThrowAway(doorData)) { continue; }

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    bool isExistingDoor = false;
                    FamilyInstance existingFamilyInstance = null;
                    FamilySymbol existingFamilySymbol = null;
                    if (revitId != null)
                    {
                        using (SubTransaction t = new SubTransaction(newDoc))
                        {
                            try
                            {
                                t.Start();

                                Element e;
                                isExistingDoor = idToElement.TryGetValue(revitId, out e);
                                if (isExistingDoor)
                                {
                                    isExistingDoor = true;
                                    existingFamilyInstance = (FamilyInstance)e;
                                    existingFamilySymbol = idToFamilySymbol[revitId];

                                    // delete original door
                                    if (existingFamilyInstance.IsValidObject) newDoc.Delete(existingFamilyInstance.Id);
                                    t.Commit();
                                }
                            }
                            catch (Exception e)
                            {
                                LogTrace(e.Message);
                            }
                        }
                    }
                    using (SubTransaction transaction = new SubTransaction(newDoc))
                    {
                        transaction.Start();
                        try
                        {
                            ST_Door st_door = new ST_Door();

                            JToken doorMeshData = doorData["meshes"].First;

                            double width = UnitsAdapter.convertToRevit(doorMeshData["width"]);
                            double height = UnitsAdapter.convertToRevit(doorMeshData["height"]);
                            XYZ direction = doorMeshData["direction"].IsNullOrEmpty()
                                ? XYZ.Zero
                                : STDataConverter.ArrayToXYZ(doorMeshData["direction"], false).Round();

                            st_door.Name = doorMeshData["name"].ToString();
                            st_door.Geom_ID = doorMeshData["storey"].ToString();
                            st_door.Position = STDataConverter.GetPosition(doorData);
                            st_door.family = doorMeshData["id"].ToString();
                            st_door.levelNumber = STDataConverter.GetLevelNumber(doorData);
                            ElementId levelIdForWall = LevelIdByNumber[st_door.levelNumber];

                            try
                            {
                                st_door.family = st_door.family.RemoveIns();

                                string fsFamilyName = st_door.family;
                                string fsName = st_door.family;

                                if (revitFamilyName != null)
                                {
                                    fsFamilyName = revitFamilyName;
                                    fsName = null;
                                }

                                Wall wall = null;
                                if (childUniqueIdToWallElementId.ContainsKey(uniqueId))
                                {
                                    ElementId wallElementId = childUniqueIdToWallElementId[uniqueId];
                                    wall = (Wall)newDoc.GetElement(wallElementId);
                                }


                                FamilySymbol familySymbol = null;
                                FamilySymbol defaultFamilySymbol = null;
                                if (isExistingDoor)
                                {
                                    defaultFamilySymbol = existingFamilySymbol;
                                    if (!defaultFamilySymbol.IsActive)
                                    {
                                        defaultFamilySymbol.Activate();
                                        newDoc.Regenerate();
                                    }
                                }
                                else
                                {
                                    if (revitFamilyName is null)
                                    {
                                        var family = LoadCustomDoorFamily(fsFamilyName);
                                        if (family is null)
                                        {
                                            LogTrace("couln't find door family");
                                            continue;
                                        }
                                    }

                                    defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, fsFamilyName, fsName);
                                }

                                if (!defaultFamilySymbol.IsActive)
                                {
                                    defaultFamilySymbol.Activate();
                                    newDoc.Regenerate();
                                }

                                // Check if familySymbol BuiltInParameter.DOOR_HEIGHT and  BuiltInParameter.DOOR_WIDTH
                                // if so, then set the height and with in the familySymbol itself, otherwise find the correct
                                // parameter in the instance.

                                Parameter heightTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
                                Parameter widthTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_WIDTH);

                                bool setHeightAndWidthParamsInFamilySymbol = (heightTypeParam.HasValue && widthTypeParam.HasValue) && (!heightTypeParam.IsReadOnly || !widthTypeParam.IsReadOnly);
                                if (setHeightAndWidthParamsInFamilySymbol)
                                {
                                    familySymbol = ST_Door.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
                                }
                                else
                                {
                                    familySymbol = defaultFamilySymbol;
                                }

                                st_door.CreateDoor(newDoc, familySymbol, levelIdForWall, wall, direction);

                                (Parameter widthInstanceParam, Parameter heightInstanceParam) = st_door.instance.FindWidthAndHeightParameters();
                                if (!setHeightAndWidthParamsInFamilySymbol)
                                {
                                    heightInstanceParam.Set(height);
                                    widthInstanceParam.Set(width);
                                }
                                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(height);
                                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(width);

                                var tstatus = transaction.Commit();
                            }
                            catch (Exception e)
                            {
                                LogTrace($"No door with name {st_door.family} {st_door.Name}");
                                LogTrace(e.Message);
                            }
                        }
                        catch (Exception e)
                        {
                            LogTrace("Error in creating door", e.ToString());
                        }
                    }
                }
                //transactionDoors.Commit();
                LogTrace("doors created");
                ST_Door.TypeStore.Clear();

                //WINDOWS

                JToken windows = geometryParent["windows"];
                foreach (var window in windows)
                {
                    if (!ShouldImport(window)) continue;

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    var windowData = window.First;
                    int uniqueId = (int)windowData["dsProps"]["uniqueID"];
                    string revitId = (string)windowData["dsProps"]["revitMetaData"]["elementId"];
                    string revitFamilyName = (string)windowData["dsProps"]["revitMetaData"]["family"];

                    if (IsThrowAway(windowData)) { continue; }

                    bool isExistingWindow = false;
                    FamilyInstance existingWindow = null;
                    FamilySymbol existingFamilySymbol = null;
                    using (SubTransaction t = new SubTransaction(newDoc))
                    {
                        try
                        {
                            t.Start();

                            Element e;
                            isExistingWindow = idToElement.TryGetValue(revitId, out e);

                            if (isExistingWindow)
                            {
                                isExistingWindow = true;
                                existingWindow = (FamilyInstance)e;
                                existingFamilySymbol = idToFamilySymbol[revitId];

                                // delete original window
                                if (existingWindow.IsValidObject) newDoc.Delete(existingWindow.Id);
                                t.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            LogTrace(e.Message);
                        }
                    }

                    using (SubTransaction transaction = new SubTransaction(newDoc))
                    {
                        transaction.Start();
                        try
                        {
                            ST_Window st_window = new ST_Window();

                            var windowMeshData = windowData["meshes"].First;

                            double width = UnitsAdapter.convertToRevit(windowMeshData["width"]);
                            double height = UnitsAdapter.convertToRevit(windowMeshData["height"]);

                            XYZ direction = windowMeshData["direction"].IsNullOrEmpty()
                                ? XYZ.Zero
                                : STDataConverter.ArrayToXYZ(windowMeshData["direction"], false).Round();

                            st_window.Name = windowMeshData["name"].ToString();
                            st_window.Geom_ID = windowMeshData["storey"].ToString();
                            st_window.Position = STDataConverter.GetPosition(windowData);
                            st_window.Scaling = STDataConverter.GetScaling(windowData);
                            st_window.Rotation = STDataConverter.GetRotation(windowData);
                            st_window.family = windowMeshData["id"].ToString();
                            //ElementId levelIdForWall = LevelIdByNumber[int.Parse(st_window.Geom_ID)];
                            st_window.levelNumber = STDataConverter.GetLevelNumber(windowData);
                            var levelIdForWall = LevelIdByNumber[st_window.levelNumber];
                            //ElementId levelIdForWall = LevelIdByNumber[1];

                            double heightScale = 1;
                            double widthScale = 1;
                            if (windowData["meshes"][0]["originalScaling"] != null) {
                                heightScale = double.Parse(windowData["meshes"][0]["scaling"][1].ToString()) / double.Parse(windowData["meshes"][0]["originalScaling"][1].ToString());
                                widthScale = double.Parse(windowData["meshes"][0]["scaling"][0].ToString()) / double.Parse(windowData["meshes"][0]["originalScaling"][0].ToString());
                            }
                            else
                            {
                                heightScale = double.Parse(windowData["meshes"][0]["scaling"][1].ToString());
                                widthScale = double.Parse(windowData["meshes"][0]["scaling"][0].ToString());
                            }

                            try
                            {
                                st_window.family = st_window.family.RemoveIns();

                                string fsFamilyName = st_window.family;
                                string fsName = st_window.family;

                                if (revitFamilyName != null)
                                {
                                    fsFamilyName = revitFamilyName;
                                    fsName = null;
                                }

                                Wall wall = null;
                                if (childUniqueIdToWallElementId.ContainsKey(uniqueId))
                                {
                                    ElementId wallElementId = childUniqueIdToWallElementId[uniqueId];
                                    wall = (Wall)newDoc.GetElement(wallElementId);
                                }

                                FamilySymbol familySymbol = null;
                                FamilySymbol defaultFamilySymbol = null;
                                if (isExistingWindow)
                                {
                                    defaultFamilySymbol = existingFamilySymbol;
                                    if (!defaultFamilySymbol.IsActive)
                                    {
                                        defaultFamilySymbol.Activate();
                                        newDoc.Regenerate();
                                    }

                                    familySymbol = ST_Window.TypeStore.GetType(new double[] { heightScale, widthScale }, defaultFamilySymbol);
                                }
                                else
                                {
                                    if (revitFamilyName is null)
                                    {
                                        LoadCustomWindowFamily(fsFamilyName);
                                    }

                                    defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, fsFamilyName, fsName);

                                    familySymbol = ST_Window.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
                                }

                                if (!defaultFamilySymbol.IsActive)
                                {
                                    defaultFamilySymbol.Activate();
                                    newDoc.Regenerate();
                                }

                                // Check if familySymbol BuiltInParameter.DOOR_HEIGHT and  BuiltInParameter.DOOR_WIDTH
                                // if so, then set the height and with in the familySymbol itself, otherwise find the correct
                                // parameter in the instance.

                                Parameter heightTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.WINDOW_HEIGHT);
                                Parameter widthTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH);

                                bool setHeightAndWidthParamsInFamilySymbol = (heightTypeParam.HasValue && widthTypeParam.HasValue) && (!heightTypeParam.IsReadOnly || !widthTypeParam.IsReadOnly);
                                if (setHeightAndWidthParamsInFamilySymbol)
                                {
                                    familySymbol = ST_Window.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
                                }
                                else
                                {
                                    familySymbol = defaultFamilySymbol;
                                }

                                st_window.CreateWindow(newDoc, familySymbol, levelIdForWall, wall, direction);

                                (Parameter widthInstanceParam, Parameter heightInstanceParam) = st_window.instance.FindWidthAndHeightParameters();
                                if (!setHeightAndWidthParamsInFamilySymbol)
                                {
                                    heightInstanceParam.Set(height);
                                    widthInstanceParam.Set(width);
                                }
                                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(height);
                                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(width);

                                var transactionStatus = transaction.Commit();
                            }
                            catch (Exception e)
                            {
                                LogTrace(e.Message);
                            }
                        }
                        catch (Exception exception)
                        {
                            LogTrace($"Error in creating window {exception}");
                        }
                    }
                }
                //transactionWindows.Commit();
                LogTrace("windows created");
                ST_Window.TypeStore.Clear();

                //STAIRCASES
                ST_Staircase st_staircase = new ST_Staircase();
                JToken stairs = geometryParent["staircases"];
                foreach (var stair in stairs)
                {
                    break;
                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    try
                    {
                        var stairData = stair.First;
                        if (IsThrowAway(stairData))
                        {
                            continue;
                        }
                        ST_Staircase stairObj = new ST_Staircase();
                        stairObj.Props = stairData["dsProps"];
                        stairObj.Mesh = stairData["meshes"].First;
                        stairObj.Scaling = stairObj.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        stairObj.SnaptrudePosition = stairObj.Mesh["position"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
                        stairObj.Type = stairObj.Props["staircaseType"].ToString();
                        stairObj.levelBottom = (from lvl in new FilteredElementCollector(newDoc).
                            OfClass(typeof(Level)).
                            Cast<Level>()
                                                where (lvl.Id == LevelIdByNumber[int.Parse(stairObj.Props["storey"].ToString())])
                                                select lvl).First();
                        stairObj.levelTop = (from lvl in new FilteredElementCollector(newDoc).
                            OfClass(typeof(Level)).
                            Cast<Level>()
                                             where (lvl.Id == LevelIdByNumber[int.Parse(stairObj.Props["storey"].ToString()) + 1])
                                             select lvl).First();

                        ElementId staircase = stairObj.CreateStairs(newDoc);
                        Stairs currStair;
                        using (StairsEditScope newStairsScope = new StairsEditScope(newDoc, "edit Stairs"))
                        {
                            ElementId newStairsId = newStairsScope.Start(staircase);
                            using (SubTransaction stairsTrans = new SubTransaction(newDoc))
                            {
                                stairsTrans.Start();
                                currStair = newDoc.GetElement(newStairsId) as Stairs;
                                currStair.DesiredRisersNumber = int.Parse(stairObj.Props["steps"].ToString());
                                StairsType stairsType = newDoc.GetElement(currStair.GetTypeId()) as StairsType;

                                StairsType newStairsType = stairsType.Duplicate("stairs_" + RandomString(5)) as StairsType;

                                newStairsType.MaxRiserHeight = UnitsAdapter.convertToRevit(stairObj.Props["riser"]);
                                newStairsType.MinRunWidth = UnitsAdapter.convertToRevit(stairObj.Props["width"]);
                                newStairsType.MinTreadDepth = UnitsAdapter.convertToRevit(stairObj.Props["tread"]);

                                currStair.ChangeTypeId(newStairsType.Id);

                                currStair
                                    .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH)
                                    .Set(UnitsAdapter.convertToRevit(stairObj.Props["tread"]));

                                stairsTrans.Commit();
                            }
                            newStairsScope.Commit(new StairsFailurePreprocessor());
                        }

                        // DELETE EXISTING RAILINGS
                        using(SubTransaction transactionDeleteRailings = new SubTransaction(newDoc))
                        {
                            transactionDeleteRailings.Start();
                            try
                            {

                                ICollection<ElementId> railingIds = currStair.GetAssociatedRailings();
                                foreach (ElementId railingId in railingIds)
                                {
                                    newDoc.Delete(railingId);
                                }
                                transactionDeleteRailings.Commit();

                            }
                            catch (Exception e)
                            {
                                LogTrace("Error in deleting staircase railings", e.ToString());
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        LogTrace("Error in creating staircase", exception.ToString());
                    }
                }
                LogTrace("staircases created");

                //FURNITURES
                JToken furnitures = geometryParent["furnitures"];

                List<ElementId> sourcesIdsToDelete = new List<ElementId>();

                foreach (var furniture in furnitures)
                {
                    if (!ShouldImport(furniture)) continue;

                    processedElements++;
                    LogProgress(processedElements, totalElements);

                    var furnitureData = furniture.First;

                    double familyRotation = 0;
                    bool isFacingFlip = false;
                    string familyType = null;
                    string sourceElementId = null;

                    XYZ localOriginOffset = XYZ.Zero;

                    string revitFamilyName = (string)furnitureData["dsProps"]["revitMetaData"]["family"];

                    try
                    {
                        if (!furnitureData["dsProps"]["revitMetaData"]["offset"].IsNullOrEmpty())
                            if (!furnitureData["dsProps"]["revitMetaData"]["offset"].First.IsNullOrEmpty())
                                localOriginOffset = STDataConverter.ArrayToXYZ(furnitureData["dsProps"]["revitMetaData"]["offset"]);

                        if (!furnitureData["dsProps"]["revitMetaData"]["familyRotation"].IsNullOrEmpty())
                            familyRotation = (double)furnitureData["dsProps"]["revitMetaData"]["familyRotation"];

                        if (!furnitureData["dsProps"]["revitMetaData"]["facingFlipped"].IsNullOrEmpty())
                            isFacingFlip = (bool)furnitureData["dsProps"]["revitMetaData"]["facingFlipped"];

                        if(!furnitureData["dsProps"]["revitMetaData"]["type"].IsNullOrEmpty())
                            familyType = (string)furnitureData["dsProps"]["revitMetaData"]["type"];

                        if(!furnitureData["dsProps"]["revitMetaData"]["sourceElementId"].IsNullOrEmpty())
                            sourceElementId = (string)furnitureData["dsProps"]["revitMetaData"]["sourceElementId"];
                    }
                    catch 
                    {

                    }


                    try
                    {
                        if (IsThrowAway(furnitureData)) continue;


                        string revitId = (string)furnitureData["dsProps"]["revitMetaData"]["elementId"];
                        bool isExistingFurniture = false;
                        FamilyInstance existingFamilyInstance = null;
                        AssemblyInstance existingAssemblyInstance = null;
                        Group existingGroup = null;
                        FamilySymbol existingFamilySymbol = null;
                        string existingFamilyType = "";

                        if (revitId == null)
                        {
                            revitId = sourceElementId;
                        }


                        if (revitId != null)
                        {
                            using (SubTransaction trans = new SubTransaction(newDoc))
                            {
                                trans.Start();
                                try
                                {
                                    Element e = newDoc.GetElement(new ElementId(int.Parse(revitId)));
                                    isExistingFurniture = idToElement.TryGetValue(revitId, out Element _e);

                                    if (isExistingFurniture || e.IsValidObject)
                                    {
                                        isExistingFurniture = true;
                                        if (e.GetType().Name == "AssemblyInstance")
                                        {
                                            existingAssemblyInstance = (AssemblyInstance)e;
                                            existingFamilyType = existingAssemblyInstance.Name;
                                        }
                                        else if(e.GetType().Name == "Group")
                                        {
                                            existingGroup = (Group)e;
                                            existingFamilyType = existingGroup.Name;
                                        }
                                        else
                                        {
                                            existingFamilyInstance = (FamilyInstance)e;
                                            existingFamilySymbol = idToFamilySymbol[revitId];
                                            existingFamilyType = existingFamilySymbol.Name;

                                            isFacingFlip = (existingFamilyInstance).FacingFlipped;
                                        }


                                        trans.Commit();
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogTrace(e.Message);
                                }
                            }
                        }
                        using (SubTransaction trans = new SubTransaction(newDoc))
                        {
                            trans.Start();

                            // Creation ...................
                            ST_Interior st_interior = new ST_Interior(furnitureData);

                            FamilySymbol familySymbol = null;
                            if (existingFamilySymbol != null && existingFamilySymbol.IsValidObject)
                            {
                                Parameter offsetParam = st_interior.GetOffsetParameter(existingFamilyInstance);
                                if (existingFamilySymbol.Category.Name == "Casework" && offsetParam == null)
                                {
                                    BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

                                    XYZ existingInstanceCenter = (bbox.Max + bbox.Min).Divide(2);

                                    ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingFamilyInstance.Id, existingInstanceCenter.Multiply(-1)).First();

                                    st_interior.element = newDoc.GetElement(newId);

                                    BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                                    XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                                    double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

                                    LocationPoint pt = (LocationPoint)st_interior.element.Location;
                                    ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                                    ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

                                    ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position);

                                    BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                                    XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

                                    if (isFacingFlip)
                                    {

                                        XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                        XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                        Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                        var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
                                    }

                                    if (st_interior.Scaling.Z < 0)
                                    {
                                        st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                                    }
                                }
                                else
                                {
                                    ElementId levelId = LevelIdByNumber[st_interior.levelNumber];
                                    Level level = (Level)newDoc.GetElement(levelId);
                                    st_interior.CreateWithFamilySymbol(existingFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                                }
                            }
                            else if (revitFamilyName != null)
                            {
                                if (existingFamilySymbol?.Category?.Name == "Casework")
                                {
                                    XYZ originalPoint = ((LocationPoint)existingFamilyInstance.Location).Point;

                                    BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

                                    XYZ center = (bbox.Max + bbox.Min).Divide(2);

                                    ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingFamilyInstance.Id, center.Multiply(-1)).First();

                                    st_interior.element = newDoc.GetElement(newId);

                                    BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                                    XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                                    double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

                                    LocationPoint pt = (LocationPoint)st_interior.element.Location;
                                    ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                                    ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

                                    ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position);

                                    BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                                    XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

                                    if (isFacingFlip)
                                    {

                                        XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                        XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                        Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                        var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
                                    }

                                    if (st_interior.Scaling.Z < 0)
                                    {
                                        st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                                    }
                                }
                                else
                                {
                                    FamilySymbol defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, revitFamilyName, familyType);
                                    if (defaultFamilySymbol is null)
                                    {
                                        defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, revitFamilyName);
                                    }
                                    if (!defaultFamilySymbol.IsActive)
                                    {
                                        defaultFamilySymbol.Activate();
                                        newDoc.Regenerate();
                                    }
                                    ElementId levelId = LevelIdByNumber[st_interior.levelNumber];
                                    Level level = (Level)newDoc.GetElement(levelId);

                                    st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                                }
                            }
                            else if (existingAssemblyInstance != null)
                            {
                                XYZ originalPoint = ((LocationPoint)existingAssemblyInstance.Location).Point;

                                BoundingBoxXYZ bbox = existingAssemblyInstance.get_BoundingBox(null);

                                //XYZ center = (bbox.Max + bbox.Min).Divide(2);
                                XYZ center = ((LocationPoint)existingAssemblyInstance.Location).Point;

                                ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingAssemblyInstance.Id, center.Multiply(-1)).First();

                                st_interior.element = newDoc.GetElement(newId);

                                BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                                //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                                //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

                                LocationPoint pt = (LocationPoint)st_interior.element.Location;
                                XYZ centerNew = pt.Point;
                                //ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                                ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

                                ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position);

                                BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                                //XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);
                                XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

                                if (isFacingFlip)
                                {

                                    XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                    XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                    Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                    var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
                                }

                                if (st_interior.Scaling.Z < 0)
                                {
                                    st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                                }
                            }
                            else if (existingGroup != null)
                            {
                                XYZ originalPoint = ((LocationPoint)existingGroup.Location).Point;

                                BoundingBoxXYZ bbox = existingGroup.get_BoundingBox(null);

                                //XYZ center = (bbox.Max + bbox.Min).Divide(2);
                                XYZ center = ((LocationPoint)existingGroup.Location).Point;

                                ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingGroup.Id, center.Multiply(-1)).First();

                                st_interior.element = newDoc.GetElement(newId);

                                BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                                //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                                //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

                                LocationPoint pt = (LocationPoint)st_interior.element.Location;
                                XYZ centerNew = pt.Point;
                                //ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);
                                ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position - localOriginOffset);

                                XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

                                if (st_interior.Scaling.Z < 0)
                                {
                                    ElementTransformUtils.RotateElement(
                                        newDoc,
                                        newId,
                                        Line.CreateBound(st_interior.Position, st_interior.Position + XYZ.BasisZ),
                                        st_interior.eulerAngles.heading);
                                }
                                else
                                {
                                    ElementTransformUtils.RotateElement(
                                        newDoc,
                                        newId,
                                        Line.CreateBound(st_interior.Position, st_interior.Position + XYZ.BasisZ),
                                        -st_interior.eulerAngles.heading);
                                }


                                if (isFacingFlip)
                                {

                                    XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                    XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                    Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                    var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
                                }

                                if (st_interior.Scaling.Z < 0)
                                {
                                    st_interior.SnaptrudeFlip(st_interior.element, st_interior.Position);
                                }
                            }
                            else
                            {
                                //String familyName = st_interior.Name.RemoveIns();
                                String familyName = st_interior.FamilyName;
                                if (familyName is null) familyName = st_interior.FamilyTypeName;

                                FamilySymbol defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, familyName);
                                //FamilySymbol defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, "Casework Assembly", "Casework 044");
                                if (defaultFamilySymbol is null)
                                {
                                    Family family = LoadCustomFamily(familyName);
                                    defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, familyName);
                                    if (defaultFamilySymbol == null)
                                    {
                                        defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, familyName.Replace("_", " "));
                                    }
                                }

                                if (!defaultFamilySymbol.IsActive)
                                {
                                    defaultFamilySymbol.Activate();
                                    newDoc.Regenerate();
                                }
                                ElementId levelId;
                                if (LevelIdByNumber.ContainsKey(st_interior.levelNumber))
                                    levelId = LevelIdByNumber[st_interior.levelNumber];
                                else
                                    levelId = LevelIdByNumber.First().Value;
                                Level level = (Level)newDoc.GetElement(levelId);

                                st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                            }
                            if (st_interior.element is null)
                            {
                                st_interior.CreateWithDirectShape(newDoc);
                            }

                            try
                            {
                                if (isExistingFurniture)
                                {
                                    // delete original furniture
                                    //if (existingFamilyInstance.IsValidObject) newDoc.Delete(existingFamilyInstance.Id);
                                    if (existingFamilyInstance != null) sourcesIdsToDelete.Add(existingFamilyInstance.Id);
                                    if (existingAssemblyInstance != null) sourcesIdsToDelete.Add(existingAssemblyInstance.Id);
                                    if (existingGroup != null) sourcesIdsToDelete.Add(existingGroup.Id);
                                }
                            }
                            catch
                            {

                            }

                            TransactionStatus tstatus = trans.Commit();
                            LogTrace(tstatus.ToString());
                        }
                        LogTrace("furniture created");
                    }
                    catch(OutOfMemoryException e)
                    {
                        LogTrace("furniture creation ERROR - out of memeroy -", e.ToString());
                        break;
                    }
                    catch(Exception e)
                    {
                        LogTrace("furniture creation ERROR", e.ToString());
                    }

                }
                try
                {
                    using (SubTransaction t = new SubTransaction(newDoc))
                    {
                        t.Start();
                        newDoc.Delete(sourcesIdsToDelete);
                        t.Commit();
                    }
                }
                catch { }
            }
        }

        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        /// <summary>
        /// Class representing a staircase.
        /// </summary>        
        public class ST_Staircase
        {
            /// <summary>
            /// Represents snaptrude dsprops.
            /// </summary>
            /// <value>
            /// Gets JToken of dsprops of current stair.
            /// </value>
            public JToken Props { get; set; }
            /// <summary>
            /// Represents snaptrude meshes.
            /// </summary>
            /// <value>
            /// Gets JToken of meshes of current stair.
            /// </value>
            public JToken Mesh { get; set; }
            /// <summary>
            /// Represents bottom level of the stair.
            /// </summary>
            /// <value>
            /// Gets bottom level of current stair.
            /// </value>
            public Level levelBottom { get; set; }
            /// <summary>
            /// Represents top level of the stair.
            /// </summary>
            /// <value>
            /// Gets top level of current stair.
            /// </value>
            public Level levelTop { get; set; }
            public string Name { get; set; }
            /// <summary>
            /// Represents type of the stair. (straight, dogLegged, lShaped or square)
            /// </summary>
            /// <value>
            /// Gets type of current stair.
            /// </value>
            public string Type { get; set; }
            /// <summary>
            /// Represents position of the stair as in Snaptrude.
            /// </summary>
            /// <value>
            /// Gets position of current stair. (double array)
            /// </value>
            public double[] SnaptrudePosition { get; set; }
            /// <summary>
            /// Represents scaling values of the stair. 
            /// </summary>
            /// <value>
            /// Gets scaling values of current stair.
            /// </value>
            public double[] Scaling { get; set; }
            /// <summary>
            /// Constructs a scaling transformation matrix.
            /// </summary>
            /// <param name="centre"> Point w.r.t which scaling will happen. A revit XYZ point.</param>
            /// <returns>Scaling Matrix.</returns>
            public double[,] getScaleMatrix(XYZ centre)
            {
                double[,] scaleMat = new double[4, 4];
                scaleMat[0, 0] = this.Scaling[0];
                scaleMat[1, 1] = this.Scaling[2];
                scaleMat[2, 2] = this.Scaling[1];
                scaleMat[3, 3] = 1.0;
                scaleMat[0, 3] = centre[0] * (1 - this.Scaling[0]);
                scaleMat[1, 3] = centre[1] * (1 - this.Scaling[2]);
                scaleMat[2, 3] = centre[2] * (1 - this.Scaling[1]);
                scaleMat[0, 1] = scaleMat[0, 2] = 0.0;
                scaleMat[1, 0] = scaleMat[1, 2] = 0.0;
                scaleMat[2, 0] = scaleMat[2, 1] = 0.0;
                scaleMat[3, 0] = scaleMat[3, 1] = scaleMat[3, 2] = 0.0;

                return scaleMat;
            }
            /// <summary>
            /// Process the given point using the given transformation matrix.
            /// </summary>
            /// <param name="point">Point to be processed. A revit XYZ point.</param>
            /// <param name="Matrix">Transformation matrix to be used. A 2D double array.</param>
            /// <returns>The processed or transformed point.</returns>
            public XYZ getProcessedPts(XYZ point, double[,] Matrix)
            {
                double[,] currPt = { { point.X }, { point.Y }, { point.Z }, { 1 } };
                double[,] prod = new double[4, 1];
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        prod[i, j] = 0;
                        for (int k = 0; k < 4; k++)
                            prod[i, j] += Matrix[i, k] * currPt[k, j];
                    }
                }
                XYZ processed = new XYZ(prod[0, 0], prod[1, 0], prod[2, 0]);
                return processed;
            }
            /// <summary>
            /// Constructs a rotation transformation matrix.
            /// </summary>
            /// <param name="q">Rotation in terms of a Quaternion quantity. A Quaternion type object.</param>
            /// <param name="centre">Point w.r.t which rotation will happen. A revit XYZ point.</param>
            /// <returns>The rotation matrix.</returns>
            private double[,] getRotMatrix(Quaternion q, XYZ centre)
            {
                double[,] rotMatrix = new double[4, 4];
                q.Normalize();
                double sqw = q.W * q.W;
                double sqx = q.X * q.X;
                double sqy = q.Y * q.Y;
                double sqz = q.Z * q.Z;
                rotMatrix[0, 0] = sqx - sqy - sqz + sqw; // since sqw + sqx + sqy + sqz =1
                rotMatrix[1, 1] = -sqx + sqy - sqz + sqw;
                rotMatrix[2, 2] = -sqx - sqy + sqz + sqw;

                double tmp1 = q.X * q.Y;
                double tmp2 = q.Z * q.W;
                rotMatrix[0, 1] = 2.0 * (tmp1 + tmp2);
                rotMatrix[1, 0] = 2.0 * (tmp1 - tmp2);

                tmp1 = q.X * q.Z;
                tmp2 = q.Y * q.W;
                rotMatrix[0, 2] = 2.0 * (tmp1 - tmp2);
                rotMatrix[2, 0] = 2.0 * (tmp1 + tmp2);

                tmp1 = q.Y * q.Z;
                tmp2 = q.X * q.W;
                rotMatrix[1, 2] = 2.0 * (tmp1 + tmp2);
                rotMatrix[2, 1] = 2.0 * (tmp1 - tmp2);

                double a1, a2, a3;

                a1 = centre.X;
                a2 = centre.Y;
                a3 = centre.Z;

                rotMatrix[0, 3] = a1 - a1 * rotMatrix[0, 0] - a2 * rotMatrix[0, 1] - a3 * rotMatrix[0, 2];
                rotMatrix[1, 3] = a2 - a1 * rotMatrix[1, 0] - a2 * rotMatrix[1, 1] - a3 * rotMatrix[1, 2];
                rotMatrix[2, 3] = a3 - a1 * rotMatrix[2, 0] - a2 * rotMatrix[2, 1] - a3 * rotMatrix[2, 2];
                rotMatrix[3, 0] = rotMatrix[3, 1] = rotMatrix[3, 2] = 0.0;
                rotMatrix[3, 3] = 1.0;

                return rotMatrix;

            }
            /// <summary>
            /// The method used to actually create stairs.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            public ElementId CreateStairs(Document document)
            {
                if (this.Type == "straight")
                {
                    return this.CreateStraightStairs(document);
                }
                else if (this.Type == "square")
                {
                    return this.CreateSquareStairs(document);
                }
                else if (this.Type == "dogLegged")
                {
                    return this.CreateDogLeggedStairs(document);
                }
                else if (this.Type == "lShaped")
                {
                    return this.CreateLShapedStairs(document);
                }
                else return null;
            }
            /// <summary>
            /// The method used to create a straight type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateStraightStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[] stepsArrange = this.Props["stepsArrangement"].First.Select(jv => (int)jv).ToArray();
                        double[] landings = this.Props["landings"].First.Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[landings.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[landings.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            var stepsNo = stepsArrange[i];
                            IList<Curve> bdryCurves = new List<Curve>();
                            IList<Curve> riserCurves = new List<Curve>();
                            IList<Curve> pathCurves = new List<Curve>();

                            XYZ pnt1 = new XYZ(position[0], position[1], position[2]);
                            XYZ pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                            XYZ pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                            XYZ pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);

                            XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                            XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                            XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                            XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                            // boundaries
                            bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                            bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                            // riser curves
                            double interval = (pnt2.X - pnt1.X) / stepsNo;
                            for (int ii = 0; ii <= stepsNo; ii++)
                            {
                                XYZ end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                XYZ end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                riserCurves.Add(Line.CreateBound(rend0, rend1));
                            }

                            //stairs path curves
                            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                            XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                            XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                            pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                            StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                            newRun1.EndsWithRiser = false;
                            topElevArr[landingInd + 1] = newRun1.TopElevation;
                            XYZ nextPos = pnt2;
                            if (landingInd < landings.Length)
                            {
                                XYZ tmp = new XYZ(pnt2.X - landings[landingInd], pnt2.Y, pnt2.Z);
                                nextPos = tmp;
                                landingCor[landingInd] = pnt2;
                                landingInd++;
                            }
                            position[0] = nextPos.X;
                            position[1] = nextPos.Y;
                            position[2] = nextPos.Z;
                        }

                        for (int i = 0; i < landings.Length; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1 = landingCor[i];
                            XYZ p2 = new XYZ(p1.X - landings[i], p1.Y, p1.Z);
                            XYZ p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                            XYZ p4 = new XYZ(p1.X - landings[i], p1.Y - runWidth, p1.Z);

                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method used to create a lShaped type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateLShapedStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => (int)jv1).ToArray()).ToArray();
                        double[][] landings = this.Props["landings"].Select(jv => jv.Select(jv1 => UnitsAdapter.convertToRevit((double)jv1)).ToArray()).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            for (int j = 0; j < stepsArrange[i].Length; j++)
                            {
                                var stepsNo = stepsArrange[i][j];
                                IList<Curve> bdryCurves = new List<Curve>();
                                IList<Curve> riserCurves = new List<Curve>();
                                IList<Curve> pathCurves = new List<Curve>();
                                XYZ pnt1, pnt2, pnt3, pnt4;
                                if (i == 0)
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                    pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }
                                else
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                    pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                                }


                                XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                                XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                                XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                                XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                                // boundaries
                                bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                                bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                                // riser curves
                                double interval;
                                if (i == 0)
                                {
                                    interval = (pnt2.X - pnt1.X) / stepsNo;
                                }
                                else
                                {
                                    interval = (pnt2.Y - pnt1.Y) / stepsNo;
                                }

                                for (int ii = 0; ii <= stepsNo; ii++)
                                {
                                    XYZ end0;
                                    XYZ end1;
                                    if (i == 0)
                                    {
                                        end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                        end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                    }
                                    else
                                    {
                                        end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                        end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                    }

                                    XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                    XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                    riserCurves.Add(Line.CreateBound(rend0, rend1));
                                }

                                //stairs path curves
                                XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                                XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                                XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                                XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                                pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                                StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                                newRun1.EndsWithRiser = false;
                                topElevArr[landingInd + 1] = newRun1.TopElevation;
                                XYZ nextPos = pnt2;
                                double offset = 0;
                                if (i == 0 && j == stepsArrange[i].Length - 1) nextPos = pnt4;
                                else if (i == 0) offset = landings[0][j];
                                landingCor[landingInd] = pnt2;
                                landingInd++;
                                position[0] = nextPos.X - offset;
                                position[1] = nextPos.Y;
                                position[2] = nextPos.Z;
                            }
                        }
                        int tmpIndex = 0;
                        for (int i = 0; i < landings.Length; i++)
                        {
                            for (int j = 0; j < landings[i].Length; j++)
                            {
                                // Add a landing between the runs
                                CurveLoop landingLoop = new CurveLoop();
                                XYZ p1, p2, p3, p4;
                                if (i == 0)
                                {
                                    p1 = landingCor[tmpIndex];
                                    p2 = new XYZ(p1.X - landings[i][j], p1.Y, p1.Z);
                                    p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                    p4 = new XYZ(p1.X - landings[i][j], p1.Y - runWidth, p1.Z);
                                }
                                else
                                {
                                    p1 = landingCor[tmpIndex];
                                    p2 = new XYZ(p1.X, p1.Y - landings[i][j], p1.Z);
                                    p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                    p4 = new XYZ(p1.X - runWidth, p1.Y - landings[i][j], p1.Z);
                                }

                                XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                                XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                                XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                                XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                                Line curve_1 = Line.CreateBound(rp1, rp2);
                                Line curve_2 = Line.CreateBound(rp2, rp4);
                                Line curve_3 = Line.CreateBound(rp4, rp3);
                                Line curve_4 = Line.CreateBound(rp3, rp1);

                                landingLoop.Append(curve_1);
                                landingLoop.Append(curve_2);
                                landingLoop.Append(curve_3);
                                landingLoop.Append(curve_4);
                                StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[tmpIndex + 1]);
                                tmpIndex++;
                            }
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method used to create a dogLegged type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateDogLeggedStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => jv1.ToString() == "" ? 0 : (int)jv1).ToArray()).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        bool singleLanding = false;
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            for (int j = 0; j < stepsArrange[i].Length; j++)
                            {
                                if (stepsArrange[i][j] == 0)
                                {
                                    singleLanding = true;
                                    position[0] = position[0];
                                    position[1] = position[1] - 0.5905511811023623; // approx 150mm
                                    position[2] = position[2];
                                    continue;
                                }

                                var stepsNo = stepsArrange[i][j];
                                IList<Curve> bdryCurves = new List<Curve>();
                                IList<Curve> riserCurves = new List<Curve>();
                                IList<Curve> pathCurves = new List<Curve>();
                                XYZ pnt1, pnt2, pnt3, pnt4;
                                if (i == 0)
                                {
                                    pnt3 = new XYZ(position[0], position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                    pnt1 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }
                                else if (i == 1)
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                    pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                                }
                                else
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0] + stepsNo * tread_depth, position[1], position[2]);
                                    pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt4 = new XYZ(position[0] + stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }

                                XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                                XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                                XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                                XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                                // boundaries
                                bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                                bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                                // riser curves
                                double interval;
                                if (i == 0 || i == 2)
                                {
                                    interval = (pnt2.X - pnt1.X) / stepsNo;
                                }
                                else
                                {
                                    interval = (pnt2.Y - pnt1.Y) / stepsNo;
                                }

                                for (int ii = 0; ii <= stepsNo; ii++)
                                {
                                    XYZ end0;
                                    XYZ end1;
                                    if (i == 0 || i == 2)
                                    {
                                        end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                        end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                    }
                                    else
                                    {
                                        end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                        end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                    }

                                    XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                    XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                    riserCurves.Add(Line.CreateBound(rend0, rend1));
                                }

                                //stairs path curves
                                XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                                XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                                XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                                XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                                pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                                StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                                newRun1.EndsWithRiser = false;
                                topElevArr[landingInd + 1] = newRun1.TopElevation;
                                XYZ nextPos = pnt2;
                                landingCor[landingInd] = pnt2;
                                landingInd++;

                                position[0] = nextPos.X;
                                position[1] = nextPos.Y;
                                position[2] = nextPos.Z;
                            }
                        }

                        for (int i = 0; i < landingInd - 1; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1, p2, p3, p4;

                            if (singleLanding == true)
                            {
                                XYZ tmp = landingCor[i];
                                p1 = new XYZ(tmp.X, tmp.Y + runWidth, tmp.Z);
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y - (2 * runWidth + 0.5905511811023623), p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - (2 * runWidth + 0.5905511811023623), p1.Z);
                            }
                            else if (i == 0 && singleLanding == false)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y + runWidth, p1.Z);
                            }
                            else if (i == 1 && singleLanding == false)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else break;


                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }

                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method to create a square type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateSquareStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => (int)jv1).ToArray()).ToArray();
                        //double[] landings = this.Props["landings"].First.Select(jv => (double)jv * 10 / 12).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            var stepsNo = stepsArrange[i][0];
                            IList<Curve> bdryCurves = new List<Curve>();
                            IList<Curve> riserCurves = new List<Curve>();
                            IList<Curve> pathCurves = new List<Curve>();
                            XYZ pnt1, pnt2, pnt3, pnt4;
                            if (i == 0)
                            {
                                pnt3 = new XYZ(position[0], position[1], position[2]);
                                pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                pnt1 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                            }
                            else if (i == 1)
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                            }
                            else if (i == 2)
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0] + stepsNo * tread_depth, position[1], position[2]);
                                pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                pnt4 = new XYZ(position[0] + stepsNo * tread_depth, position[1] - runWidth, position[2]);
                            }
                            else
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0], position[1] + stepsNo * tread_depth, position[2]);
                                pnt3 = new XYZ(position[0] + runWidth, position[1], position[2]);
                                pnt4 = new XYZ(position[0] + runWidth, position[1] + stepsNo * tread_depth, position[2]);
                            }

                            XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                            XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                            XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                            XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                            // boundaries
                            bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                            bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                            // riser curves
                            double interval;
                            if (i == 0 || i == 2)
                            {
                                interval = (pnt2.X - pnt1.X) / stepsNo;
                            }
                            else
                            {
                                interval = (pnt2.Y - pnt1.Y) / stepsNo;
                            }

                            for (int ii = 0; ii <= stepsNo; ii++)
                            {
                                XYZ end0;
                                XYZ end1;
                                if (i == 0 || i == 2)
                                {
                                    end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                    end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                }
                                else
                                {
                                    end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                    end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                }

                                XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                riserCurves.Add(Line.CreateBound(rend0, rend1));
                            }

                            //stairs path curves
                            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                            XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                            XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                            pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                            StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                            newRun1.EndsWithRiser = false;
                            topElevArr[landingInd + 1] = newRun1.TopElevation;
                            XYZ nextPos = pnt2;
                            landingCor[landingInd] = pnt2;
                            landingInd++;
                            position[0] = nextPos.X;
                            position[1] = nextPos.Y;
                            position[2] = nextPos.Z;
                        }

                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1, p2, p3, p4;

                            if (i == 0)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y + runWidth, p1.Z);
                            }
                            else if (i == 1)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else if (i == 2)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X + runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p4 = new XYZ(p1.X + runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p3 = new XYZ(p1.X + runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X + runWidth, p1.Y + runWidth, p1.Z);
                            }

                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
        }
        /// <summary>
        /// FailurePreprocessor class required for StairsEditScope
        /// </summary>
        class StairsFailurePreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // Use default failure processing
                return FailureProcessingResult.Continue;
            }
        }

        // MATERIAL IMPLMENTATION WRAPPER
        public class ST_Material
        {
            private const string urlDomain = "http://www.snaptru.de/";
            private const string redundantUrl1 = "http://127.0.0.1:8000";
            private const string redundantUrl2 = "http://127.0.0.1:8000/D:Snaptrudestagingsnaptrudestaging..";
            private const string fileLoc = @"D:\RevitExport\Textures\";
            public static bool CheckIfMultiMaterial(string materialId, JToken materialJSON)
            {
                if (materialJSON[materialId] != null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public static JObject CreateMaterialsJSON(JToken materialParam)
            {
                JObject materials = new JObject();

                for (int i = 0; i < materialParam.Count(); i++)
                {
                    bool checkFlag = CheckIfMaterialExists(materialParam, materialParam[i]["name"].ToString(), i);
                    if (!checkFlag)
                    {
                        materials.Add(materialParam[i]["name"].ToString(), materialParam[i]);
                    }
                }
                return materials;
            }
            public static void LoadFamilyMaterial(Autodesk.Revit.ApplicationServices.Application app, Document doc, JToken material)
            {
                string fileTarget = "";
                Asset asset = app.GetAssets(AssetType.Appearance)[0];
                if (material["diffuse"] != null)
                {
                    if (material["diffuseTexture"] != null)
                    {
                        if (material["diffuseTexture"]["url"] != null)
                        {
                            // Download Texture Image
                            //fileTarget = DownloadTextures(material, material["diffuseTexture"]["url"].ToString());
                            fileTarget = "/texture.png";
                            // Create material
                            // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                        }
                    }
                    else
                    {
                        // Create material without texture
                        // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                    }
                }
                else if (material["albedo"] != null)
                {
                    if (material["albedoTexture"] != null)
                    {
                        if (material["albedoTexture"]["url"] != null)
                        {
                            // Download Texture Image
                            //fileTarget = DownloadTextures(material, material["albedoTexture"]["url"].ToString());
                            fileTarget = "/texture.png";
                            // Create material
                            // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                        }
                    }
                    else
                    {
                        // Create material without texture
                        // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                    }
                }
            }
            public static void LoadMaterials(Autodesk.Revit.ApplicationServices.Application app, Document doc, JToken mats)
            {
                for (int i = 0; i < mats.Count(); i++)
                {
                    string fileTarget = "";
                    Asset asset = app.GetAssets(AssetType.Appearance)[0];
                    var material = mats[i];
                    string matName = material["name"].ToString();
                    bool materialCheckFlag = false;
                    bool nameCheckFlag;


                    // Check if material already exists
                    materialCheckFlag = CheckIfMaterialExists(mats, matName, i);
                    nameCheckFlag = NamingUtils.IsValidName(matName);

                    if (materialCheckFlag || !nameCheckFlag)
                    {
                        continue;
                    }
                    else
                    {
                        if (material["diffuse"] != null)
                        {
                            if (material["diffuseTexture"] != null)
                            {
                                if (material["diffuseTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["diffuseTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                            }
                        }
                        else if (material["albedo"] != null)
                        {
                            if (material["albedoTexture"] != null)
                            {
                                if (material["albedoTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["albedoTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            public static void LoadMultiMaterials(Autodesk.Revit.ApplicationServices.Application app, Document doc, JToken multiMats, JObject matsJSON)
            {
                for (int i = 0; i < multiMats.Count(); i++)
                {
                    string fileTarget = "";
                    Asset asset = app.GetAssets(AssetType.Appearance)[0];
                    var material = multiMats[i];
                    string multiMatName = material["name"].ToString();
                    bool materialCheckFlag = false;
                    bool nameCheckFlag;

                    // Check if material already exists
                    materialCheckFlag = CheckIfMaterialExists(multiMats, multiMatName, i);
                    nameCheckFlag = NamingUtils.IsValidName(multiMatName);

                    if (materialCheckFlag || !nameCheckFlag)
                    {
                        continue;
                    }
                    else
                    {
                        string matName = material["materials"].Last.ToString();
                        material = matsJSON[matName];
                        material["name"] = multiMatName;
                        if (material["diffuse"] != null)
                        {
                            if (material["diffuseTexture"] != null)
                            {
                                if (material["diffuseTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["diffuseTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                            }
                        }
                        else if (material["albedo"] != null)
                        {
                            if (material["albedoTexture"] != null)
                            {
                                if (material["albedoTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["albedoTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            private static bool CheckIfMaterialExists(JToken mats, string name, int matIdx)
            {
                for (int i = 0; i < matIdx; i++)
                {
                    string checkName = mats[i]["name"].ToString();
                    if (checkName == name)
                    {
                        return true;
                    }
                }
                return false;
            }
            private static string DownloadTextures(JToken material, string matURL)
            {
                string url_ = matURL;
                string imageName = url_.Split('/').Last();
                string url = null;
                string targetLocation = fileLoc + imageName;

                if (!(File.Exists(targetLocation)))
                {
                    // Temporary Logic
                    if (url_.StartsWith(redundantUrl1))
                    {
                        if (url_.StartsWith(redundantUrl2))
                        {
                            url_ = url_.Replace(redundantUrl2, "/");
                            url = urlDomain + url_;
                        }
                        else
                        {
                            url_ = url_.Replace(redundantUrl1, "");
                            url = urlDomain + url_;
                        }
                    }
                    else
                    {
                        url = urlDomain + url_;
                    }

                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(new Uri(url), targetLocation);
                    }

                    return targetLocation;
                }
                else
                {
                    return targetLocation;
                }
            }
        }

        /// <summary>
        /// This will appear on the Design Automation output
        /// </summary>
        public static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }
        //public static void LogTrace(string format, params object[] args) { Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(format, args); }
        private static void LogProgress(int processed, int total)
        {
            if (processed % 10 == 0)
            {
                LogTrace(
                  "!ACESAPI:acesHttpOperation({0},\"\",\"\",{1},null)",
                  "onProgress",
                  "{ \"current-progress\": " + processed + ", \"total\": " + total + " }"
                );
            }
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static bool IsThrowAway(string meshType)
        {
            return meshType.Contains("throwAway");
        }
        private static bool IsThrowAway(JToken data)
        {
            return IsThrowAway(data["meshes"][0]["type"].ToString());
        }

        public static String getMaterialNameFromMaterialId ( String materialnameWithId, JArray subMeshes, JArray materials, JArray multiMaterials, int materialIndex )
        {
            if(materialnameWithId == null)
            {
                return null;
            }
            if(subMeshes == null)
            {
                subMeshes = new JArray();
            }

            if (materials is null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            if (multiMaterials is null)
            {
                throw new ArgumentNullException(nameof(multiMaterials));
            }

            String materialName = null;
            
            //materialIndex = (int)subMeshes[0]["materialIndex"];

            foreach ( JToken eachMaterial in materials ){

                if ( materialnameWithId == (String)eachMaterial["id"] )
                {
                    materialName = materialnameWithId;
                }

            }

            if (materialName == null)
            {
                foreach (JToken eachMultiMaterial in multiMaterials )
                {
                    if ( materialnameWithId == (String)eachMultiMaterial["id"])
                    {
                        if( !eachMultiMaterial["materials"].IsNullOrEmpty() )
                        {
                            materialName = (String)eachMultiMaterial["materials"][materialIndex];
                        }
                    }
                }

            }

            return materialName;

        }

        private static bool ShouldImport(JToken data)
        {
            if (data.First["dsProps"]["revitMetaData"].IsNullOrEmpty()) return true;

            if (data.First["dsProps"]["revitMetaData"]["elementId"].IsNullOrEmpty())
            {
                if (data.First["dsProps"]["revitMetaData"]["isStackedWall"].IsNullOrEmpty()) return true;
            }

            if (data.First["dsProps"]["revitMetaData"]["isModified"].IsNullOrEmpty()) return false;

            return (bool) data.First["dsProps"]["revitMetaData"]["isModified"];
        }
        
        private static bool IsStackedWall(JToken data)
        {
            if (data.First["dsProps"]["revitMetaData"]["isStackedWall"].IsNullOrEmpty()) return false;

            return (bool)data.First["dsProps"]["revitMetaData"]["isStackedWall"];
        }
        private static bool IsParentStackedWall(JToken data)
        {
            if (IsStackedWall(data))
            {
                return data.First["dsProps"]["revitMetaData"]["stackedWallParentId"].IsNullOrEmpty();
            }

            return false;
        }

        public static Dictionary<string, Family> LoadedFamilies = new Dictionary<string, Family>();

        public static Family LoadCustomFamily(String familyName)
        {
            if (LoadedFamilies.ContainsKey(familyName))
            {
                return LoadedFamilies[familyName];
            }

            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string filePath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/{familyName}.rfa";

                GlobalVariables.Document.LoadFamily(filePath, out Family family);

                LoadedFamilies.Add(familyName, family);

                return family;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static Family LoadCustomDoorFamily(String familyName)
        {
            if (LoadedFamilies.ContainsKey(familyName))
            {
                return LoadedFamilies[familyName];
            }

            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string filePath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Doors/{familyName}.rfa";

                GlobalVariables.Document.LoadFamily(filePath, out Family family);

                LoadedFamilies.Add(familyName, family);

                return family;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static Family LoadCustomWindowFamily(String familyName)
        {
            if (LoadedFamilies.ContainsKey(familyName))
            {
                return LoadedFamilies[familyName];
            }

            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string filePath = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Windows/{familyName}.rfa";

                GlobalVariables.Document.LoadFamily(filePath, out Family family);

                LoadedFamilies.Add(familyName, family);

                return family;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        private void ShowSuccessDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Import Status");
            mainDialog.MainInstruction = "Snaptrude Import Status";
            mainDialog.MainContent = "Finished importing your Snaptrude file!";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }
        private void ShowTrudeFileNotSelectedDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Import Status");
            mainDialog.MainInstruction = "Snaptrude Import Status";
            mainDialog.MainContent = "File not selected, import cancelled.";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }
    }
}
