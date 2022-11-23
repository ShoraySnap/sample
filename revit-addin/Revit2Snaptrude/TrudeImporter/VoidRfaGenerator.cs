using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;

namespace Snaptrude
{
    public class VoidRfaGenerator
    {
        static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static string BASE_DIRECTORY = documentsPath + "/" + Configs.CUSTOM_FAMILY_DIRECTORY + "tmp_voids";
        string TEMPLATE_FILE_NAME = documentsPath + "/" + Configs.CUSTOM_FAMILY_DIRECTORY + "/resourceFile/Metric Generic Model wall based.rft";
        public double height;

        public static void DeleteAll()
        {
            if (Directory.Exists(BASE_DIRECTORY)) Directory.Delete(BASE_DIRECTORY, true);
        }
        public bool CreateRFAFile(Application app, string familyName, List<XYZ> _countour, double thickness)
        {
            Directory.CreateDirectory(BASE_DIRECTORY);

            Document fdoc = app.NewFamilyDocument(TEMPLATE_FILE_NAME);

            if (fdoc is null) throw new Exception("failed creating fdoc");


            using(Transaction t = new Transaction(fdoc, "create extrusion"))
            {
                t.Start();
                Extrusion extrusion = CreateExtrusion(fdoc, _countour, thickness);

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

                loop.Append(Line.CreateBound(new XYZ(pts[j].X, thickness / 2, pts[j].Z), new XYZ(pts[i].X, thickness / 2, pts[i].Z)));
            }
            return loop;
        }

        private Extrusion CreateExtrusion(Document doc, List<XYZ> pts, double thickness)
        {
            CurveArray loop = CreateLoop(pts, thickness);
            CurveArrArray profile = new CurveArrArray();
            profile.Append(loop);

            ReferencePlane referencePlane = Utils.FindElement(doc, typeof(ReferencePlane), "Wall") as ReferencePlane;
            SketchPlane sketch = SketchPlane.Create(doc, referencePlane.GetPlane());

            Extrusion extrusion = doc.FamilyCreate.NewExtrusion(false, profile, sketch, thickness);

            return extrusion;
        }
    }
}
