using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Snaptrude
{
    public class BeamRfaGenerator
    {
        private const string BASE_DIRECTORY = "tmp_beams";
        const string TEMPLATE_FILE_NAME = "resourceFile/Metric Structural Framing - Beams and Braces.rft";
        public double height;

        public static void DeleteAll()
        {
            if (Directory.Exists(BASE_DIRECTORY)) Directory.Delete(BASE_DIRECTORY, true);
        }
        public void CreateRFAFile(Application app, string familyName, double height, List<XYZ> _countour)
        {
            Directory.CreateDirectory(BASE_DIRECTORY);

            Document fdoc = app.NewFamilyDocument(TEMPLATE_FILE_NAME);

            if (fdoc is null) throw new Exception("failed creating fdoc");

            Extrusion extrusion = CreateExtrusion(fdoc, _countour);

            AddAlignments(fdoc, extrusion);

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

                // XYZ p0 = new XYZ(pts[j].X, pts[j].Y, pts[j].Z + height / 2);
                // XYZ p1 = new XYZ(pts[i].X, pts[i].Y, pts[i].Z + height / 2);
                // loop.Append(Line.CreateBound(p0, p1));
                loop.Append(Line.CreateBound(pts[j], pts[i]));
            }
            return loop;
        }

        private Extrusion CreateExtrusion(Document doc, List<XYZ> pts)
        {
            ReferencePlane referencePlane = Utils.FindElement(doc, typeof(ReferencePlane), "Member Right") as ReferencePlane;
            Extrusion existingExtrusion = Utils.FindElement(doc, typeof(Extrusion)) as Extrusion;

            doc.Delete(existingExtrusion.Id);

            CurveArray loop = CreateLoop(pts);
            CurveArrArray profile = new CurveArrArray();
            profile.Append(loop);

            Extrusion extrusion;
            SketchPlane sketch = SketchPlane.Create(doc, referencePlane.GetPlane());
            // Use 2500m, top face MUST align with left member reference plane for alignment
            extrusion = doc.FamilyCreate.NewExtrusion(true, profile, sketch, UnitsAdapter.MMToFeet(2500));

            doc.Regenerate();

            return extrusion;
        }

        private void AddAlignments(Document _doc, Extrusion pBox)
        {

            View pViewPlan = Utils.FindElement(_doc, typeof(ViewPlan), "Ref. Level") as View;

            // find reference planes
            ReferencePlane refRight = Utils.FindElement(_doc, typeof(ReferencePlane), "Member Right") as ReferencePlane;
            ReferencePlane refLeft = Utils.FindElement(_doc, typeof(ReferencePlane), "Member Left") as ReferencePlane;

            // find the face of the box
            PlanarFace faceRight = FindFace(pBox, new XYZ(1.0, 0.0, 0.0));
            PlanarFace faceLeft = FindFace(pBox, new XYZ(-1.0, 0.0, 0.0));

            // create alignments
            _doc.FamilyCreate.NewAlignment(pViewPlan, refRight.GetReference(), faceRight.Reference);
            _doc.FamilyCreate.NewAlignment(pViewPlan, refLeft.GetReference(), faceLeft.Reference);

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
