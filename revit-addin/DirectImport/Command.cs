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
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using TrudeImporter;
using TrudeSerializer;
using TrudeSerializer.Importer;

namespace DirectImport
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    /// <summary>
    ///     This is the main class responsible for all the operations to create the revit document.
    /// </summary>
    public class Command : IExternalDBApplication
    {
        internal Action<SerializedTrudeData> OnCleanSerializedTrudeData;
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
            string filename = Path.GetFileName(filePath);
            if (extension == ".rvt")
            {
                LogTrace("Processing Revit file....");
                Document doc = e.DesignAutomationData.RevitDoc ?? throw new InvalidOperationException("Could not open document.");
                LogTrace("Recieved File: {0}", filePath);
                ExportTrude(e.DesignAutomationData, filename);
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
        ///  This method exports the trude file.
        /// </summary>
        private void ExportTrude(DesignAutomationData data, string filename)
        {
            if (data == null) throw new InvalidDataException(nameof(data));
            if (data.RevitApp == null) throw new InvalidDataException(nameof(data.RevitApp));

            //List<data> _data = new List<data>();
            //_data.Add(new data()
            //{
            //    Id = 1,
            //    SSN = 2,
            //    Message = "A Message"
            //});
            //string json = JsonConvert.SerializeObject(_data.ToArray(), Formatting.Indented);
            //using (StreamWriter sw = File.CreateText("result.trude"))
            //{
            //    sw.WriteLine(JsonConvert.SerializeObject(_data, Formatting.Indented));

            //    sw.Close();
            //}

            Application rvtApp = data.RevitApp;
            Document newDoc = rvtApp.OpenDocumentFile(filename);
            if (newDoc == null) throw new InvalidOperationException("Could not create new document.");
            View3D view = Get3dView(newDoc);
            SerializedTrudeData serializedData = ExportViewUsingCustomExporter(newDoc, view);
            ComponentHandler.Instance.CleanSerializedData(serializedData);
            OnCleanSerializedTrudeData?.Invoke(serializedData);
            string serializedObject = JsonConvert.SerializeObject(serializedData);

            using (StreamWriter sw = File.CreateText("result.trude"))
            {
                sw.WriteLine(serializedObject);
                sw.Close();
            }

            LogTrace("Json file created successfully....");
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

        private SerializedTrudeData ExportViewUsingCustomExporter(Document doc, View3D view)
        {
            if (doc.IsFamilyDocument)
            {
                TrudeCustomExporterForRFA exporterContextForRFA = new TrudeCustomExporterForRFA(doc);
                CustomExporter exporterForRFA = new CustomExporter(doc, exporterContextForRFA);

                exporterForRFA.Export(view);
                return exporterContextForRFA.GetExportData();
            }

            TrudeCustomExporter exporterContext = new TrudeCustomExporter(doc);
            CustomExporter exporter = new CustomExporter(doc, exporterContext);

            exporter.Export(view);
            return exporterContext.GetExportData();
        }

        private View3D Get3dView(Document doc)
        {
            View currentView = doc.ActiveView;
            if (currentView is View3D) return currentView as View3D;

            Element default3DView = new FilteredElementCollector(doc).OfClass(typeof(View3D)).ToElements().FirstOrDefault();

            return default3DView as View3D;
        }
    }

    public class data
    {
        public int Id { get; set; }
        public int SSN { get; set; }
        public string Message { get; set; }
    }
}

