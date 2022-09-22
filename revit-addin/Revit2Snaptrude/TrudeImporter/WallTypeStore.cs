using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Snaptrude
{
    public class WallTypeStore : TypeStore<ST_Layer[], WallType>
    {
        public override string KeyAdapter(ST_Layer[] wallLayers, WallType _defaultType)
        {
            string layers = wallLayers[0].BaseTypeName;
            foreach(ST_Layer w in wallLayers)
            {
                layers += "_" + w.ToString();

                if (w.IsCore) layers += "-core";
            }

            return layers;
        }

        public override void TypeModifier(ST_Layer[] wallLayers, WallType wallType)
        {
            CompoundStructure compoundStructure = wallType.GetCompoundStructure();

            ElementId materialId;

            IList<CompoundStructureLayer> layers = new List<CompoundStructureLayer>();

            int numberOfExteriorShellLayers = 0;
            int numberOfInteriorShellLayers = 0;

            bool sawCore = false;
            int coreIndex = 0;
            foreach (ST_Layer wallLayer in wallLayers)
            {
                if (wallLayer.IsCore)
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

                Material material = Utils.FindElement(GlobalVariables.Document, typeof(Material), wallLayer.Name) as Material;

                if (material is null)
                {
                    materialId = Material.Create(GlobalVariables.Document, wallLayer.Name);

                    material = GlobalVariables.Document.GetElement(materialId) as Material;
                    material.Color = new Color(127, 127, 127);
                }
                else
                {
                    materialId = material.Id;
                }

                SetFillPattern(wallLayer.Name, material);


                MaterialFunctionAssignment materialFunctionAssignment = wallLayer.IsCore
                    ? MaterialFunctionAssignment.Structure
                    : MaterialFunctionAssignment.Finish1;

                CompoundStructureLayer newLayer = wallLayer.ToCompoundStructureLayer(materialId, materialFunctionAssignment);

                layers.Add(newLayer);
            }

            if (compoundStructure is null)
            {
                compoundStructure = CompoundStructure.CreateSimpleCompoundStructure(layers);
            } else
            {
                compoundStructure.SetLayers(layers);
            }
            compoundStructure.SetNumberOfShellLayers(ShellLayerType.Exterior, numberOfExteriorShellLayers);
            compoundStructure.SetNumberOfShellLayers(ShellLayerType.Interior, numberOfInteriorShellLayers);
            compoundStructure.StructuralMaterialIndex = coreIndex;

            wallType.SetCompoundStructure(compoundStructure);
        }

        private void SetFillPattern(string layerName, Material material)
        {
            FillPattern fillPattern;
            FillPatternElement fillPatternElement = GlobalVariables.Document.GetElement(material.CutForegroundPatternId) as FillPatternElement;

            if (layerName == null)
            {
                layerName = "undefined";
            }

            switch (layerName.ToLower())
            {
                case "glass":
                    // do nothing
                    return;
                case "concrete":
                    fillPatternElement = Utils.GetSolidFillPatternElement(GlobalVariables.Document);

                    material.CutForegroundPatternId = fillPatternElement.Id;
                    material.CutForegroundPatternColor = new Color(120, 120, 120);
                    break;
                case "brick":
                    if (fillPatternElement is null)
                    {
                        fillPattern = new FillPattern("Diagonal up",
                            FillPatternTarget.Drafting,
                            FillPatternHostOrientation.ToView,
                            0.785398,
                            UnitsAdapter.MMToFeet(1.5));
                        fillPatternElement = Utils.FindElement(GlobalVariables.Document, typeof(FillPatternElement), "Diagonal up") as FillPatternElement;

                        if (fillPatternElement is null)
                        {
                            fillPatternElement = FillPatternElement.Create(GlobalVariables.Document, fillPattern);
                        }
                        fillPatternElement.SetFillPattern(fillPattern);
                    }

                    material.CutForegroundPatternId = fillPatternElement.Id;
                    material.CutForegroundPatternColor = new Color(127, 127, 127);
                    break;
                default:
                    if (fillPatternElement is null)
                    {
                        fillPattern = new FillPattern("Diagonal crosshatch",
                            FillPatternTarget.Drafting,
                            FillPatternHostOrientation.ToView,
                            0.785398,
                            UnitsAdapter.MMToFeet(1.0),
                            UnitsAdapter.MMToFeet(1.0));

                        fillPatternElement = Utils.FindElement(GlobalVariables.Document, typeof(FillPatternElement), "Diagonal crosshatch") as FillPatternElement;

                        if (fillPatternElement is null)
                        {
                            fillPatternElement = FillPatternElement.Create(GlobalVariables.Document, fillPattern);
                        }
                        fillPatternElement.SetFillPattern(fillPattern);
                    }

                    material.CutForegroundPatternId = fillPatternElement.Id;
                    material.CutForegroundPatternColor = new Color(127, 127, 127);
                    break;
            }
        }
    }
}
