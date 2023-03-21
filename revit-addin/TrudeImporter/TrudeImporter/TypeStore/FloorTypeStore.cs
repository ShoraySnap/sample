using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class FloorTypeStore : TypeStore<TrudeLayer[], FloorType>
    {
        public override string KeyAdapter(TrudeLayer[] floorLayers, FloorType _defaultType)
        {
            string layers = floorLayers[0].BaseTypeName;
            foreach(TrudeLayer w in floorLayers)
            {
                layers += "_" + w.ToString();

                if (w.IsCore) layers += "-core";
            }

            return layers;
        }

        public override void TypeModifier(TrudeLayer[] floorLayers, FloorType floorType)
        {
            CompoundStructure compoundStructure = floorType.GetCompoundStructure();

            ElementId materialId;

            IList<CompoundStructureLayer> layers = new List<CompoundStructureLayer>();

            int numberOfExteriorShellLayers = 0;
            int numberOfInteriorShellLayers = 0;

            bool sawCore = false;
            int coreIndex = 0;
            foreach (TrudeLayer floorLayer in floorLayers)
            {
                if (floorLayer.IsCore)
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

                Material material = Utils.FindElement(GlobalVariables.Document, typeof(Material), floorLayer.Name) as Material;

                if (material is null)
                {
                    materialId = Material.Create(GlobalVariables.Document, floorLayer.Name);

                    Material newMaterial = GlobalVariables.Document.GetElement(materialId) as Material;
                    newMaterial.Color = new Color(127, 127, 127);
                }
                else
                {
                    materialId = material.Id;
                }

                MaterialFunctionAssignment materialFunctionAssignment = floorLayer.IsCore
                    ? MaterialFunctionAssignment.Structure
                    : MaterialFunctionAssignment.Finish1;

                CompoundStructureLayer newLayer = floorLayer.ToCompoundStructureLayer(materialId, materialFunctionAssignment);

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

            IDictionary<int, CompoundStructureError> errMap;
            IDictionary<int, int> twoLayerErrMap;

            bool isValid = compoundStructure.IsValid(GlobalVariables.Document, out errMap, out twoLayerErrMap);
            floorType.SetCompoundStructure(compoundStructure);
        }
    }
}
