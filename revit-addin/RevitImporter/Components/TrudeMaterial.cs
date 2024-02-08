using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using RevitImporter.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RevitImporter.Components
{
    internal class TrudeMaterial
    {
        public Double[] diffuseColor;
        public String name;
        public String type;
        public double uScale;
        public double vScale;
        public double uOffset;
        public double vOffset;
        public double wAng;
        public String texturepath;

        public TrudeMaterial()
        {
        }
        //public TrudeMaterial()
        //{
        //    this.diffuseColor = new double[] { 0, 0, 0 };
        //    this.name = "default";
        //}

        public TrudeMaterial(double[] diffuseColor, String name)
        {
            this.diffuseColor = diffuseColor;
            this.name = name;
        }

        public static TrudeMaterial GetMaterial(Material material)
        {
            Document document = GlobalVariables.Document;

            TrudeMaterial trudeMaterial = new TrudeMaterial();

            ElementId apperanceAssetId = material.AppearanceAssetId;

            if (!(document.GetElement(apperanceAssetId) is AppearanceAssetElement appearanceElem))
            {
                trudeMaterial.SetConsistentColor(material);
                return trudeMaterial;
            }

            Asset renderingAsset = appearanceElem.GetRenderingAsset();

            if (renderingAsset.Size == 0)
            {
                trudeMaterial.SetConsistentColor(material);
                return trudeMaterial;
            }

            String name = appearanceElem.Name;

            trudeMaterial.name = name;

            String materialClass = material.MaterialClass;

            trudeMaterial.type = materialClass;

            if (materialClass == "Glass")
            {
                trudeMaterial.SetGlassMaterial(renderingAsset);
                return trudeMaterial;
            }

            for (int idx = 0; idx < renderingAsset.Size; idx++)
            {
                AssetProperty currentAsset = renderingAsset.Get(idx);
                if (currentAsset is null)
                {
                    continue;
                }

                String propertyName = currentAsset.Name;

                if (propertyName == "unifiedbitmap_Bitmap")
                {
                    AssetPropertyString assetPropertyString = currentAsset as AssetPropertyString;
                    String texturePath = assetPropertyString.Value;
                    trudeMaterial.SetTextureIndformation(currentAsset, texturePath);
                }

                Boolean isValidProperty = IsValidAssetForDiffuseColor(currentAsset);

                if (currentAsset.NumberOfConnectedProperties > 0 && isValidProperty)
                {
                    if (currentAsset is AssetPropertyDoubleArray4d)
                    {
                        IList<Double> color = (currentAsset as AssetPropertyDoubleArray4d).GetValueAsDoubles();
                        trudeMaterial.diffuseColor = new double[] { color[0], color[1], color[2], color[3] };
                        AssetPropertyDouble alphaProperty = renderingAsset.FindByName("generic_transparency") as AssetPropertyDouble;
                        if (alphaProperty != null && alphaProperty.Value != 0)
                        {
                            double alpha = alphaProperty.Value;
                            trudeMaterial.diffuseColor[3] = 1 - alpha;
                        }
                    }
                    AssetProperty connectedProperty = currentAsset.GetConnectedProperty(0);
                    if (!(connectedProperty is Asset) || (connectedProperty as Asset).Size == 0) continue;

                    for (int i = 0; i < (connectedProperty as Asset).Size; i++)
                    {
                        AssetProperty connectedPropertyAsset = (connectedProperty as Asset).Get(i);
                        if (connectedPropertyAsset.Name == "unifiedbitmap_Bitmap")
                        {
                            AssetPropertyString connectAssetPropertyString = connectedPropertyAsset as AssetPropertyString;
                            String connectedTexturePath = connectAssetPropertyString.Value;
                            trudeMaterial.SetTextureIndformation(connectedProperty, connectedTexturePath);
                            break;
                        }
                    }
                }
                else if (currentAsset is AssetPropertyDoubleArray4d && isValidProperty)
                {
                    IList<Double> color = (currentAsset as AssetPropertyDoubleArray4d).GetValueAsDoubles();
                    trudeMaterial.diffuseColor = new double[] { color[0], color[1], color[2], color[3] };
                    AssetPropertyDouble alphaProperty = renderingAsset.FindByName("generic_transparency") as AssetPropertyDouble;
                    if (alphaProperty != null && alphaProperty.Value != 0)
                    {
                        double alpha = alphaProperty.Value;
                        trudeMaterial.diffuseColor[3] = 1 - alpha;
                    }
                }
            }
            return trudeMaterial;
        }

        private static Boolean IsValidAssetForDiffuseColor(AssetProperty asset)
        {
            string propertyName = asset.Name.ToString();

            Regex diffuseRegex = new Regex("(?=(diffuse))");
            Regex colorRegex = new Regex(@"(?=(color))");
            Regex glazingRegex = new Regex(@"(?=(glazing))");

            if (diffuseRegex.IsMatch(propertyName) || colorRegex.IsMatch(propertyName) || glazingRegex.IsMatch(propertyName))
            {
                Boolean isCommonTintColor = propertyName == "common_Tint_color";
                return true && !isCommonTintColor;
            }
            return false;
        }

        public static TrudeMaterial GetDefaultMaterial()
        {
            double[] defaultDiffuseColor = { 80 / 255, 80 / 255, 80 / 255 };
            String name = "default";
            return new TrudeMaterial(defaultDiffuseColor, name);
        }

        private void SetConsistentColor(Material mat)
        {
            double[] color = { mat.Color.Red / 255, mat.Color.Green / 255, mat.Color.Blue / 255, 1 - mat.Transparency / 100 };
            String name = mat.Id.ToString();
            this.diffuseColor = color;
            this.name = name;
        }

        private void SetGlassMaterial(Asset renderingAsset)
        {
            String[] GLAZING_COLOR = { "Clear", "Green", "Gray", "Blue", "Bluegreen", "Bronze", "Custom" };
            AssetProperty colorMap = renderingAsset.FindByName("glazing_transmittance_color");
            int index = (int)(colorMap as AssetPropertyInteger).Value;

            String glassColorName = GLAZING_COLOR[index];

            switch(glassColorName)
            {
                case "Clear":
                    this.diffuseColor = new double[] { 0, 0, 0, 0.2 };
                    break;
                case "Green":
                    this.diffuseColor = new double[] { 0, 1, 0, 0.2 };
                    break;
                case "Gray":
                    this.diffuseColor = new double[] { 0.32, 0.32, 0.32, 0.2 };
                    break;
                case "Blue":
                    this.diffuseColor = new double[] { 0, 0, 1, 0.2 };
                    break;
                case "Bluegreen":
                    this.diffuseColor = new double[] { 0.05, 0.059, 0.73, 0.2 };
                    break;
                case "Bronze":
                    this.diffuseColor = new double[] { 0.8, 0.5, 0.2, 0.2 };
                    break;
                case "Custom":
                    IList<Double>customColor = (renderingAsset.FindByName("glazing_transmittance_custom_color") as AssetPropertyDoubleArray4d).GetValueAsDoubles();
                    this.diffuseColor = new double[] { customColor[0], customColor[1], customColor[2]};
                    this.diffuseColor[3] = 0.2;
                    break;
                case "default":
                    this.diffuseColor = new double[] { 0, 0, 0, 0.2 };
                    break;
            }
            
            this.name = "Glass";
            this.type = "Glass";
        }

        private void SetTextureIndformation(AssetProperty asset, String texturePath)
        {
            Asset textureAsset = asset as Asset;
            if (textureAsset == null) return;
            this.texturepath = Path.GetFileName(texturePath);
            if (textureAsset.FindByName("texture_RealWorldScaleX") != null && textureAsset.FindByName("texture_RealWorldScaleY") != null)
            {
                AssetPropertyDistance uScaleProperty = textureAsset.FindByName("texture_RealWorldScaleX") as AssetPropertyDistance;
                AssetPropertyDistance vScaleProperty = textureAsset.FindByName("texture_RealWorldScaleY") as AssetPropertyDistance;
                double uScaleInRealWorld = uScaleProperty.Value;
                double vScaleInRealWorld = vScaleProperty.Value;

                ForgeTypeId uScaleUnit = uScaleProperty.GetUnitTypeId();
                ForgeTypeId vScaleUnit = vScaleProperty.GetUnitTypeId();

                this.uScale = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(uScaleInRealWorld, uScaleUnit);
                this.vScale = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(vScaleInRealWorld, vScaleUnit);
            }
            else
            {
                if (textureAsset.FindByName("texture_UScale") != null)
                {
                    this.uScale = (textureAsset.FindByName("texture_UScale") as AssetPropertyDouble).Value;
                }
                if (textureAsset.FindByName("texture_VScale") != null)
                {
                    this.vScale = (textureAsset.FindByName("texture_VScale") as AssetPropertyDouble).Value;
                }
            }
            if (textureAsset.FindByName("texture_UOffset") != null)
            {
                this.uOffset = (textureAsset.FindByName("texture_UOffset") as AssetPropertyDouble).Value;
            }
            if (textureAsset.FindByName("texture_VOffset") != null)
            {
                this.vOffset = (textureAsset.FindByName("texture_VOffset") as AssetPropertyDouble).Value;
            }
            if (textureAsset.FindByName("texture_WAngle") != null)
            {
                this.wAng = (textureAsset.FindByName("texture_WAngle") as AssetPropertyDouble).Value;
            }
        }
    }
}