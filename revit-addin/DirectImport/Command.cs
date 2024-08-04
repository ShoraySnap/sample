using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Events;
using DesignAutomationFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Markup;
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
                LogTrace("Recieved File: {0}", filePath);
                List<data> _data = new List<data>();
                _data.Add(new data()
                {
                    Id = 1,
                    SSN = 2,
                    Message = "A Message"
                });
                string json = JsonConvert.SerializeObject(_data.ToArray(), Formatting.Indented);
                using (StreamWriter sw = File.CreateText("result.trude"))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(_data, Formatting.Indented));

                    sw.Close();
                }
                LogTrace("Json file created successfully....");
            }
            else
            {
                LogTrace("Unsupported file type: {0}", extension);
            }
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

    }

    public class data
    {
        public int Id { get; set; }
        public int SSN { get; set; }
        public string Message { get; set; }
    }
}

