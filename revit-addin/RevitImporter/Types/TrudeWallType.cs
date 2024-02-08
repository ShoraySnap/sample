using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using RevitImporter.Components;
using RevitImporter.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RevitImporter.Types
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
                double width = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(layer.Width, UnitTypeId.Feet);
                string function = layer.Function.ToString();

                Material material = document.GetElement(layer.MaterialId) as Material;


                TrudeMaterial snaptrudeMaterial = TrudeMaterial.GetMaterial(material);
                
                TrudeLayer Snaptrudelayer = new TrudeLayer(width, function, snaptrudeMaterial);

                layersData.Add(Snaptrudelayer);


            }
            return new TrudeWallType(layersData);
        }


    }
}
