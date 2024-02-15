using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;

namespace TrudeImporter
{
    public class VoidRfaGenerator
    {
        static string documentsPath = GlobalVariables.ForForge
                ? "."
                : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "/" + Configs.CUSTOM_FAMILY_DIRECTORY;
        private static string BASE_DIRECTORY = "tmp_voids";
        //string TEMPLATE_FILE_NAME = documentsPath + "/" + Configs.CUSTOM_FAMILY_DIRECTORY + "/resourceFile/Metric Generic Model wall based.rft";
        string TEMPLATE_FILE_NAME = $"{documentsPath}/resourceFile/{GlobalVariables.RvtApp.VersionNumber}/Metric Generic Model.rft";
        public double height;

        public static void DeleteAll()
        {
            if (Directory.Exists(BASE_DIRECTORY)) Directory.Delete(BASE_DIRECTORY, true);
        }
        public bool CreateRFAFile(Application app, string familyName, List<XYZ> _countour, double thickness, Plane plane)
        {
            Directory.CreateDirectory(BASE_DIRECTORY);

            Document fdoc = app.NewFamilyDocument(GlobalVariables.ForForge ? "resourceFile/Metric Generic Model.rft" : TEMPLATE_FILE_NAME);

            if (fdoc is null) throw new Exception("failed creating fdoc");


            using(Transaction t = new Transaction(fdoc, "create extrusion"))
            {
                t.Start();

                CreateExtrusion(fdoc, _countour, thickness, plane);

                fdoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_ALLOW_CUT_WITH_VOIDS).Set(1);

                fdoc.Regenerate();

                t.Commit();
            }

            SaveAsOptions opt = new SaveAsOptions();
            opt.OverwriteExistingFile = true;

            fdoc.SaveAs(fileName(familyName), opt);
            return fdoc.Close(true);
        }

        public string fileName(string familyName)
        {
            return $"{BASE_DIRECTORY}/{familyName}.rfa";
        }

        private CurveArray CreateLoop(List<XYZ> pts, double thickness)
        {
            CurveArray loop = new CurveArray();

            int _n = pts.Count;

            for (int i = 0; i < _n; ++i)
            {
                int j = (0 == i) ? _n - 1 : i - 1;

                loop.Append(Line.CreateBound(pts[j], pts[i]));
            }
            return loop;
        }

        private void CreateExtrusion(Document doc, List<XYZ> pts, double thickness, Plane plane)
        {
            CurveArray loop = CreateLoop(pts, thickness);
            CurveArrArray profile = new CurveArrArray();
            profile.Append(loop);

            //ReferencePlane referencePlane = Utils.FindElement(doc, typeof(ReferencePlane), "Wall") as ReferencePlane;
            //SketchPlane sketch = SketchPlane.Create(doc, referencePlane.GetPlane());

            plane = Plane.CreateByThreePoints(pts[0], pts[1], pts[2]);
            SketchPlane sketch = SketchPlane.Create(doc, plane);

            //Extrusion extrusion = doc.FamilyCreate.NewExtrusion(false, profile, sketch, thickness + UnitsAdapter.MMToFeet(100));
            doc.FamilyCreate.NewExtrusion(false, profile, sketch, -thickness / 2);
            doc.FamilyCreate.NewExtrusion(false, profile, sketch, thickness / 2);
        }
    }
}
