using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrudeImporter;
using Document = Autodesk.Revit.DB.Document;

namespace MaterialOperations
{
    public class MaterialOperations
    {
        public static double SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR = 39.37;
        // Calculated using scale_set_by_revit * size_of_texture_in_snaptrude /size_of_texture_in_revit
        public static Material CreateMaterialFromTexture(Document doc, string matname, TextureProperties textureProps, float alpha = 1)
        {
            matname = GlobalVariables.sanitizeString(matname);
            System.Diagnostics.Debug.WriteLine("Creating texture material: " + matname);
            Dictionary<string, Material> materialsDict = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .GroupBy(x => x.Name)
                .Select(x => x.First())
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
            if (appearanceAssetElement == null)
            {
                IEnumerable<Material> materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>();
                foreach (Material material in materials)
                {
                    if (material.Name == "Template Snaptrude Material")
                    {
                        System.Diagnostics.Debug.WriteLine("Found Template Snaptrude Material");
                        appearanceAssetElement = doc.GetElement(material.AppearanceAssetId) as AppearanceAssetElement;
                        break;
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("AppearanceAssetElement: " + appearanceAssetElement);
            if (appearanceAssetElement != null)
            {
                Element newAppearanceAsset = appearanceAssetElement.Duplicate(matname + "AppearanceAsset");
                ElementId material = Material.Create(doc, matname);
                var newmat = doc.GetElement(material) as Material;
                newmat.AppearanceAssetId = newAppearanceAsset.Id;
                using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(doc))
                {
                    Asset editableAsset = editScope.Start(
                      newAppearanceAsset.Id);
                    //SetTransparency(editableAsset, 1 - alpha);
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
                        if (connectedAsset.Name.Contains("UnifiedBitmap"))
                        {
                            AssetPropertyString path = connectedAsset
                              .FindByName(UnifiedBitmap.UnifiedbitmapBitmap)
                                as AssetPropertyString;
                            if (path.IsValidValue(textureProps.TexturePath))
                            {
                                path.Value = textureProps.TexturePath;
                            }
                            AssetPropertyDistance scaleX = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldScaleX) as AssetPropertyDistance;

                            AssetPropertyDistance scaleY = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldScaleY) as AssetPropertyDistance;

                            scaleX.Value = textureProps.UScale * SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR; ;
                            scaleY.Value = textureProps.VScale * SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR; ;


                            AssetPropertyDistance texture_uOffset = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldOffsetX) as AssetPropertyDistance;
                            AssetPropertyDistance texture_vOffset = connectedAsset.FindByName(UnifiedBitmap.TextureRealWorldOffsetY) as AssetPropertyDistance;
                            AssetPropertyDistance wAng = connectedAsset.FindByName(UnifiedBitmap.TextureWAngle) as AssetPropertyDistance;

                            texture_uOffset.Value = textureProps.UOffset + SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR;
                            texture_vOffset.Value = textureProps.VOffset + SNAPTRUDE_TO_REVIT_TEXTURE_SCALING_FACTOR;
                            //wAng.Value = wAngle;

                        }
                        editScope.Commit(true);
                    }
                }
                newmat.UseRenderAppearanceForShading = true;
                newmat.Transparency = (1 - (int)alpha) * 100;
                newmat.MaterialClass = "Snaptrude";
                System.Diagnostics.Debug.WriteLine("Material created: " + matname + " with Id: " + material.ToString());
                return newmat;
            }
            else
            {
                return null;
            }
        }

        public static Material CreateMaterialFromHEX(Document doc, string matname, string hex, float alpha = 1)
        {
            matname = GlobalVariables.sanitizeString(matname);
            System.Diagnostics.Debug.WriteLine("Creating Hex material: " + matname);
            Dictionary<string, Material> materialsDict = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .GroupBy(x => x.Name)
                .Select(x => x.First())
                .ToDictionary(mat => mat.Name.ToLower(), mat => mat);

            if (materialsDict.TryGetValue(matname, out Material existingMaterial))
            {
                System.Diagnostics.Debug.WriteLine("Material already exists: " + matname);
                return existingMaterial;
            }
            ElementId material = Material.Create(doc, matname);
            hex = hex.Replace("#", "");
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            var newmat = doc.GetElement(material) as Material;
            Color color = new Color(r, g, b);
            newmat.Color = color;
            newmat.Transparency = (int)((1 - alpha) * 100);
            System.Diagnostics.Debug.WriteLine("Material created: " + matname + " with Id: " + material.ToString());
            return newmat;
        }

        public static bool CopyMaterialsFromTemplate(Document currentDoc, Autodesk.Revit.ApplicationServices.Application app)
        {
            string templatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Configs.CUSTOM_FAMILY_DIRECTORY, "resourceFile", "SnaptrudeTemplate.rte");
            System.Diagnostics.Debug.WriteLine("Copying materials from template: " + templatePath);
            try
            {
                Document templateDoc = app.OpenDocumentFile(templatePath);
                FilteredElementCollector materialCollector = new FilteredElementCollector(templateDoc);

                Material templateMaterial = materialCollector
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .FirstOrDefault(m => m.Name == "Template Snaptrude Material");

                if (templateMaterial != null) {
                    ICollection<ElementId> elementIds = new List<ElementId> { templateMaterial.Id };
                    ICollection<ElementId> received = ElementTransformUtils.CopyElements(templateDoc, elementIds, currentDoc, null, null);
                    ElementId newMaterialId = received.First();
                    Material newMaterial = currentDoc.GetElement(newMaterialId) as Material;
                    System.Diagnostics.Debug.WriteLine("Material created: " + newMaterial.Name + " with Id: " + newMaterial.Id.ToString());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Template material not found");
                    return false;
                }

                templateDoc.Close(false);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error copying materials from template: " + ex.Message);
                return false;
            }
        }

        private static void SetTransparency(Asset editableAsset, double transparency)
        {
            AssetPropertyDouble genericTransparency = editableAsset.FindByName("generic_transparency") as AssetPropertyDouble;
            genericTransparency.Value = Convert.ToDouble(transparency);
        }

    }
}