using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Types
{
    internal class TrudeWallType
    {
        public List<TrudeLayer> layersData;

        public TrudeWallType(List<TrudeLayer> layersData)
        {
            this.layersData = layersData;
        }

        static public TrudeWallType GetLayersData(Wall wall)
        {
            List<TrudeLayer> layersData = new List<TrudeLayer>();
            Document document = GlobalVariables.Document;
            CompoundStructure compoundStructure = wall.WallType.GetCompoundStructure();
            if (compoundStructure == null) return new TrudeWallType(layersData);
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
            foreach (CompoundStructureLayer layer in layers)
            {
                double width = UnitConversion.ConvertToMillimeter(layer.Width, TRUDE_UNIT_TYPE.FEET);
                string function = layer.Function.ToString();

                Material material = document.GetElement(layer.MaterialId) as Material;

                TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material, TrudeCategory.Wall);


                TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                layersData.Add(Snaptrudelayer);
            }

            return new TrudeWallType(layersData);
        }


    }
}