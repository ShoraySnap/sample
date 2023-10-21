using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Net;
using System.IO;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB.Visual;
using Document = Autodesk.Revit.DB.Document;
using System.Linq;

namespace TrudeImporter
{
    public static class GlobalVariables
    {
        public static Document Document;
        public static Autodesk.Revit.ApplicationServices.Application RvtApp;

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
        public static string DownloadTexture(string url, string filename, string path, bool overwrite = false)
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
                        return fullPath;
                    }
                    client.DownloadFile(new Uri(url), fullPath);
                    Console.WriteLine("Texture downloaded successfully at: " + fullPath);
                    System.Diagnostics.Debug.WriteLine("Texture downloaded successfully at: " + fullPath);
                    return fullPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while downloading the texture: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("An error occurred while downloading the texture: " + ex.Message);
                    return "";
                }
            }
        }

        public static Material CreateMaterial(Document doc, string matname, string texturepath, float alpha=100) {
            IEnumerable<AppearanceAssetElement> appearanceAssetElementEnum = new FilteredElementCollector(doc).OfClass(typeof(AppearanceAssetElement)).Cast<AppearanceAssetElement>();
            AppearanceAssetElement appearanceAssetElement = null;
            var i = 0;
            foreach (var tempappearanceAssetElement in appearanceAssetElementEnum)
            {
                if (i == 1)
                {
                    appearanceAssetElement = tempappearanceAssetElement;
                    break;
                }
                i++;
            }
            if (appearanceAssetElement != null)
            {
                Element newAppearanceAsset = appearanceAssetElement.Duplicate(matname+"AppearanceAsset");
                ElementId material = Material.Create(doc, matname);
                var newmat = doc.GetElement(material) as Material;
                newmat.AppearanceAssetId = newAppearanceAsset.Id;
                using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(doc))
                {
                    Asset editableAsset = editScope.Start(
                      newAppearanceAsset.Id);

                    AssetProperty assetProperty = editableAsset
                      .FindByName("generic_diffuse");
                    Asset connectedAsset = assetProperty.GetSingleConnectedAsset();
                    if (connectedAsset == null)
                    {
                        assetProperty.AddConnectedAsset("UnifiedBitmap");
                        connectedAsset = assetProperty.GetSingleConnectedAsset();
                    }
                    if (connectedAsset != null)
                    {
                        if (connectedAsset.Name == "UnifiedBitmap")
                        {
                            AssetPropertyString path = connectedAsset
                              .FindByName(UnifiedBitmap.UnifiedbitmapBitmap)
                                as AssetPropertyString;
                            if (path.IsValidValue(texturepath))
                            {
                                path.Value = texturepath;
                            }
                        }
                        editScope.Commit(true);
                    }
                }
                newmat.UseRenderAppearanceForShading = true;
                newmat.Transparency = (int)alpha;
                return newmat;
            }
            else
            {
                return null;
            }
        }
    }
}