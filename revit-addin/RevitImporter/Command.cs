using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitImporter.Importer;
using RevitImporter.Utils;
using System;
using System.IO;
using System.Linq;

namespace RevitImporter
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            GlobalVariables.Document = doc;
            GlobalVariables.RvtApp = uiapp.Application;

            uiapp.Application.FailuresProcessing += Application_FailuresProcessing;
            try
            {
                bool status = false;

                View3D view = Get3dView(doc);
                SetDetailViewToFine(doc, view);
                SerializedTrudeData serializedData = ExportViewUsingCustomExporter(doc, view);

                string serializedObject = JsonConvert.SerializeObject(serializedData);
                string snaptrudeManagerPath = "snaptrude-manager";
                string configFileName = "config.json";
                string fileName = "serializedData.json"; // for debugging

                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    snaptrudeManagerPath,
                    configFileName
                );
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    snaptrudeManagerPath,
                    fileName
                );

                string config = File.ReadAllText(configPath);
                Config configObject = JsonConvert.DeserializeObject<Config>(config);
                string floorKey = configObject.floorKey;
                string projectName = floorKey + ".json";
                File.WriteAllText(filePath, serializedObject); // for debugging

                //Uploader.S3helper.UploadJSON(projectName, filePath);

                string requestURL = "snaptrude://finish?name=test";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(requestURL) { UseShellExecute = true });

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("catch", ex.ToString());
                GlobalVariables.cleanGlobalVariables();
                return Result.Failed;
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
                GlobalVariables.cleanGlobalVariables();
            }
        }

        private SerializedTrudeData ExportViewUsingCustomExporter(Document doc, View3D view)
        {
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

        private void SetDetailViewToFine(Document doc, View3D view)
        {
            if (view.DetailLevel == ViewDetailLevel.Fine) return;

            using (Transaction tx = new Transaction(doc, "Update detail level to fine"))
            {
                tx.Start();
                try
                {
                    view.DetailLevel = ViewDetailLevel.Fine;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e); // debug change it to log
                }
                tx.Commit();
            }
        }

        private void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor fa = e.GetFailuresAccessor();

            fa.DeleteAllWarnings();
        }
    }
}