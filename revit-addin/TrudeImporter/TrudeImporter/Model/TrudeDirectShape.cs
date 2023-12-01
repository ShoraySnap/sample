using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeDirectShape
    {
        public static void GenerateObjectFromFaces(List<List<XYZ>> faces, List<int> faceMaterialIds, string materialName, BuiltInCategory category)
        {
            Document doc = GlobalVariables.Document; // Assuming GlobalVariables.Document is the active Revit Document
            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
            builder.OpenConnectedFaceSet(true);

            // Collect all materials once to avoid querying in each iteration
            FilteredElementCollector materialCollector = new FilteredElementCollector(doc).OfClass(typeof(Material));
            IList<Material> materials = materialCollector.ToElements().Cast<Material>().ToList();

            for (int faceIndex = 0; faceIndex < faces.Count; faceIndex++)
            {
                List<XYZ> face = faces[faceIndex];
                int materialIndex = faceMaterialIds[faceIndex];

                // Get the material name from the utility function
                string snaptrudeMaterialName = Utils.getMaterialNameFromMaterialId(
                    materialName,
                    GlobalVariables.materials,
                    GlobalVariables.multiMaterials,
                    materialIndex);
                snaptrudeMaterialName = GlobalVariables.sanitizeString(snaptrudeMaterialName);

                // Find the material by name
                Material materialElement = materials.FirstOrDefault(m => GlobalVariables.sanitizeString(m.Name) == snaptrudeMaterialName);

                // If material is not found and the name contains "glass", try to find a glass material
                if (materialElement == null && snaptrudeMaterialName.ToLower().Contains("glass"))
                {
                    materialElement = materials.FirstOrDefault(m => m.Name.ToLower().Contains("glass"));
                }

                // If material is still not found, use a default or invalid material ID
                ElementId materialId = materialElement != null ? materialElement.Id : ElementId.InvalidElementId;

                // Create the TessellatedFace with the specific material ID
                TessellatedFace tFace = new TessellatedFace(face, materialId);
                builder.AddFace(tFace);
            }

            builder.CloseConnectedFaceSet();
            builder.Target = TessellatedShapeBuilderTarget.Mesh;
            builder.Fallback = TessellatedShapeBuilderFallback.Salvage;

            TessellatedShapeBuilderResult result;
            try
            {
                builder.Build();
                result = builder.GetBuildResult();

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(category));
                ds.ApplicationId = "Application id";
                ds.ApplicationDataId = "Geometry object id";
                ds.SetShape(result.GetGeometricalObjects());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error building the tessellated shape: " + e.Message);
                throw;
            }
        }
    }
}
