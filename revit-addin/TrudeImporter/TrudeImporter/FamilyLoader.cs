using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Converters;

namespace TrudeImporter
{
    public class FamilyLoader
    {
        public static Dictionary<string, Family> LoadedFamilies = new Dictionary<string, Family>();
        public enum FamilyFolder
        {
            Doors,
            Windows,
            Beams,
            Columns,
            Furniture
        }
        public static Family LoadCustomFamily(string familyName, FamilyFolder folder)
        {
            if (LoadedFamilies.ContainsKey(familyName))
            {
                return LoadedFamilies[familyName];
            }
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string filePath = GlobalVariables.ForForge
                    ? $"resourceFile/Windows/{familyName}.rfa"
                    : $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/{GlobalVariables.RvtApp.VersionNumber}/{folder}/{familyName}.rfa";

                GlobalVariables.Document.LoadFamily(filePath, new FamilyLoadOptions(), out Family family);

                LoadedFamilies.Add(familyName, family);

                return family;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        class FamilyLoadOptions : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse,
              out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(
              Family sharedFamily,
              bool familyInUse,
              out FamilySource source,
              out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }

        }
    }
}
