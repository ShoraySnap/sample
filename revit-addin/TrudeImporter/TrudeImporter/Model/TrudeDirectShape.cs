using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeDirectShape
    {
        public static DirectShape GenerateObjectFromFaces(DirectShapeProperties directShapeProps, BuiltInCategory category)
        {
            List<List<XYZ>> faces = directShapeProps.AllFaceVertices;
            List<int> faceMaterialIds = directShapeProps.FaceMaterialIds;

            Document doc = GlobalVariables.Document;
            FilteredElementCollector materialCollector = new FilteredElementCollector(doc).OfClass(typeof(Material));
            IList<Material> materials = materialCollector.ToElements().Cast<Material>().ToList();

            // Group faces by material
            var facesGroupedByMaterial = faces.Zip(faceMaterialIds, (face, id) => new { Face = face, MaterialId = id })
                .GroupBy(item => item.MaterialId)
                .ToList();

            List<GeometryObject> allGeometry = new List<GeometryObject>();

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
                        materialName = GlobalVariables.sanitizeString(materialName) + "_snaptrude";

                        Material materialElement = materials.FirstOrDefault(m => GlobalVariables.sanitizeString(m.Name) == materialName);
                        ElementId materialId = materialElement != null ? materialElement.Id : ElementId.InvalidElementId;

                        TessellatedFace tFace = new TessellatedFace(face, materialId);
                        builder.AddFace(tFace);
                    }

                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.Mesh;
                    builder.Fallback = TessellatedShapeBuilderFallback.Salvage;
                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();

                    allGeometry.AddRange(result.GetGeometricalObjects());
                }

                // Create a single DirectShape and assign all the accumulated geometries
                DirectShape combinedDirectShape = DirectShape.CreateElement(doc, new ElementId(category));
                combinedDirectShape.ApplicationId = "Combined Application id";
                combinedDirectShape.ApplicationDataId = "Combined Geometry object id";
                combinedDirectShape.SetShape(allGeometry);
                combinedDirectShape.Name = "Combined Trude DirectShape";
                return combinedDirectShape;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error combining the tessellated shapes: " + e.Message);
                throw;
            }
        }
    }
}
