using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SnaptrudeManagerAddin.Launcher;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrudeCommon.DataTransfer;
using TrudeCommon.Events;
using TrudeCommon.Logging;
using TrudeCommon.Utils;
using TrudeImporter;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {

        public static Application Instance;
        public static UIControlledApplication UIControlledApplication;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static DataTransferManager TransferManager;
        public bool IsAnyDocumentOpened;
        public bool _abortExport = false;
        public bool IsViewActivatedSubscribed = false;
        private object mutex = new object();
        string version = "REVIT";
        public bool AbortExportFlag
        {
            get
            {
                return _abortExport;
            }
            set
            {
                lock(mutex)
                {
                    _abortExport = value;
                }
            }
        }

        public Result OnStartup(UIControlledApplication application)
        {
            LogsConfig.Initialize("ManagerAddin_" + Process.GetCurrentProcess().Id);
            logger.Info("Startup Snaptrude Manager Addin...");
            Instance = this;
            UIControlledApplication = application;
            version = application.ControlledApplication.VersionName;

            Assembly myAssembly = typeof(Application).Assembly;
            string assemblyPath = myAssembly.Location;

            string tabName = "Snaptrude";
            string panelName = "Snaptrude";
            FileUtils.Initialize();

            // Create Ribbon Tab
            application.CreateRibbonTab(tabName);

            // Create Ribbon Panel to host the button
            RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);

            // Create the push button
            string className = TypeDescriptor.GetClassName(typeof(LauncherCommand));
            string commandName = typeof(LauncherCommand).FullName;
            PushButtonData buttonData = new PushButtonData(commandName, "Snaptrude Manager", assemblyPath, className);
            PushButton button = panel.AddItem(buttonData) as PushButton;

            BitmapIcons bitmapIcons = new BitmapIcons(Assembly.GetExecutingAssembly(), "SnaptrudeManagerAddin.Icons.logo256.png", application);
            button.Image = bitmapIcons.MediumBitmap();
            button.LargeImage = bitmapIcons.LargeBitmap();
            button.ToolTip = "Export the model to Snaptrude";

            SetupDataChannels();
            SetupEvents();
            application.ControlledApplication.DocumentClosing += DocumentClosing;

            return Result.Succeeded;
        }

        public void OnProgressChanged(object sender, Autodesk.Revit.DB.Events.ProgressChangedEventArgs e)
        {
            if (e.Cancellable && AbortExportFlag)
            {
                e.Cancel();
            }
        }

        public void OnRevitIdling(object sender, IdlingEventArgs e)
        {
            Task.Delay(100);
            ProcessEventQueue();
            e.SetRaiseWithoutDelay();
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            logger.Info("Shutting down Snaptrude Manager Addin...");
            application.ViewActivated -= OnViewActivated;
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_CLOSED);
            TrudeEventSystem.Instance.Shutdown();
            LogsConfig.Shutdown();
            LaunchProcess.StartProcess(new string[] { }, true);
            return Result.Succeeded;
        }


        public void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            IsViewActivatedSubscribed = true;
            IsAnyDocumentOpened = true;
            View currentView = e.CurrentActiveView;
            UpdateButtonState(currentView is View3D);
            UpdateNameAndFiletype(e.Document.Title, e.Document.IsFamilyDocument ? "rfa" : "rvt");
        }

        private void DocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (!e.Document.Title.Contains("_custom_"))
            {
            IsAnyDocumentOpened = false;
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_DOCUMENT_CLOSED);
            }
            // SHOULD WE CLOSE THE MANAGER UI IF NO DOCUMENT IS OPEN?
        }

        public static void UpdateButtonState(bool is3DView)
        {
            if (is3DView)
                TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D);
            else
                TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER);

        }

        public static void UpdateNameAndFiletype(string projectName, string fileType)
        {

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "projectName", projectName.Replace(".rfa","") },
                { "fileType", fileType }
            };
            string serializedData = JsonConvert.SerializeObject(data);
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_PROJECTNAME_AND_FILETYPE, serializedData, TransferManager);
        }

        private ImageSource GetEmbeddedImage(System.Reflection.Assembly assemb, string imageName)
        {
            System.IO.Stream file = assemb.GetManifestResourceStream(imageName);
            PngBitmapDecoder bd = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return bd.Frames[0];
        }

        private void SetupDataChannels()
        {
            TransferManager = new DataTransferManager();
        }

        private void SetupEvents()
        {
            TrudeEventSystem.Instance.Init();

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_OPEN);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_CLOSE);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_MAIN_WINDOW_RMOUSE);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.DATA_FROM_MANAGER_UI);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT, false);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_IMPORT, false);
            TrudeEventSystem.Instance.AddThreadEventHandler(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_IMPORT, () =>
            {
                // IF Handshake is valid
                TrudeImporter.TrudeImporterMain.Abort = true; // NOTE: Mutexed this flag, but don't know if better structure is required, but it WORKS
            });
            TrudeEventSystem.Instance.AddThreadEventHandler(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT, () =>
            {
                Application.Instance.AbortExportFlag = true;
            });

            TrudeEventSystem.Instance.Start();
        }

        private void ProcessEventQueue()
        {
            ConcurrentQueue<TRUDE_EVENT> eventQueue = TrudeEventSystem.Instance.GetQueue();
            while (!eventQueue.IsEmpty)
            {
                if (eventQueue.TryDequeue(out TRUDE_EVENT eventType))
                {
                    logger.Info("Processing event from main queue: {0}", TrudeEventUtils.GetEventName(eventType));
                    switch (eventType)
                    {
                        case TRUDE_EVENT.MANAGER_UI_OPEN:
                            break;
                        case TRUDE_EVENT.MANAGER_UI_CLOSE:
                            UIControlledApplication.ViewActivated -= OnViewActivated;
                            UIControlledApplication.Idling -= OnRevitIdling;
                            Application.Instance.IsViewActivatedSubscribed = false;
                            break;
                        case TRUDE_EVENT.MANAGER_UI_MAIN_WINDOW_RMOUSE:
                            break;
                        case TRUDE_EVENT.DATA_FROM_MANAGER_UI:
                            {
                                logger.Debug("Got data incoming from ui!");
                                string data = TransferManager.ReadString(TRUDE_EVENT.DATA_FROM_MANAGER_UI);
                                logger.Debug("data : \"{0}\"", data);
                            }
                            break;
                        case TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT:
                            {
                                string[] data = TransferManager.ReadString(TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT).Split(';');
                                logger.Info("Got Req to import from UI: {0}", data);

                                // START THE IMPORT
                                JObject trudeData = JObject.Parse(File.ReadAllText(data[0]));
                                TrudeImporter.GlobalVariables.TrudeFileName = Path.GetFileName(data[0]);
                                TrudeImporter.GlobalVariables.materials = trudeData["materials"] as JArray;
                                TrudeImporter.GlobalVariables.multiMaterials = trudeData["multiMaterials"] as JArray;
                                TrudeImporter.GlobalVariables.ImportLabels = data[1] == "True";

                                Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer()
                                {
                                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                                    DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                                };
                                serializer.Converters.Add(new TrudeImporter.XyzConverter());
                                TrudeImporter.GlobalVariables.TrudeProperties = trudeData.ToObject<TrudeImporter.TrudeProperties>(serializer);

                                ExternalEvent evt = ExternalEvent.Create(new ImportToRevitEEH());
                                evt.Raise();
                            }
                            break;
                        case TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE:
                            {
                                string[] data = TransferManager.ReadString(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE).Split(';');
                                logger.Info("Got Request to export from UI: {0}", data);

                                logger.Info("Export to snaptrude start");
                                ExternalEvent evt = ExternalEvent.Create(new TrudeSerializer.ExportToSnaptrudeEEH());
                                evt.Raise();
                            }
                            break;
                        case TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT:
                            {
                                logger.Info("Abort export");
                                Application.Instance.AbortExportFlag = true;
                            }
                            break;
                    }
                }
            }
        }

        internal void UpdateProgressForImport(int progress, string message)
        {
            string data = progress + ";" + message;
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_PROGRESS_UPDATE, data, TransferManager);
        }

        internal void UpdateProgressForExport(int progress, string message)
        {
            string data = progress + ";" + message;
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_PROGRESS_UPDATE, data, TransferManager);
        }

        internal void EmitRequestUploads(string processId)
        {
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_REQUEST_UPLOAD_TO_SNAPTRUDE, processId, TransferManager);
        }

        internal void EmitAbortEvent()
        {
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_ABORTED);
        }

        internal void FinishExportSuccess(string floorkey)
        {
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_SUCCESS, floorkey, TransferManager);
        }

        internal void ExportFailure()
        {
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_FAILED);
        }

        internal string GetVersion()
        {
            return version;
        }
    }
}
