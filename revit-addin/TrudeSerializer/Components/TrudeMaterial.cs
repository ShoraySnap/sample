using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using System;
using System.Collections.Generic;
using System.IO;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeMaterial
    {
        public double[] diffuseColor;
        public string name;
        public string type;
        public double uScale;
        public double vScale;
        public double uOffset;
        public double vOffset;
        public double wAng;
        public string texturePath;

        private readonly Dictionary<string, double[]> GLASS_COLOR_MAP = new Dictionary<string, double[]>
        {
            { "Blue", new double[] { 0, 0, 1, 0.2 } },
            { "Green", new double[] { 0, 1, 0, 0.2} },
            { "Bronze", new double[] { 0.8, 0.5, 0.2, 0.2 } },
            { "Gray", new double[] { 0.32, 0.32, 0.32, 0.2} },
            { "Bluegreen", new double[] { 0.05, 0.59, 0.73, 0.2 } },
            { "Clear", new double[] { 0, 0, 0, 0.2 } }
        };

        private readonly static double[] DEFAULT_DIFFUSE_COLOR = { 80.0 / 255.0, 80.0 / 255.0, 80.0 / 255.0, 1 };

        public TrudeMaterial()
        {
            this.diffuseColor = DEFAULT_DIFFUSE_COLOR;
        }

        public TrudeMaterial(double[] diffuseColor, String name)
        {
            this.diffuseColor = diffuseColor;
            this.name = name;
        }

        public static TrudeMaterial GetDefaultMaterial()
        {
            String name = "default";
            return new TrudeMaterial(DEFAULT_DIFFUSE_COLOR, name);
        }

        public static TrudeMaterial GetMaterial(Material material)
        {
            if (material == null)
            {
                return GetDefaultMaterial();
            }
            Document document = GlobalVariables.CurrentDocument;

            TrudeMaterial trudeMaterial = new TrudeMaterial();

            ElementId apperanceAssetId = material.AppearanceAssetId;

            AppearanceAssetElement appearanceElem = document.GetElement(apperanceAssetId) as AppearanceAssetElement;

            if (appearanceElem == null || appearanceElem.GetRenderingAsset().Size == 0)
            {
                trudeMaterial.SetConsistentColor(material);
                return trudeMaterial;
            }

            Asset renderingAsset = appearanceElem.GetRenderingAsset();

            String name = appearanceElem.Name;

            trudeMaterial.name = name;

            String materialClass = material.MaterialClass;

            trudeMaterial.type = materialClass;

            if (IsGlassMaterial(renderingAsset, material))
            {
                trudeMaterial.SetGlassMaterial(renderingAsset);
                return trudeMaterial;
            }

            trudeMaterial.ReadMaterialInformationFromAsset(renderingAsset);

            return trudeMaterial;
        }

        private void SetConsistentColor(Material mat)
        {
            double[] color = { mat.Color.Red / 255.0, mat.Color.Green / 255.0, mat.Color.Blue / 255.0, (1 - mat.Transparency) / 100.0 };
            String name = mat.Id.ToString();
            this.diffuseColor = color;
            this.name = name;
        }

        private static bool IsGlassMaterial(Asset renderingAsset, Material material)
        {
            string materialClass = material.MaterialClass;
            if (!(renderingAsset.FindByName("localname") is AssetPropertyString localNameAsset))
            {
                return materialClass == "Glass";
            }

            return localNameAsset.Value == "Glazing";
        }

        private void SetGlassMaterial(Asset renderingAsset)
        {
            String[] GLAZING_COLOR = { "Clear", "Green", "Gray", "Blue", "Bluegreen", "Bronze", "Custom" };
            AssetProperty colorMap = renderingAsset.FindByName("glazing_transmittance_color");
            int index = (int)(colorMap as AssetPropertyInteger).Value;

            String glassColorName = GLAZING_COLOR[Math.Min(index, GLAZING_COLOR.Length - 1)];

            SetGlassColor(renderingAsset, glassColorName);

            this.name = "Glass";
            this.type = "Glass";
        }

        private void SetGlassColor(Asset renderingAsset, string name)
        {
            if (GLASS_COLOR_MAP.ContainsKey(name))
            {
                this.diffuseColor = GLASS_COLOR_MAP[name];
                return;
            }

            SetCustomGlassColor(renderingAsset);
        }

        private void SetCustomGlassColor(Asset renderingAsset)
        {
            AssetProperty customColorProperty = renderingAsset.FindByName("glazing_transmittance_custom_color");

            if (customColorProperty is AssetPropertyDoubleArray4d)
            {
                IList<Double> customColor = (customColorProperty as AssetPropertyDoubleArray4d).GetValueAsDoubles();
                this.diffuseColor = new double[] { customColor[0], customColor[1], customColor[2], 0.2 };
            }
            else
            {
                this.diffuseColor = new double[] { 0, 0, 0, 0.2 };
            }
        }

        private void ReadMaterialInformationFromAsset(Asset asset)
        {
            for (int idx = 0; idx < asset.Size; idx++)
            {
                AssetProperty currentAsset = asset.Get(idx);
                if (currentAsset is null)
                {
                    continue;
                }

                String propertyName = currentAsset.Name;

                if (IsTextureAssetProperty(propertyName))
                {
                    SetTextureIndformation(asset, currentAsset);
                    break;
                }

                if (!IsValidAssetForDiffuseColor(currentAsset)) continue;

                if (currentAsset is AssetPropertyDoubleArray4d)
                {
                    IList<Double> color = (currentAsset as AssetPropertyDoubleArray4d)?.GetValueAsDoubles();
                    if (color == null) continue;
                    diffuseColor = new double[] { color[0], color[1], color[2], color[3] };
                    AssetPropertyDouble alphaProperty = asset.FindByName("generic_transparency") as AssetPropertyDouble;
                    if (alphaProperty != null && alphaProperty.Value != 0)
                    {
                        double alpha = alphaProperty.Value;
                        diffuseColor[3] = 1 - alpha;
                    }
                }

                if (currentAsset.NumberOfConnectedProperties == 0) continue;

                AssetProperty connectedAssetProperty = currentAsset.GetConnectedProperty(0);
                if (!(connectedAssetProperty is Asset) || (connectedAssetProperty as Asset).Size == 0) continue;

                ReadMaterialInformationFromAsset(connectedAssetProperty as Asset);
                //break;
            }
        }

        private static bool IsTextureAssetProperty(String assetName)
        {
            return assetName == "unifiedbitmap_Bitmap";
        }

        private void SetTextureIndformation(AssetProperty mainAsset, AssetProperty connectTextureAsset)
        {
            if (!(mainAsset is Asset textureAsset) || !(connectTextureAsset is AssetPropertyString connectTextureString))
                return;

            string[] texturePath = connectTextureString.Value.Split('|');
            this.texturePath = texturePath.Length == 0 ? "" : Path.GetFileName(texturePath[0]);

            SetTextureScales(textureAsset);
            SetTextureOffset(textureAsset);
            SetTextureAngle(textureAsset);
        }

        private void SetTextureScales(Asset textureAsset)
        {
            if (textureAsset.FindByName("texture_RealWorldScaleX") != null && textureAsset.FindByName("texture_RealWorldScaleY") != null)
            {
                AssetPropertyDistance uScaleProperty = textureAsset.FindByName("texture_RealWorldScaleX") as AssetPropertyDistance;
                AssetPropertyDistance vScaleProperty = textureAsset.FindByName("texture_RealWorldScaleY") as AssetPropertyDistance;
                double uScaleInRealWorld = uScaleProperty.Value;
                double vScaleInRealWorld = vScaleProperty.Value;

                ForgeTypeId uScaleUnit = uScaleProperty.GetUnitTypeId();
                ForgeTypeId vScaleUnit = vScaleProperty.GetUnitTypeId();

                this.uScale = UnitConversion.ConvertToSnaptrudeUnits(uScaleInRealWorld, uScaleUnit);
                this.vScale = UnitConversion.ConvertToSnaptrudeUnits(vScaleInRealWorld, vScaleUnit);

                return;
            }

            if (textureAsset.FindByName("texture_UScale") != null)
            {
                this.uScale = (textureAsset.FindByName("texture_UScale") as AssetPropertyDouble).Value;
            }
            if (textureAsset.FindByName("texture_VScale") != null)
            {
                this.vScale = (textureAsset.FindByName("texture_VScale") as AssetPropertyDouble).Value;
            }
        }

        private void SetTextureOffset(Asset textureAsset)
        {
            if (textureAsset.FindByName("texture_UOffset") != null)
            {
                this.uOffset = (textureAsset.FindByName("texture_UOffset") as AssetPropertyDouble).Value;
            }
            if (textureAsset.FindByName("texture_VOffset") != null)
            {
                this.vOffset = (textureAsset.FindByName("texture_VOffset") as AssetPropertyDouble).Value;
            }
        }

        private void SetTextureAngle(Asset textureAsset)
        {
            if (textureAsset.FindByName("texture_WAngle") != null)
            {
                this.wAng = (textureAsset.FindByName("texture_WAngle") as AssetPropertyDouble).Value;
            }
        }

        private static bool IsValidAssetForDiffuseColor(AssetProperty asset)
        {
            string propertyName = asset.Name.ToString();

            bool isCommonTintColor = propertyName.Equals("common_Tint_color");

            bool isDiffuseColorProperty = propertyName.Contains("diffuse") || propertyName.Contains("color") || propertyName.Contains("glazing");

            return isDiffuseColorProperty && !isCommonTintColor;
        }
    }
}