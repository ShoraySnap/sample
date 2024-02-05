using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.IO;

namespace TrudeImporter
{
    internal class TrudeDoor : TrudeModel
    {
        string doorFamilyName = null;
        string fsName = null;
        XYZ CenterPosition = null;
        public static bool skipAllMissingFamilies = false;
        public static DoorTypeStore TypeStore = new DoorTypeStore();

        public TrudeDoor(DoorProperties doorProps, ElementId levelId)
        {
            XYZ direction = doorProps.Direction == null
                                ? XYZ.Zero
                                : doorProps.Direction;
            CenterPosition = doorProps.CenterPosition;
            try
            {
                if (doorProps.RevitFamilyName != null)
                {
                    doorFamilyName = doorProps.RevitFamilyName;
                }
                else
                {
                    doorFamilyName = doorProps.Name.RemoveIns();
                    fsName = doorFamilyName;
                }

                //getting wall to add door to
                Wall wall = null;
                if (GlobalVariables.childUniqueIdToWallElementId.ContainsKey(doorProps.UniqueId))
                {
                    ElementId wallElementId = GlobalVariables.childUniqueIdToWallElementId[doorProps.UniqueId];
                    wall = (Wall)GlobalVariables.Document.GetElement(wallElementId);
                }

                FamilySymbol familySymbol = null;
                FamilySymbol defaultFamilySymbol = null;
                if (doorProps.ExistingElementId == null)
                {
                    if (doorProps.RevitFamilyName == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Creating door with name " + doorFamilyName);
                        var family = LoadCustomDoorFamilyWithDialog(doorFamilyName);
                        if (family is null && !skipAllMissingFamilies)
                        {
                            System.Diagnostics.Debug.WriteLine("Couldn't find or load door family: " + doorFamilyName);
                            return;
                        }
                        if (family is null)
                        {
                            System.Diagnostics.Debug.WriteLine("couln't find door family: "+ doorFamilyName);
                            return;
                        }
                    }
                }

                defaultFamilySymbol = GetFamilySymbolByName(GlobalVariables.Document, doorFamilyName, fsName);
                if (defaultFamilySymbol is null)
                {
                    System.Diagnostics.Debug.WriteLine("No door with name " + doorFamilyName);
                    return;
                }
                if (!defaultFamilySymbol.IsActive)
                {
                    defaultFamilySymbol.Activate();
                    GlobalVariables.Document.Regenerate();
                }
                // Check if familySymbol BuiltInParameter.DOOR_HEIGHT and  BuiltInParameter.DOOR_WIDTH
                // if so, then set the height and with in the familySymbol itself, otherwise find the correct
                // parameter in the instance.

                Parameter heightTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
                Parameter widthTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_WIDTH);

                bool setHeightAndWidthParamsInFamilySymbol = (heightTypeParam.HasValue && widthTypeParam.HasValue) && (!heightTypeParam.IsReadOnly || !widthTypeParam.IsReadOnly);
                if (setHeightAndWidthParamsInFamilySymbol)
                {
                    familySymbol = TypeStore.GetType(new double[] { doorProps.Height, doorProps.Width }, defaultFamilySymbol);
                }
                else
                {
                    familySymbol = defaultFamilySymbol;
                }

                var instance = CreateDoor(familySymbol, levelId, wall, direction);

                (Parameter widthInstanceParam, Parameter heightInstanceParam) = instance.FindWidthAndHeightParameters();
                if (!setHeightAndWidthParamsInFamilySymbol)
                {
                    heightInstanceParam.Set(doorProps.Height);
                    widthInstanceParam.Set(doorProps.Width);
                }
                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(doorProps.Height);
                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(doorProps.Width);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"No door with name {doorProps.RevitFamilyName}, UniqueId: {doorProps.UniqueId}\n", e.Message);
            }
        }

        private FamilyInstance CreateDoor(FamilySymbol familySymbol, ElementId levelId, Wall wall, XYZ direction)
        {
            FamilyInstance instance;
            var doc = GlobalVariables.Document;
            Level level = doc.GetElement(levelId) as Level;

            XYZ xyz = new XYZ(CenterPosition.X, CenterPosition.Y, 0.0);

            if (wall is null)
            {
                wall = GetProximateWall(xyz, doc, level.Id);
            }

            BoundingBoxXYZ bbox = wall.get_BoundingBox(null);
            XYZ loc = new XYZ(CenterPosition.X, CenterPosition.Y, bbox.Min.Z);

            instance = doc.Create.NewFamilyInstance(loc, familySymbol, wall, (Level)doc.GetElement(wall.LevelId), StructuralType.NonStructural);

            // Done to make sure door is cutting the wall
            // See https://forums.autodesk.com/t5/revit-api-forum/create-doors-but-not-cutting-through-wall/td-p/5564330
            instance.flipFacing();
            doc.Regenerate();

            instance.flipFacing();
            doc.Regenerate();

            if (!instance.FacingOrientation.IsAlmostEqualTo(direction))
            {
                instance.flipFacing();
                instance.flipHand();
            }

            return instance;
        }

        private Family LoadCustomDoorFamilyWithDialog(string familyName)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string directoryPath = GlobalVariables.ForForge
                ? "resourceFile/Doors"
                : Path.Combine(documentsPath, $"{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Doors");
            string filePath = Path.Combine(directoryPath, $"{familyName}.rfa");

            if (skipAllMissingFamilies)
            {
                return null;
            }

            if (File.Exists(filePath))
            {
                GlobalVariables.Document.LoadFamily(filePath, out Family family);
                return family;
            }
            else
            {
                if (AskUserForFamilyUpload(familyName))
                {
                    return UploadAndLoadFamily(familyName, directoryPath);
                }
                else
                {
                    return null;
                }
            }
        }

        private bool AskUserForFamilyUpload(string familyName)
        {
            if (skipAllMissingFamilies)
            {
                return false;
            }

            TaskDialog mainDialog = new TaskDialog("Family Not Found");
            mainDialog.MainInstruction = $"The family '{familyName}' was not found.";
            mainDialog.MainContent = "Would you like to upload the family file, or skip all missing families?";
            mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Yes, I want to upload the family.");
            mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "No, skip this family.");
            mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "No, skip all missing families.");
            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();

            if (tResult == TaskDialogResult.CommandLink3)
            {
                skipAllMissingFamilies = true;
            }

            return tResult == TaskDialogResult.CommandLink1;
        }

        private Family UploadAndLoadFamily(string familyName, string directoryPath)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Family File For " + familyName;
            openFileDialog.Filter = "Revit Families (*.rfa)|*.rfa";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                string sourcePath = openFileDialog.FileName;
                string destinationPath = Path.Combine(directoryPath, Path.GetFileName(sourcePath));

                if (!Path.GetFileNameWithoutExtension(sourcePath).Equals(familyName, StringComparison.OrdinalIgnoreCase))
                {
                    TaskDialog.Show("Error", "The selected family does not match the expected family name. Please select the correct family file.");
                    // show the dialog again
                    return UploadAndLoadFamily(familyName, directoryPath);
                }

                try
                {
                    File.Copy(sourcePath, destinationPath, true);
                    if (GlobalVariables.Document.LoadFamily(destinationPath, out Family family))
                    {
                        // Check if the loaded family is the correct one
                        if (family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                        {
                            // REMOVE THIS LINE IN PRODUCTION FOR TESTING PURPOSES ONLY
                            // File.Delete(destinationPath);
                            return family;
                        }
                        else
                        {
                            TaskDialog.Show("Error", $"The loaded family '{family.Name}' does not match the expected family name '{familyName}'.");
                            // Delete the copied family file if it's incorrect
                            File.Delete(destinationPath);
                        }
                    }
                    else
                    {
                        TaskDialog.Show("Error", "The family file could not be loaded into the Revit document. Please try again or contact support.");
                    }
                }
                catch (Exception e)
                {
                    TaskDialog.Show("Error", "Failed to load the family file.\n" + e.Message);
                }
            }

            return null;
        }

    }
}