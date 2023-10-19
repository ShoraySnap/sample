using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Net;
using System.IO;

namespace TrudeImporter
{
    public static class GlobalVariables
    {
        public static Document Document;
        public static Application RvtApp;

        public static IDictionary<int, ElementId> LevelIdByNumber = new Dictionary<int, ElementId>();
        public static IDictionary<int, ElementId> childUniqueIdToWallElementId = new Dictionary<int, ElementId>();

        public static JArray materials;
        public static JArray multiMaterials;

        public static Dictionary<String, Element> idToElement = new Dictionary<String, Element>();
        public static Dictionary<String, FamilySymbol> idToFamilySymbol = new Dictionary<String, FamilySymbol>();

        public static void cleanGlobalVariables()
        {
            Document = null;
            RvtApp = null;
            LevelIdByNumber = new Dictionary<int, ElementId>();
            childUniqueIdToWallElementId = new Dictionary<int, ElementId>();

            materials = null;
            multiMaterials = null;

            idToElement = new Dictionary<String, Element>();
            idToFamilySymbol = new Dictionary<String, FamilySymbol>();
        }
        public static void DownloadTexture(string url, string filename, string path, bool overwrite = false)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string extension = Path.GetExtension(url);
                    string fullFilename = $"{filename}{extension}";
                    string fullPath = Path.Combine(path, fullFilename);
                    if (File.Exists(fullPath) && !overwrite)
                    {
                        Console.WriteLine("Texture already exists at: " + fullPath);
                        System.Diagnostics.Debug.WriteLine("Texture already exists at: " + fullPath);
                        return;
                    }
                    client.DownloadFile(new Uri(url), fullPath);
                    Console.WriteLine("Texture downloaded successfully at: " + fullPath);
                    System.Diagnostics.Debug.WriteLine("Texture downloaded successfully at: " + fullPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while downloading the texture: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("An error occurred while downloading the texture: " + ex.Message);
                }
            }
        }

    }
}