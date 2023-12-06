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
        public static double SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR = 39.37;
        // Calculated using scale_set_by_revit * size_of_texture_in_snaptrude /size_of_texture_in_revit
        public static Material CreateMaterial(Document doc, string matname, string texturepath, float alpha = 100, double uScale=1, double vScale=1, double uOffset = 0, double vOffset = 0, double wAngle = 0)
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
                            AssetPropertyDistance scaleX = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldScaleX) as AssetPropertyDistance;

                            AssetPropertyDistance scaleY = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldScaleY) as AssetPropertyDistance;
                            
                            scaleX.Value = uScale*SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR;
                            scaleY.Value = vScale*SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR;


                            AssetPropertyDistance texture_uOffset = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldOffsetX) as AssetPropertyDistance;
                            AssetPropertyDistance texture_vOffset = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldOffsetY) as AssetPropertyDistance;
                            AssetPropertyDistance wAng = connectedAsset.FindByName(UnifiedBitmap.TextureWAngle) as AssetPropertyDistance;

                            texture_uOffset.Value = uOffset + SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR;
                            texture_vOffset.Value = vOffset + SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR;
                            //wAng.Value = wAngle;

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