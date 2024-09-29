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
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            CompoundStructure compoundStructure = floor.FloorType.GetCompoundStructure();
            if (compoundStructure == null) return new TrudeFloorType(layersData);
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
            foreach (CompoundStructureLayer layer in layers)
            {
                double width = UnitConversion.ConvertToMillimeter(layer.Width, TRUDE_UNIT_TYPE.FEET);
                string function = layer.Function.ToString();

                Material material = document.GetElement(layer.MaterialId) as Material;

                TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material, TrudeCategory.Floor);

                TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                layersData.Add(Snaptrudelayer);
            }
            return new TrudeFloorType(layersData);
        }
    }
}
