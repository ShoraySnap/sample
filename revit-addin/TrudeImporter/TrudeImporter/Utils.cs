﻿using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class Utils
    {
        public static List<Solid> JoinSolids(List<Solid> solids)
        {
            List<Solid> joinedSolids = new List<Solid>();
            foreach (var solid in solids)
            {
                if (solid == null)
                {
                    continue;
                }
                if (joinedSolids.Any())
                {
                    joinedSolids.Add(solid);
                }
                else
                {
                    try
                    {
                        BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(joinedSolids[0], solid, BooleanOperationsType.Union);
                    }
                    catch (Exception)
                    {
                        joinedSolids.Add(solid);
                    }
                }
            }
            return joinedSolids;
        }

        public static bool IsPointInsideElementGeometryProjection(Element element, XYZ point, FindReferenceTarget findReferenceTarget)
        {
            View3D default3DView = new FilteredElementCollector(GlobalVariables.Document)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(x => !x.IsTemplate);
            if (default3DView == null)
            {
                ViewFamilyType template3DView = new FilteredElementCollector(GlobalVariables.Document)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vt => vt.ViewFamily == ViewFamily.ThreeDimensional) as ViewFamilyType;
                default3DView = View3D.CreateIsometric(GlobalVariables.Document, template3DView.Id);
            }
            ReferenceIntersector intersector = new ReferenceIntersector(element.Id, findReferenceTarget, default3DView);
            IList<ReferenceWithContext> references = intersector.Find(point - XYZ.BasisZ * 10000, XYZ.BasisZ);
            return references.Any();
        }

        public static bool CheckIfPointIsInsideSolidProjection(List<Solid> solids, XYZ point)
        {
            Curve intersectCurve = Line.CreateBound(point + new XYZ(0, 0, 10000), point - new XYZ(0, 0, 10000));
            bool intersect = false;
            foreach (var solid in solids)
            {
                if (solid == null)
                {
                    continue;
                }
                SolidCurveIntersection intersection1 = solid.IntersectWithCurve(intersectCurve, new SolidCurveIntersectionOptions());
                if (intersection1.SegmentCount != 0)
                {
                    intersect = true;
                    break;
                }
            }
            return intersect;
        }

        public static Solid GetElementSolid(Element element)
        {
            Options opt = new Options
            {
                IncludeNonVisibleObjects = true,
                ComputeReferences = true
            };

            Solid solid1 = null;

            GeometryElement geoEle1 = element.get_Geometry(opt);

            if (geoEle1 != null)
            {

                foreach (GeometryObject geoObj in geoEle1)
                {
                    if (geoObj is Solid && ((Solid)geoObj).Volume > 0.0001)
                    {
                        if (solid1 == null)
                        {
                            solid1 = geoObj as Solid;
                        }
                        else
                        {
                            BooleanOperationsUtils.ExecuteBooleanOperation(solid1, geoObj as Solid, BooleanOperationsType.Union);
                        }
                    }
                    else if (geoObj is GeometryInstance)
                    {
                        foreach (GeometryObject geoInstanceObj in (geoObj as GeometryInstance).GetInstanceGeometry())
                        {
                            if (solid1 == null)
                            {
                                solid1 = geoInstanceObj as Solid;
                            }
                            else
                            {
                                BooleanOperationsUtils.ExecuteBooleanOperation(solid1, geoInstanceObj as Solid, BooleanOperationsType.Union);
                            }
                        }
                    }
                }
            }
            return solid1;
        }

        public static bool DocHasFamily(Document doc, string familyName)
        {
            return new FilteredElementCollector(doc)
                        .OfClass(typeof(Family))
                        .Where(f => f.Name == familyName)
                        .Any();
        }

        public static String getMaterialNameFromMaterialId(String materialnameWithId, JArray materials, JArray multiMaterials, int materialIndex)
        {
            if (materialnameWithId == null)
            {
                return null;
            }

            if (materials is null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            if (multiMaterials is null)
            {
                throw new ArgumentNullException(nameof(multiMaterials));
            }

            String materialName = null;

            foreach (JToken eachMaterial in materials)
            {

                if (materialnameWithId == (String)eachMaterial["id"])
                {
                    materialName = materialnameWithId;
                }

            }

            if (materialName == null)
            {
                foreach (JToken eachMultiMaterial in multiMaterials)
                {
                    if (materialnameWithId == (String)eachMultiMaterial["id"])
                    {
                        if (!eachMultiMaterial["materials"].IsNullOrEmpty())
                        {
                            materialName = (String)eachMultiMaterial["materials"][materialIndex];
                        }
                    }
                }

            }

            return materialName;
        }



        private static Random random = new Random();
        public static string RandomString(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void LogProgress(double progress, string step)
        {
            Utils.LogTrace("!ACESAPI:acesHttpOperation({0},\"\",\"\",{1},null)",
                    "onProgress",
                    "{ \"current-progress\": " + progress + ", \"step\": \"" + step + "\" }");
        }

        public static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }

        public static Element FindElement(Document doc, Type targetType, string targetName = null)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            if (targetName is null)
            {
                return collector.OfClass(targetType).FirstOrDefault<Element>();
            }
            else
            {
                return collector.OfClass(targetType).FirstOrDefault<Element>(e => e.Name.ToLower().Equals(targetName.ToLower()));
            }
        }
        public static FillPatternElement GetSolidFillPatternElement(Document Document)
        {
            FilteredElementCollector FEC = new FilteredElementCollector(Document);
            ElementClassFilter ECF = new ElementClassFilter(typeof(FillPatternElement));
            List<FillPatternElement> Els = FEC.WherePasses(ECF).ToElements().Cast<FillPatternElement>().ToList();
            FillPatternElement SFFP = Els.Find(x => x.GetFillPattern().IsSolidFill);
            if (SFFP == null)
            {
                throw new Exception("Solid fill pattern not found, should exist.");
            }
            return SFFP;
        }

        public static List<Element> GetElements(Document doc, Type targetType)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            return collector.OfClass(targetType).ToList<Element>();
        }
        public static CurveLoop GetProfileLoop(List<XYZ> vertices)
        {
            CurveLoop curves = new CurveLoop();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    i++;
                    if (i > vertices.Count() + 3) break;
                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }

        public static bool IsElementInsideBoundingBox(Element element, BoundingBoxXYZ boundingBox)
        {
            BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(null);

            if (elementBoundingBox == null)
                return false;

            XYZ minPoint = elementBoundingBox.Min;
            XYZ maxPoint = elementBoundingBox.Max;

            // Check if all corners of the element's bounding box are within the given bounding box
            return IsPointInsideBoundingBox(minPoint, boundingBox) && IsPointInsideBoundingBox(maxPoint, boundingBox);
        }

        public static bool IsPointInsideBoundingBox(XYZ point, BoundingBoxXYZ boundingBox)
        {
            return (point.X >= boundingBox.Min.X && point.X <= boundingBox.Max.X &&
                    point.Y >= boundingBox.Min.Y && point.Y <= boundingBox.Max.Y &&
                    point.Z >= boundingBox.Min.Z && point.Z <= boundingBox.Max.Z);
        }

        public static string ConvertColorToHex(Color color)
        {
            return ("#" + color.Red.ToString("X2") + color.Green.ToString("X2") + color.Blue.ToString("X2")).ToLower();
        }

        public static TransactionStatus TryStartTransaction()
        {
            if (GlobalVariables.Transaction.HasStarted()) GlobalVariables.Transaction.Commit();
            var status = GlobalVariables.Transaction.Start();
            HandleWarnings(GlobalVariables.Transaction);

            return status;
        }

        public static void HandleWarnings(Transaction trans)
        {
            FailureHandlingOptions options = trans.GetFailureHandlingOptions();
            PreProcessor preproccessor = new PreProcessor();
            options.SetClearAfterRollback(true);
            options.SetFailuresPreprocessor(preproccessor);
            trans.SetFailureHandlingOptions(options);
        }

        class PreProcessor : IFailuresPreprocessor
        {
            FailureProcessingResult IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();

                int resolvedFailures = 0;
                if (fmas.Count == 0)
                    return FailureProcessingResult.Continue;
                foreach (FailureMessageAccessor fma in fmas)
                {
                    if (fma.GetSeverity() == FailureSeverity.Error)
                    {
                        try
                        {
                            TrudeExportLogger.Instance.LogError(
                            GlobalVariables.Document.GetElement(fma.GetFailingElementIds().First()).Category.Name,
                                fma.GetDescriptionText(),
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                                (int)fma.GetFailingElementIds().First().IntegerValue
#else
                                (int)fma.GetFailingElementIds().First().Value
#endif
                            );
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (fma.GetFailureDefinitionId() == BuiltInFailures.EditingFailures.ElementReversed)
                    {
                        GlobalVariables.WallElementIdsToRecreate.Add(fma.GetFailingElementIds().First());
                        failuresAccessor.ResolveFailure(fma);
                    }
                    if (fma.GetDefaultResolutionCaption() == "Unjoin Elements")
                    {
                        failuresAccessor.ResolveFailure(fma);
                        resolvedFailures++;
                    }
                }
                if (resolvedFailures != 0)
                {
                    return FailureProcessingResult.ProceedWithCommit;
                }
                else
                {
                    return FailureProcessingResult.Continue;
                }
            }
        }
    }
}
