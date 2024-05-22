﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Linq;
using TrudeSerializer.Debug;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExportToSnaptrudeEEH : IExternalEventHandler
    {
        internal bool isDone = false;
        internal Action<string, UIApplication, Document> OnInit;
        internal Action<View3D, Document> OnView3D;
        internal Action<SerializedTrudeData> OnCleanSerializedTrudeData;
        public void Execute(UIApplication app)
        {
            ExecuteWithUIApplication(app);
        }

        public string GetName()
        {
            return "ExportToSnaptrude";
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return ExecuteWithUIApplication(commandData.Application);
        }

        internal Result ExecuteWithUIApplication(UIApplication uiapp, bool testMode = false)
        {
            TrudeLogger logger = new TrudeLogger();
            logger.Init();
            string processId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            Document doc = uiapp.ActiveUIDocument.Document;

            GlobalVariables.Document = doc;
            GlobalVariables.RvtApp = uiapp.Application;

            uiapp.Application.FailuresProcessing += Application_FailuresProcessing;

            OnInit?.Invoke(processId, uiapp, doc);
            try
            {
                View3D view = Get3dView(doc);
                //SetDetailViewToFine(doc, view);
                OnView3D?.Invoke(view, doc);

                SerializedTrudeData serializedData = ExportViewUsingCustomExporter(doc, view);
                serializedData.SetProcessId(processId);

                ComponentHandler.Instance.CleanSerializedData(serializedData);
                OnCleanSerializedTrudeData?.Invoke(serializedData);

                string serializedObject = JsonConvert.SerializeObject(serializedData);


                logger.SerializeDone(true);
                TrudeDebug.StoreSerializedData(serializedObject);
                try
                {
                    if(!testMode)
                    {
                        Uploader.S3helper.UploadAndRedirectToSnaptrude(serializedData);
                    }
                    logger.UploadDone(true);
                }
                catch(Exception ex)
                {
                    logger.UploadDone(false);
                    TaskDialog.Show("catch", ex.ToString());
                    return Result.Failed;
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                logger.SerializeDone(false);
                logger.UploadDone(false);
                TaskDialog.Show("catch", ex.ToString());
                GlobalVariables.CleanGlobalVariables();
                return Result.Failed;
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
                GlobalVariables.CleanGlobalVariables();
                logger.Save();
                if (!testMode)
                    Uploader.S3helper.UploadLog(logger, processId);
                isDone = true;
            }
        }

        private SerializedTrudeData ExportViewUsingCustomExporter(Document doc, View3D view)
        {
            if(doc.IsFamilyDocument)
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