using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeCeilingType
    {
        public List<TrudeLayer> layersData;

        private static readonly double DEFAULT_LAYER_WIDTH = 25;
        private static readonly string DEFAULT_LAYER_FUNCTION = "Structure";

        public TrudeCeilingType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeCeilingType GetLayersData(Ceiling ceiling)
        {
            string category = "Ceilings";
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            var elemType = document.GetElement(ceiling.GetTypeId()) as CeilingType;
            CompoundStructure compoundStructure = elemType?.GetCompoundStructure();
            if (elemType == null || compoundStructure == null)
            {
                TrudeMaterial snaptrudeMaterial;

                ICollection<ElementId> materialIds = ceiling.GetMaterialIds(false);
                if (materialIds.Count == 0)
                {
                    snaptrudeMaterial = TrudeMaterial.GetMaterial(null, TrudeCategory.Ceiling);
                }
                else
                {
                    snaptrudeMaterial = TrudeMaterial.GetMaterial(document.GetElement(materialIds.First()) as Material, TrudeCategory.Ceiling);
                }

                TrudeLayer Snaptrudelayer = new TrudeLayer(DEFAULT_LAYER_WIDTH, DEFAULT_LAYER_FUNCTION, snaptrudeMaterial);
                layersData.Add(Snaptrudelayer);
            }
            else
            {
                IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
                foreach (CompoundStructureLayer layer in layers)
                {
                    double width = UnitConversion.ConvertToMillimeter(layer.Width, TRUDE_UNIT_TYPE.FEET);
                    string function = layer.Function.ToString();

                    Material material = document.GetElement(layer.MaterialId) as Material;

                    TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material, TrudeCategory.Ceiling);

                    TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                    layersData.Add(Snaptrudelayer);
                }
            }

            return new TrudeCeilingType(layersData);
        }
    }
}
