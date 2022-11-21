using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;

namespace Snaptrude
{
    public class VoidRfaGenerator
    {
        private const string BASE_DIRECTORY = "tmp_voids";
        static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string TEMPLATE_FILE_NAME = documentsPath + "/" + Configs.CUSTOM_FAMILY_DIRECTORY + "/resourceFile/Metric Generic Model.rft";
        public double height;

        public static void DeleteAll()
        {
            if (Directory.Exists(BASE_DIRECTORY)) Directory.Delete(BASE_DIRECTORY, true);
        }
        public void CreateRFAFile(Application app, string familyName, List<XYZ> _countour, double rotationAngle)
        {
            Directory.CreateDirectory(BASE_DIRECTORY);

            Document fdoc = app.NewFamilyDocument(TEMPLATE_FILE_NAME);

            if (fdoc is null) throw new Exception("failed creating fdoc");


            using(Transaction t = new Transaction(fdoc, "create extrusion"))
            {
                t.Start();
                Extrusion extrusion = CreateExtrusion(fdoc, _countour);
                extrusion.Location.Rotate(Line.CreateBound(XYZ.Zero, XYZ.BasisZ), rotationAngle);
                t.Commit();
            }

            SaveAsOptions opt = new SaveAsOptions();
            opt.OverwriteExistingFile = true;

            fdoc.SaveAs(fileName(familyName), opt);
            fdoc.Close(true);
        }

        public string fileName(string familyName)
        {
            return $"{BASE_DIRECTORY}/{familyName}.rfa";
        }

        private CurveArray CreateLoop(List<XYZ> pts)
        {
            CurveArray loop = new CurveArray();

            int _n = pts.Count;

            for (int i = 0; i < _n; ++i)
            {
                int j = (0 == i) ? _n - 1 : i - 1;

                loop.Append(Line.CreateBound(pts[j] - new XYZ(5, 0, 0), pts[i] - new XYZ(5, 0, 0)));
            }
            return loop;
        }

        private Extrusion CreateExtrusion(Document doc, List<XYZ> pts)
        {

            CurveArray loop = CreateLoop(pts);
            CurveArrArray profile = new CurveArrArray();
            profile.Append(loop);

            Extrusion extrusion;

            // Build now data before creation
            XYZ bubbleEnd = new XYZ(0, 0, -100);   // bubble end applied to reference plane
            XYZ freeEnd = new XYZ(0, 0, 100);    // free end applied to reference plane
                                                // Third point should not be on the bubbleEnd-freeEnd line 
            XYZ thirdPnt = new XYZ(00, 100, 0);   // 3rd point to define reference plane

            // Create the reference plane in X-Y, applying the active view
            //ReferencePlane refPlane = doc.FamilyCreate.NewReferencePlane2(bubbleEnd, freeEnd, thirdPnt, doc.ActiveView);
            ReferencePlane refPlane = Utils.FindElement(doc, typeof(ReferencePlane), "Center (Left/Right)") as ReferencePlane;
            SketchPlane sketch = SketchPlane.Create(doc, refPlane.GetPlane());

            extrusion = doc.FamilyCreate.NewExtrusion(false, profile, sketch, UnitsAdapter.MMToFeet(3000));

            Parameter p = doc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_ALLOW_CUT_WITH_VOIDS);
            p.Set(1);

            doc.Regenerate();

            return extrusion;
        }
    }
}
