﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Autodesk.Revit.DB;


namespace TrudeImporter.TrudeImporter.Model
{
    internal class TrudeMissing
    {
        public static void ImportMissingDoors(List<DoorProperties> doorProps)
        {
            if (GlobalVariables.MissingDoorFamiliesCount.Count == 0) return;
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directoryPath = GlobalVariables.ForForge
                ? "resourceFile/Doors"
                : Path.Combine(documentsPath, $"{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/{GlobalVariables.RvtApp.VersionNumber}/Doors");
            foreach (var missingFamily in GlobalVariables.MissingDoorFamiliesCount)
            {
                bool isChecked = missingFamily.Value.IsChecked;
                string sourcePath = missingFamily.Value.path;
                string destinationPath = Path.Combine(directoryPath, missingFamily.Key + ".rfa");

                if (isChecked)
                {
                    if (File.Exists(sourcePath))
                    {
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        File.Copy(sourcePath, destinationPath, true);
                        System.Diagnostics.Debug.WriteLine("Family: " + missingFamily.Key + " Copied to: " + destinationPath);
                    }
                }
            }

            foreach (var index in GlobalVariables.MissingDoorIndexes)
            {
                DoorProperties door = doorProps[index];
                string doorName = door.Name.RemoveIns();
                if (GlobalVariables.MissingDoorFamiliesCount[doorName].IsChecked)
                {
                    deleteOld(door.ExistingElementId);
                    try
                    {
                        new TrudeDoor(door, GlobalVariables.LevelIdByNumber[door.Storey], index);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Door: " + door.UniqueId + "\nError is: " + e.Message + "\n");
                    }
            }
            }
        }

        public static void ImportMissingWindows(List<WindowProperties> windowProps)
        {
            if (GlobalVariables.MissingWindowFamiliesCount.Count == 0) return;
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directoryPath = GlobalVariables.ForForge
                ? "resourceFile/Windows"
                : Path.Combine(documentsPath, $"{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/{GlobalVariables.RvtApp.VersionNumber}/Windows");
            foreach (var missingFamily in GlobalVariables.MissingWindowFamiliesCount)
            {
                bool isChecked = missingFamily.Value.IsChecked;
                string sourcePath = missingFamily.Value.path;
                string destinationPath = Path.Combine(directoryPath, missingFamily.Key + ".rfa");

                if (isChecked)
                {
                    if (File.Exists(sourcePath))
                    {
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        File.Copy(sourcePath, destinationPath, true);
                        System.Diagnostics.Debug.WriteLine("Family: " + missingFamily.Key + " Copied to: " + destinationPath);
                    }
                }
            }

            foreach (var index in GlobalVariables.MissingWindowIndexes)
            {
                WindowProperties window = windowProps[index];
                string windowName = window.Name.RemoveIns();
                if (GlobalVariables.MissingWindowFamiliesCount[windowName].IsChecked)
                {
                    deleteOld(window.ExistingElementId);
                    try
                    {
                        new TrudeWindow(window, GlobalVariables.LevelIdByNumber[window.Storey], index);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Window: " + window.UniqueId + "\nError is: " + e.Message + "\n");
                    }
                }
            }
        }

        public static void ImportMissingFurniture(List<FurnitureProperties> furnitureProps)
        {
            if (GlobalVariables.MissingFurnitureFamiliesCount.Count == 0) return;
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directoryPath = GlobalVariables.ForForge
                ? "resourceFile/Furniture"
                : Path.Combine(documentsPath, $"{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/{GlobalVariables.RvtApp.VersionNumber}/Furniture");
            foreach (var missingFamily in GlobalVariables.MissingFurnitureFamiliesCount)
            {
                bool isChecked = missingFamily.Value.IsChecked;
                string sourcePath = missingFamily.Value.path;
                string destinationPath = Path.Combine(directoryPath, missingFamily.Key + ".rfa");

                if (isChecked)
                {
                    if (File.Exists(sourcePath))
                    {
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        File.Copy(sourcePath, destinationPath, true);
                        System.Diagnostics.Debug.WriteLine("Family: " + missingFamily.Key + " Copied to: " + destinationPath);
                    }
                }
            }

            List<ElementId> sourceIdsToDelete = new List<ElementId>();
            foreach (var index in GlobalVariables.MissingFurnitureIndexes)
            {
                FurnitureProperties furniture = furnitureProps[index];
                string furnitureName = furniture.RevitFamilyName;
                if (furnitureName == null) furnitureName = furniture.Name;
                if (GlobalVariables.MissingFurnitureFamiliesCount[furnitureName].IsChecked)
                {
                    try
                    {
                        new TrudeFurniture(furniture, sourceIdsToDelete, index);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Importing Furniture: " + furniture.UniqueId + "\nError is: " + e.Message + "\n");
                    }
                }
            }
            GlobalVariables.Document.Delete(sourceIdsToDelete);
        }

        //public static void ImportMissingWindows(WindowProperties window)
        //{

        //    foreach (var missingFamily in GlobalVariables.MissingDoorFamilies)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Missing Family: " + missingFamily.Value);
        //    }

        //}

        public static void deleteOld(int? elementId)
        {
            if (GlobalVariables.ForForge)
                return;
            if (elementId != null)
            {
#if (REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023)
                ElementId id = new ElementId((int)elementId);
#else
                ElementId id = new ElementId((long)elementId);
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
}
