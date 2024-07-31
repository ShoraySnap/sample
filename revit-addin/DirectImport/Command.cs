using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Events;
using DesignAutomationFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Media.Media3D;
using TrudeImporter;

namespace DirectImport
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
            string filePath = e.DesignAutomationData.FilePath;
            string extension = Path.GetExtension(filePath);
            if (extension == ".rvt")
            {
                LogTrace("Processing Revit file....");
                Document doc = e.DesignAutomationData.RevitDoc;
                if (doc == null) throw new InvalidOperationException("Could not open document.");
                int count = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements().Count;
                LogTrace("There are " + count + " elements in the document");
            }
            else if (extension == ".trude")
            {
                LogTrace("Processing Trude file...");
                ParseTrude(e.DesignAutomationData);
            }
            else
            {
                LogTrace("Unsupported file type: {0}", extension);
            }
            //ParseTrude(e.DesignAutomationData);
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

            List<View> printableViews = Utils.GetElements(newDoc, typeof(View))
                                       .Select(e => e as View)
                                       .Where(e => e.CanBePrinted)
                                       .ToList();

            using(Transaction t = new Transaction(newDoc, "Set View details levels and filter overrides"))
            {
                t.Start();

                // ThinWallFilter should be defined in host.rvt
                FilterElement filterElement = Utils.FindElement(newDoc, typeof(FilterElement), "ThinWallFilter") as FilterElement;

                foreach (View v in printableViews)
                {
                    v.DetailLevel = ViewDetailLevel.Fine;

                    if (v.GetFilters().Contains(filterElement.Id)) continue;
                    v.AddFilter(filterElement.Id);

                    OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                    overrideGraphicSettings.SetCutLineColor(new Color(0, 200, 200));
                    overrideGraphicSettings.SetCutLineWeight(1);

                    v.SetFilterOverrides(filterElement.Id, overrideGraphicSettings);

                    OverrideGraphicSettings overrides = new OverrideGraphicSettings();
                    overrides.SetSurfaceTransparency(50);
                    v.SetCategoryOverrides(new ElementId(BuiltInCategory.OST_Floors), overrides);
                }

                t.Commit();
            }

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
                    ExportPDF(newDoc, printableViews);
                    break;
                default:
                    SaveDocument(newDoc);
                    break;
            }
        }

        private void ExportPDF(Document newDoc, List<View> allViews)
        {
#if REVIT2019 || REVIT2020 || REVIT2021
            return;
#else
            Directory.CreateDirectory(Configs.PDF_EXPORT_DIRECTORY);

            List<ElementId> allViewIds = allViews.Select(v => v.Id).ToList();

            using (Transaction t = new Transaction(newDoc, "Export to PDF"))
            {
                t.Start();

                PDFExportOptions options = new PDFExportOptions
                {
                    ColorDepth = ColorDepthType.Color,
                    Combine = false,
                    ExportQuality = PDFExportQualityType.DPI4000,
                    //HideCropBoundaries = true,
                    PaperFormat = ExportPaperFormat.Default,
                    //HideReferencePlane = true,
                    //HideScopeBoxes = true,
                    //HideUnreferencedViewTags = true,
                    //MaskCoincidentLines = true,
                    //StopOnError = true,
                    //ViewLinksInBlue = false,
                    ZoomType = ZoomType.Zoom,
                    ZoomPercentage = 100
                };
                newDoc.Export(Configs.PDF_EXPORT_DIRECTORY, allViewIds, options);
                t.Commit();
            }

            if (File.Exists(Configs.OUTPUT_FILE)) File.Delete(Configs.OUTPUT_FILE);
            ZipFile.CreateFromDirectory(Configs.PDF_EXPORT_DIRECTORY, Configs.OUTPUT_FILE);

            Directory.Delete(Configs.PDF_EXPORT_DIRECTORY, true);
#endif
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
    }
}

