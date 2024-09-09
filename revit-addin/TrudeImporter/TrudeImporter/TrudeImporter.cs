using Autodesk.Revit.DB;
using System.Collections.Generic;
using System;
using System.Linq;
using TrudeImporter.TrudeImporter.Model;
using System.Diagnostics;
using NLog;
using System.Data.Common;
using Autodesk.Revit.DB.Architecture;
#if !FORGE
using TrudeCommon.Analytics;
using TrudeCommon.Utils;
#endif

using Autodesk.Revit.UI;



#if !FORGE
using SnaptrudeManagerAddin;
#endif

namespace TrudeImporter
{
    public class TrudeImporterMain
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static int prevProgress = 0;
        private static bool abort = false;
        private static object mutex = new object();
        public static bool Abort
        {
            get
            {
                return abort;
            }
            set
            {
                lock (mutex)
                {
                    abort = value;
                }
            }
        }

        private static string ImportDurationMessage;
        public static void Import(TrudeProperties trudeProperties)
        {
            TrudeExportLogger.Instance.CountInputElements(trudeProperties.SnaptrudeLog);
            TrudeExportLogger.Instance.LogExportStatus(
                trudeProperties.TrudeGeneration,
                "snaptrude"
            );
            if (trudeProperties.Errors != null)
            {
                foreach (var error in trudeProperties.Errors)
                {
                    TrudeExportLogger.Instance.LogError(error);
                }
            }
            ExportIdentifier identifier = trudeProperties.Identifier;

            Abort = false;
            GlobalVariables.MissingDoorFamiliesCount.Clear();
            GlobalVariables.MissingWindowFamiliesCount.Clear();

            GlobalVariables.MissingDoorIndexes.Clear();
            GlobalVariables.MissingWindowIndexes.Clear();

            prevProgress = 0;

            ImportDurationMessage = "Import duration: ";
            Stopwatch sw = Stopwatch.StartNew();

            ImportCategory("Textures", () => new FetchTextures.FetchTextures(), "Fetching textures...", 5);
            ImportCategory("Deletion", () => DeleteRemovedElements(GlobalVariables.TrudeProperties.DeletedElements), "Deleting removed elements...", 5);
            ImportCategory("Storeys", () => ImportStories(trudeProperties.Storeys, trudeProperties.IsRevitImport), "Importing Stories...", 5);
            ImportCategory("StairCases", () => ImportStairCases(trudeProperties.Staircases), "Importing Stairs...", 5);
            ImportCategory("Walls", () => ImportWalls(trudeProperties.Walls), "Importing Walls...", 10);
            ImportCategory("Floors", () => ImportFloors(trudeProperties.Floors), "Importing Floors...", 5);
            ImportCategory("Columns", () => ImportColumns(trudeProperties.Columns), "Importing Columns...", 5);
            ImportCategory("Masses", () => ImportMasses(trudeProperties.Masses), "Importing Masses...", 5);
            ImportCategory("Rooms", () => { if (GlobalVariables.ImportLabels) ImportRooms(); }, "Importing Rooms...", 5);
            ImportCategory("Beams", () => ImportBeams(trudeProperties.Beams), "Importing Beams...", 5);
#if REVIT2019 || REVIT2020 || REVIT2021
            ImportCategory("Ceilings", () => ImportFloors(trudeProperties.Ceilings), "Importing Ceilings...", 5);
#else
            ImportCategory("Ceilings", () => ImportCeilings(trudeProperties.Ceilings), "Importing Ceilings...", 5);
#endif
            ImportCategory("Slabs", () => ImportSlabs(trudeProperties.Slabs), "Importing Slabs...", 5);
            ImportCategory("Doors", () => ImportDoors(trudeProperties.Doors), "Importing Doors...", 10);
            ImportCategory("Windows", () => ImportWindows(trudeProperties.Windows), "Importing Windows...", 10);
            ImportCategory("Furniture", () => ImportFurniture(trudeProperties.Furniture), "Importing Furniture...", 10);
            if (GlobalVariables.MissingDoorFamiliesCount.Count > 0 || GlobalVariables.MissingWindowFamiliesCount.Count > 0 || GlobalVariables.MissingFurnitureFamiliesCount.Count > 0)
                ImportCategory("Missing", () => ImportMissing(trudeProperties.Doors, trudeProperties.Windows, trudeProperties.Furniture), "Please link missing rfas in the other window...", 5);

            ImportDurationMessage += $"Total: {Math.Round(sw.Elapsed.TotalSeconds, 2)}s.";
            logger.Info(ImportDurationMessage);
            TrudeExportLogger.Instance.LogExportStatus(
                sw.Elapsed.TotalSeconds,
                "success",
                trudeProperties.IsRevitImport ? "existing" : "new",
                "revit"
            );
            TrudeExportLogger.Instance.Save();

#if !FORGE

            if (identifier != null)
            {
                Config config = Config.GetConfigObject();
                string hash = Util.GetUniqueHash(GlobalVariables.Document.PathName, 12);
                string version = Application.Instance.GetVersion();
                AnalyticsManager.SetIdentifer(identifier.email, config.userId, identifier.floorkey, identifier.units, identifier.env, hash, version);
                AnalyticsManager.SetData(TrudeExportLogger.Instance.GetSerializedObject());
                AnalyticsManager.Save("export_analytics.json");
                AnalyticsManager.SaveForUpload();
            }
            else
            {
                logger.Error("Unsupported .trude file for analytics!");
            }
#endif
        }

