using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeFloorType
    {
        public List<TrudeLayer> layersData;

        public TrudeFloorType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeFloorType GetLayersData(Floor floor)
        {
            string category = "Floors";
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            CompoundStructure compoundStructure = floor.FloorType.GetCompoundStructure();
            if (compoundStructure == null) return new TrudeFloorType(layersData);
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
            return new TrudeFloorType(layersData);
        }
    }
}
