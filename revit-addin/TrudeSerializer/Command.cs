using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Linq;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer
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
                serializedData.CleanSerializedData();
                string serializedObject = JsonConvert.SerializeObject(serializedData);

                TrudeDebug.StoreSerializedData(serializedObject);

                Uploader.S3helper.UploadAndRedirectToSnaptrude(serializedData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("catch", ex.ToString());
                GlobalVariables.CleanGlobalVariables();
                return Result.Failed;
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
                GlobalVariables.CleanGlobalVariables();
            }
        }

        private SerializedTrudeData ExportViewUsingCustomExporter(Document doc, View3D view)
        {
            TrudeCustomExporter exporterContext = new TrudeCustomExporter(doc);
            CustomExporter exporter = new CustomExporter(doc, exporterContext);

            exporter.Export(view as View);
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