using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeDirectShape
    {
        public static void GenerateObjectFromFaces(DirectShapeProperties directShapeProps, BuiltInCategory category)
        {
            List<List<XYZ>> faces = directShapeProps.AllFaceVertices;
            List<int> faceMaterialIds = directShapeProps.FaceMaterialIds;

            Document doc = GlobalVariables.Document; // Assuming GlobalVariables.Document is the active Revit Document
            FilteredElementCollector materialCollector = new FilteredElementCollector(doc).OfClass(typeof(Material));
            IList<Material> materials = materialCollector.ToElements().Cast<Material>().ToList();

            // Group faces by material
            var facesGroupedByMaterial = faces.Zip(faceMaterialIds, (face, id) => new { Face = face, MaterialId = id })
                .GroupBy(item => item.MaterialId)
                .ToList();

            try
            {
                foreach (var group in facesGroupedByMaterial)
                {
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(false);

                    foreach (var faceInfo in group)
                    {
                        List<XYZ> face = faceInfo.Face;
                        int materialIndex = faceInfo.MaterialId;
                        string materialName = Utils.getMaterialNameFromMaterialId(
                            directShapeProps.MaterialName,
                            GlobalVariables.materials,
                            GlobalVariables.multiMaterials,
                            materialIndex);
                        materialName = GlobalVariables.sanitizeString(materialName);

                        Material materialElement = materials.FirstOrDefault(m => GlobalVariables.sanitizeString(m.Name) == materialName);
                        ElementId materialId = materialElement != null ? materialElement.Id : ElementId.InvalidElementId;

                        TessellatedFace tFace = new TessellatedFace(face, materialId);
                        builder.AddFace(tFace);
                    }

                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
                    builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();
                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(category));
                    ds.ApplicationId = "Application id";
                    ds.ApplicationDataId = "Geometry object id";
                    ds.SetShape(result.GetGeometricalObjects());
                    ds.Name = "Trude DirectShape Material " + group.Key;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error building the tessellated shapes: " + e.Message);
                throw;
            }
        }
    }
}
