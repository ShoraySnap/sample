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
                string filePath = GlobalVariables.ForForge
                    ? $"resourceFile/Doors/{familyName}.rfa"
                    : $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Doors/{familyName}.rfa";

                Utils.LogTrace(filePath);

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
                string filePath = GlobalVariables.ForForge 
                    ? $"resourceFile/Windows/{familyName}.rfa"
                    : $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/Windows/{familyName}.rfa";

                GlobalVariables.Document.LoadFamily(filePath, out Family family);

                LoadedFamilies.Add(familyName, family);

                return family;
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
}