        private static void ImportCategory(string categoryName, Action task, string progressMessage, int progressValue)
        {
#if !FORGE
            if (!Abort)
            {
                Application.Instance.UpdateProgressForImport(prevProgress, progressMessage);
                Stopwatch sw = Stopwatch.StartNew();
#endif
                task();
#if !FORGE
                ImportDurationMessage += $"{categoryName}: {Math.Round(sw.Elapsed.TotalSeconds, 2)}s | ";
                sw.Stop();
                prevProgress += progressValue;
            }
            else
            {
                logger.Info("Import aborted.");
                if (GlobalVariables.Transaction.GetStatus() == TransactionStatus.Started)
                {
                    GlobalVariables.Transaction.RollBack();
                }
            }
#endif
        }

        private static void DeleteRemovedElements(List<int> elementIds)
        {
            foreach (int elementId in elementIds)
            {
                try
                {
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                    ElementId id = new ElementId((int)elementId);
#else
                    ElementId id = new ElementId((Int64)elementId);
#endif
                    Element element = GlobalVariables.Document.GetElement(id);

                    TrudeExportLoggerHelper.DeleteCountLogger(element);

                    if (!element.GroupId.Equals(ElementId.InvalidElementId))
                        TrudeImporterMain.deleteIfInGroup(element);
                    else
                        GlobalVariables.Document.Delete(id);
                }
                catch (Exception e)
                {
                    logger.Error("Exception in removing deleted elements:" + e.Message);
                    System.Diagnostics.Debug.WriteLine("Exception in removing deleted elements:" + e.Message);
                }
            }
        }

