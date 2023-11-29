using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using System.Collections.Generic;
using System.Linq;
using TrudeImporter;
using Document = Autodesk.Revit.DB.Document;

namespace MaterialOperations
{
    public class MaterialOperations
    {
        public static Material CreateMaterial(Document doc, string matname, string texturepath, float alpha = 100)
        {
            matname = GlobalVariables.sanitizeString(matname);
            System.Diagnostics.Debug.WriteLine("Creating material: " + matname);
            Dictionary<string, Material> materialsDict = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .ToDictionary(mat => mat.Name.ToLower(), mat => mat);

            if (materialsDict.TryGetValue(matname, out Material existingMaterial))
            {
                System.Diagnostics.Debug.WriteLine("Material already exists: " + matname);
                return existingMaterial;
            }
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
                Element newAppearanceAsset = appearanceAssetElement.Duplicate(matname + "AppearanceAsset");
                ElementId material = Material.Create(doc, matname);
                System.Diagnostics.Debug.WriteLine("Material created: " + matname + " with Id: " + material.ToString());
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