﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Newtonsoft.Json.Linq;
using NLog;
using SnaptrudeManagerAddin.Launcher;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrudeCommon.DataTransfer;
using TrudeCommon.Events;
using TrudeCommon.Logging;
using TrudeImporter;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        public static Application Instance;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static DataTransferManager TransferManager;

        public Result OnStartup(UIControlledApplication application)
        {
            LogsConfig.Initialize("ManagerAddin");
            logger.Info("Startup Snaptrude Manager Addin...");
            Instance = this;

            application.ViewActivated += OnViewActivated;
            Assembly myAssembly = typeof(Application).Assembly;
            string assemblyPath = myAssembly.Location;

            string tabName = "Snaptrude";
            string panelName = "Snaptrude";

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
            application.Idling += OnRevitIdling;


            return Result.Succeeded;
        }

        private void OnRevitIdling(object sender, IdlingEventArgs e)
        {
            ProcessEventQueue();
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            logger.Info("Shutting down Snaptrude Manager Addin...");
            application.ViewActivated -= OnViewActivated;
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_CLOSED);
            TrudeEventSystem.Instance.Shutdown();
            LogsConfig.Shutdown();
            return Result.Succeeded;
        }


        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            View currentView = e.CurrentActiveView;
            UpdateButtonState(currentView is View3D);
        }

        public static void UpdateButtonState(bool is3DView)
        {
            if (is3DView)
                TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D);
            else
                TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER);
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

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_IMPORT, false);
            TrudeEventSystem.Instance.AddThreadEventHandler(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_IMPORT, () =>
            {
                TrudeImporterMain.Abort = true; // TODO: Mutexed this flag, but don't know if better structure is required, but it WORKS
            });

            TrudeEventSystem.Instance.Start();
        }

        private void ProcessEventQueue()
        {
            ConcurrentQueue<TRUDE_EVENT> eventQueue = TrudeEventSystem.Instance.GetQueue();
            while(!eventQueue.IsEmpty)
            {
                if(eventQueue.TryDequeue(out TRUDE_EVENT eventType))
                {
                    logger.Info("Processing event from main queue: {0}", TrudeEventUtils.GetEventName(eventType));
                    switch(eventType)
                    {
                        case TRUDE_EVENT.MANAGER_UI_OPEN:
                            break;
                        case TRUDE_EVENT.MANAGER_UI_CLOSE:
                            break;
                        case TRUDE_EVENT.MANAGER_UI_MAIN_WINDOW_RMOUSE:
                            break;
                        case TRUDE_EVENT.DATA_FROM_MANAGER_UI:
                            {
                                logger.Info("Got data incoming from ui!");
                                string data = TransferManager.ReadString(TRUDE_EVENT.DATA_FROM_MANAGER_UI);
                                logger.Info("data : \"{0}\"", data);
                            }
                            break;
                        case TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT:
                            {
                                string[] data = TransferManager.ReadString(TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT).Split(';');
                                logger.Info("Got data from UI: {0}", data);

                                // START THE IMPORT
                                JObject trudeData = JObject.Parse(File.ReadAllText(data[0]));
                                GlobalVariables.TrudeFileName = Path.GetFileName(data[0]);
                                GlobalVariables.materials = trudeData["materials"] as JArray;
                                GlobalVariables.multiMaterials = trudeData["multiMaterials"] as JArray;
                                GlobalVariables.ImportLabels = data[1] == "True";

                                Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer()
                                {
                                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                                    DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                                };
                                serializer.Converters.Add(new XyzConverter());
                                GlobalVariables.TrudeProperties = trudeData.ToObject<TrudeProperties>(serializer);

                                TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_START);
                                ExternalEvent evt = ExternalEvent.Create(new ImportToRevitEEH());
                                evt.Raise();
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
    }
}
