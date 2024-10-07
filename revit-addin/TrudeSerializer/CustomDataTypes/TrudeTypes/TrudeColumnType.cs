using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeColumnType
    {
        public List<TrudeLayer> layersData;
        static public double DEFAULT_WIDTH = 0.0;
        static public string DEFAULT_FUNCTION = "Structure";

        public TrudeColumnType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeColumnType GetLayersData(Element column)
        {
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            TrudeMaterial snaptrudeMaterial;

            ICollection<ElementId> materialIds = column.GetMaterialIds(false);
            if (materialIds.Count == 0)
            {
                snaptrudeMaterial = TrudeMaterial.GetMaterial(null, TrudeCategory.Column);
            }
            else
            {
                snaptrudeMaterial = TrudeMaterial.GetMaterial(document.GetElement(materialIds.First()) as Material, TrudeCategory.Column);
            }

            TrudeLayer Snaptrudelayer = new TrudeLayer(DEFAULT_WIDTH, DEFAULT_FUNCTION, snaptrudeMaterial);
            layersData.Add(Snaptrudelayer);
            return new TrudeColumnType(layersData);
        }
    }
}
