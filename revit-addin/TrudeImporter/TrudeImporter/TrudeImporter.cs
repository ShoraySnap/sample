using Autodesk.Revit.DB;
using System.Collections.Generic;
using System;

namespace TrudeImporter
{
    public class TrudeImporterMain
    {
        public static void Import(TrudeProperties trudeProperties)
        {
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
        }

        private static void ImportStories(List<StoreyProperties> propsList)
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
                        GlobalVariables.LevelIdByNumber.Add(newStorey.LevelNumber, newStorey.Level.Id);

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

                if (!GlobalVariables.ForForge && !props.LowerLevelElementId.IsNull())
                {
                    ElementId elementId = new ElementId((BuiltInParameter)props.LowerLevelElementId);
                    GlobalVariables.LevelIdByNumber.Add(newStorey.LevelNumber, elementId);

                    continue;
                }

                try
                {

                    using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                    {
                        t.Start();

                        newStorey.CreateLevel(GlobalVariables.Document);
                        GlobalVariables.LevelIdByNumber.Add(newStorey.LevelNumber, newStorey.Level.Id);

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

        private static void ImportWalls(List<WallProperties> propsList)
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

        private static void ImportBeams(List<BeamProperties> propsList)
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

        private static void ImportColumns(List<ColumnProperties> propsList)
        {
            foreach (var column in propsList)
            {
                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    foreach (var instance in column.Instances)
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

        private static void ImportFloors(List<FloorProperties> propsList)
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
        private static void ImportSlabs(List<SlabProperties> propsList)
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

        private static void ImportDoors(List<DoorProperties> propsList)
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

        private static void ImportWindows(List<WindowProperties> propsList)
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

        private static void ImportCeilings(List<FloorProperties> propsList)
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
}