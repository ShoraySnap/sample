using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class CeilingTypeStore : TypeStore<TrudeLayer[], CeilingType>
    {
        public override string KeyAdapter(TrudeLayer[] ceilingLayers, CeilingType _defaultType)
        {
            string layers = ceilingLayers[0].BaseTypeName;

            foreach (TrudeLayer w in ceilingLayers)
            {
                layers += "_" + w.ToString();

                if (w.IsCore) layers += "-core";
            }

            return layers;
        }

        public override void TypeModifier(TrudeLayer[] ceilingLayers, CeilingType ceilingType)
        {
            CompoundStructure compoundStructure = ceilingType.GetCompoundStructure();
            ElementId materialId;
            IList<CompoundStructureLayer> layers = new List<CompoundStructureLayer>();
            int numberOfExteriorShellLayers = 0;
            int numberOfInteriorShellLayers = 0;
            bool sawCore = false;
            int coreIndex = 0;

            foreach (TrudeLayer ceilingLayer in ceilingLayers)
            {
                if (ceilingLayer.IsCore)
                {
                    sawCore = true;
                }
                else
                {
                    if (sawCore)
                    {
                        numberOfInteriorShellLayers++;
                    }
                    else
                    {
                        numberOfExteriorShellLayers++;
                        coreIndex++;
                    }
                }


                if (!(Utils.FindElement(GlobalVariables.Document, typeof(Material), ceilingLayer.Name) is Material material))
                {
                    materialId = Material.Create(GlobalVariables.Document, ceilingLayer.Name);

                    Material newMaterial = GlobalVariables.Document.GetElement(materialId) as Material;
                    newMaterial.Color = new Color(127, 127, 127);
                }
                else
                {
                    materialId = material.Id;
                }

                MaterialFunctionAssignment materialFunctionAssignment = ceilingLayer.IsCore
                    ? MaterialFunctionAssignment.Structure
                    : MaterialFunctionAssignment.Finish1;

                CompoundStructureLayer newLayer = ceilingLayer.ToCompoundStructureLayer(materialId, materialFunctionAssignment);
                layers.Add(newLayer);
            }

            if (numberOfExteriorShellLayers == layers.Count)
            {
                numberOfExteriorShellLayers = layers.Count - 1;
                coreIndex = 0;
            }

            compoundStructure.SetLayers(layers);
            compoundStructure.SetNumberOfShellLayers(ShellLayerType.Exterior, numberOfExteriorShellLayers);
            compoundStructure.SetNumberOfShellLayers(ShellLayerType.Interior, numberOfInteriorShellLayers);
            compoundStructure.StructuralMaterialIndex = coreIndex;
            ceilingType.SetCompoundStructure(compoundStructure);
        }
    }
}
