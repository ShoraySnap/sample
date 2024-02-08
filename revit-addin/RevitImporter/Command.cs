using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitImporter.Importer;
using RevitImporter.Utils;
using SnaptrudeManagerAddin;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Xml.Linq;

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

                ImportData importData = exporterContext.importData;

                var serializedObject = JsonConvert.SerializeObject(importData);
                string fileName = "test.json";
                string filePath = @"C:\Users\pooja\Documents\test\test.json";
                string configPath = @"C:\Users\pooja\AppData\Roaming\snaptrude-manager\config.json";
                string config = File.ReadAllText(configPath);
                var configObject = JsonConvert.DeserializeObject<Config>(config);
                var floorKey = configObject.floorKey;
                string projectName = floorKey + ".json";
                File.WriteAllText(filePath, serializedObject);

                //Uploader.S3helper.UploadJSON(projectName, filePath);

                //var server = new NamedPipeServerStream("snaptrudeRevitPipe");

                //server.WaitForConnection();

                //var writer = new StreamWriter(server);
                //writer.AutoFlush = true;
                //var REVIT_PIPE_MSG_DONE_IMPORT = "done-Import"; // 11 characters


                //writer.WriteLine(REVIT_PIPE_MSG_DONE_IMPORT);

                string requestURL = "snaptrude://finish?name=test";
                // System.Diagnostics.Process.Start("explorer", requestURL);
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
