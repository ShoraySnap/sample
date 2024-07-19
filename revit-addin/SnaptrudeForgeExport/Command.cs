using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Events;
using DesignAutomationFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using TrudeImporter;

namespace SnaptrudeForgeExport
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    /// <summary>
    ///     This is the main class responsible for all the operations to create the revit document.
    /// </summary>
    public class Command : IExternalDBApplication
    {
        //Path of the project(i.e)project where your Window family files are present
        public static IDictionary<int, ElementId> LevelIdByNumber = new Dictionary<int, ElementId>();
        public ExternalDBApplicationResult OnStartup(ControlledApplication application)
        {
            DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
            return ExternalDBApplicationResult.Succeeded;
        }
        private void HandleDesignAutomationReadyEvent(object sender, DesignAutomationReadyEventArgs e)
        {
            LogTrace("Design Automation Ready event triggered...");

            // Hook up a custom FailuresProcessing.
            Application rvtApp = e.DesignAutomationData.RevitApp;
            rvtApp.FailuresProcessing += OnFailuresProcessing;

            e.Succeeded = true;
            ParseTrude(e.DesignAutomationData);
        }

        // Overwrite the failure processor to ignore all warnings and resolve all resolvable errors.
        private void OnFailuresProcessing(object sender, FailuresProcessingEventArgs e)
        {
            var fa = e?.GetFailuresAccessor();

            // Ignore all warnings.
            fa.DeleteAllWarnings();

            // Resolve all resolvable errors.
            var failures = fa.GetFailureMessages();
            if (!failures.Any())
            {
                return;
            }

            var preprocessorMessages = fa.GetFailureMessages(FailureSeverity.Error)
                .Union(fa.GetFailureMessages(FailureSeverity.Warning))
                .Where(x => x.HasResolutionOfType(FailureResolutionType.DeleteElements) || x.HasResolutionOfType(FailureResolutionType.DetachElements))
                .ToList();

            if (preprocessorMessages.Count == 0)
                return;

            foreach (var failureAccessor in preprocessorMessages)
            {
                failureAccessor.SetCurrentResolutionType(failureAccessor.HasResolutionOfType(FailureResolutionType.DetachElements) ? FailureResolutionType.DetachElements : FailureResolutionType.DeleteElements);

                fa.ResolveFailure(failureAccessor);
            }

            failures = failures.Where(fail => fail.HasResolutions()).ToList();
            fa.ResolveFailures(failures);

            e.SetProcessingResult(FailureProcessingResult.ProceedWithCommit);
        }

        /// <summary>
        ///  This method parses the trude file and creates corresponding revit document.
        /// </summary>
        private void ParseTrude(DesignAutomationData data)
        {
            if (data == null) throw new InvalidDataException(nameof(data));
            if (data.RevitApp == null) throw new InvalidDataException(nameof(data.RevitApp));

            JObject trudeData = JObject.Parse(File.ReadAllText(Configs.INPUT_TRUDE));

            Application rvtApp = data.RevitApp;
            Document newDoc = rvtApp.OpenDocumentFile("host.rvt");

            GlobalVariables.RvtApp = rvtApp;
            GlobalVariables.Document = newDoc;
            GlobalVariables.ForForge = true;
            GlobalVariables.ForForgePDFExport = ((string)trudeData["outputFormat"] == "pdf");

            if (newDoc == null) throw new InvalidOperationException("Could not create new document.");

            GlobalVariables.materials = trudeData["materials"] as JArray;
            GlobalVariables.multiMaterials = trudeData["multiMaterials"] as JArray;

            //JsonSchemaGenerator jsonSchemaGenerator = new JsonSchemaGenerator();
            //JsonSchema jsonSchema = jsonSchemaGenerator.Generate(typeof(TrudeProperties));

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
            };
            serializer.Converters.Add(new XyzConverter());

            TrudeProperties trudeProperties = trudeData.ToObject<TrudeProperties>(serializer);

            using (TransactionGroup tg = new TransactionGroup(newDoc, "Parse Trude"))
            {
                tg.Start();
                using (Transaction t = new Transaction(newDoc, "Parse Trude"))
                {
                    GlobalVariables.Transaction = t;
                    t.Start();
                    TrudeImporterMain.Import(trudeProperties);
                    t.Commit();
                }
                tg.Assimilate();
            }

            //ImportSnaptrude(structureCollection, newDoc);

            try
            {
                using (Transaction t = new Transaction(newDoc, "remove structural view"))
                {

                    View structuralView = Utils.GetElements(newDoc, typeof(View))
                                               .Select(e => e as View)
                                               .Where(e => e.Title == "Structural Plan: 0")
                                               .ToList().First();
                    t.Start();
                    newDoc.Delete(structuralView.Id);
                    t.Commit();
                }
            } catch { }

            string outputFormat = (string)trudeData["outputFormat"];
            switch(outputFormat)
            {
                case "dwg":
                    ExportAllViewsAsDWG(newDoc);
                    break;
                case "ifc":
                    ExportIFC(newDoc);
                    break;
                case "pdf":
                    ExportPDF(newDoc, trudeProperties);
                    break;
                default:
                    SaveDocument(newDoc);
                    break;
            }
        }

        private void ExportPDF(Document newDoc, TrudeProperties trudeProperties)
        {
#if REVIT2019 || REVIT2020 || REVIT2021
            return;
#else
            using (Transaction t = new Transaction(newDoc, "Set up project information"))
            {
                t.Start();

                ProjectInfo projectInfo = newDoc.ProjectInformation;
                projectInfo.ClientName = trudeProperties.PDFExport.CompanyName;
                projectInfo.LookupParameter("Project Name").Set(trudeProperties.PDFExport.ProjectName);
                newDoc.SetUnits(new Units(trudeProperties.Project.Unit == UnitEnum.Metric ? UnitSystem.Metric : UnitSystem.Imperial));

                t.Commit();
            }

            // This must be created in host.rvt
            ViewPlan template = Utils.FindElement(newDoc, typeof(ViewPlan), "View Template") as ViewPlan;
            List<ViewSheet> sheets = trudeProperties.Views.Select(viewProperties =>
            {
                using (Transaction t = new Transaction(newDoc, "Set up view detail levels, color scheme and sheet"))
                {
                    t.Start();
                    ViewPlan viewPlan = DuplicateViewFromTemplateWithRoomTags(newDoc, viewProperties, template);
                    ViewSheet sheet = null;
                    if (viewPlan != null)
                    {
                        viewPlan.ApplyViewTemplateParameters(template);
                        viewPlan.Scale = viewProperties.Sheet.Scale;
                        SetCropBoxToFitPaperSize(newDoc, viewPlan, viewProperties.Camera.BottomLeft, viewProperties.Camera.TopRight, viewProperties, trudeProperties);
                        FamilySymbol titleBlockType = Utils.GetElements(newDoc, typeof(FamilySymbol))
                                .Cast<FamilySymbol>()
                                .Where(f => f.FamilyName.Replace(" ", "_").ToLower().Contains(Enum.GetName(typeof(SheetSizeEnum), viewProperties.Sheet.SheetSize).ToLower()) && f.Name.Contains("Snaptrude"))
                                .FirstOrDefault();
                        sheet = ViewSheet.Create(newDoc, titleBlockType.Id);
                        sheet.Name = viewProperties.Name;
                        Viewport vp = Viewport.Create(newDoc, sheet.Id, viewPlan.Id, GetViewPosition(viewProperties.Sheet.SheetSize));
                        SetViewColorScheme(newDoc, viewPlan, trudeProperties, viewProperties);
                        HideHiddenViewElements(newDoc, viewPlan, viewProperties);
                        newDoc.GetElement(vp.GetTypeId()).get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL).Set(0);
                        t.Commit();
                    }
                    return sheet;
                }
            }).Where(sheet => sheet != null).ToList();

            Directory.CreateDirectory(Configs.PDF_EXPORT_DIRECTORY);

            PDFExportProperties pdfExport = trudeProperties.PDFExport;

            using (Transaction t = new Transaction(newDoc, "Export to PDF"))
            {
                t.Start();

                PDFExportOptions options = new PDFExportOptions
                {
                    ColorDepth = ColorDepthType.Color,
                    Combine = pdfExport.MergePDFs,
                    ExportQuality = PDFExportQualityType.DPI4000,
                    HideCropBoundaries = true,
                    PaperFormat = ExportPaperFormat.Default,
                    FileName = pdfExport.ProjectName + "_Sheets",
                    //HideReferencePlane = true,
                    //HideScopeBoxes = true,
                    //HideUnreferencedViewTags = true,
                    //MaskCoincidentLines = true,
                    //StopOnError = true,
                    //ViewLinksInBlue = false,
                    ZoomType = ZoomType.Zoom,
                    ZoomPercentage = 100
                };
                newDoc.Export(Configs.PDF_EXPORT_DIRECTORY, sheets.Select(sheet => sheet.Id).ToList(), options);
                t.Commit();
            }

            if (File.Exists(Configs.OUTPUT_FILE)) File.Delete(Configs.OUTPUT_FILE);
            ZipFile.CreateFromDirectory(Configs.PDF_EXPORT_DIRECTORY, Configs.OUTPUT_FILE);

            Directory.Delete(Configs.PDF_EXPORT_DIRECTORY, true);
