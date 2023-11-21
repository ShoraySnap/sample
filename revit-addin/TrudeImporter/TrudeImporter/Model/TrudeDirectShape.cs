using Autodesk.Revit.DB;
using System.Collections.Generic;
using System;

namespace TrudeImporter
{
    public class TrudeDirectShape
    {
        public static void GenerateObjectFromFaces(List<List<XYZ>> faces, BuiltInCategory category, ElementId materialId = null)
        {
            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
            builder.OpenConnectedFaceSet(true);

            foreach (List<XYZ> face in faces)
            {
                var tFace = new TessellatedFace(face, materialId != null ? materialId : ElementId.InvalidElementId);
                builder.AddFace(tFace);
            }

            builder.CloseConnectedFaceSet();
            builder.Target = TessellatedShapeBuilderTarget.Mesh;
            builder.Fallback = TessellatedShapeBuilderFallback.Salvage;

            TessellatedShapeBuilderResult result;
            try
            {
                builder.Build();
            }
            catch (Exception e)
            {
                result = builder.GetBuildResult();
                for (int i = 0; i < faces.Count; i++)
                {
                    var issues = result.GetIssuesForFaceSet(i);
                    foreach (var issue in issues)
                        System.Diagnostics.Debug.WriteLine("i=" + i + " : " + issue.GetIssueDescription());
                }

                throw e;
            }

            result = builder.GetBuildResult();

            DirectShape ds = DirectShape.CreateElement(GlobalVariables.Document, new ElementId(category));
            ds.ApplicationId = "Application id";
            ds.ApplicationDataId = "Geometry object id";

            ds.SetShape(result.GetGeometricalObjects());
        }
    }
}