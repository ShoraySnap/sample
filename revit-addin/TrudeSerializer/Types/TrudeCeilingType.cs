using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeCeilingType
    {
        public List<TrudeLayer> layersData;

        public TrudeCeilingType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeCeilingType GetLayersData(Ceiling ceiling)
        {
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            var elemType = document.GetElement(ceiling.GetTypeId()) as CeilingType;
            CompoundStructure compoundStructure = elemType?.GetCompoundStructure();
            if (elemType == null || compoundStructure == null) return new TrudeCeilingType(layersData);
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
            foreach (CompoundStructureLayer layer in layers)
            {
#if REVIT2019 || REVIT2020
                double width = UnitConversion.ConvertToMillimeter(layer.Width, DisplayUnitType.DUT_DECIMAL_FEET);
#else
                double width = UnitConversion.ConvertToMillimeter(layer.Width, UnitTypeId.Feet);
#endif
                string function = layer.Function.ToString();

                Material material = document.GetElement(layer.MaterialId) as Material;

                TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material);

                TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                layersData.Add(Snaptrudelayer);
            }
            return new TrudeCeilingType(layersData);
        }
    }
}
