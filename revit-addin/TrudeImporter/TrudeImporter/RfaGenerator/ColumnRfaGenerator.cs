using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TrudeImporter
{

    public class ColumnRfaGenerator
    {
        private static string BASE_DIRECTORY = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Snaptrude\tmp\tmp_columns";
        static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        static string TEMPLATE_FILE_NAME = $"{documentsPath}/{Configs.CUSTOM_FAMILY_DIRECTORY}/resourceFile/{GlobalVariables.RvtApp.VersionNumber}/Metric Column.rft";

        //const string TEMPLATE_FILE_NAME = "resourceFile/Metric Column.rft";

        public static void DeleteAll()
        {
            if (Directory.Exists(BASE_DIRECTORY)) Directory.Delete(BASE_DIRECTORY, true);
        }
        public void CreateRFAFile(Application app, string familyName, List<XYZ> _countour, double width, double depth)
        {
            Directory.CreateDirectory(BASE_DIRECTORY);

            Document fdoc = app.NewFamilyDocument(GlobalVariables.ForForge ? "resourceFile/Metric Column.rft" : TEMPLATE_FILE_NAME);

            if (fdoc is null) throw new Exception("failed creating fdoc");

            using (Transaction transaction = new Transaction(fdoc, "create column rfa file"))
            {
                transaction.Start();

                FamilyParameter depthParam = fdoc.FamilyManager.Parameters.Cast<FamilyParameter>().First(p => p.Definition.Name == "Depth");
                FamilyParameter widthParam = fdoc.FamilyManager.Parameters.Cast<FamilyParameter>().First(p => p.Definition.Name == "Width");
                fdoc.FamilyManager.Set(depthParam, depth);
                fdoc.FamilyManager.Set(widthParam, width);

                Extrusion extrusion = CreateExtrusion(fdoc, _countour);
                ApplyMaterial(fdoc, extrusion);
                AddAlignments(fdoc, extrusion);

                transaction.Commit();
            }

            SaveAsOptions opt = new SaveAsOptions();
            opt.OverwriteExistingFile = true;

            fdoc.SaveAs(fileName(familyName), opt);
            fdoc.Close(true);
        }

        private void ApplyMaterial(Document fdoc, Extrusion extrusion)
        {
            string materialName = "Concrete for Columns";
            Material material = Utils.FindElement(GlobalVariables.Document, typeof(Material), materialName) as Material;
            ElementId materialId;
            if (material is null)
            {
                materialId = Material.Create(fdoc, materialName);

                material = fdoc.GetElement(materialId) as Material;
                material.Color = new Color(127, 127, 127);
            }
            else
            {
                materialId = material.Id;
            }

            FillPatternElement fillPatternElement = Utils.GetSolidFillPatternElement(fdoc);

            material.CutForegroundPatternColor = new Color(120, 120, 120);
            try
            {
                material.CutForegroundPatternId = fillPatternElement.Id;
            }
            catch (Exception e)
            {
                Utils.LogTrace("Failed to set fill pattern of custom column", e.Message);
            }

            extrusion.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM).Set(materialId);
        }

        public string fileName(string familyName)
        {
            return $"{BASE_DIRECTORY}/{familyName}.rfa";
        }

        private CurveArray CreateLoop(List<XYZ> vertices)
        {
            CurveArray curves = new CurveArray();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];
                bool samePoint = false;


                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    // This can be potentially handled on snaptrude side by sending correct vertices.Currently, some points are duplicate.
                    if (pt1.X == pt2.X && pt1.Y == pt2.Y && pt1.Z == pt2.Z)
                    {
                        samePoint = true;
                        break;
                    }


                    i++;
                    if (i > vertices.Count() + 3) break;

                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                if (samePoint) continue;
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }

        private Extrusion CreateExtrusion(Document doc, List<XYZ> pts)
        {
            ReferencePlane referencePlane = Utils.FindElement(doc, typeof(ReferencePlane), "Reference Plane") as ReferencePlane;

            CurveArray loop = CreateLoop(pts);
            CurveArrArray profile = new CurveArrArray();
            profile.Append(loop);

            Extrusion extrusion;
            SketchPlane sketch = SketchPlane.Create(doc, referencePlane.GetPlane());
            // Use 4000mm, top face MUST align with upper reference plane
            extrusion = doc.FamilyCreate.NewExtrusion(true, profile, sketch, UnitsAdapter.MMToFeet(4000));

            doc.Regenerate();

            return extrusion;
        }

        private void AddAlignments(Document _doc, Extrusion pBox)
        {
            // (1) we want to constrain the upper face of the column to the "Upper Ref Level"

            View pView = Utils.FindElement(_doc, typeof(View), "Front") as View;

            Level upperLevel = Utils.FindElement(_doc, typeof(Level), "Upper Ref Level") as Level;
            Reference ref1 = upperLevel.GetPlaneReference();

            PlanarFace upperFace = FindFace(pBox, new XYZ(0.0, 0.0, 1.0)); // find a face whose normal is z-up.
            Reference ref2 = upperFace.Reference;

            _doc.FamilyCreate.NewAlignment(pView, ref1, ref2);

            // (2) do the same for the lower level

            Level lowerLevel = Utils.FindElement(_doc, typeof(Level), "Lower Ref. Level") as Level;
            Reference ref3 = lowerLevel.GetPlaneReference();

            PlanarFace lowerFace = FindFace(pBox, new XYZ(0.0, 0.0, -1.0)); // find a face whose normal is z-down.
            Reference ref4 = lowerFace.Reference;

            _doc.FamilyCreate.NewAlignment(pView, ref3, ref4);

            return;
        }

        private PlanarFace FindFace(Extrusion pBox, XYZ normal)
        {
            // get the geometry object of the given element
            Options op = new Options();
            op.ComputeReferences = true;
            GeometryElement geomElem = pBox.get_Geometry(op);

            // loop through the array and find a face with the given normal
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid) // solid is what we are interested in.
                {
                    Solid pSolid = geomObj as Solid;
                    FaceArray faces = pSolid.Faces;
                    foreach (Face pFace in faces)
                    {
                        PlanarFace pPlanarFace = pFace as PlanarFace;
                        if ((pPlanarFace != null) && pPlanarFace.FaceNormal.IsAlmostEqualTo(normal)) // we found the face
                        {
                            return pPlanarFace;
                        }
                    }
                }
            }

            return null;
        }
    }
}
