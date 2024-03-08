using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeRoofType
    {
        public List<TrudeLayer> layersData;

        public TrudeRoofType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeRoofType GetLayersData(Element ceiling)
        {
            string category = "Roofs";
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            var elemType = document.GetElement(ceiling.GetTypeId()) as CeilingType;
            CompoundStructure compoundStructure = elemType?.GetCompoundStructure();
            if (elemType == null || compoundStructure == null) return new TrudeRoofType(layersData);
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
            foreach (CompoundStructureLayer layer in layers)
            {
                double width = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(layer.Width, UnitTypeId.Feet);
                string function = layer.Function.ToString();

                Material material = document.GetElement(layer.MaterialId) as Material;

                TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material, category);

                TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                layersData.Add(Snaptrudelayer);
            }
            return new TrudeRoofType(layersData);
        }
    }
}
