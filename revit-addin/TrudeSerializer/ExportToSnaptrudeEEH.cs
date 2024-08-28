using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerAddin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TrudeCommon.Analytics;
using TrudeCommon.Utils;
using TrudeSerializer.Components;
using TrudeSerializer.Debug;
using TrudeSerializer.Importer;
using TrudeSerializer.Uploader;
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


        internal int processedElements = 0;
        internal Dictionary<ElementId, bool> elementsDone;

        internal Logger classLogger = LogManager.GetCurrentClassLogger();

        public static bool ExportInterrupted = false;

        public void Execute(UIApplication app)
        {
            Application.Instance.AbortExportFlag = false;
            app.DialogBoxShowing += App_DialogBoxShowing;
            ExecuteWithUIApplication(app);
            app.DialogBoxShowing -= App_DialogBoxShowing;
        }

        private static void App_DialogBoxShowing(object sender, DialogBoxShowingEventArgs e)
        {
            if(e is TaskDialogShowingEventArgs tde)
            {
                if(tde.Message.ToLower().Contains("interrupted"))
                {
                    ExportInterrupted = true;
                }
            }
        }

        public static void InterruptWithAbort()
        {
            InterruptsReset();
            Application.Instance.AbortExportFlag = true;
        }

        public static void InterruptsReset()
        {
            if(ExportInterrupted)
            {
                ExportInterrupted = false;
            }
        }

        public string GetName()
        {
            return "ExportToSnaptrude";
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Application.Instance.AbortExportFlag = false;
            var result = ExecuteWithUIApplication(commandData.Application);
            return result;
        }

        internal string GetUniqueProcessId(Document doc, int length)
        {
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string guid = doc.PathName.ToString();

            string combined = string.Join("", new string[] {timestamp, guid});

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashString.Substring(0, length);
            }
        }


        void CountTotalElements(Document doc, View view)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id);

            var list = collector.WhereElementIsNotElementType().Where(e =>
            {
                if (e is Autodesk.Revit.DB.Group) return true;
                if (e.Category == null) return false;
                if (e.ViewSpecific) return false;
                if (((BuiltInCategory)e.Category.Id.Value) == BuiltInCategory.OST_HVAC_Zones) return false;

                return e.Category.CategoryType == CategoryType.Model && e.Category.CanAddSubcategory;
            });

            foreach (var item in list)
            {
                elementsDone.Add(item.Id, false);
            }
        }

        static void UploadProgressUpdate(float p, string message)
        {
            if (IsImportAborted()) return;
            int progress = (int)Math.Round(60.0 + p * 40.0);

            Application.Instance.UpdateProgressForExport(progress, message);
        }
        void CountCleansAndUpdateProgress(int cleans, int total)
        {
            if (IsImportAborted()) return;
            float p = cleans / (float)total;
            int progress = (int)Math.Round(40.0 + p * 20.0);

            Application.Instance.UpdateProgressForExport(progress,$"Cleaning Serialized Data... {cleans} / {total}");
        }

        void CountElementAndUpdateProgress(TrudeComponent component, Element e)
        {
            if (IsImportAborted()) return;

            if(elementsDone.TryGetValue(e.Id, out bool done))
            {
                if(done)
                {
                    classLogger.Warn("Same element serializing twice!");
                }
                else if(component != null)
                {
                    elementsDone[e.Id] = true;
                }
            }
            else
            {
                classLogger.Info("Element not in list : {0}, {1}", component.elementId, e);
            }

            processedElements = elementsDone.Where((elem) => elem.Value == true).Count();
            float p = processedElements / (float)elementsDone.Count();
            int progress = (int)Math.Round(p * 40.0);

            Application.Instance.UpdateProgressForExport(progress,$"Serializing Elements... {processedElements} / {elementsDone.Count()}");
        }


        internal Result ExecuteWithUIApplication(UIApplication uiapp, bool testMode = false)
        {
            TrudeLogger logger = new TrudeLogger();
            logger.Init();

            Document doc = uiapp.ActiveUIDocument.Document;
            string processId = GetUniqueProcessId(doc, 12);

            GlobalVariables.Document = doc;
            GlobalVariables.RvtApp = uiapp.Application;

            uiapp.Application.FailuresProcessing += Application_FailuresProcessing;
            string floorkey = Config.GetConfigObject().floorKey;

            OnInit?.Invoke(processId, uiapp, doc);

            processedElements = 0;
            elementsDone = new Dictionary<ElementId, bool>();

            try
            {
                View3D view = Get3dView(doc);
                //SetDetailViewToFine(doc, view);
                OnView3D?.Invoke(view, doc);

                Application.Instance.UpdateProgressForExport(0, "Serializing Revit project...");
                CountTotalElements(doc, view);
                ComponentHandler.OnCountOutput += CountElementAndUpdateProgress;

                SerializedTrudeData serializedData = ExportViewUsingCustomExporter(doc, view);
                if (IsImportAborted())
                {
                    return Result.Cancelled;
                }

                ComponentHandler.OnCountOutput -= CountElementAndUpdateProgress;
                classLogger.Info("Serialization done. Elements serialized: {0} / {1}", processedElements, elementsDone.Count());

                serializedData.SetProcessId(processId);

                // Analytics Id
                var config = Config.GetConfigObject();

                string version = Application.Instance.GetVersion();
                AnalyticsManager.SetIdentifer("EMAIL", config.userId, config.floorKey, serializedData.ProjectProperties.ProjectUnit, URLsConfig.GetSnaptrudeReactUrl(), processId, version);

                Application.Instance.UpdateProgressForExport(40, "Cleaning Serialized data...");
                SerializedTrudeData.CleanProgressUpdate += CountCleansAndUpdateProgress;
                ComponentHandler.Instance.CleanSerializedData(serializedData);
                OnCleanSerializedTrudeData?.Invoke(serializedData);
                SerializedTrudeData.CleanProgressUpdate -= CountCleansAndUpdateProgress;

                string serializedObject = JsonConvert.SerializeObject(serializedData);
                if (IsImportAborted())
                {
                    return Result.Cancelled;
                }

                logger.SerializeDone(true);
                TrudeLocalAppData.StoreSerializedData(serializedObject);
                if (IsImportAborted())
                {
                    return Result.Cancelled;
                }

                Application.Instance.UpdateProgressForExport(80, "Uploading Serialized data...");
                try
                {
                    floorkey = Config.GetConfigObject().floorKey;
                    if(!testMode)
                    {
                        S3UploadHelper.SetProgressUpdate(UploadProgressUpdate);
                        S3UploadHelper.Upload(serializedData.GetSerializedObject(), floorkey);
                        MaterialUploader.Instance.Upload();
                    }
                    logger.UploadDone(true);

                }
                catch(Exception ex)
                {
                    logger.UploadDone(false);
                    //TaskDialog.Show("catch", ex.ToString());
                    Application.Instance.ExportFailure();
                    return Result.Failed;
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                logger.SerializeDone(false);
                logger.UploadDone(false);
                //TaskDialog.Show("catch", ex.ToString());
                Application.Instance.ExportFailure();
                GlobalVariables.CleanGlobalVariables();
                return Result.Failed;
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
                GlobalVariables.CleanGlobalVariables();
                logger.Save();

                // Analytics Save
                AnalyticsManager.SetData(logger.GetSerializedObject());
                AnalyticsManager.Save("import_analytics.json");

                if (!testMode)
                {
                    S3helper.UploadLog(logger.GetSerializedObject(), processId);
                    S3helper.UploadAnalytics(processId, "revitImport");
                }
                isDone = true;
                if (IsImportAborted())
                {
                    Application.Instance.EmitAbortEvent();
                }
                else
                {
                }
            }
        }

        public static bool IsImportAborted()
        {
            if (Application.Instance.AbortExportFlag)
            {
                S3helper.abortFlag = true;
                return true;
            }

            S3helper.abortFlag = false;
            return false;
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