        private static void ImportStories(List<StoreyProperties> propsList, bool isRevitImport)
        {
            if (propsList == null) return;
            var storiesWithMatchingLevelIds = new List<(TrudeStorey Storey, Level Level)>();
            var storiesToCreate = new List<TrudeStorey>();
            var levelsToCheckElevation = new List<Level>();
            var levelsToDelete = new List<Level>();

            // Get levels to create, delete and change
            try
            {
                if (Abort) return;

                var existingLevels = new FilteredElementCollector(GlobalVariables.Document)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Levels)
                    .Cast<Level>();

                var existingLevelNames = existingLevels.Select(level => level.Name);
                var storeyNames = new List<string>();

                foreach (var storeyProperties in propsList)
                {
                    TrudeStorey storey = new TrudeStorey(storeyProperties);
                    storeyNames.Add(storey.RevitName);
#if (REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023)
                    var levelWithSameId = existingLevels.FirstOrDefault(l => l.Id.IntegerValue == storeyProperties.LowerLevelElementId);
#else
                    var levelWithSameId = existingLevels.FirstOrDefault(l => l.Id.Value == storeyProperties.LowerLevelElementId);
#endif
                    if (!levelWithSameId.IsNull())
                    {
                        storiesWithMatchingLevelIds.Add((storey, levelWithSameId));
                        if (existingLevelNames.Contains(levelWithSameId.Name))
                        {
                            existingLevelNames = existingLevelNames.Where(a => a != levelWithSameId.Name);
                        }
                    }
                    else
                        if (!existingLevelNames.Contains(storey.RevitName)) storiesToCreate.Add(storey);
                }

                foreach (var level in existingLevels)
                {
                    if (storeyNames.Contains(level.Name)) levelsToCheckElevation.Add(level);
                    else
                    {
                        if (!isRevitImport)
                        {
                            ElementLevelFilter levelFilter = new ElementLevelFilter(level.Id);
                            var elementsInLevel = new FilteredElementCollector(GlobalVariables.Document)
                                .WhereElementIsNotElementType()
                                .WherePasses(levelFilter)
                                .ToList();
                            if (!elementsInLevel.Any())
                                levelsToDelete.Add(level);
                        }
                    }
                }

                //Revit dont allow to delete the level that is associated with the current activeView
                //If the activeView's GenLevel is set to be deleted, this will change the name and elevation of this level to equals the first in storiesToCreate, and remove it from the deleted list
                var levelAssociatedWithActiveView = GlobalVariables.Document.ActiveView.GenLevel;
                if (levelAssociatedWithActiveView != null)
                {
                    if (levelsToDelete.Select(l => l.Name).Contains(levelAssociatedWithActiveView.Name))
                    {
                        TrudeStorey firstStorey = storiesToCreate.Any() ? storiesToCreate[0] : storiesWithMatchingLevelIds[0].Storey;

                        levelAssociatedWithActiveView.Name = firstStorey.RevitName;
                        levelAssociatedWithActiveView.Elevation = firstStorey.Elevation;
                        GlobalVariables.LevelIdByNumber.Add(firstStorey.LevelNumber, levelAssociatedWithActiveView.Id);

                        if (storiesToCreate.Any())
                            storiesToCreate = storiesToCreate.Skip(1).ToList();
                        else
                            storiesWithMatchingLevelIds = storiesWithMatchingLevelIds.Skip(1).ToList();
                        levelsToDelete = levelsToDelete.Where(l => l.Name != firstStorey.RevitName).ToList();
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }


            if (propsList.Count == 0)
            {
                try
                {
                    const int levelNumber = 0;
                    const double elevation = 0;
                    TrudeStorey newStorey = new TrudeStorey(levelNumber, elevation, Utils.RandomString());
                    newStorey.CreateLevel(GlobalVariables.Document);
                    GlobalVariables.LevelIdByNumber.Add(newStorey.LevelNumber, newStorey.Level.Id);
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }
            }

            try
            {
                foreach (var matchById in storiesWithMatchingLevelIds)
                {
                    matchById.Level.Name = matchById.Storey.RevitName;
                    matchById.Level.Elevation = matchById.Storey.Elevation;
                }

                foreach (var level in levelsToCheckElevation)
                {
                    StoreyProperties matchProp = propsList.First(prop => level.Name == prop.Name || level.Name == (prop.LevelNumber - 1).ToString());
                    if (!GlobalVariables.LevelIdByNumber.ContainsKey(matchProp.LevelNumber))
                        GlobalVariables.LevelIdByNumber.Add(matchProp.LevelNumber, level.Id);
                    if (matchProp.Elevation != level.Elevation) level.Elevation = matchProp.Elevation;
                }
                if (levelsToDelete.Any()) GlobalVariables.Document.Delete(levelsToDelete.Select(level => level.Id).ToList());

                logger.Info("Stories edited/deleted");
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }
            logger.Info("Existing stories handled");

            foreach (TrudeStorey newStorey in storiesToCreate)
            {
                try
                {
                    newStorey.CreateLevel(GlobalVariables.Document);
                    GlobalVariables.LevelIdByNumber.Add(newStorey.LevelNumber, newStorey.Level.Id);
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                    LogTrace(e.Message);
                }
            }
            logger.Info("Stories created");

        }

        private static void ImportWalls(List<WallProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            Utils.TryStartTransaction();
            foreach (WallProperties props in propsList)
            {
                if (Abort) return;
                if (props.IsStackedWall && !props.IsStackedWallParent) continue;
                // if (props.Storey is null) continue;

                try
                {
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        props.MaterialName,
                        props.FaceMaterialIds,
                        props.AllFaceVertices
                    );

                    if (props.AllFaceVertices != null)
                    {
                        TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_Walls);
                    }
                    else
                    {
                        TrudeWall trudeWall = new TrudeWall(props, false);
                    }
                    deleteOld(props.ExistingElementId);

                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_WALL_KEY,
                        props.AllFaceVertices == null,
                        props.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit wall",
                        e.Message,
                        props.UniqueId
                    );
                    logger.Error("Exception in Importing Wall: " + props.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Wall: " + props.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }

            Utils.TryStartTransaction();
            foreach (var wallIdToRecreate in GlobalVariables.WallElementIdsToRecreate)
            {
                int matchUniqueId = GlobalVariables.UniqueIdToElementId.First(x => x.Value == wallIdToRecreate).Key;
                WallProperties props = propsList.First(p => matchUniqueId == p.UniqueId);
                TrudeWall trudeWall = new TrudeWall(props, true);
            }
            Utils.TryStartTransaction(); // Temporary start before complete refactor of transactions

            TrudeWall.TypeStore.Clear();
            LogTrace("Finished Walls");
        }

        private static void ImportBeams(List<BeamProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            GlobalVariables.Transaction.Commit();

            foreach (var beam in propsList)
            {
                if (Abort) return;

                try
                {
                    Utils.TryStartTransaction();
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        beam.MaterialName,
                        beam.FaceMaterialIds,
                        beam.AllFaceVertices
                    );
                    if (beam.AllFaceVertices != null)
                    {
                        TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_StructuralFraming);
                    }
                    else
                    {
                        new TrudeBeam(beam, GlobalVariables.LevelIdByNumber[beam.Storey]);
                    }

                    deleteOld(beam.ExistingElementId);
                    GlobalVariables.Transaction.Commit();

                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_BEAM_KEY,
                        beam.AllFaceVertices == null,
                        beam.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit beam",
                        e.Message,
                        beam.UniqueId
                    );
                    GlobalVariables.Transaction.RollBack();
                    logger.Error("Exception in Importing Beam:" + beam.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Beam:" + beam.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }

            Utils.TryStartTransaction();
        }

        private static void ImportColumns(List<ColumnProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            foreach (var column in propsList)
            {
                if (Abort) return;

                try
                {
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        column.MaterialName,
                        column.FaceMaterialIds,
                        column.AllFaceVertices
                        );

                    if (column.AllFaceVertices != null)
                    {
                        TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_Columns);
                        deleteOld(column.ExistingElementIdDS);
                    }
                    else
                    {
                        new TrudeColumn(column);

                        foreach (var instance in column.Instances)
                        {
                            deleteOld(instance.ExistingElementId);
                        }
                    }

                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_COLUMN_KEY,
                        column.AllFaceVertices == null,
                        column.ExistingElementIdDS != null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit column",
                        e.Message,
                        column.UniqueIdDS
                    );
                    int logUniqueID = column.AllFaceVertices == null ? column.Instances[0].UniqueId : column.UniqueIdDS;
                    logger.Error("Exception in Importing Column: " + logUniqueID + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Column: " + logUniqueID + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportRooms()
        {
            if (!GlobalVariables.CreatedFloorsByLevel.Any()) return;
            GlobalVariables.Document.Regenerate();

            double computationalHeightInMM = 950;
            List<(Element Element, Solid Solid)> roomBoundingElements = new List<(Element Element, Solid)>();
            try
            {
                roomBoundingElements = new FilteredElementCollector(GlobalVariables.Document)
                    .WhereElementIsNotElementType()
                    .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory> { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Columns }))
                    .Select(e => (e, Utils.GetElementSolid(e)))
                    .ToList();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception in getting room boundaing elements" + "\nError is: " + e.Message + "\n");
            }

            foreach (var levelId in GlobalVariables.CreatedFloorsByLevel.Keys)
            {
                if (Abort) return;

                try
                {
                    double cutPlaneElevation = (GlobalVariables.Document.GetElement(levelId) as Level).ProjectElevation + UnitsAdapter.MMToFeet(computationalHeightInMM);

                    Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, cutPlaneElevation));

                    List<(Element Element, Solid Solid)> roomBoundingElementsInLevel = new List<(Element Element, Solid Solid)>();

                    foreach (var e in roomBoundingElements)
                    {
                        if (e.Solid != null)
                        {
                            Solid cut = BooleanOperationsUtils.CutWithHalfSpace(e.Solid, plane);
                            if (cut != null)
                            {
                                if (e.Solid.Volume - cut.Volume > 0.1)
                                {
                                    roomBoundingElementsInLevel.Add(e);
                                }
                            }
                        }
                    }

                    List<Solid> solidsInLevel = Utils.JoinSolids(roomBoundingElementsInLevel.Select(x => x.Solid).ToList());

                    GlobalVariables.Document.GetElement(levelId).get_Parameter(BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT)
                        .Set(UnitsAdapter.MMToFeet(computationalHeightInMM));

                    CurveArray curveArray = new CurveArray();

                    foreach (var floor in GlobalVariables.CreatedFloorsByLevel[levelId])
                    {
                        if (!floor.IsDirectShape)
                        {
                            floor.Solid = Utils.GetElementSolid(GlobalVariables.Document.GetElement(floor.Id));
                        }
                        if (floor.CurveArray != null)
                        {
                            foreach (Curve curve in floor.CurveArray)
                            {
                                curveArray.Append(curve);
                            }
                        }
                    }

                    List<(Curve Curve, XYZ Direction)> elementBoundariesInLevel = new List<(Curve Curve, XYZ Direction)>();

                    foreach (var element in roomBoundingElementsInLevel)
                    {
                        try
                        {
                            Face bottomFace = element.Solid.Faces
                                .Cast<Face>()
                                .Where(f => f.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(-XYZ.BasisZ))
                                .OrderBy(f => f.Area)
                                .LastOrDefault();

                            if (bottomFace != null)
                            {
                                List<Curve> curves = bottomFace.GetEdgesAsCurveLoops()[0].OrderBy(c => c.Length).ToList();
                                for (int i = element.Element is Wall ? curves.Count - 2 : 0; i < curves.Count; i++)
                                {
                                    XYZ curveDirection = (curves[i].GetEndPoint(1) - curves[i].GetEndPoint(0)).Normalize();
                                    Curve curveInHeight0 = curves[i].CreateTransformed(Transform.CreateTranslation(-XYZ.BasisZ * curves[i].GetEndPoint(0).Z));
                                    elementBoundariesInLevel.Add((curveInHeight0, curveDirection));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("Exception in getting Wall solid" + "\nError is: " + e.Message + "\n");
                        }
                    }

                    CurveArray roomBoundariesToCreate = new CurveArray();
                    for (int i = 0; i < curveArray.Size; i++)
                    {
                        Curve roomBoundary = curveArray.get_Item(i);
                        if (roomBoundary.GetEndPoint(0).Z != roomBoundary.GetEndPoint(1).Z) continue;

                        if (!Utils.CheckIfPointIsInsideSolidProjection(solidsInLevel, roomBoundary.GetEndPoint(0)) ||
                            !Utils.CheckIfPointIsInsideSolidProjection(solidsInLevel, roomBoundary.GetEndPoint(1)) ||
                            !Utils.CheckIfPointIsInsideSolidProjection(solidsInLevel, roomBoundary.Evaluate(0.5, true)))
                            roomBoundariesToCreate.Append(roomBoundary);
                    }

                    if (roomBoundariesToCreate.Size > 0)
                    {
                        Autodesk.Revit.DB.View levelView = new FilteredElementCollector(GlobalVariables.Document)
                        .OfClass(typeof(ViewPlan))
                        .Cast<Autodesk.Revit.DB.View>()
                        .FirstOrDefault(v => v.GenLevel?.Id == levelId);

                        SketchPlane levelPlane = SketchPlane.Create(GlobalVariables.Document, levelId);

                        GlobalVariables.Document.Create.NewRoomBoundaryLines(levelPlane, roomBoundariesToCreate, levelView);
                    }

                    List<ElementId> createdRoomIds = GlobalVariables.Document.Create
                        .NewRooms2(GlobalVariables.Document.GetElement(levelId) as Level)
                        .ToList();

                    List<ElementId> roomsToDelete = new List<ElementId>();

                    foreach (var roomId in createdRoomIds)
                    {
                        XYZ roomLocation = (GlobalVariables.Document.GetElement(roomId).Location as LocationPoint).Point;
                        bool roomMatched = false;
                        var filteredDictionary = GlobalVariables.CreatedFloorsByLevel[levelId].Where(x => !x.RoomMatched);
                        for (int i = 0; i < filteredDictionary.Count(); i++)
                        {
                            var floor = filteredDictionary.ElementAt(i);
                            bool roomInProjection = false;
                            if (floor.IsDirectShape)
                            {
                                if (Utils.IsPointInsideElementGeometryProjection(GlobalVariables.Document.GetElement(floor.Id), roomLocation, FindReferenceTarget.Element))
                                {
                                    roomInProjection = true;
                                }
                            }
                            else
                            {
                                if (Utils.CheckIfPointIsInsideSolidProjection(new List<Solid> { floor.Solid }, roomLocation))
                                {
                                    roomInProjection = true;
                                }
                            }
                            if (roomInProjection)
                            {
                                roomMatched = true;
                                floor.RoomMatched = true;
                                GlobalVariables.Document.GetElement(roomId).get_Parameter(BuiltInParameter.ROOM_NAME).Set(floor.Label);
                                break;
                            }
                        }
                        if (!roomMatched)
                        {
                            roomsToDelete.Add(roomId);
                        }
                    }

                    foreach (var roomId in roomsToDelete)
                    {
                        GlobalVariables.Document.Delete(roomId);
                    }
                }
                catch (Exception e)
                {
                    logger.Error("Exception in generating room labels for level: " + levelId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in generating room labels for level: " + levelId + "\nError is: " + e.Message + "\n");

                }
            }

        }

        private static void ImportFloors(List<FloorProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            foreach (var floor in propsList)
            {
                if (Abort) return;

                try
                {
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        floor.MaterialName,
                        floor.FaceMaterialIds,
                        floor.AllFaceVertices
                        );
                    if (floor.AllFaceVertices != null)
                    {
                        DirectShape directShape = TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_Floors);
                        if (GlobalVariables.ImportLabels && floor.FaceVertices != null)
                        {
                            ElementId levelId = GlobalVariables.LevelIdByNumber[floor.Storey];
                            CurveArray profile = TrudeRoom.getProfile(floor.FaceVertices);
                            TrudeRoom.StoreRoomData(levelId, floor.RoomType, directShape, profile);
                        }
                    }
                    else
                    {
                        new TrudeFloor(floor, GlobalVariables.LevelIdByNumber[floor.Storey]);
                    }

                    deleteOld(floor.ExistingElementId);

                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_FLOOR_KEY,
                        floor.AllFaceVertices == null,
                        floor.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {

                    TrudeExportLogger.Instance.LogError(
                        "revit floor",
                        e.Message,
                        floor.UniqueId
                    );
                    logger.Error("Exception in Importing Floor: " + floor.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Floor: " + floor.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportSlabs(List<SlabProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            foreach (var slab in propsList)
            {
                if (Abort) return;

                try
                {
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        slab.MaterialName,
                        slab.FaceMaterialIds,
                        slab.AllFaceVertices
                        );
                    if (slab.AllFaceVertices != null)
                    {
                        TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_Floors);
                    }
                    else
                    {
                        new TrudeSlab(slab);
                    }

                    deleteOld(slab.ExistingElementId);

                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_SLAB_KEY,
                        slab.AllFaceVertices == null,
                        slab.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit slab",
                        e.Message,
                        slab.UniqueId
                    );
                    logger.Error("Exception in Importing Slab: " + slab.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Slab: " + slab.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportDoors(List<DoorProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            foreach (var (door, index) in propsList.WithIndex())
            {
                if (Abort) return;

                deleteOld(door.ExistingElementId);
                try
                {
                    new TrudeDoor(door, GlobalVariables.LevelIdByNumber[door.Storey], index);
                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_DOOR_KEY,
                        true,
                        door.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit door",
                        e.Message,
                        door.UniqueId
                    );
                    logger.Error("Exception in Importing Door: " + door.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Door: " + door.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportWindows(List<WindowProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            foreach (var (window, index) in propsList.WithIndex())
            {
                if (Abort) return;

                deleteOld(window.ExistingElementId);
                try
                {
                    new TrudeWindow(window, GlobalVariables.LevelIdByNumber[window.Storey], index);
                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_WINDOW_KEY,
                        true,
                        window.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit window",
                        e.Message,
                        window.UniqueId
                    );
                    logger.Error("Exception in Importing Window: " + window.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Window: " + window.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportFurniture(List<FurnitureProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            List<ElementId> sourceIdsToDelete = new List<ElementId>();
            foreach (var (furniture, index) in propsList.WithIndex())
            {
                if (Abort) return;
                try
                {
                    new TrudeFurniture(furniture, sourceIdsToDelete, index);
                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_FURNITURE_KEY,
                        true,
                        furniture.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit furniture",
                        e.Message,
                        furniture.UniqueId
                    );
                    logger.Error("Exception in Importing Furniture: " + furniture.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Furniture: " + furniture.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
            GlobalVariables.Document.Delete(sourceIdsToDelete);
        }

        private static void ImportCeilings(List<FloorProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;

            foreach (var ceiling in propsList)
            {
                if (Abort) return;

                try
                {
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        ceiling.MaterialName,
                        ceiling.FaceMaterialIds,
                        ceiling.AllFaceVertices
                        );
                    if (ceiling.AllFaceVertices != null)
                    {
                        TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_Ceilings);
                    }
                    else
                    {
                        new TrudeCeiling(ceiling, GlobalVariables.LevelIdByNumber[ceiling.Storey]);
                    }

                    deleteOld(ceiling.ExistingElementId);
                    TrudeExportLogger.Instance.CountOutputElements(
                        TrudeExportLoggerHelper.BASIC_CEILING_KEY,
                        ceiling.AllFaceVertices == null,
                        ceiling.ExistingElementId == null ? "added" : "updated"
                    );
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit ceiling",
                        e.Message,
                        ceiling.UniqueId
                    );
                    logger.Error("Exception in Importing Ceiling: " + ceiling.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Ceiling: " + ceiling.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportMasses(List<MassProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;

            foreach (var mass in propsList)
            {
                if (Abort) return;

                try
                {
                    DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        mass.MaterialName,
                        mass.FaceMaterialIds,
                        mass.AllFaceVertices
                        );
                    if (mass.AllFaceVertices != null)
                    {
                        DirectShape directShape = TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_GenericModel);
                        if (mass.Type == "Room" && mass.RoomType != "Default" && mass.BottomFaceVertices != null)
                        {
                            CurveArray profile = TrudeRoom.getProfile(mass.BottomFaceVertices);
                            ElementId levelId = GlobalVariables.LevelIdByNumber[mass.Storey];
                            TrudeRoom.StoreRoomData(levelId, mass.RoomType, directShape, profile);
                        }
                        TrudeExportLogger.Instance.CountOutputElements(
                            TrudeExportLoggerHelper.MASSES_KEY,
                            false,
                            mass.ExistingElementId == null ? "added" : "updated"
                        );
                    }
                    deleteOld(mass.ExistingElementId);
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit mass",
                        e.Message,
                        mass.UniqueId
                    );
                    logger.Error("Exception in Importing Mass:" + mass.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Mass:" + mass.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
        }

        private static void ImportStairCases(List<StairCaseProperties> propsList)
        {
            if (propsList == null || !propsList.Any()) return;
            if (GlobalVariables.Transaction.HasStarted()) GlobalVariables.Transaction.Commit();

            GlobalVariables.StairsEditScope = new StairsEditScope(GlobalVariables.Document, "Stairs");
            foreach (var staircase in propsList)
            {
                if (Abort) return;

                try
                {
                    if (staircase.AllFaceVertices != null)
                    {
                        DirectShapeProperties directShapeProps = new DirectShapeProperties(
                        staircase.MaterialName,
                        staircase.FaceMaterialIds,
                        staircase.AllFaceVertices
                    );
                        Utils.TryStartTransaction();
                        TrudeDirectShape.GenerateObjectFromFaces(directShapeProps, BuiltInCategory.OST_Stairs);
                        GlobalVariables.Transaction.Commit();
                    }
                    else
                    {
                        new TrudeStaircase(staircase, GlobalVariables.LevelIdByNumber[staircase.Storey]);
                    }

                    deleteOld(staircase.ExistingElementId);
                }
                catch (Exception e)
                {
                    TrudeExportLogger.Instance.LogError(
                        "revit staircase",
                        e.Message,
                        staircase.UniqueId
                    );
                    if (GlobalVariables.Transaction.HasStarted()) GlobalVariables.Transaction.RollBack();
                    if (GlobalVariables.StairsEditScope.IsActive) GlobalVariables.StairsEditScope.Cancel();
                    logger.Error("Exception in Importing Staircase: " + staircase.UniqueId + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Staircase: " + staircase.UniqueId + "\nError is: " + e.Message + "\n");
                }
            }
            Utils.TryStartTransaction();

        }


        private static void ImportMissing(List<DoorProperties> propsListDoors, List<WindowProperties> propsListWindows, List<FurnitureProperties> propsListFurniture)
        {
            if (propsListDoors.Count == 0 && propsListWindows.Count == 0 && propsListFurniture.Count == 0) return;
            TrudeExportLogger.Instance.LogMissingRFA("window", GlobalVariables.MissingWindowFamiliesCount.Count);
            TrudeExportLogger.Instance.LogMissingRFA("door", GlobalVariables.MissingDoorFamiliesCount.Count);
            TrudeExportLogger.Instance.LogMissingRFA("furniture", GlobalVariables.MissingFurnitureFamiliesCount.Count);

#if !FORGE
            FamilyUploadMVVM familyUploadMVVM = new FamilyUploadMVVM();
            var result = familyUploadMVVM.ShowDialog();
            if (!familyUploadMVVM.WindowViewModel._skipAll)
            {
                System.Diagnostics.Debug.WriteLine("Importing Missing Families");
                try
                {
                    if (Abort) return;
                    if (GlobalVariables.MissingDoorFamiliesCount.Count > 0)
                    {
                        TrudeMissing.ImportMissingDoors(propsListDoors);
                    }
                    if (Abort) return;
                    if (GlobalVariables.MissingWindowFamiliesCount.Count > 0)
                    {
                        TrudeMissing.ImportMissingWindows(propsListWindows);
                    }
                    if (Abort) return;
                    if (GlobalVariables.MissingFurnitureFamiliesCount.Count > 0)
                    {
                        TrudeMissing.ImportMissingFurniture(propsListFurniture);
                    }
                }
                catch (Exception e)
                {
                    logger.Error("Exception in Importing Missing Families: " + "\nError is: " + e.Message + "\n");
                    System.Diagnostics.Debug.WriteLine("Exception in Importing Missing Families: " + "\nError is: " + e.Message + "\n");
                }
            }
#endif
        }


        /// <summary>
        /// This will appear on the Design Automation output
        /// </summary>
        public static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }
        //public static void LogTrace(string format, params object[] args) { Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(format, args); }

        // Delete old elements if they already exists in the revit document
        /// <summary>
        /// This function deletes existing elements within Revit if imported again from snaptrude based on Element Id.
        /// </summary>
        /// <param name="elementId">Element Id from revit to sanptrude.</param>
        public static void deleteOld(int? elementId)
        {
            if (GlobalVariables.ForForge)
                return;
            if (elementId != null)
            {
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                ElementId id = new ElementId((int)elementId);
#else
                ElementId id = new ElementId((Int64)elementId);
#endif
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
        public static void deleteIfInGroup(Element element)
        {
            if (GlobalVariables.ForForge)
                return;

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

    }
    public static class IEnumerableExtensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
           => self.Select((item, index) => (item, index));
    }
}