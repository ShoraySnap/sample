using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DesignAutomationFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Media.Media3D;
using TrudeImporter;

namespace SnaptrudeForgeExport
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    /// <summary>
    ///     This is the main class responsible for all the operations to create the revit document.
    /// </summary>
    public class Command : IExternalDBApplication
    {
        //Path of the project(i.e)project where your Window family files are present
        public static IDictionary<int, ElementId> LevelIdByNumber = new Dictionary<int, ElementId>();
        public ExternalDBApplicationResult OnStartup(ControlledApplication application)
        {
            DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
            return ExternalDBApplicationResult.Succeeded;
        }
        private void HandleDesignAutomationReadyEvent(object sender, DesignAutomationReadyEventArgs e)
        {
            LogTrace("Design Automation Ready event triggered...");
            e.Succeeded = true;
            ParseTrude(e.DesignAutomationData);
        }

        /// <summary>
        ///  This method parses the trude file and creates corresponding revit document.
        /// </summary>
        private void ParseTrude(DesignAutomationData data)
        {
            if (data == null) throw new InvalidDataException(nameof(data));
            if (data.RevitApp == null) throw new InvalidDataException(nameof(data.RevitApp));

            JObject structureCollection = JObject.Parse(File.ReadAllText(Configs.INPUT_TRUDE));

            Application rvtApp = data.RevitApp;

            UnitsAdapter.metricSystem = (int) structureCollection.GetValue("userSettings")["unitsType"];
            //UnitSystem system = UnitsAdapter.metricSystem <= 2 ? UnitSystem.Metric : UnitSystem.Imperial;
            //Document newDoc = rvtApp.NewProjectDocument(system);
            Document newDoc = rvtApp.OpenDocumentFile("host.rvt");

            GlobalVariables.RvtApp = rvtApp;
            GlobalVariables.Document = newDoc;


            if (newDoc == null) throw new InvalidOperationException("Could not create new document.");

            ImportSnaptrude(structureCollection, newDoc);

            try
            {
                using (Transaction t = new Transaction(newDoc, "remove structural view"))
                {

                    View structuralView = Utils.GetElements(newDoc, typeof(View))
                                               .Select(e => e as View)
                                               .Where(e => e.Title == "Structural Plan: Level 1")
                                               .ToList().First();
                    t.Start();
                    newDoc.Delete(structuralView.Id);
                    t.Commit();
                }
            } catch { }

            List<View> printableViews = Utils.GetElements(newDoc, typeof(View))
                                       .Select(e => e as View)
                                       .Where(e => e.CanBePrinted)
                                       .ToList();

            using(Transaction t = new Transaction(newDoc, "Set View details levels and filter overrides"))
            {
                t.Start();

                // ThinWallFilter should be defined in host.rvt
                FilterElement filterElement = Utils.FindElement(newDoc, typeof(FilterElement), "ThinWallFilter") as FilterElement;

                foreach (View v in printableViews)
                {
                    v.DetailLevel = ViewDetailLevel.Fine;

                    if (v.GetFilters().Contains(filterElement.Id)) continue;
                    v.AddFilter(filterElement.Id);

                    OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                    overrideGraphicSettings.SetCutLineColor(new Color(0, 200, 200));
                    overrideGraphicSettings.SetCutLineWeight(1);

                    v.SetFilterOverrides(filterElement.Id, overrideGraphicSettings);

                    OverrideGraphicSettings overrides = new OverrideGraphicSettings();
                    overrides.SetSurfaceTransparency(50);
                    v.SetCategoryOverrides(new ElementId(BuiltInCategory.OST_Floors), overrides);
                }

                t.Commit();
            }

            string outputFormat = (string)structureCollection["outputFormat"];
            switch(outputFormat)
            {
                case "dwg":
                    ExportAllViewsAsDWG(newDoc);
                    break;
                case "ifc":
                    ExportIFC(newDoc);
                    break;
                case "pdf":
                    ExportPDF(newDoc, printableViews);
                    break;
                default:
                    SaveDocument(newDoc);
                    break;
            }
        }

        private void ExportPDF(Document newDoc, List<View> allViews)
        {
            Directory.CreateDirectory(Configs.PDF_EXPORT_DIRECTORY);

            List<ElementId> allViewIds = allViews.Select(v => v.Id).ToList();

            //using (Transaction t = new Transaction(newDoc, "Export to PDF"))
            //{
            //    t.Start();

            //    PDFExportOptions options = new PDFExportOptions
            //    {
            //        ColorDepth = ColorDepthType.Color,
            //        Combine = false,
            //        ExportQuality = PDFExportQualityType.DPI4000,
            //        //HideCropBoundaries = true,
            //        PaperFormat = ExportPaperFormat.Default,
            //        //HideReferencePlane = true,
            //        //HideScopeBoxes = true,
            //        //HideUnreferencedViewTags = true,
            //        //MaskCoincidentLines = true,
            //        //StopOnError = true,
            //        //ViewLinksInBlue = false,
            //        ZoomType = ZoomType.Zoom,
            //        ZoomPercentage = 100
            //    };
            //    newDoc.Export(Configs.PDF_EXPORT_DIRECTORY, allViewIds, options);
            //    t.Commit();
            //}

            if (File.Exists(Configs.OUTPUT_FILE)) File.Delete(Configs.OUTPUT_FILE);
            ZipFile.CreateFromDirectory(Configs.PDF_EXPORT_DIRECTORY, Configs.OUTPUT_FILE);

            Directory.Delete(Configs.PDF_EXPORT_DIRECTORY, true);
        }

        private void SaveDocument(Document newDoc)
        {
            ModelPath ProjectModelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(Configs.OUTPUT_FILE);
            SaveAsOptions SAO = new SaveAsOptions();
            SAO.OverwriteExistingFile = true;

            newDoc.SaveAs(ProjectModelPath, SAO);
            newDoc.Close();
        }

        private void ExportAllViewsAsDWG(Document newDoc)
        {
            Directory.CreateDirectory(Configs.DWG_EXPORT_DIRECTORY);

            List<View> allViews = Utils.GetElements(newDoc, typeof(View))
                                       .Select(e => e as View)
                                       .Where(e => e.CanBePrinted)
                                       .ToList();

            foreach (var view in allViews)
            {
                ExportDWG(newDoc, view);
            }

            if (File.Exists(Configs.OUTPUT_FILE)) File.Delete(Configs.OUTPUT_FILE);
            ZipFile.CreateFromDirectory(Configs.DWG_EXPORT_DIRECTORY, Configs.OUTPUT_FILE);

            Directory.Delete(Configs.DWG_EXPORT_DIRECTORY, true);
        }

        private bool ExportDWG(Document newDoc, View view)
        {
            List<ElementId> viewIds = new List<ElementId>(1);
            viewIds.Add(view.Id);

            bool exported = false;
            using (Transaction t = new Transaction(newDoc, "Export to DWG"))
            {
                t.Start();

                string filename = String.Concat(view.Title.Split(Path.GetInvalidFileNameChars()));

                exported = newDoc.Export(Configs.DWG_EXPORT_DIRECTORY, filename, viewIds, new DWGExportOptions());

                t.Commit();
            }

            return exported;
        }
        private void ExportIFC(Document newDoc)
        {
            using (Transaction t = new Transaction(newDoc, "Export to IFC"))
            {
                t.Start();

                IFCExportOptions options = new IFCExportOptions();
                newDoc.Export(".", Configs.OUTPUT_FILE, options);

                t.Commit();

                File.Move(Configs.OUTPUT_FILE + ".ifc", Configs.OUTPUT_FILE);
            }
        }

        private void ImportSnaptrude(JObject jObject, Document newDoc)
        {
            foreach (JToken structure in jObject.GetValue("structures"))
            {
                JToken structureData = structure.First;

                // STOREYS
                Level baseLevel = new FilteredElementCollector(newDoc).OfClass(typeof(Level)).FirstElement() as Level;
                ElementId baseLevelId = baseLevel.Id;

                LevelIdByNumber.Clear();
                LevelIdByNumber.Add(1, baseLevelId);

                JToken storeys = structureData["storeys"];
                if (!storeys.HasValues) continue;

                foreach (JToken storey in storeys)
                {
                    JToken storeyData = storey.First;
                    TrudeStorey newStorey = new TrudeStorey(storeyData);

                    using (Transaction t = new Transaction(newDoc))
                    {
                        t.Start("Create Level");
                        if (newStorey.levelNumber == 1)
                        {
                            baseLevel.Elevation = newStorey.basePosition;
                        }
                        else
                        {
                            newStorey.CreateLevel(newDoc);
                            LevelIdByNumber.Add(newStorey.levelNumber, newStorey.level.Id);
                        }
                        t.Commit();
                    }
                }
                LogTrace("storey created");

                JToken geometryParent = structureData["01"];
                if (geometryParent is null) continue;

                //WALLS
                JToken walls = geometryParent["walls"];
                int wallCount = 0;

                IDictionary<int, ElementId> childUniqueIdToWallElementId = new Dictionary<int, ElementId>();

                foreach (JToken wall in walls)
                {
                    try
                    {
                        JToken wallData = wall.First;
                        int uniqueId = (int)wallData["dsProps"]["uniqueID"];

                        using (Transaction trans = new Transaction(newDoc))
                        {
                            TrudeWall st_wall = new TrudeWall();
                            trans.Start("Create Walls");
                            try
                            {
                                if (IsThrowAway(wallData))
                                {
                                    continue;
                                }

                                List<Point3D> profilePoints = new List<Point3D>();
                                
                                foreach(JToken pointArray in wallData["profile"])
                                {
                                    profilePoints.Add(TrudeRepository.ArrayToXYZ(pointArray).ToPoint3D());
                                }

                                double baseHeight = UnitsAdapter.convertToRevit(wallData["baseHeight"]);

                                double height = -1;
                                try
                                {
                                    height = UnitsAdapter.convertToRevit(wallData["height"]);
                                }
                                catch
                                {

                                }

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
                                                 ? TrudeRepository.ArrayToXYZ(wallMeshDataforLevel["position"])
                                                 : TrudeRepository.ArrayToXYZ(wallMeshData["position"]);

                                st_wall.Layers = TrudeRepository.GetLayers(wallData);

                                st_wall.Scaling = TrudeRepository.GetScaling(wallData);

                                if (wallMeshDataforLevel["storey"] == null)
                                {
                                    continue;
                                }

                                st_wall.levelNumber = int.Parse(wallMeshDataforLevel["storey"].ToString());

                                List<XYZ> profilePointsXYZ = profilePoints.Select(p => p.ToXYZ()).ToList();
                                IList<Curve> profile = TrudeWall.GetProfile(profilePointsXYZ);

                                // Calculate and set thickness
                                string wallDirection = wallData["dsProps"]["direction"].Value<string>();

                                float thickness = wallData["thickness"] is null ? -1 : (float) wallData["thickness"];

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

                                    st_wall.Layers[index].IsCore = true;
                                    if (thickness > 0) st_wall.Layers[index].ThicknessInMm = thickness;
                                }

                                ElementId levelIdForWall;
                                levelIdForWall = LevelIdByNumber[st_wall.levelNumber];
                                Level level = (Level)newDoc.GetElement(levelIdForWall);

                                WallType wallType = TrudeWall.GetWallTypeByWallLayers(st_wall.Layers, newDoc);

                                st_wall.wall = st_wall.CreateWall(newDoc, profile, wallType.Id, level, height, baseHeight);

                                ElementId wallId = st_wall.wall.Id;

                                WallType _wallType = st_wall.wall.WallType;

                                TransactionStatus transactionStatus = trans.Commit();

                                // For some reason in a few rare cases, some transactions rolledback when walls are joined.
                                // This handles those cases to create the wall without being joined.
                                // This is not a perfect solution, ideally wall should be joined.
                                if (transactionStatus == TransactionStatus.RolledBack)
                                {
                                    trans.Start();
                                    st_wall.CreateWall(newDoc, profile, _wallType.Id, level, height);
                                    wallId = st_wall.wall.Id;

                                    WallUtils.DisallowWallJoinAtEnd(st_wall.wall, 0);
                                    WallUtils.DisallowWallJoinAtEnd(st_wall.wall, 1);

                                    //st_wall.wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(baseOffset);
                                    transactionStatus = trans.Commit();
                                }

                                //if (!(revitId is null))
                                //{
                                //    trans.Start();
                                //    StoreDataInElement(st_wall.wall, revitId);
                                //    //GetDataFromElement(st_wall);
                                //    trans.Commit();
                                //}

                                LogTrace("wall created");
                                wallCount++;

                                foreach (JToken childUID in wallData["meshes"][0]["childrenComp"])
                                {
                                    childUniqueIdToWallElementId.Add((int)childUID, wallId);
                                }

                            }
                            catch (Exception e)
                            {
                                LogTrace("Error in creating wall", e.ToString());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogTrace(e.Message);
                    }
                }
                TrudeWall.TypeStore.Clear();
                LogTrace("Walls Success");


                JToken floors = geometryParent["floors"];
                int count = 0;

                foreach (var floor in floors)
                {
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


                    Autodesk.Revit.DB.Material _materialElement = null;

                    foreach (var materialElement in materialsEnum)
                    {
                        String matName = materialElement.Name;

                        if (matName.Replace("_", " ") == _materialName)
                        {
                            _materialElement = materialElement;
                        }
                    }

                    using (Transaction transactionFloor = new Transaction(newDoc, "Create Floor"))
                    {
                        transactionFloor.Start();
                        try
                        {
                            if (IsThrowAway(floorData))
                            {
                                continue;
                            }

                            ElementId levelId = Command.LevelIdByNumber[TrudeRepository.GetLevelNumber(floorData)];
                            TrudeFloor st_floor = new TrudeFloor(floorData, newDoc, levelId);

                            try
                            {
                                List<List<XYZ>> holes = TrudeRepository.GetHoles(floorData);

                                foreach (var hole in holes)
                                {
                                    var holeProfile = TrudeWall.GetProfile(hole);
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

                            TransactionStatus status = transactionFloor.Commit();
                        }
                        catch (Exception e)
                        {
                            LogTrace("Error in creating floorslab", e.ToString());
                        }
                    }
                }
                LogTrace("floors created");

                JToken roofs = geometryParent["roofs"];
                foreach (var roof in roofs)
                {
                    using (Transaction transactionRoofs = new Transaction(newDoc, "Create Roofs"))
                    {
                        transactionRoofs.Start();

                        try
                        {
                            JToken roofData = roof.First;

                            if (IsThrowAway(roofData))
                            {
                                continue;
                            }

                            ElementId levelId = LevelIdByNumber[TrudeRepository.GetLevelNumber(roofData)];
                            TrudeRoof st_roof = new TrudeRoof(roofData, newDoc, levelId);

                            try
                            {
                                List<List<XYZ>> holes = TrudeRepository.GetHoles(roofData);

                                foreach (var hole in holes)
                                {
                                    var holeProfile = TrudeWall.GetProfile(hole);
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

                            TransactionStatus status = transactionRoofs.Commit();
                        }
                        catch (Exception e)
                        {
                            LogTrace("Error in creating floorslab", e.ToString());
                        }
                    }
                }
                TrudeFloor.TypeStore.Types.Clear();
                LogTrace("Roofs created");

                // Columns and Beams
                JToken masses = geometryParent["masses"];
                foreach (var mass in masses)
                {
                    try
                    {
                        using (Transaction transaction = new Transaction(newDoc, "create mass"))
                        {
                            transaction.Start();

                            JToken massData = mass.First;
                            JToken massMeshData = massData["meshes"].First;
                            JToken massGeometry = massData["geometries"];

                            if (IsThrowAway(massData)) continue;
                            if (massGeometry is null) continue;
                            if (massData["dsProps"]["storey"].Value<String>() is null) continue;

                            string massType = massData["dsProps"]["massType"].Value<String>();
                            if (massType.Equals("Column"))
                            {
                                ElementId levelId = LevelIdByNumber[TrudeRepository.GetLevelNumber(massData)];
                                TrudeColumn
                                    .FromMassData(massData)
                                    .CreateColumn(newDoc, levelId);
                            }
                            else if (massType.Equals("Beam"))
                            {
                                ElementId levelId = LevelIdByNumber[TrudeRepository.GetLevelNumber(massData)];
                                TrudeBeam
                                    .FromMassData(massData)
                                    .CreateBeam(newDoc, levelId);
                            }
                            transaction.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        LogTrace("Error Creating beam/column");
                    }
                }
                TrudeColumn.NewLevelsByElevation.Clear();
                TrudeColumn.types.Clear();
                TrudeBeam.types.Clear();

                LogTrace("beams and columns created");

                //DOORS
                JToken doors = geometryParent["doors"];
                //Transaction transactionDoors = new Transaction(newDoc);
                //transactionDoors.Start("Create doors");
                foreach (var door in doors)
                {
                    Transaction transaction = new Transaction(newDoc);
                    transaction.Start("Creating door");
                    try
                    {
                        TrudeDoor st_door = new TrudeDoor();

                        var doorData = door.First;
                        int uniqueId = (int)doorData["dsProps"]["uniqueID"];
                        JToken doorMeshData = doorData["meshes"].First;

                        double width = UnitsAdapter.convertToRevit(doorMeshData["width"]);
                        double height = UnitsAdapter.convertToRevit(doorMeshData["height"]);
                        XYZ direction = doorMeshData["direction"].IsNullOrEmpty()
                            ? XYZ.Zero
                            : TrudeRepository.ArrayToXYZ(doorMeshData["direction"], false).Round();

                        st_door.Name = doorMeshData["name"].ToString();
                        st_door.Geom_ID = doorMeshData["storey"].ToString();
                        st_door.Position = TrudeRepository.GetPosition(doorData);
                        st_door.family = doorMeshData["id"].ToString();
                        st_door.levelNumber = TrudeRepository.GetLevelNumber(doorData);
                        ElementId levelIdForWall = LevelIdByNumber[st_door.levelNumber];

                        try
                        {
                            st_door.family = st_door.family.RemoveIns();

                            string fsFamilyName = st_door.family;
                            string fsName = st_door.family;

                            Wall wall = null;
                            if (childUniqueIdToWallElementId.ContainsKey(uniqueId))
                            {
                                ElementId wallElementId = childUniqueIdToWallElementId[uniqueId];
                                wall = (Wall)newDoc.GetElement(wallElementId);
                            }


                            FamilySymbol familySymbol = null;
                            FamilySymbol defaultFamilySymbol = null;

                            var family = FamilyLoader.LoadCustomDoorFamily(fsFamilyName);
                            if (family is null)
                            {
                                LogTrace("couln't find door family");
                                continue;
                            }

                            defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, fsFamilyName, fsName);

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
                                familySymbol = TrudeDoor.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
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
                        transaction.RollBack();
                        LogTrace("Error in creating door", e.ToString());
                    }
                }
                LogTrace("doors created");
                TrudeDoor.TypeStore.Clear();

                JToken windows = geometryParent["windows"];
                foreach (var window in windows)
                {
                    using (Transaction transaction = new Transaction(newDoc, "Create Window"))
                    {
                        transaction.Start();
                        try
                        {
                            TrudeWindow st_window = new TrudeWindow();

                            var windowData = window.First;
                            int uniqueId = (int)windowData["dsProps"]["uniqueID"];
                            var windowMeshData = windowData["meshes"].First;

                            double width = UnitsAdapter.convertToRevit(windowMeshData["width"]);
                            double height = UnitsAdapter.convertToRevit(windowMeshData["height"]);

                            XYZ direction = windowMeshData["direction"].IsNullOrEmpty()
                                ? XYZ.Zero
                                : TrudeRepository.ArrayToXYZ(windowMeshData["direction"], false).Round();

                            st_window.Name = windowMeshData["name"].ToString();
                            st_window.Geom_ID = windowMeshData["storey"].ToString();
                            st_window.Position = TrudeRepository.GetPosition(windowData);
                            st_window.Scaling = TrudeRepository.GetScaling(windowData);
                            st_window.Rotation = TrudeRepository.GetRotation(windowData);
                            st_window.family = windowMeshData["id"].ToString();
                            //ElementId levelIdForWall = LevelIdByNumber[int.Parse(st_window.Geom_ID)];
                            st_window.levelNumber = TrudeRepository.GetLevelNumber(windowData);
                            var levelIdForWall = LevelIdByNumber[st_window.levelNumber];
                            //ElementId levelIdForWall = LevelIdByNumber[1];

                            double heightScale = 1;
                            double widthScale = 1;
                            if (windowData["meshes"][0]["originalScaling"] != null)
                            {
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

                                Wall wall = null;
                                if (childUniqueIdToWallElementId.ContainsKey(uniqueId))
                                {
                                    ElementId wallElementId = childUniqueIdToWallElementId[uniqueId];
                                    wall = (Wall)newDoc.GetElement(wallElementId);
                                }

                                FamilySymbol familySymbol = null;
                                FamilySymbol defaultFamilySymbol = null;
                                FamilyLoader.LoadCustomWindowFamily(fsFamilyName);

                                defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, fsFamilyName, fsName);

                                familySymbol = TrudeWindow.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);

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
                                    familySymbol = TrudeWindow.TypeStore.GetType(new double[] { height, width }, defaultFamilySymbol);
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
                TrudeWindow.TypeStore.Clear();

                //STAIRCASES
                ST_Staircase st_staircase = new ST_Staircase();
                JToken stairs = geometryParent["staircases"];
                foreach (var stair in stairs)
                {
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
                            using (Transaction stairsTrans = new Transaction(newDoc, "edit existing stairs"))
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
                        Transaction transactionDeleteRailings = new Transaction(newDoc, "Delete Railings");
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
                            transactionDeleteRailings.RollBack();
                            LogTrace("Error in deleting staircase railings", e.ToString());
                        }
                    }
                    catch (Exception exception)
                    {
                        //transaction.RollBack();
                        LogTrace("Error in creating staircase", exception.ToString());
                    }
                }
                LogTrace("staircases created");

                //FURNITURES
                JToken furnitures = geometryParent["furnitures"];

                foreach (var furniture in furnitures)
                {
                    var furnitureData = furniture.First;

                    try
                    {
                        if (IsThrowAway(furnitureData)) continue;

                        TrudeInterior st_interior = new TrudeInterior(furnitureData);

                        using (Transaction trans = new Transaction(newDoc))
                        {

                            trans.Start("Create furniture");

                            st_interior.CreateWithDirectShape(newDoc);

                            trans.Commit();
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
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
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
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
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
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
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
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
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

        /// <summary>
        /// This will appear on the Design Automation output
        /// </summary>
        public static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }
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

        public static String getMaterialNameFromMaterialId(String materialnameWithId, JArray subMeshes, JArray materials, JArray multiMaterials, int materialIndex)
        {
            if (materialnameWithId == null)
            {
                return null;
            }
            if (subMeshes == null)
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

            foreach (JToken eachMaterial in materials)
            {

                if (materialnameWithId == (String)eachMaterial["id"])
                {
                    materialName = materialnameWithId;
                }

            }

            if (materialName == null)
            {
                foreach (JToken eachMultiMaterial in multiMaterials)
                {
                    if (materialnameWithId == (String)eachMultiMaterial["id"])
                    {
                        if (!eachMultiMaterial["materials"].IsNullOrEmpty())
                        {
                            materialName = (String)eachMultiMaterial["materials"][materialIndex];
                        }
                    }
                }

            }

            return materialName;
        }
    }
}

