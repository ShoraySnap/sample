using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media.Media3D;
using TrudeImporter;
//using Amazon.S3;
//using Amazon;
//using System.Threading.Tasks;
//using Amazon.S3.Model;
//using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
//using System.Threading;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            GlobalVariables.Document = doc;
            GlobalVariables.RvtApp = uiapp.Application;

            uiapp.Application.FailuresProcessing += Application_FailuresProcessing;
            try
            {
                bool status = false;
                using (Transaction t = new Transaction(doc, "Parse Trude"))
                {
                    t.Start();
                    status = ParseTrude();
                    t.Commit();
                }

                if (status) ShowSuccessDialogue();
                GlobalVariables.cleanGlobalVariables();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("catch", ex.ToString());
                GlobalVariables.cleanGlobalVariables();
                return Result.Failed;
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
                GlobalVariables.cleanGlobalVariables();
            }
        }

        void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor fa = e.GetFailuresAccessor();

            fa.DeleteAllWarnings();
        }

        private bool ParseTrude()
        {
            GlobalVariables.LevelIdByNumber.Clear();
            FamilyLoader.LoadedFamilies.Clear();

            FileOpenDialog trudeFileOpenDialog = new FileOpenDialog("Trude (*.trude)|*.trude");

            trudeFileOpenDialog.Show();

            if (trudeFileOpenDialog.GetSelectedModelPath() == null)
            {
                ShowTrudeFileNotSelectedDialogue();

                return false;
            }

            if (GlobalVariables.Document.IsReadOnly)
            {
                ShowReadOnlyDialogue();

                return false;
            }

            String path = ModelPathUtils.ConvertModelPathToUserVisiblePath(trudeFileOpenDialog.GetSelectedModelPath());

            JObject trudeData = JObject.Parse(File.ReadAllText(path));

            GlobalVariables.materials = trudeData["materials"] as JArray;
            GlobalVariables.multiMaterials = trudeData["multiMaterials"] as JArray;

            //JsonSchemaGenerator jsonSchemaGenerator = new JsonSchemaGenerator();
            //JsonSchema jsonSchema = jsonSchemaGenerator.Generate(typeof(TrudeProperties));

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                DefaultValueHandling= Newtonsoft.Json.DefaultValueHandling.Ignore,
            };
            serializer.Converters.Add(new XyzConverter());

            TrudeProperties trudeProperties = trudeData.ToObject<TrudeProperties>(serializer);
            deleteRemovedElements(trudeProperties.DeletedElements);
            ImportStories(trudeProperties.Storeys);
            ImportWalls(trudeProperties.Walls); // these are structural components of the building
            ImportBeams(trudeProperties.Beams); // these are structural components of the building
            ImportColumns(trudeProperties.Columns); // these are structural components of the building
            ImportFloors(trudeProperties.Floors);
            if (int.Parse(GlobalVariables.RvtApp.VersionNumber) < 2022)
                ImportFloors(trudeProperties.Ceilings);
            else
                ImportCeilings(trudeProperties.Ceilings);
            ImportSlabs(trudeProperties.Slabs); // these are structural components of the building
            ImportDoors(trudeProperties.Doors);
            ImportWindows(trudeProperties.Windows);
            //ImportSnaptrude(trudeData, GlobalVariables.Document);

            FamilyLoader.LoadedFamilies.Clear();
            GlobalVariables.LevelIdByNumber.Clear();

            TrudeWall.TypeStore.Clear();
            TrudeFloor.TypeStore.Clear();
            TrudeBeam.types.Clear();
            TrudeColumn.types.Clear();
            TrudeColumn.NewLevelsByElevation.Clear();
            TrudeSlab.TypeStore.Clear();
            TrudeDoorold.TypeStore.Clear();
            TrudeWindowOld.TypeStore.Clear();

            return true;
        }

        // Delete old elements if they already exists in the revit document
        /// <summary>
        /// This function deletes existing elements within Revit if imported again from snaptrude based on Element Id.
        /// </summary>
        /// <param name="elementId">Element Id from revit to sanptrude.</param>
        public void deleteOld(int? elementId)
        {
            if (elementId != null)
            {
                ElementId id = new ElementId((int)elementId);
                Element element = GlobalVariables.Document.GetElement(id);
                if (element != null)
                {
                    if (!element.GroupId.Equals(ElementId.InvalidElementId))
                        deleteIfInGroup(element);
                    else
                        GlobalVariables.Document.Delete(element.Id);
                }
            }
        }

        private void deleteRemovedElements(List<int> elementIds)
        {
            foreach (int elementId in elementIds)
            {
                try
                { 
                    ElementId id = new ElementId((int)elementId);
                    Element element = GlobalVariables.Document.GetElement(id);
                    if (!element.GroupId.Equals(ElementId.InvalidElementId))
                        deleteIfInGroup(element);
                    else
                        GlobalVariables.Document.Delete(id);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Exception in removing deleted elements:" + e.Message);
                }
            }
        }

        public void deleteIfInGroup(Element element)
        {
            Group group = GlobalVariables.Document.GetElement(element.GroupId) as Group;
            //string groupName = group.Name;
            //GroupType groupType = group.GroupType;
            IList<ElementId> groupMemberdIds = group.GetMemberIds();
            foreach (ElementId item in groupMemberdIds)
            {
                if (item == element.Id)
                {
                    group.UngroupMembers();
                    GlobalVariables.Document.Delete(element.Id);
                    groupMemberdIds.Remove(element.Id);
                    var newGroup = GlobalVariables.Document.Create.NewGroup(groupMemberdIds);
                    break;
                }
            }
        }

        private void ImportStories(List<StoreyProperties> propsList)
        {
            if (propsList.Count == 0)
            {
                try
                {
                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                    {
                        t.Start();

                        const int levelNumber = 0;
                        const double elevation = 0;
                        TrudeStorey newStorey = new TrudeStorey(levelNumber, elevation, Utils.RandomString());
                        newStorey.CreateLevel(GlobalVariables.Document);
                        GlobalVariables.LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);

                        t.Commit();
                    }

                }
                catch (Exception e)
                {
                    LogTrace(e.Message);
                }
            }

            foreach (StoreyProperties props in propsList)
            {
                TrudeStorey newStorey = new TrudeStorey(props);

                if (!props.LowerLevelElementId.IsNull())
                {
                    ElementId elementId = new ElementId((BuiltInParameter)props.LowerLevelElementId);
                    GlobalVariables.LevelIdByNumber.Add(newStorey.levelNumber, elementId);

                    continue;
                }

                try
                {

                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                    {
                        t.Start();

                        newStorey.CreateLevel(GlobalVariables.Document);
                        GlobalVariables.LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);

                        t.Commit();
                    }

                }
                catch (Exception e)
                {
                    LogTrace(e.Message);
                }
            }
            LogTrace("storey created");
        }

        private void ImportWalls(List<WallProperties> propsList)
        {
            foreach (WallProperties props in propsList)
            {
                if (props.IsStackedWall && !props.IsStackedWallParent) continue;
                // if (props.Storey is null) continue;

                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    try
                    {
                        TrudeWall trudeWall = new TrudeWall(props);
                        deleteOld(props.ExistingElementId);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Wall: " + props.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }

            TrudeWall.TypeStore.Clear();
            LogTrace("Finished Walls");
        }

        private void ImportBeams(List<BeamProperties> propsList)
        {
            foreach (var beam in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    deleteOld(beam.ExistingElementId);
                    try
                    {
                        new TrudeBeam(beam, GlobalVariables.LevelIdByNumber[beam.Storey]);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Beam:" + beam.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        private void ImportColumns(List<ColumnProperties> propsList)
        {
            foreach (var column in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    foreach(var instance in column.Instances)
                    {
                        deleteOld(instance.ExistingElementId);
                    }

                    try
                    {
                        new TrudeColumn(column);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Column: " + column.Instances[0].UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        private void ImportFloors(List<FloorProperties> propsList)
        {
            foreach (var floor in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    try
                    {
                        new TrudeFloor(floor, GlobalVariables.LevelIdByNumber[floor.Storey]);
                        deleteOld(floor.ExistingElementId);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Floor: " + floor.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        /// <summary>
        /// Slabs are basically outer shell floors and roofs
        /// </summary>
        /// <param name="propsList"></param>
        /// <remarks>Keeping them seperate from Import Floors since data structures couble be changed at a later stage</remarks>
        private void ImportSlabs(List<SlabProperties> propsList)
        {
            foreach (var slab in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    try
                    {
                        new TrudeSlab(slab);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Slab: " + slab.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        private void ImportDoors(List<DoorProperties> propsList)
        {
            foreach (var door in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    deleteOld(door.ExistingElementId);
                    try
                    {
                        new TrudeDoor(door, GlobalVariables.LevelIdByNumber[door.Storey]);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Door: " + door.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        private void ImportWindows(List<WindowProperties> propsList)
        {
            foreach (var window in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    deleteOld(window.ExistingElementId);
                    try
                    {
                        new TrudeWindow(window, GlobalVariables.LevelIdByNumber[window.Storey]);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Window: " + window.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        private void ImportCeilings(List<FloorProperties> propsList)
        {
            foreach (var ceiling in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    try
                    {
                        new TrudeCeiling(ceiling, GlobalVariables.LevelIdByNumber[ceiling.Storey]);
                        deleteOld(ceiling.ExistingElementId);
                        if (t.Commit() != TransactionStatus.Committed)
                        {
                            t.RollBack();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Ceiling: " + ceiling.UniqueId + "\nError is: " + e.Message + "\n");
                        t.RollBack();
                    }
                }
            }
        }

        // ______________________ Import Furniture will be fixed later _____________________
        //private void ImportFurniture()
        //{
        //    var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/1.skp";
        //    SKPImportOptions options = new SKPImportOptions();
        //    options.Unit = ImportUnit.Foot;
        //    options.Placement = ImportPlacement.Centered;
        //    options.ReferencePoint = new XYZ(0, 0, 0);

        //    //var url = "https://revitfurniture.s3.amazonaws.com/1.skp";
        //    //var request = WebRequest.Create(url);
        //    //request.Method = "GET";
        //    //using var webResponse = request.GetResponse();
        //    //using var webStream = webResponse.GetResponseStream();

        //    //using var reader = new StreamReader(webStream);
        //    //var data = reader.ReadToEnd();
        //    //File.WriteAllText(@"C:\Users\ROG\Desktop\1.skp", data);
        //    //ElementId elementId = GlobalVariables.Document.Import(path, options, GlobalVariables.Document.ActiveView);

        //    //System.Diagnostics.Debug.WriteLine("SKP FILE: .............................................\n" + data);

        //    //IAmazonS3 client = new AmazonS3Client(RegionEndpoint.USEast1);
        //    //string bucketName = "revitfurniture";
        //    //string objectName = "1.skp";
        //    //string filePath = @"C:\Users\ROG\Desktop";

        //    //ReadObjectDataAsync(client, bucketName, objectName, filePath).Wait();

        //    ElementId elementId = GlobalVariables.Document.Import(path, options, GlobalVariables.Document.ActiveView);

        //}

        //public static async Task<bool> ReadObjectDataAsync(
        //    IAmazonS3 client,
        //    string bucketName,
        //    string objectName,
        //    string filePath)
        //{
        //    // Create a GetObject request
        //    var request = new GetObjectRequest
        //    {
        //        BucketName = bucketName,
        //        Key = objectName,
        //    };

        //    // Issue request and remember to dispose of the response
        //    using GetObjectResponse response = await client.GetObjectAsync(request);

        //    try
        //    {
        //        // Save object to local file
        //        await response.WriteResponseStreamToFileAsync($"{filePath}\\{objectName}", true, CancellationToken.None);
        //        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error saving {objectName}: {ex.Message}");
        //        return false;
        //    }
        //}
        //___________________________________________________________

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
        public bool AreLayersSame(TrudeLayer[] stLayers, FloorType wallType)
        {
            CompoundStructure compoundStructure = wallType.GetCompoundStructure();
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();

            if (stLayers.Length != layers.Count) return false;

            bool areSame = true;
            for (int i = 0; i < stLayers.Length; i++)
            {
                TrudeLayer stLayer = stLayers[i];
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

        //private void ImportSnaptrude(JObject jObject)
        //{
        //    JArray _materials = jObject["materials"].Value<JArray>();
        //    JArray _multiMaterials = jObject["multiMaterials"].Value<JArray>();

        //    int totalElements = countTotalElement(jObject);
        //    int processedElements = 0;
            
        //    try
        //    {
        //        List<Element> existingElements = TrudeModel.GetAllElements(GlobalVariables.Document);

        //        foreach (Element e in existingElements)
        //        {
        //            string id = GetDataFromElement(e);
        //            if (id is null)
        //            {
        //                id = e.Id.ToString();
        //            }
        //            GlobalVariables.idToElement.Add(id, e);

        //            try
        //            {
        //                if (e.GetType().Name == "Wall") continue;
        //                GlobalVariables.idToFamilySymbol.Add(id, ((FamilyInstance)e).Symbol);
        //            } catch
        //            {

        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        LogTrace(e.Message);
        //    }

        //    foreach (JToken structure in jObject.GetValue("structures"))
        //    {

        //        JToken structureData = structure.First;

        //        // STOREYS
        //        Level baseLevel = new FilteredElementCollector(GlobalVariables.Document).OfClass(typeof(Level)).FirstElement() as Level;

        //        GlobalVariables.LevelIdByNumber.Clear();
        //        //GlobalVariables.LevelIdByNumber.Add(1, baseLevelId);

        //        JToken storeys = structureData["storeys"];
        //        if (!storeys.HasValues) continue;

        //        foreach (JToken storey in storeys)
        //        {
        //            processedElements++;
        //            LogProgress(processedElements, totalElements);

        //            JToken storeyData = storey.First;
        //            TrudeStorey newStorey = new TrudeStorey();

        //            if (storeyData.IsNullOrEmpty())
        //            {
        //                try
        //                {

        //                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //                    {
        //                        t.Start();
        //                        newStorey.CreateLevel(GlobalVariables.Document);
        //                        GlobalVariables.LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);
        //                        t.Commit();
        //                    }

        //                }
        //                catch (Exception e)
        //                {
        //                    LogTrace(e.Message);
        //                }
        //            }
        //            else
        //            {
        //                if (storeyData["revitMetaData"].IsNullOrEmpty() || storeyData["revitMetaData"]["revitLowerLevel"].IsNullOrEmpty())
        //                {
        //                    try
        //                    {

        //                        using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //                        {
        //                            t.Start();

        //                            newStorey.CreateLevel(GlobalVariables.Document);
        //                            GlobalVariables.LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);

        //                            t.Commit();
        //                        }

        //                    }
        //                    catch (Exception e)
        //                    {
        //                        LogTrace(e.Message);
        //                    }
        //                }
        //                else
        //                {
        //                    ElementId elementId = new ElementId((int)storeyData["revitMetaData"]["revitLowerLevel"]);
        //                    GlobalVariables.LevelIdByNumber.Add(newStorey.levelNumber, elementId);
        //                }
        //            }
        //        }
        //        LogTrace("storey created");

        //        JToken geometryParent = structureData["01"];
        //        if (geometryParent is null) continue;

        //        //WALLS
        //        JToken walls = geometryParent["walls"];
        //        int wallCount = 0;

        //        Dictionary<int, Exception> failedWalls = new Dictionary<int, Exception>();

        //        //FLOORS ......................................................................
        //        //JToken floors = geometryParent["floors"];
        //        //int count = 0;

        //        //foreach (var floor in floors)
        //        //{
        //        //    if (!ShouldImport(floor)) continue;

        //        //    var floorData = floor.First;

        //        //    if (IsThrowAway(floorData)) { continue; }

        //        //    String _materialNameWithId = (String)floorData["meshes"][0]["materialId"];

        //        //    if (_materialNameWithId == null || _materialNameWithId == String.Empty)
        //        //    {
        //        //        _materialNameWithId = (String)floorData["materialName"];
        //        //    }

        //        //    JArray subMeshes = null;

        //        //    if (floorData["meshes"][0]["subMeshes"].IsNullOrEmpty())
        //        //    {
        //        //        if (!floorData["subMeshes"].IsNullOrEmpty())
        //        //        {
        //        //            subMeshes = floorData["subMeshes"].Value<JArray>();
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        subMeshes = floorData["meshes"][0]["subMeshes"].Value<JArray>();
        //        //    }

        //        //    String _materialName = Utils.getMaterialNameFromMaterialId(_materialNameWithId, _materials, _multiMaterials, 0);

        //        //    FilteredElementCollector collector1 = new FilteredElementCollector(GlobalVariables.Document).OfClass(typeof(Autodesk.Revit.DB.Material));

        //        //    IEnumerable<Autodesk.Revit.DB.Material> materialsEnum = collector1.ToElements().Cast<Autodesk.Revit.DB.Material>();

        //        //    Autodesk.Revit.DB.Material _materialElement = null;

        //        //    foreach (var materialElement in materialsEnum)
        //        //    {
        //        //        String matName = materialElement.Name;

        //        //        if (matName.Replace("_", " ") == _materialName)
        //        //        {
        //        //            _materialElement = materialElement;
        //        //        }
        //        //    }

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    using (SubTransaction transactionFloor = new SubTransaction(GlobalVariables.Document))
        //        //    {
        //        //        transactionFloor.Start();

        //        //        FloorType existingFloorType = null;
        //        //        int revitId;
        //        //        ElementId existingElementId = null;

        //        //        bool isExistingFloor = false;
        //        //        try
        //        //        {
        //        //            if (!floorData["dsProps"]["revitMetaData"].IsNullOrEmpty())
        //        //            {
        //        //                isExistingFloor = true;
        //        //                String _revitId = (String)floorData["dsProps"]["revitMetaData"]["elementId"];
        //        //                revitId = (int)floorData["dsProps"]["revitMetaData"]["elementId"];
        //        //                existingElementId = new ElementId(revitId);
        //        //                Floor existingFloor = GlobalVariables.Document.GetElement(existingElementId) as Floor;
        //        //                existingFloorType = existingFloor.FloorType;
        //        //            }
        //        //        }
        //        //        catch
        //        //        {

        //        //        }

        //        //        try
        //        //        {
        //        //            if (IsThrowAway(floorData))
        //        //            {
        //        //                continue;
        //        //            }

        //        //            ElementId levelId = GlobalVariables.LevelIdByNumber[TrudeRepository.GetLevelNumber(floorData)];
        //        //            TrudeFloorOld st_floor = new TrudeFloorOld(floorData, GlobalVariables.Document, levelId, existingFloorType);

        //        //            try
        //        //            {
        //        //                List<List<XYZ>> holes = TrudeRepository.GetHoles(floorData);

        //        //                foreach (var hole in holes)
        //        //                {
        //        //                    var holeProfile = TrudeWall.GetProfile(hole);
        //        //                    CurveArray curveArray1 = new CurveArray();
        //        //                    foreach (Curve c in holeProfile)
        //        //                    {
        //        //                        curveArray1.Append(c);
        //        //                    }
        //        //                    GlobalVariables.Document.Create.NewOpening(st_floor.floor, curveArray1, true);
        //        //                }
        //        //            }
        //        //            catch { }

        //        //            if (_materialElement != null)
        //        //            {
        //        //                st_floor.ApplyPaintByMaterial(GlobalVariables.Document, st_floor.floor, _materialElement);
        //        //            }

        //        //            count++;

        //        //            if (isExistingFloor)
        //        //            {
        //        //                try
        //        //                {
        //        //                    GlobalVariables.Document.Delete(existingElementId);
        //        //                }
        //        //                catch
        //        //                {

        //        //                }
        //        //            }

        //        //            TransactionStatus status = transactionFloor.Commit();
        //        //        }
        //        //        catch (Exception e)
        //        //        {
        //        //            LogTrace("Error in creating floorslab", e.ToString());
        //        //        }
        //        //    }
        //        //}
        //        LogTrace("floors created");
        //        // ......................................................................

        //        //BASE FLOORS AND INTERMEDIATE FLOORS ......................................................................
        //        //JToken roofs = geometryParent["roofs"];
        //        //count = 0;

        //        //foreach (var roof in roofs)
        //        //{
        //        //    if (!ShouldImport(roof)) continue;

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    using (SubTransaction transactionRoofs = new SubTransaction(GlobalVariables.Document))
        //        //    {
        //        //        transactionRoofs.Start();

        //        //        try
        //        //        {
        //        //            JToken roofData = roof.First;


        //        //            FloorType existingFloorType = null;
        //        //            int revitId;
        //        //            ElementId existingElementId = null;

        //        //            bool isExistingFloor = false;
        //        //            try
        //        //            {
        //        //                if (!roofData["dsProps"]["revitMetaData"].IsNullOrEmpty())
        //        //                {
        //        //                    isExistingFloor = true;
        //        //                    String _revitId = (String)roofData["dsProps"]["revitMetaData"]["elementId"];
        //        //                    revitId = (int)roofData["dsProps"]["revitMetaData"]["elementId"];
        //        //                    existingElementId = new ElementId(revitId);
        //        //                    Floor existingFloor = GlobalVariables.Document.GetElement(existingElementId) as Floor;
        //        //                    existingFloorType = existingFloor.FloorType;
        //        //                }
        //        //            }
        //        //            catch
        //        //            {

        //        //            }

        //        //            if (IsThrowAway(roofData))
        //        //            {
        //        //                continue;
        //        //            }

        //        //            ElementId levelId = GlobalVariables.LevelIdByNumber[TrudeRepository.GetLevelNumber(roofData)];
        //        //            TrudeRoof st_roof = new TrudeRoof(roofData, GlobalVariables.Document, levelId, existingFloorType);

        //        //            try
        //        //            {
        //        //                List<List<XYZ>> holes = TrudeRepository.GetHoles(roofData);

        //        //                foreach (var hole in holes)
        //        //                {
        //        //                    var holeProfile = TrudeWall.GetProfile(hole);
        //        //                    CurveArray curveArray1 = new CurveArray();
        //        //                    foreach (Curve c in holeProfile)
        //        //                    {
        //        //                        curveArray1.Append(c);
        //        //                    }
        //        //                    GlobalVariables.Document.Create.NewOpening(st_roof.floor, curveArray1, true);
        //        //                }
        //        //            }
        //        //            catch { }

        //        //            count++;


        //        //            if (isExistingFloor)
        //        //            {
        //        //                try
        //        //                {
        //        //                    GlobalVariables.Document.Delete(existingElementId);
        //        //                }
        //        //                catch
        //        //                {

        //        //                }
        //        //            }

        //        //            TransactionStatus status = transactionRoofs.Commit();
        //        //        }
        //        //        catch (Exception e)
        //        //        {
        //        //            LogTrace("Error in creating floorslab", e.ToString());
        //        //        }
        //        //    }
        //        //}
        //        //TrudeFloorOld.TypeStore.Types.Clear();
        //        //LogTrace("Roofs created");
        //        // ......................................................................

        //        // Ceilings ......................................................................
        //        //JToken ceilings = geometryParent["ceilings"];
        //        //foreach (var ceiling in ceilings)
        //        //{
        //        //    if (!ShouldImport(ceiling)) continue;

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    try
        //        //    {
        //        //        JToken ceilingData = ceiling.First;

        //        //        string revitId = (string)ceilingData["dsProps"]["revitMetaData"]["elementId"];

        //        //        if (IsThrowAway(ceilingData)) continue;
        //        //        if (ceilingData["dsProps"]["storey"].Value<String>() is null) continue;

        //        //        if (ceilingData["dsProps"]["revitMetaData"]["curveId"] != null)
        //        //        {
        //        //            string curveId = (string)ceilingData["dsProps"]["revitMetaData"]["curveId"];

        //        //            ElementId ceilingId = new ElementId(int.Parse(revitId));

        //        //            Element ceilingElement = GlobalVariables.Document.GetElement(ceilingId);

        //        //            //get ceiling sketch
        //        //            ElementClassFilter filter = new ElementClassFilter(typeof(Sketch));

        //        //            ElementId sketchId = ceilingElement.GetDependentElements(filter).First();

        //        //            Sketch ceilingSketch = GlobalVariables.Document.GetElement(sketchId) as Sketch;

        //        //            CurveArrArray ceilingProfile = ceilingSketch.Profile;

        //        //            filter = new ElementClassFilter(typeof(CurveElement));

        //        //            IEnumerable<Element> curves = ceilingElement.GetDependentElements(filter)
        //        //                .Select(id => GlobalVariables.Document.GetElement(id));

        //        //            IEnumerable<ModelLine> modelLines = curves.Where(e => e is ModelLine).Cast<ModelLine>();//target

        //        //            if (curves.Count() != modelLines.Count())
        //        //                throw new Exception("The ceiling contains non straight lines");



        //        //            IList<IList<ModelLine>> editableSketch = new List<IList<ModelLine>>();

        //        //            Dictionary<String, CurveArray> profiles = new Dictionary<string, CurveArray>();

        //        //            foreach (CurveArray loop in ceilingProfile)
        //        //            {
        //        //                profiles.Add(loop.GenerateCurveId(revitId), loop);
        //        //            }

        //        //            List<XYZ> profilePoints = TrudeRepository.ListToPoint3d(ceilingData["topVertices"])
        //        //                .Distinct()
        //        //                .Select((Point3D p) => p.ToXYZ())
        //        //                .ToList();

        //        //            Dictionary<String, List<XYZ>> allProfiles = new Dictionary<String, List<XYZ>>();

        //        //            allProfiles.Add(curveId, profilePoints);

        //        //            if (!ceilingData["voids"].IsNullOrEmpty())
        //        //            {
        //        //                foreach (var voidj in ceilingData["voids"])
        //        //                {
        //        //                    string key = (string)voidj["curveId"];
        //        //                    List<XYZ> _profilePoints = TrudeRepository.ListToPoint3d(voidj["profile"])
        //        //                        .Select((Point3D p) => p.ToXYZ())
        //        //                        .ToList();

        //        //                    allProfiles.Add(key, _profilePoints);
        //        //                }
        //        //            }

        //        //            foreach (CurveArray loop in ceilingProfile)
        //        //            {
        //        //                List<ModelLine> newLoop = new List<ModelLine>();

        //        //                var currentLoopId = loop.GenerateCurveId(revitId);

        //        //                if (!allProfiles.ContainsKey(currentLoopId)) continue;

        //        //                var currentProfilePoints = allProfiles[currentLoopId];

        //        //                foreach (Curve edge in loop)
        //        //                {
        //        //                    foreach (ModelLine modelLine in modelLines)
        //        //                    {

        //        //                        Curve currentLine = ((modelLine as ModelLine)
        //        //                          .Location as LocationCurve).Curve;

        //        //                        if (currentLine.Intersect(edge) == SetComparisonResult.Equal)
        //        //                        {

        //        //                            newLoop.Add(modelLine);
        //        //                            break;
        //        //                        }

        //        //                        editableSketch.Add(newLoop);
        //        //                    }
        //        //                }

        //        //                using (SubTransaction transaction = new SubTransaction(GlobalVariables.Document))
        //        //                {
        //        //                    transaction.Start();

        //        //                    for (int i = 0; i < newLoop.Count(); i++)
        //        //                    {
        //        //                        LocationCurve lCurve = newLoop[i].Location as LocationCurve;

        //        //                        XYZ pt1New = currentProfilePoints[i];
        //        //                        XYZ pt2New = currentProfilePoints[(i + 1).Mod(newLoop.Count())];
        //        //                        Line newLine = Line.CreateBound(pt1New, pt2New);
        //        //                        lCurve.Curve = newLine;
        //        //                    }

        //        //                    transaction.Commit();
        //        //                }
        //        //            }

        //        //            try
        //        //            {
        //        //                List<List<XYZ>> holes = TrudeRepository.GetHoles(ceilingData);

        //        //                foreach (var hole in holes)
        //        //                {
        //        //                    var holeProfile = TrudeWall.GetProfile(hole);
        //        //                    CurveArray curveArray1 = new CurveArray();
        //        //                    foreach (Curve c in holeProfile)
        //        //                    {
        //        //                        curveArray1.Append(c);
        //        //                    }
        //        //                    GlobalVariables.Document.Create.NewOpening(ceilingElement, curveArray1, true);
        //        //                }
        //        //            }
        //        //            catch { }
        //        //        }
        //        //        else
        //        //        {
        //        //            // TODO: Ceiling creation is not supported by revit 2019 API!!!! SMH FML
        //        //        }

        //        //        if (revitId != null)
        //        //        {
        //        //            try
        //        //            {
        //        //                Element e;
        //        //                bool isExistingMass = GlobalVariables.idToElement.TryGetValue(revitId, out e);
        //        //                if (isExistingMass)
        //        //                {
        //        //                    Element existingMass = e;
        //        //                    ElementId existingLevelId = existingMass.LevelId;

        //        //                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //        //                    {
        //        //                        t.Start();
        //        //                        var val = GlobalVariables.Document.Delete(existingMass.Id);
        //        //                        t.Commit();
        //        //                    }
        //        //                }
        //        //            }
        //        //            catch (Exception e)
        //        //            {
        //        //                LogTrace(e.Message);
        //        //            }
        //        //        }
        //        //    }
        //        //    catch (Exception e)
        //        //    {
        //        //        LogTrace("Error Creating beam/column");
        //        //    }
        //        //}
        //        // ......................................................................

        //        // Columns and Beams ......................................................................
        //        //JToken masses = geometryParent["masses"];
        //        //foreach (var mass in masses)
        //        //{

        //        //    if (!ShouldImport(mass)) continue;

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    try
        //        //    {
        //        //        JToken massData = mass.First;
        //        //        JToken massMeshData = massData["meshes"].First;
        //        //        JToken massGeometry = massData["geometries"];

        //        //        string revitId = (string)massData["dsProps"]["revitMetaData"]["elementId"];

        //        //        if (IsThrowAway(massData)) continue;
        //        //        if (massGeometry is null) continue;
        //        //        if (massData["dsProps"]["storey"].Value<String>() is null) continue;

        //        //        string massType = massData["dsProps"]["massType"].Value<String>();
        //        //        if (massType.Equals("Column"))
        //        //        {

        //        //            ElementId levelId = GlobalVariables.LevelIdByNumber[TrudeRepository.GetLevelNumber(massData)];
        //        //            TrudeColumnOld
        //        //                .FromMassData(massData)
        //        //                .CreateColumn(GlobalVariables.Document, levelId);
        //        //        }
        //        //        else if (massType.Equals("Beam"))
        //        //        {
        //        //            ElementId levelId = GlobalVariables.LevelIdByNumber[TrudeRepository.GetLevelNumber(massData)];
        //        //            TrudeBeamOld
        //        //                .FromMassData(massData)
        //        //                .CreateBeam(GlobalVariables.Document, levelId);
        //        //        }

        //        //        if (revitId != null)
        //        //        {
        //        //            try
        //        //            {
        //        //                Element e;
        //        //                bool isExistingMass = GlobalVariables.idToElement.TryGetValue(revitId, out e);
        //        //                if (isExistingMass)
        //        //                {
        //        //                    Element existingMass = e;
        //        //                    ElementId existingLevelId = existingMass.LevelId;

        //        //                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //        //                    {
        //        //                        t.Start();
        //        //                        var val = GlobalVariables.Document.Delete(existingMass.Id);
        //        //                        t.Commit();
        //        //                    }
        //        //                }
        //        //            }
        //        //            catch (Exception e)
        //        //            {
        //        //                LogTrace(e.Message);
        //        //            }
        //        //        }
        //        //    }
        //        //    catch (Exception e)
        //        //    {
        //        //        LogTrace("Error Creating beam/column");
        //        //    }
        //        //}
        //        //TrudeColumnOld.NewLevelsByElevation.Clear();
        //        //TrudeColumnOld.types.Clear();
        //        //TrudeBeamOld.types.Clear();

        //        //LogTrace("beams and columns created");
        //        // ......................................................................

        //        //DOORS ......................................................................
        //        //JToken doors = geometryParent["doors"];
        //        //foreach (var door in doors)
        //        //{
        //        //    if (!ShouldImport(door)) continue;

        //        //    var doorData = door.First;
        //        //    int uniqueId = (int)doorData["dsProps"]["uniqueID"];
        //        //    string revitId = (string)doorData["dsProps"]["revitMetaData"]["elementId"];
        //        //    string revitFamilyName = (string)doorData["dsProps"]["revitMetaData"]["family"];

        //        //    if (IsThrowAway(doorData)) { continue; }

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    bool isExistingDoor = false;
        //        //    FamilyInstance existingFamilyInstance = null;
        //        //    FamilySymbol existingFamilySymbol = null;
        //        //    if (revitId != null)
        //        //    {
        //        //        using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //        //        {
        //        //            try
        //        //            {
        //        //                t.Start();

        //        //                Element e;
        //        //                isExistingDoor = GlobalVariables.idToElement.TryGetValue(revitId, out e);
        //        //                if (isExistingDoor)
        //        //                {
        //        //                    isExistingDoor = true;
        //        //                    existingFamilyInstance = (FamilyInstance)e;
        //        //                    existingFamilySymbol = GlobalVariables.idToFamilySymbol[revitId];

        //        //                    // delete original door
        //        //                    if (existingFamilyInstance.IsValidObject) GlobalVariables.Document.Delete(existingFamilyInstance.Id);
        //        //                    t.Commit();
        //        //                }
        //        //            }
        //        //            catch (Exception e)
        //        //            {
        //        //                LogTrace(e.Message);
        //        //            }
        //        //        }
        //        //    }
        //        //    using (SubTransaction transaction = new SubTransaction(GlobalVariables.Document))
        //        //    {
        //        //        transaction.Start();
        //        //        try
        //        //        {
        //        //            TrudeDoorold st_door = new TrudeDoorold();

        //        //            JToken doorMeshData = doorData["meshes"].First;

        //        //            double width = UnitsAdapter.convertToRevit(doorMeshData["width"]);
        //        //            double height = UnitsAdapter.convertToRevit(doorMeshData["height"]);
        //        //            XYZ direction = doorMeshData["direction"].IsNullOrEmpty()
        //        //                ? XYZ.Zero
        //        //                : TrudeRepository.ArrayToXYZ(doorMeshData["direction"], false).Round();

        //        //            st_door.Name = doorMeshData["name"].ToString();
        //        //            st_door.Geom_ID = doorMeshData["storey"].ToString();
        //        //            st_door.Position = TrudeRepository.GetPosition(doorData);
        //        //            st_door.family = doorMeshData["id"].ToString();
        //        //            st_door.levelNumber = TrudeRepository.GetLevelNumber(doorData);
        //        //            ElementId levelIdForWall = GlobalVariables.LevelIdByNumber[st_door.levelNumber];

        //        //            try
        //        //            {
        //        //                st_door.family = st_door.family.RemoveIns();

        //        //                string fsFamilyName = st_door.family;
        //        //                string fsName = st_door.family;

        //        //                if (revitFamilyName != null)
        //        //                {
        //        //                    fsFamilyName = revitFamilyName;
        //        //                    fsName = null;
        //        //                }

        //        //                Wall wall = null;
        //        //                if (GlobalVariables.childUniqueIdToWallElementId.ContainsKey(uniqueId))
        //        //                {
        //        //                    ElementId wallElementId = GlobalVariables.childUniqueIdToWallElementId[uniqueId];
        //        //                    wall = (Wall)GlobalVariables.Document.GetElement(wallElementId);
        //        //                }


        //        //                FamilySymbol familySymbol = null;
        //        //                FamilySymbol defaultFamilySymbol = null;
        //        //                if (isExistingDoor)
        //        //                {
        //        //                    defaultFamilySymbol = existingFamilySymbol;
        //        //                    if (!defaultFamilySymbol.IsActive)
        //        //                    {
        //        //                        defaultFamilySymbol.Activate();
        //        //                        GlobalVariables.Document.Regenerate();
        //        //                    }
        //        //                }
        //        //                else
        //        //                {
        //        //                    if (revitFamilyName is null)
        //        //                    {
        //        //                        var family = FamilyLoader.LoadCustomDoorFamily(fsFamilyName);
        //        //                        if (family is null)
        //        //                        {
        //        //                            LogTrace("couln't find door family");
        //        //                            continue;
        //        //                        }
        //        //                    }

        //        //                    defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, fsFamilyName, fsName);
        //        //                }

        //        //                if (!defaultFamilySymbol.IsActive)
        //        //                {
        //        //                    defaultFamilySymbol.Activate();
        //        //                    GlobalVariables.Document.Regenerate();
        //        //                }

        //        //                // Check if familySymbol BuiltInParameter.DOOR_HEIGHT and  BuiltInParameter.DOOR_WIDTH
        //        //                // if so, then set the height and with in the familySymbol itself, otherwise find the correct
        //        //                // parameter in the instance.

        //        //                Parameter heightTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
        //        //                Parameter widthTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_WIDTH);

        //        //                bool setHeightAndWidthParamsInFamilySymbol = (heightTypeParam.HasValue && widthTypeParam.HasValue) && (!heightTypeParam.IsReadOnly || !widthTypeParam.IsReadOnly);
        //        //                if (setHeightAndWidthParamsInFamilySymbol)
        //        //                {
        //        //                    familySymbol = TrudeDoorold.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
        //        //                }
        //        //                else
        //        //                {
        //        //                    familySymbol = defaultFamilySymbol;
        //        //                }

        //        //                st_door.CreateDoor(GlobalVariables.Document, familySymbol, levelIdForWall, wall, direction);

        //        //                (Parameter widthInstanceParam, Parameter heightInstanceParam) = st_door.instance.FindWidthAndHeightParameters();
        //        //                if (!setHeightAndWidthParamsInFamilySymbol)
        //        //                {
        //        //                    heightInstanceParam.Set(height);
        //        //                    widthInstanceParam.Set(width);
        //        //                }
        //        //                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(height);
        //        //                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(width);

        //        //                var tstatus = transaction.Commit();
        //        //            }
        //        //            catch (Exception e)
        //        //            {
        //        //                LogTrace($"No door with name {st_door.family} {st_door.Name}");
        //        //                LogTrace(e.Message);
        //        //            }
        //        //        }
        //        //        catch (Exception e)
        //        //        {
        //        //            LogTrace("Error in creating door", e.ToString());
        //        //        }
        //        //    }
        //        //}
        //        ////transactionDoors.Commit();
        //        //LogTrace("doors created");
        //        //TrudeDoorold.TypeStore.Clear();
        //        // ......................................................................

        //        //WINDOWS......................................................................

        //        //JToken windows = geometryParent["windows"];
        //        //foreach (var window in windows)
        //        //{
        //        //    if (!ShouldImport(window)) continue;

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    var windowData = window.First;
        //        //    int uniqueId = (int)windowData["dsProps"]["uniqueID"];
        //        //    string revitId = (string)windowData["dsProps"]["revitMetaData"]["elementId"];
        //        //    string revitFamilyName = (string)windowData["dsProps"]["revitMetaData"]["family"];

        //        //    if (IsThrowAway(windowData)) { continue; }

        //        //    bool isExistingWindow = false;
        //        //    FamilyInstance existingWindow = null;
        //        //    FamilySymbol existingFamilySymbol = null;
        //        //    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //        //    {
        //        //        try
        //        //        {
        //        //            t.Start();

        //        //            Element e;
        //        //            isExistingWindow = GlobalVariables.idToElement.TryGetValue(revitId, out e);

        //        //            if (isExistingWindow)
        //        //            {
        //        //                isExistingWindow = true;
        //        //                existingWindow = (FamilyInstance)e;
        //        //                existingFamilySymbol = GlobalVariables.idToFamilySymbol[revitId];

        //        //                // delete original window
        //        //                if (existingWindow.IsValidObject) GlobalVariables.Document.Delete(existingWindow.Id);
        //        //                t.Commit();
        //        //            }
        //        //        }
        //        //        catch (Exception e)
        //        //        {
        //        //            LogTrace(e.Message);
        //        //        }
        //        //    }

        //        //    using (SubTransaction transaction = new SubTransaction(GlobalVariables.Document))
        //        //    {
        //        //        transaction.Start();
        //        //        try
        //        //        {
        //        //            TrudeWindowOld st_window = new TrudeWindowOld();

        //        //            var windowMeshData = windowData["meshes"].First;

        //        //            double width = UnitsAdapter.convertToRevit(windowMeshData["width"]);
        //        //            double height = UnitsAdapter.convertToRevit(windowMeshData["height"]);

        //        //            XYZ direction = windowMeshData["direction"].IsNullOrEmpty()
        //        //                ? XYZ.Zero
        //        //                : TrudeRepository.ArrayToXYZ(windowMeshData["direction"], false).Round();

        //        //            st_window.Name = windowMeshData["name"].ToString();
        //        //            st_window.Geom_ID = windowMeshData["storey"].ToString();
        //        //            st_window.Position = TrudeRepository.GetPosition(windowData);
        //        //            st_window.Scaling = TrudeRepository.GetScaling(windowData);
        //        //            st_window.Rotation = TrudeRepository.GetRotation(windowData);
        //        //            st_window.family = windowMeshData["id"].ToString();
        //        //            //ElementId levelIdForWall = GlobalVariables.LevelIdByNumber[int.Parse(st_window.Geom_ID)];
        //        //            st_window.levelNumber = TrudeRepository.GetLevelNumber(windowData);
        //        //            var levelIdForWall = GlobalVariables.LevelIdByNumber[st_window.levelNumber];
        //        //            //ElementId levelIdForWall = GlobalVariables.LevelIdByNumber[1];

        //        //            double heightScale = 1;
        //        //            double widthScale = 1;
        //        //            if (windowData["meshes"][0]["originalScaling"] != null) {
        //        //                heightScale = double.Parse(windowData["meshes"][0]["scaling"][1].ToString()) / double.Parse(windowData["meshes"][0]["originalScaling"][1].ToString());
        //        //                widthScale = double.Parse(windowData["meshes"][0]["scaling"][0].ToString()) / double.Parse(windowData["meshes"][0]["originalScaling"][0].ToString());
        //        //            }
        //        //            else
        //        //            {
        //        //                heightScale = double.Parse(windowData["meshes"][0]["scaling"][1].ToString());
        //        //                widthScale = double.Parse(windowData["meshes"][0]["scaling"][0].ToString());
        //        //            }

        //        //            try
        //        //            {
        //        //                st_window.family = st_window.family.RemoveIns();

        //        //                string fsFamilyName = st_window.family;
        //        //                string fsName = st_window.family;

        //        //                if (revitFamilyName != null)
        //        //                {
        //        //                    fsFamilyName = revitFamilyName;
        //        //                    fsName = null;
        //        //                }

        //        //                Wall wall = null;
        //        //                if (GlobalVariables.childUniqueIdToWallElementId.ContainsKey(uniqueId))
        //        //                {
        //        //                    ElementId wallElementId = GlobalVariables.childUniqueIdToWallElementId[uniqueId];
        //        //                    wall = (Wall)GlobalVariables.Document.GetElement(wallElementId);
        //        //                }

        //        //                FamilySymbol familySymbol = null;
        //        //                FamilySymbol defaultFamilySymbol = null;
        //        //                if (isExistingWindow)
        //        //                {
        //        //                    defaultFamilySymbol = existingFamilySymbol;
        //        //                    if (!defaultFamilySymbol.IsActive)
        //        //                    {
        //        //                        defaultFamilySymbol.Activate();
        //        //                        GlobalVariables.Document.Regenerate();
        //        //                    }

        //        //                    familySymbol = TrudeWindowOld.TypeStore.GetType(new double[] { heightScale, widthScale }, defaultFamilySymbol);
        //        //                }
        //        //                else
        //        //                {
        //        //                    if (revitFamilyName is null)
        //        //                    {
        //        //                        FamilyLoader.LoadCustomWindowFamily(fsFamilyName);
        //        //                    }

        //        //                    defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, fsFamilyName, fsName);

        //        //                    familySymbol = TrudeWindowOld.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
        //        //                }

        //        //                if (!defaultFamilySymbol.IsActive)
        //        //                {
        //        //                    defaultFamilySymbol.Activate();
        //        //                    GlobalVariables.Document.Regenerate();
        //        //                }

        //        //                // Check if familySymbol BuiltInParameter.DOOR_HEIGHT and  BuiltInParameter.DOOR_WIDTH
        //        //                // if so, then set the height and with in the familySymbol itself, otherwise find the correct
        //        //                // parameter in the instance.

        //        //                Parameter heightTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.WINDOW_HEIGHT);
        //        //                Parameter widthTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH);

        //        //                bool setHeightAndWidthParamsInFamilySymbol = (heightTypeParam.HasValue && widthTypeParam.HasValue) && (!heightTypeParam.IsReadOnly || !widthTypeParam.IsReadOnly);
        //        //                if (setHeightAndWidthParamsInFamilySymbol)
        //        //                {
        //        //                    familySymbol = TrudeWindowOld.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
        //        //                }
        //        //                else
        //        //                {
        //        //                    familySymbol = defaultFamilySymbol;
        //        //                }

        //        //                st_window.CreateWindow(GlobalVariables.Document, familySymbol, levelIdForWall, wall, direction);

        //        //                (Parameter widthInstanceParam, Parameter heightInstanceParam) = st_window.instance.FindWidthAndHeightParameters();
        //        //                if (!setHeightAndWidthParamsInFamilySymbol)
        //        //                {
        //        //                    heightInstanceParam.Set(height);
        //        //                    widthInstanceParam.Set(width);
        //        //                }
        //        //                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(height);
        //        //                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(width);

        //        //                var transactionStatus = transaction.Commit();
        //        //            }
        //        //            catch (Exception e)
        //        //            {
        //        //                LogTrace(e.Message);
        //        //            }
        //        //        }
        //        //        catch (Exception exception)
        //        //        {
        //        //            LogTrace($"Error in creating window {exception}");
        //        //        }
        //        //    }
        //        //}
        //        ////transactionWindows.Commit();
        //        //LogTrace("windows created");
        //        //TrudeWindowOld.TypeStore.Clear();
        //        // ......................................................................

        //        //STAIRCASES......................................................................
        //        //ST_Staircase st_staircase = new ST_Staircase();
        //        //JToken stairs = geometryParent["staircases"];
        //        //foreach (var stair in stairs)
        //        //{
        //        //    break;
        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    try
        //        //    {
        //        //        var stairData = stair.First;
        //        //        if (IsThrowAway(stairData))
        //        //        {
        //        //            continue;
        //        //        }
        //        //        ST_Staircase stairObj = new ST_Staircase();
        //        //        stairObj.Props = stairData["dsProps"];
        //        //        stairObj.Mesh = stairData["meshes"].First;
        //        //        stairObj.Scaling = stairObj.Mesh["scaling"].Select(jv => (double)jv).ToArray();
        //        //        stairObj.SnaptrudePosition = stairObj.Mesh["position"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
        //        //        stairObj.Type = stairObj.Props["staircaseType"].ToString();
        //        //        stairObj.levelBottom = (from lvl in new FilteredElementCollector(GlobalVariables.Document).
        //        //            OfClass(typeof(Level)).
        //        //            Cast<Level>()
        //        //                                where (lvl.Id == GlobalVariables.LevelIdByNumber[int.Parse(stairObj.Props["storey"].ToString())])
        //        //                                select lvl).First();
        //        //        stairObj.levelTop = (from lvl in new FilteredElementCollector(GlobalVariables.Document).
        //        //            OfClass(typeof(Level)).
        //        //            Cast<Level>()
        //        //                             where (lvl.Id == GlobalVariables.LevelIdByNumber[int.Parse(stairObj.Props["storey"].ToString()) + 1])
        //        //                             select lvl).First();

        //        //        ElementId staircase = stairObj.CreateStairs(GlobalVariables.Document);
        //        //        Stairs currStair;
        //        //        using (StairsEditScope newStairsScope = new StairsEditScope(GlobalVariables.Document, "edit Stairs"))
        //        //        {
        //        //            ElementId newStairsId = newStairsScope.Start(staircase);
        //        //            using (SubTransaction stairsTrans = new SubTransaction(GlobalVariables.Document))
        //        //            {
        //        //                stairsTrans.Start();
        //        //                currStair = GlobalVariables.Document.GetElement(newStairsId) as Stairs;
        //        //                currStair.DesiredRisersNumber = int.Parse(stairObj.Props["steps"].ToString());
        //        //                StairsType stairsType = GlobalVariables.Document.GetElement(currStair.GetTypeId()) as StairsType;

        //        //                StairsType newStairsType = stairsType.Duplicate("stairs_" + RandomString(5)) as StairsType;

        //        //                newStairsType.MaxRiserHeight = UnitsAdapter.convertToRevit(stairObj.Props["riser"]);
        //        //                newStairsType.MinRunWidth = UnitsAdapter.convertToRevit(stairObj.Props["width"]);
        //        //                newStairsType.MinTreadDepth = UnitsAdapter.convertToRevit(stairObj.Props["tread"]);

        //        //                currStair.ChangeTypeId(newStairsType.Id);

        //        //                currStair
        //        //                    .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH)
        //        //                    .Set(UnitsAdapter.convertToRevit(stairObj.Props["tread"]));

        //        //                stairsTrans.Commit();
        //        //            }
        //        //            newStairsScope.Commit(new StairsFailurePreprocessor());
        //        //        }

        //        //        // DELETE EXISTING RAILINGS
        //        //        using(SubTransaction transactionDeleteRailings = new SubTransaction(GlobalVariables.Document))
        //        //        {
        //        //            transactionDeleteRailings.Start();
        //        //            try
        //        //            {

        //        //                ICollection<ElementId> railingIds = currStair.GetAssociatedRailings();
        //        //                foreach (ElementId railingId in railingIds)
        //        //                {
        //        //                    GlobalVariables.Document.Delete(railingId);
        //        //                }
        //        //                transactionDeleteRailings.Commit();

        //        //            }
        //        //            catch (Exception e)
        //        //            {
        //        //                LogTrace("Error in deleting staircase railings", e.ToString());
        //        //            }
        //        //        }
        //        //    }
        //        //    catch (Exception exception)
        //        //    {
        //        //        LogTrace("Error in creating staircase", exception.ToString());
        //        //    }
        //        //}
        //        //LogTrace("staircases created");
        //        // ......................................................................

        //        //FURNITURES......................................................................
        //        //JToken furnitures = geometryParent["furnitures"];

        //        //List<ElementId> sourcesIdsToDelete = new List<ElementId>();

        //        //foreach (var furniture in furnitures)
        //        //{
        //        //    if (!ShouldImport(furniture)) continue;

        //        //    processedElements++;
        //        //    LogProgress(processedElements, totalElements);

        //        //    var furnitureData = furniture.First;

        //        //    double familyRotation = 0;
        //        //    bool isFacingFlip = false;
        //        //    string familyType = null;
        //        //    string sourceElementId = null;

        //        //    XYZ localOriginOffset = XYZ.Zero;

        //        //    string revitFamilyName = (string)furnitureData["dsProps"]["revitMetaData"]["family"];

        //        //    try
        //        //    {
        //        //        if (!furnitureData["dsProps"]["revitMetaData"]["offset"].IsNullOrEmpty())
        //        //            if (!furnitureData["dsProps"]["revitMetaData"]["offset"].First.IsNullOrEmpty())
        //        //                localOriginOffset = TrudeRepository.ArrayToXYZ(furnitureData["dsProps"]["revitMetaData"]["offset"]);

        //        //        if (!furnitureData["dsProps"]["revitMetaData"]["familyRotation"].IsNullOrEmpty())
        //        //            familyRotation = (double)furnitureData["dsProps"]["revitMetaData"]["familyRotation"];

        //        //        if (!furnitureData["dsProps"]["revitMetaData"]["facingFlipped"].IsNullOrEmpty())
        //        //            isFacingFlip = (bool)furnitureData["dsProps"]["revitMetaData"]["facingFlipped"];

        //        //        if(!furnitureData["dsProps"]["revitMetaData"]["type"].IsNullOrEmpty())
        //        //            familyType = (string)furnitureData["dsProps"]["revitMetaData"]["type"];

        //        //        if(!furnitureData["dsProps"]["revitMetaData"]["sourceElementId"].IsNullOrEmpty())
        //        //            sourceElementId = (string)furnitureData["dsProps"]["revitMetaData"]["sourceElementId"];
        //        //    }
        //        //    catch 
        //        //    {

        //        //    }


        //        //    try
        //        //    {
        //        //        if (IsThrowAway(furnitureData)) continue;


        //        //        string revitId = (string)furnitureData["dsProps"]["revitMetaData"]["elementId"];
        //        //        bool isExistingFurniture = false;
        //        //        FamilyInstance existingFamilyInstance = null;
        //        //        AssemblyInstance existingAssemblyInstance = null;
        //        //        Group existingGroup = null;
        //        //        FamilySymbol existingFamilySymbol = null;
        //        //        string existingFamilyType = "";

        //        //        if (revitId == null)
        //        //        {
        //        //            revitId = sourceElementId;
        //        //        }


        //        //        if (revitId != null)
        //        //        {
        //        //            using (SubTransaction trans = new SubTransaction(GlobalVariables.Document))
        //        //            {
        //        //                trans.Start();
        //        //                try
        //        //                {
        //        //                    Element e = GlobalVariables.Document.GetElement(new ElementId(int.Parse(revitId)));
        //        //                    isExistingFurniture = GlobalVariables.idToElement.TryGetValue(revitId, out Element _e);

        //        //                    if (isExistingFurniture || e.IsValidObject)
        //        //                    {
        //        //                        isExistingFurniture = true;
        //        //                        if (e.GetType().Name == "AssemblyInstance")
        //        //                        {
        //        //                            existingAssemblyInstance = (AssemblyInstance)e;
        //        //                            existingFamilyType = existingAssemblyInstance.Name;
        //        //                        }
        //        //                        else if(e.GetType().Name == "Group")
        //        //                        {
        //        //                            existingGroup = (Group)e;
        //        //                            existingFamilyType = existingGroup.Name;
        //        //                        }
        //        //                        else
        //        //                        {
        //        //                            existingFamilyInstance = (FamilyInstance)e;
        //        //                            existingFamilySymbol = GlobalVariables.idToFamilySymbol[revitId];
        //        //                            existingFamilyType = existingFamilySymbol.Name;

        //        //                            isFacingFlip = (existingFamilyInstance).FacingFlipped;
        //        //                        }


        //        //                        trans.Commit();
        //        //                    }
        //        //                }
        //        //                catch (Exception e)
        //        //                {
        //        //                    LogTrace(e.Message);
        //        //                }
        //        //            }
        //        //        }
        //        //        using (SubTransaction trans = new SubTransaction(GlobalVariables.Document))
        //        //        {
        //        //            trans.Start();

        //        //            // Creation ...................
        //        //            TrudeInterior st_interior = new TrudeInterior(furnitureData);

        //        //            FamilySymbol familySymbol = null;
        //        //            if (existingFamilySymbol != null && existingFamilySymbol.IsValidObject)
        //        //            {
        //        //                Parameter offsetParam = st_interior.GetOffsetParameter(existingFamilyInstance);
        //        //                if (existingFamilySymbol.Category.Name == "Casework" && offsetParam == null)
        //        //                {
        //        //                    BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

        //        //                    XYZ existingInstanceCenter = (bbox.Max + bbox.Min).Divide(2);

        //        //                    ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingFamilyInstance.Id, existingInstanceCenter.Multiply(-1)).First();

        //        //                    st_interior.element = GlobalVariables.Document.GetElement(newId);

        //        //                    BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
        //        //                    XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

        //        //                    double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

        //        //                    LocationPoint pt = (LocationPoint)st_interior.element.Location;
        //        //                    ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

        //        //                    ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

        //        //                    ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);

        //        //                    BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
        //        //                    XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

        //        //                    if (isFacingFlip)
        //        //                    {

        //        //                        XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
        //        //                        XYZ origin = (st_interior.element.Location as LocationPoint).Point;
        //        //                        Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
        //        //                        var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
        //        //                    }

        //        //                    if (st_interior.Scaling.Z < 0)
        //        //                    {
        //        //                        st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
        //        //                    }
        //        //                }
        //        //                else
        //        //                {
        //        //                    ElementId levelId = GlobalVariables.LevelIdByNumber[st_interior.levelNumber];
        //        //                    Level level = (Level)GlobalVariables.Document.GetElement(levelId);
        //        //                    st_interior.CreateWithFamilySymbol(existingFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
        //        //                }
        //        //            }
        //        //            else if (revitFamilyName != null)
        //        //            {
        //        //                if (existingFamilySymbol?.Category?.Name == "Casework")
        //        //                {
        //        //                    XYZ originalPoint = ((LocationPoint)existingFamilyInstance.Location).Point;

        //        //                    BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

        //        //                    XYZ center = (bbox.Max + bbox.Min).Divide(2);

        //        //                    ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingFamilyInstance.Id, center.Multiply(-1)).First();

        //        //                    st_interior.element = GlobalVariables.Document.GetElement(newId);

        //        //                    BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
        //        //                    XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

        //        //                    double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

        //        //                    LocationPoint pt = (LocationPoint)st_interior.element.Location;
        //        //                    ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

        //        //                    ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

        //        //                    ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);

        //        //                    BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
        //        //                    XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

        //        //                    if (isFacingFlip)
        //        //                    {

        //        //                        XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
        //        //                        XYZ origin = (st_interior.element.Location as LocationPoint).Point;
        //        //                        Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
        //        //                        var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
        //        //                    }

        //        //                    if (st_interior.Scaling.Z < 0)
        //        //                    {
        //        //                        st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
        //        //                    }
        //        //                }
        //        //                else
        //        //                {
        //        //                    FamilySymbol defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, revitFamilyName, familyType);
        //        //                    if (defaultFamilySymbol is null)
        //        //                    {
        //        //                        defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, revitFamilyName);
        //        //                    }
        //        //                    if (!defaultFamilySymbol.IsActive)
        //        //                    {
        //        //                        defaultFamilySymbol.Activate();
        //        //                        GlobalVariables.Document.Regenerate();
        //        //                    }
        //        //                    ElementId levelId = GlobalVariables.LevelIdByNumber[st_interior.levelNumber];
        //        //                    Level level = (Level)GlobalVariables.Document.GetElement(levelId);

        //        //                    st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
        //        //                }
        //        //            }
        //        //            else if (existingAssemblyInstance != null)
        //        //            {
        //        //                XYZ originalPoint = ((LocationPoint)existingAssemblyInstance.Location).Point;

        //        //                BoundingBoxXYZ bbox = existingAssemblyInstance.get_BoundingBox(null);

        //        //                //XYZ center = (bbox.Max + bbox.Min).Divide(2);
        //        //                XYZ center = ((LocationPoint)existingAssemblyInstance.Location).Point;

        //        //                ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingAssemblyInstance.Id, center.Multiply(-1)).First();

        //        //                st_interior.element = GlobalVariables.Document.GetElement(newId);

        //        //                BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
        //        //                //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

        //        //                //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

        //        //                LocationPoint pt = (LocationPoint)st_interior.element.Location;
        //        //                XYZ centerNew = pt.Point;
        //        //                //ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

        //        //                ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

        //        //                ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);

        //        //                BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
        //        //                //XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);
        //        //                XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

        //        //                if (isFacingFlip)
        //        //                {

        //        //                    XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
        //        //                    XYZ origin = (st_interior.element.Location as LocationPoint).Point;
        //        //                    Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
        //        //                    var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
        //        //                }

        //        //                if (st_interior.Scaling.Z < 0)
        //        //                {
        //        //                    st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
        //        //                }
        //        //            }
        //        //            else if (existingGroup != null)
        //        //            {
        //        //                XYZ originalPoint = ((LocationPoint)existingGroup.Location).Point;

        //        //                BoundingBoxXYZ bbox = existingGroup.get_BoundingBox(null);

        //        //                //XYZ center = (bbox.Max + bbox.Min).Divide(2);
        //        //                XYZ center = ((LocationPoint)existingGroup.Location).Point;

        //        //                ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingGroup.Id, center.Multiply(-1)).First();

        //        //                st_interior.element = GlobalVariables.Document.GetElement(newId);

        //        //                BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
        //        //                //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

        //        //                //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

        //        //                LocationPoint pt = (LocationPoint)st_interior.element.Location;
        //        //                XYZ centerNew = pt.Point;
        //        //                //ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);
        //        //                ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position - localOriginOffset);

        //        //                XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

        //        //                if (st_interior.Scaling.Z < 0)
        //        //                {
        //        //                    ElementTransformUtils.RotateElement(
        //        //                        GlobalVariables.Document,
        //        //                        newId,
        //        //                        Line.CreateBound(st_interior.Position, st_interior.Position + XYZ.BasisZ),
        //        //                        st_interior.eulerAngles.heading);
        //        //                }
        //        //                else
        //        //                {
        //        //                    ElementTransformUtils.RotateElement(
        //        //                        GlobalVariables.Document,
        //        //                        newId,
        //        //                        Line.CreateBound(st_interior.Position, st_interior.Position + XYZ.BasisZ),
        //        //                        -st_interior.eulerAngles.heading);
        //        //                }


        //        //                if (isFacingFlip)
        //        //                {
        //        //                    XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
        //        //                    XYZ origin = (st_interior.element.Location as LocationPoint).Point;
        //        //                    Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
        //        //                    var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
        //        //                }

        //        //                if (st_interior.Scaling.Z < 0)
        //        //                {
        //        //                    st_interior.SnaptrudeFlip(st_interior.element, st_interior.Position);
        //        //                }
        //        //            }
        //        //            else
        //        //            {
        //        //                //String familyName = st_interior.Name.RemoveIns();
        //        //                String familyName = st_interior.FamilyName;
        //        //                if (familyName is null) familyName = st_interior.FamilyTypeName;

        //        //                FamilySymbol defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName);
        //        //                //FamilySymbol defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(GlobalVariables.Document, "Casework Assembly", "Casework 044");
        //        //                if (defaultFamilySymbol is null)
        //        //                {
        //        //                    Family family = FamilyLoader.LoadCustomFamily(familyName);
        //        //                    defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName);
        //        //                    if (defaultFamilySymbol == null)
        //        //                    {
        //        //                        defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName.Replace("_", " "));
        //        //                    }
        //        //                }

        //        //                if (!defaultFamilySymbol.IsActive)
        //        //                {
        //        //                    defaultFamilySymbol.Activate();
        //        //                    GlobalVariables.Document.Regenerate();
        //        //                }
        //        //                ElementId levelId;
        //        //                if (GlobalVariables.LevelIdByNumber.ContainsKey(st_interior.levelNumber))
        //        //                    levelId = GlobalVariables.LevelIdByNumber[st_interior.levelNumber];
        //        //                else
        //        //                    levelId = GlobalVariables.LevelIdByNumber.First().Value;
        //        //                Level level = (Level)GlobalVariables.Document.GetElement(levelId);

        //        //                st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
        //        //            }
        //        //            if (st_interior.element is null)
        //        //            {
        //        //                st_interior.CreateWithDirectShape(GlobalVariables.Document);
        //        //            }

        //        //            try
        //        //            {
        //        //                if (isExistingFurniture)
        //        //                {
        //        //                    // delete original furniture
        //        //                    //if (existingFamilyInstance.IsValidObject) GlobalVariables.Document.Delete(existingFamilyInstance.Id);
        //        //                    if (existingFamilyInstance != null) sourcesIdsToDelete.Add(existingFamilyInstance.Id);
        //        //                    if (existingAssemblyInstance != null) sourcesIdsToDelete.Add(existingAssemblyInstance.Id);
        //        //                    if (existingGroup != null) sourcesIdsToDelete.Add(existingGroup.Id);
        //        //                }
        //        //            }
        //        //            catch
        //        //            {

        //        //            }

        //        //            TransactionStatus tstatus = trans.Commit();
        //        //            LogTrace(tstatus.ToString());
        //        //        }
        //        //        LogTrace("furniture created");
        //        //    }
        //        //    catch(OutOfMemoryException e)
        //        //    {
        //        //        LogTrace("furniture creation ERROR - out of memeroy -", e.ToString());
        //        //        break;
        //        //    }
        //        //    catch(Exception e)
        //        //    {
        //        //        LogTrace("furniture creation ERROR", e.ToString());
        //        //    }

        //        //}
        //        //try
        //        //{
        //        //    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
        //        //    {
        //        //        t.Start();
        //        //        GlobalVariables.Document.Delete(sourcesIdsToDelete);
        //        //        t.Commit();
        //        //    }
        //        //}
        //        //catch { }
        //        // ......................................................................
        //    }
        //}

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

        private void ShowReadOnlyDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Import Status");
            mainDialog.MainInstruction = "Snaptrude Import Status";
            mainDialog.MainContent = "Revit file is in read-only mode.";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }
    }
}
