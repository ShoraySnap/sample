using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeColumnType
    {
        public List<TrudeLayer> layersData;

        public TrudeColumnType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeColumnType GetLayersData(Element column)
        {
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            var elemType = document.GetElement(column.GetTypeId()) as HostObjAttributes;
            CompoundStructure compoundStructure = elemType?.GetCompoundStructure();
            if (elemType == null || compoundStructure == null) return new TrudeColumnType(layersData);
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
            foreach (CompoundStructureLayer layer in layers)
            {
                double width = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(layer.Width, UnitTypeId.Feet);
                string function = layer.Function.ToString();

                Material material = document.GetElement(layer.MaterialId) as Material;

                TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material);

                TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                layersData.Add(Snaptrudelayer);
            }
            return new TrudeColumnType(layersData);
        }
    }
}
