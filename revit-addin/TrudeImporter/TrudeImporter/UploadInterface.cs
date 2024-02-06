using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.IO;

namespace TrudeImporter
{
    internal class UploadInterface
    {
        public static bool AskUserForFamilyUpload(string familyName, ref bool skipAllMissingFamilies)
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

        public static Family UploadAndLoadFamily(string familyName, string directoryPath, ref bool skipAllMissingFamilies, Document doc)
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
                    TaskDialog mainDialog = new TaskDialog("Incorrect Family");
                    mainDialog.MainInstruction = $"The selected family file does not match the expected family name.";
                    mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Upload Again");
                    mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Skip this family.");

                    TaskDialogResult tResult = mainDialog.Show();

                    if (tResult == TaskDialogResult.CommandLink1)
                    {
                        return UploadAndLoadFamily(familyName, directoryPath, ref skipAllMissingFamilies, doc);
                    }
                    else
                    {
                        return null;
                    }
                }

                try
                {
                    File.Copy(sourcePath, destinationPath, true);
                    if (doc.LoadFamily(destinationPath, out Family family))
                    {
                        // Check if the loaded family is the correct one
                        if (family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                        {
                            // REMOVE THIS LINE IN PRODUCTION FOR TESTING PURPOSES ONLY
                            File.Delete(destinationPath);
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
