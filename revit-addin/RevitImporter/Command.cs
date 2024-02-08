using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitImporter.Importer;
using RevitImporter.Utils;
using System;
using System.IO;

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

                TrudeCustomExporter exporterContext = new TrudeCustomExporter(doc);
                CustomExporter exporter2 = new CustomExporter(doc, exporterContext);

                exporter2.Export(doc.ActiveView);

                SerializedData importData = exporterContext.importData;

                var serializedObject = JsonConvert.SerializeObject(importData);
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
                var configObject = JsonConvert.DeserializeObject<Config>(config);
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

        void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor fa = e.GetFailuresAccessor();

            fa.DeleteAllWarnings();
        }
    }
}