#endif
        }

        private XYZ GetViewPosition(SheetSizeEnum sheetSize)
        {
            UV size = GetSheetSize(sheetSize);
            UV PDFPaddingX = GlobalVariables.PDFPaddingX;
            UV PDFPaddingY = GlobalVariables.PDFPaddingY;
            double x = (size.V / 2) + 0.008 + (PDFPaddingX.V - PDFPaddingX.U) / 2;
            double y = (size.U / 2) - 0.0035 + (PDFPaddingY.V - PDFPaddingY.U) / 2;
            return new XYZ(x, y, 0);
        }

        private ViewPlan DuplicateViewFromTemplateWithRoomTags(Document doc, ViewProperties viewProperties, View template)
        {
            // Duplicate the view
            ViewFamilyType floorPlanType = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .FirstOrDefault(x => ViewFamily.FloorPlan == x.ViewFamily);
            bool doesLevelExist = GlobalVariables.LevelIdByNumber.TryGetValue(viewProperties.Storey, out ElementId lvlId);
            if (doesLevelExist)
            {
                Level lvl = doc.GetElement(lvlId) as Level;
                ViewPlan ogView = Utils.FindElement(doc, typeof(ViewPlan), lvl.Name) as ViewPlan;
                ViewPlan newView = ViewPlan.Create(doc, floorPlanType.Id, lvl.Id);
                newView.ApplyViewTemplateParameters(template);
                newView.Scale = viewProperties.Sheet.Scale;

                // Collect room tags in the original view
                FilteredElementCollector collector = new FilteredElementCollector(doc, ogView.Id);

                ElementId roomTagTypeId = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(rtt => rtt.FamilyName == GetRoomTagTypeFamilyName(viewProperties))
                    .Select(rtt => rtt.Id)
                    .FirstOrDefault();
                RoomTagType roomTagType = doc.GetElement(roomTagTypeId) as RoomTagType;

                foreach (RoomTag roomTag in collector.OfClass(typeof(SpatialElementTag)).Where(s => s.GetType() == typeof(RoomTag)).Cast<RoomTag>())
                {
                    // Get room tag location
                    LocationPoint locPoint = roomTag.Location as LocationPoint;
                    XYZ tagLocation = locPoint.Point;

                    // Get the referenced room
                    Room room = doc.GetElement(roomTag.Room.Id) as Room;

                    // Create a new room tag in the new view at the same location
                    RoomTag newRoomTag = doc.Create.NewRoomTag(new LinkElementId(room.Id), new UV(tagLocation.X, tagLocation.Y), newView.Id);
                    newRoomTag.RoomTagType = roomTagType; // Copy the tag type
                }

                return newView;
            }
            else
            {
                Utils.LogTrace("Failed to get level for storey " + viewProperties.Storey.ToString());
                return null;
            }
        }

        private void SetViewColorScheme(Document doc, ViewPlan viewPlan, TrudeProperties trudeProperties, ViewProperties viewProperties)
        {
            if (viewProperties.Color.Scheme == ColorSchemeEnum.texture)
            {
                viewPlan.DisplayStyle = DisplayStyle.Textures;
                trudeProperties.Masses.ForEach(mass =>
                {
                    bool doesMassExist = GlobalVariables.UniqueIdToElementId.TryGetValue(mass.UniqueId, out ElementId elemId);
                    if (doesMassExist)
                    {
                        string hex = mass.MaterialHex != null ? mass.MaterialHex.Replace("#", "") : "ffffff";
                        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                        OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                        overrideGraphicSettings.SetCutForegroundPatternColor(new Color(r, g, b));
                        overrideGraphicSettings.SetCutForegroundPatternId(Utils.GetSolidFillPatternElement(doc).Id);
                        overrideGraphicSettings.SetCutForegroundPatternVisible(true);

                        overrideGraphicSettings.SetCutBackgroundPatternColor(new Color(r, g, b));
                        overrideGraphicSettings.SetCutBackgroundPatternId(Utils.GetSolidFillPatternElement(doc).Id);
                        overrideGraphicSettings.SetCutBackgroundPatternVisible(true);

                        viewPlan.SetElementOverrides(elemId, overrideGraphicSettings);
                    }
                });
            }
            else
            {
                viewPlan.DisplayStyle = DisplayStyle.HLR;
            }
        }

        private void HideHiddenViewElements(Document doc, ViewPlan viewPlan, ViewProperties viewProperties) {
            List<ElementId> elementsToHide = viewProperties.Elements.HiddenIds.Select(id =>
            {
                bool elementExists = GlobalVariables.UniqueIdToElementId.TryGetValue(id, out ElementId elementId);
                if (elementExists)
                {
                    return elementId;
                }
                else
                {
                    return null;
                }
            }).Where(elementId => elementId != null).Where(elementId => !doc.GetElement(elementId).IsHidden(viewPlan)).ToList();
            if (elementsToHide.Count > 0)
            {
                viewPlan.HideElements(elementsToHide);
            }

            List<ElementId> roomsToHide = viewProperties.Elements.HiddenIds.Select(id =>
            {
                bool elementExists = GlobalVariables.UniqueIdToRoomId.TryGetValue(id, out ElementId elementId);
                if (elementExists)
                {
                    return elementId;
                }
                else
                {
                    return null;
                }
            }).Where(elementId => elementId != null).Where(elementId => !doc.GetElement(elementId).IsHidden(viewPlan)).ToList();
            if (roomsToHide.Count > 0)
            {
                viewPlan.HideElements(roomsToHide);
            }
        }

        private string GetRoomTagTypeFamilyName(ViewProperties viewProperties)
        {
            string area = viewProperties.Label.Selected.Any(value => value == LabelsEnum.areas) ? "+Area" : "";
            switch (viewProperties.Sheet.Scale)
            {
                case 50:
                case 64:
                    return "Room Tag" + area + "_Snaptrude_3-16";

                case 100:
                case 96:
                    return "Room Tag" + area + "_Snaptrude_1-8";

                case 150:
                case 128:
                    return "Room Tag" + area + "_Snaptrude_3-32";

                case 200:
                case 192:
                    return "Room Tag" + area + "_Snaptrude_1-16";
            }

            return "Room Tag" + area + "_Snaptrude_1-8";
        }

        private void SetCropBoxToFitPaperSize(Document doc, ViewPlan view, XYZ min, XYZ max, ViewProperties viewProperties, TrudeProperties trudeProperties)
        {
            SheetSettings sheetSettings = viewProperties.Sheet;
            BoundingBoxXYZ cropBox = new BoundingBoxXYZ();
            UV PDFPaddingX = GlobalVariables.PDFPaddingX;
            UV PDFPaddingY = GlobalVariables.PDFPaddingY;

            double height_delta = ((max.Y - min.Y) - GetSheetSize(sheetSettings.SheetSize).U * view.Scale) / 2;

            cropBox.Min = new XYZ(min.X + PDFPaddingX.U * view.Scale, min.Y + height_delta + PDFPaddingY.U * view.Scale, cropBox.Min.Z);
            cropBox.Max = new XYZ(max.X - PDFPaddingX.V * view.Scale, max.Y - height_delta - PDFPaddingY.V * view.Scale, cropBox.Max.Z);

            view.CropBox = cropBox;
            view.CropBoxActive = true;
            view.CropBoxVisible = false;
            List<ElementId> roomsToHide = trudeProperties.Masses
                .Select(mass =>
                    {
                        bool doesRoomExist = GlobalVariables.UniqueIdToRoomId.TryGetValue(mass.UniqueId, out ElementId elementId);
                        if (doesRoomExist && 
                            !Utils.IsElementInsideBoundingBox(doc.GetElement(elementId), cropBox) && 
                            !doc.GetElement(elementId).IsHidden(view))
                        {
                            return elementId;
                        }
                        return null;
                    })
                .Where(elementId => elementId != null)
                .ToList();
            if (roomsToHide.Count > 0)
            {
                view.HideElements(roomsToHide);
            }
        }

        private UV GetSheetSize(SheetSizeEnum size)
        {
            switch (size)
            {
                case SheetSizeEnum.ANSI_A: return new UV(0.708, 0.917); // 8.5 x 11 inches
                case SheetSizeEnum.ANSI_B: return new UV(0.917, 1.417); // 11 x 17 inches
                case SheetSizeEnum.ANSI_C: return new UV(1.417, 1.833); // 17 x 22 inches
                case SheetSizeEnum.ANSI_D: return new UV(1.833, 2.833); // 22 x 34 inches
                case SheetSizeEnum.ANSI_E: return new UV(2.833, 3.666); // 34 x 44 inches
                case SheetSizeEnum.ISO_A0: return new UV(2.759, 3.901); // 841 x 1189 mm
                case SheetSizeEnum.ISO_A1: return new UV(1.949, 2.759); // 594 x 841 mm
                case SheetSizeEnum.ISO_A2: return new UV(1.378, 1.949); // 420 x 594 mm
                case SheetSizeEnum.ISO_A3: return new UV(0.974, 1.378); // 297 x 420 mm
                case SheetSizeEnum.ISO_A4: return new UV(0.689, 0.974); // 210 x 297 mm
                case SheetSizeEnum.ARCH_A: return new UV(0.75, 1.0); // 9 x 12 inches
                case SheetSizeEnum.ARCH_B: return new UV(1.0, 1.5); // 12 x 18 inches
                case SheetSizeEnum.ARCH_C: return new UV(1.5, 2.0); // 18 x 24 inches
                case SheetSizeEnum.ARCH_D: return new UV(2.0, 3.0); // 24 x 36 inches
                case SheetSizeEnum.ARCH_E: return new UV(3.0, 4.0); // 36 x 48 inches
                case SheetSizeEnum.ARCH_E1: return new UV(2.5, 3.5); // 30 x 42 inches
                default: throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

        private void SaveDocument(Document newDoc)
        {
            ModelPath ProjectModelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(Configs.OUTPUT_FILE);
            SaveAsOptions SAO = new SaveAsOptions();
            SAO.OverwriteExistingFile = true;

            newDoc.SaveAs(ProjectModelPath, SAO);
            newDoc.Close();
        }

        private void ExportAllViewsAsDWG(Document newDoc)
        {
            Directory.CreateDirectory(Configs.DWG_EXPORT_DIRECTORY);

            List<View> allViews = Utils.GetElements(newDoc, typeof(View))
                                       .Select(e => e as View)
                                       .Where(e => e.CanBePrinted)
                                       .ToList();

            foreach (var view in allViews)
            {
                ExportDWG(newDoc, view);
            }

            if (File.Exists(Configs.OUTPUT_FILE)) File.Delete(Configs.OUTPUT_FILE);
            ZipFile.CreateFromDirectory(Configs.DWG_EXPORT_DIRECTORY, Configs.OUTPUT_FILE);

            Directory.Delete(Configs.DWG_EXPORT_DIRECTORY, true);
        }

        private bool ExportDWG(Document newDoc, View view)
        {
            List<ElementId> viewIds = new List<ElementId>(1);
            viewIds.Add(view.Id);

            bool exported = false;
            using (Transaction t = new Transaction(newDoc, "Export to DWG"))
            {
                t.Start();

                string filename = String.Concat(view.Title.Split(Path.GetInvalidFileNameChars()));

                exported = newDoc.Export(Configs.DWG_EXPORT_DIRECTORY, filename, viewIds, new DWGExportOptions());

                t.Commit();
            }

            return exported;
        }
        private void ExportIFC(Document newDoc)
        {
            using (Transaction t = new Transaction(newDoc, "Export to IFC"))
            {
                t.Start();

                IFCExportOptions options = new IFCExportOptions();
                newDoc.Export(".", Configs.OUTPUT_FILE, options);

                t.Commit();

                File.Move(Configs.OUTPUT_FILE + ".ifc", Configs.OUTPUT_FILE);
            }
        }


        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        /// <summary>
        /// Class representing a staircase.
        /// </summary>        
        public class ST_Staircase
        {
            /// <summary>
            /// Represents snaptrude dsprops.
            /// </summary>
            /// <value>
            /// Gets JToken of dsprops of current stair.
            /// </value>
            public JToken Props { get; set; }
            /// <summary>
            /// Represents snaptrude meshes.
            /// </summary>
            /// <value>
            /// Gets JToken of meshes of current stair.
            /// </value>
            public JToken Mesh { get; set; }
            /// <summary>
            /// Represents bottom level of the stair.
            /// </summary>
            /// <value>
            /// Gets bottom level of current stair.
            /// </value>
            public Level levelBottom { get; set; }
            /// <summary>
            /// Represents top level of the stair.
            /// </summary>
            /// <value>
            /// Gets top level of current stair.
            /// </value>
            public Level levelTop { get; set; }
            public string Name { get; set; }
            /// <summary>
            /// Represents type of the stair. (straight, dogLegged, lShaped or square)
            /// </summary>
            /// <value>
            /// Gets type of current stair.
            /// </value>
            public string Type { get; set; }
            /// <summary>
            /// Represents position of the stair as in Snaptrude.
            /// </summary>
            /// <value>
            /// Gets position of current stair. (double array)
            /// </value>
            public double[] SnaptrudePosition { get; set; }
            /// <summary>
            /// Represents scaling values of the stair. 
            /// </summary>
            /// <value>
            /// Gets scaling values of current stair.
            /// </value>
            public double[] Scaling { get; set; }
            /// <summary>
            /// Constructs a scaling transformation matrix.
            /// </summary>
            /// <param name="centre"> Point w.r.t which scaling will happen. A revit XYZ point.</param>
            /// <returns>Scaling Matrix.</returns>
            public double[,] getScaleMatrix(XYZ centre)
            {
                double[,] scaleMat = new double[4, 4];
                scaleMat[0, 0] = this.Scaling[0];
                scaleMat[1, 1] = this.Scaling[2];
                scaleMat[2, 2] = this.Scaling[1];
                scaleMat[3, 3] = 1.0;
                scaleMat[0, 3] = centre[0] * (1 - this.Scaling[0]);
                scaleMat[1, 3] = centre[1] * (1 - this.Scaling[2]);
                scaleMat[2, 3] = centre[2] * (1 - this.Scaling[1]);
                scaleMat[0, 1] = scaleMat[0, 2] = 0.0;
                scaleMat[1, 0] = scaleMat[1, 2] = 0.0;
                scaleMat[2, 0] = scaleMat[2, 1] = 0.0;
                scaleMat[3, 0] = scaleMat[3, 1] = scaleMat[3, 2] = 0.0;

                return scaleMat;
            }
            /// <summary>
            /// Process the given point using the given transformation matrix.
            /// </summary>
            /// <param name="point">Point to be processed. A revit XYZ point.</param>
            /// <param name="Matrix">Transformation matrix to be used. A 2D double array.</param>
            /// <returns>The processed or transformed point.</returns>
            public XYZ getProcessedPts(XYZ point, double[,] Matrix)
            {
                double[,] currPt = { { point.X }, { point.Y }, { point.Z }, { 1 } };
                double[,] prod = new double[4, 1];
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        prod[i, j] = 0;
                        for (int k = 0; k < 4; k++)
                            prod[i, j] += Matrix[i, k] * currPt[k, j];
                    }
                }
                XYZ processed = new XYZ(prod[0, 0], prod[1, 0], prod[2, 0]);
                return processed;
            }
            /// <summary>
            /// Constructs a rotation transformation matrix.
            /// </summary>
            /// <param name="q">Rotation in terms of a Quaternion quantity. A Quaternion type object.</param>
            /// <param name="centre">Point w.r.t which rotation will happen. A revit XYZ point.</param>
            /// <returns>The rotation matrix.</returns>
            private double[,] getRotMatrix(Quaternion q, XYZ centre)
            {
                double[,] rotMatrix = new double[4, 4];
                q.Normalize();
                double sqw = q.W * q.W;
                double sqx = q.X * q.X;
                double sqy = q.Y * q.Y;
                double sqz = q.Z * q.Z;
                rotMatrix[0, 0] = sqx - sqy - sqz + sqw; // since sqw + sqx + sqy + sqz =1
                rotMatrix[1, 1] = -sqx + sqy - sqz + sqw;
                rotMatrix[2, 2] = -sqx - sqy + sqz + sqw;

                double tmp1 = q.X * q.Y;
                double tmp2 = q.Z * q.W;
                rotMatrix[0, 1] = 2.0 * (tmp1 + tmp2);
                rotMatrix[1, 0] = 2.0 * (tmp1 - tmp2);

                tmp1 = q.X * q.Z;
                tmp2 = q.Y * q.W;
                rotMatrix[0, 2] = 2.0 * (tmp1 - tmp2);
                rotMatrix[2, 0] = 2.0 * (tmp1 + tmp2);

                tmp1 = q.Y * q.Z;
                tmp2 = q.X * q.W;
                rotMatrix[1, 2] = 2.0 * (tmp1 + tmp2);
                rotMatrix[2, 1] = 2.0 * (tmp1 - tmp2);

                double a1, a2, a3;

                a1 = centre.X;
                a2 = centre.Y;
                a3 = centre.Z;

                rotMatrix[0, 3] = a1 - a1 * rotMatrix[0, 0] - a2 * rotMatrix[0, 1] - a3 * rotMatrix[0, 2];
                rotMatrix[1, 3] = a2 - a1 * rotMatrix[1, 0] - a2 * rotMatrix[1, 1] - a3 * rotMatrix[1, 2];
                rotMatrix[2, 3] = a3 - a1 * rotMatrix[2, 0] - a2 * rotMatrix[2, 1] - a3 * rotMatrix[2, 2];
                rotMatrix[3, 0] = rotMatrix[3, 1] = rotMatrix[3, 2] = 0.0;
                rotMatrix[3, 3] = 1.0;

                return rotMatrix;

            }
            /// <summary>
            /// The method used to actually create stairs.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            public ElementId CreateStairs(Document document)
            {
                if (this.Type == "straight")
                {
                    return this.CreateStraightStairs(document);
                }
                else if (this.Type == "square")
                {
                    return this.CreateSquareStairs(document);
                }
                else if (this.Type == "dogLegged")
                {
                    return this.CreateDogLeggedStairs(document);
                }
                else if (this.Type == "lShaped")
                {
                    return this.CreateLShapedStairs(document);
                }
                else return null;
            }
            /// <summary>
            /// The method used to create a straight type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateStraightStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[] stepsArrange = this.Props["stepsArrangement"].First.Select(jv => (int)jv).ToArray();
                        double[] landings = this.Props["landings"].First.Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[landings.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[landings.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            var stepsNo = stepsArrange[i];
                            IList<Curve> bdryCurves = new List<Curve>();
                            IList<Curve> riserCurves = new List<Curve>();
                            IList<Curve> pathCurves = new List<Curve>();

                            XYZ pnt1 = new XYZ(position[0], position[1], position[2]);
                            XYZ pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                            XYZ pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                            XYZ pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);

                            XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                            XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                            XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                            XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                            // boundaries
                            bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                            bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                            // riser curves
                            double interval = (pnt2.X - pnt1.X) / stepsNo;
                            for (int ii = 0; ii <= stepsNo; ii++)
                            {
                                XYZ end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                XYZ end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                riserCurves.Add(Line.CreateBound(rend0, rend1));
                            }

                            //stairs path curves
                            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                            XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                            XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                            pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                            StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                            newRun1.EndsWithRiser = false;
                            topElevArr[landingInd + 1] = newRun1.TopElevation;
                            XYZ nextPos = pnt2;
                            if (landingInd < landings.Length)
                            {
                                XYZ tmp = new XYZ(pnt2.X - landings[landingInd], pnt2.Y, pnt2.Z);
                                nextPos = tmp;
                                landingCor[landingInd] = pnt2;
                                landingInd++;
                            }
                            position[0] = nextPos.X;
                            position[1] = nextPos.Y;
                            position[2] = nextPos.Z;
                        }

                        for (int i = 0; i < landings.Length; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1 = landingCor[i];
                            XYZ p2 = new XYZ(p1.X - landings[i], p1.Y, p1.Z);
                            XYZ p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                            XYZ p4 = new XYZ(p1.X - landings[i], p1.Y - runWidth, p1.Z);

                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method used to create a lShaped type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateLShapedStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => (int)jv1).ToArray()).ToArray();
                        double[][] landings = this.Props["landings"].Select(jv => jv.Select(jv1 => UnitsAdapter.convertToRevit((double)jv1)).ToArray()).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            for (int j = 0; j < stepsArrange[i].Length; j++)
                            {
                                var stepsNo = stepsArrange[i][j];
                                IList<Curve> bdryCurves = new List<Curve>();
                                IList<Curve> riserCurves = new List<Curve>();
                                IList<Curve> pathCurves = new List<Curve>();
                                XYZ pnt1, pnt2, pnt3, pnt4;
                                if (i == 0)
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                    pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }
                                else
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                    pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                                }


                                XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                                XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                                XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                                XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                                // boundaries
                                bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                                bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                                // riser curves
                                double interval;
                                if (i == 0)
                                {
                                    interval = (pnt2.X - pnt1.X) / stepsNo;
                                }
                                else
                                {
                                    interval = (pnt2.Y - pnt1.Y) / stepsNo;
                                }

                                for (int ii = 0; ii <= stepsNo; ii++)
                                {
                                    XYZ end0;
                                    XYZ end1;
                                    if (i == 0)
                                    {
                                        end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                        end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                    }
                                    else
                                    {
                                        end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                        end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                    }

                                    XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                    XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                    riserCurves.Add(Line.CreateBound(rend0, rend1));
                                }

                                //stairs path curves
                                XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                                XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                                XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                                XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                                pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                                StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                                newRun1.EndsWithRiser = false;
                                topElevArr[landingInd + 1] = newRun1.TopElevation;
                                XYZ nextPos = pnt2;
                                double offset = 0;
                                if (i == 0 && j == stepsArrange[i].Length - 1) nextPos = pnt4;
                                else if (i == 0) offset = landings[0][j];
                                landingCor[landingInd] = pnt2;
                                landingInd++;
                                position[0] = nextPos.X - offset;
                                position[1] = nextPos.Y;
                                position[2] = nextPos.Z;
                            }
                        }
                        int tmpIndex = 0;
                        for (int i = 0; i < landings.Length; i++)
                        {
                            for (int j = 0; j < landings[i].Length; j++)
                            {
                                // Add a landing between the runs
                                CurveLoop landingLoop = new CurveLoop();
                                XYZ p1, p2, p3, p4;
                                if (i == 0)
                                {
                                    p1 = landingCor[tmpIndex];
                                    p2 = new XYZ(p1.X - landings[i][j], p1.Y, p1.Z);
                                    p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                    p4 = new XYZ(p1.X - landings[i][j], p1.Y - runWidth, p1.Z);
                                }
                                else
                                {
                                    p1 = landingCor[tmpIndex];
                                    p2 = new XYZ(p1.X, p1.Y - landings[i][j], p1.Z);
                                    p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                    p4 = new XYZ(p1.X - runWidth, p1.Y - landings[i][j], p1.Z);
                                }

                                XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                                XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                                XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                                XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                                Line curve_1 = Line.CreateBound(rp1, rp2);
                                Line curve_2 = Line.CreateBound(rp2, rp4);
                                Line curve_3 = Line.CreateBound(rp4, rp3);
                                Line curve_4 = Line.CreateBound(rp3, rp1);

                                landingLoop.Append(curve_1);
                                landingLoop.Append(curve_2);
                                landingLoop.Append(curve_3);
                                landingLoop.Append(curve_4);
                                StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[tmpIndex + 1]);
                                tmpIndex++;
                            }
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method used to create a dogLegged type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateDogLeggedStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => jv1.ToString() == "" ? 0 : (int)jv1).ToArray()).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        bool singleLanding = false;
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            for (int j = 0; j < stepsArrange[i].Length; j++)
                            {
                                if (stepsArrange[i][j] == 0)
                                {
                                    singleLanding = true;
                                    position[0] = position[0];
                                    position[1] = position[1] - 0.5905511811023623; // approx 150mm
                                    position[2] = position[2];
                                    continue;
                                }

                                var stepsNo = stepsArrange[i][j];
                                IList<Curve> bdryCurves = new List<Curve>();
                                IList<Curve> riserCurves = new List<Curve>();
                                IList<Curve> pathCurves = new List<Curve>();
                                XYZ pnt1, pnt2, pnt3, pnt4;
                                if (i == 0)
                                {
                                    pnt3 = new XYZ(position[0], position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                    pnt1 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }
                                else if (i == 1)
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                    pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                                }
                                else
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0] + stepsNo * tread_depth, position[1], position[2]);
                                    pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt4 = new XYZ(position[0] + stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }

                                XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                                XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                                XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                                XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                                // boundaries
                                bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                                bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                                // riser curves
                                double interval;
                                if (i == 0 || i == 2)
                                {
                                    interval = (pnt2.X - pnt1.X) / stepsNo;
                                }
                                else
                                {
                                    interval = (pnt2.Y - pnt1.Y) / stepsNo;
                                }

                                for (int ii = 0; ii <= stepsNo; ii++)
                                {
                                    XYZ end0;
                                    XYZ end1;
                                    if (i == 0 || i == 2)
                                    {
                                        end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                        end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                    }
                                    else
                                    {
                                        end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                        end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                    }

                                    XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                    XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                    riserCurves.Add(Line.CreateBound(rend0, rend1));
                                }

                                //stairs path curves
                                XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                                XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                                XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                                XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                                pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                                StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                                newRun1.EndsWithRiser = false;
                                topElevArr[landingInd + 1] = newRun1.TopElevation;
                                XYZ nextPos = pnt2;
                                landingCor[landingInd] = pnt2;
                                landingInd++;

                                position[0] = nextPos.X;
                                position[1] = nextPos.Y;
                                position[2] = nextPos.Z;
                            }
                        }

                        for (int i = 0; i < landingInd - 1; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1, p2, p3, p4;

                            if (singleLanding == true)
                            {
                                XYZ tmp = landingCor[i];
                                p1 = new XYZ(tmp.X, tmp.Y + runWidth, tmp.Z);
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y - (2 * runWidth + 0.5905511811023623), p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - (2 * runWidth + 0.5905511811023623), p1.Z);
                            }
                            else if (i == 0 && singleLanding == false)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y + runWidth, p1.Z);
                            }
                            else if (i == 1 && singleLanding == false)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else break;


                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }

                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method to create a square type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateSquareStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (Transaction stairsTrans = new Transaction(document, "Add Runs and Landings to Stairs"))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => (int)jv1).ToArray()).ToArray();
                        //double[] landings = this.Props["landings"].First.Select(jv => (double)jv * 10 / 12).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            var stepsNo = stepsArrange[i][0];
                            IList<Curve> bdryCurves = new List<Curve>();
                            IList<Curve> riserCurves = new List<Curve>();
                            IList<Curve> pathCurves = new List<Curve>();
                            XYZ pnt1, pnt2, pnt3, pnt4;
                            if (i == 0)
                            {
                                pnt3 = new XYZ(position[0], position[1], position[2]);
                                pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                pnt1 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                            }
                            else if (i == 1)
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                            }
                            else if (i == 2)
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0] + stepsNo * tread_depth, position[1], position[2]);
                                pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                pnt4 = new XYZ(position[0] + stepsNo * tread_depth, position[1] - runWidth, position[2]);
                            }
                            else
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0], position[1] + stepsNo * tread_depth, position[2]);
                                pnt3 = new XYZ(position[0] + runWidth, position[1], position[2]);
                                pnt4 = new XYZ(position[0] + runWidth, position[1] + stepsNo * tread_depth, position[2]);
                            }

                            XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                            XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                            XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                            XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                            // boundaries
                            bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                            bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                            // riser curves
                            double interval;
                            if (i == 0 || i == 2)
                            {
                                interval = (pnt2.X - pnt1.X) / stepsNo;
                            }
                            else
                            {
                                interval = (pnt2.Y - pnt1.Y) / stepsNo;
                            }

                            for (int ii = 0; ii <= stepsNo; ii++)
                            {
                                XYZ end0;
                                XYZ end1;
                                if (i == 0 || i == 2)
                                {
                                    end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                    end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                }
                                else
                                {
                                    end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                    end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                }

                                XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                riserCurves.Add(Line.CreateBound(rend0, rend1));
                            }

                            //stairs path curves
                            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                            XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                            XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                            pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                            StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                            newRun1.EndsWithRiser = false;
                            topElevArr[landingInd + 1] = newRun1.TopElevation;
                            XYZ nextPos = pnt2;
                            landingCor[landingInd] = pnt2;
                            landingInd++;
                            position[0] = nextPos.X;
                            position[1] = nextPos.Y;
                            position[2] = nextPos.Z;
                        }

                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1, p2, p3, p4;

                            if (i == 0)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y + runWidth, p1.Z);
                            }
                            else if (i == 1)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else if (i == 2)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X + runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p4 = new XYZ(p1.X + runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p3 = new XYZ(p1.X + runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X + runWidth, p1.Y + runWidth, p1.Z);
                            }

                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
        }

        /// <summary>
        /// FailurePreprocessor class required for StairsEditScope
        /// </summary>
        class StairsFailurePreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // Use default failure processing
                return FailureProcessingResult.Continue;
            }
        }

        /// <summary>
        /// This will appear on the Design Automation output
        /// </summary>
        public static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static bool IsThrowAway(string meshType)
        {
            return meshType.Contains("throwAway");
        }
        private static bool IsThrowAway(JToken data)
        {
            return IsThrowAway(data["meshes"][0]["type"].ToString());
        }

        public static String getMaterialNameFromMaterialId(String materialnameWithId, JArray subMeshes, JArray materials, JArray multiMaterials, int materialIndex)
        {
            if (materialnameWithId == null)
            {
                return null;
            }
            if (subMeshes == null)
            {
                subMeshes = new JArray();
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

            //materialIndex = (int)subMeshes[0]["materialIndex"];

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
    }
}

