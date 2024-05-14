using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using NLog;
using SnaptrudeManagerAddin.Launcher;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrudeCommon.Events;
using TrudeCommon.Logging;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        public static Application Instance;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Result OnStartup(UIControlledApplication application)
        {
            LogsConfig.Initialize("ManagerAddin");
            Instance = this;

            application.ViewActivated += OnViewActivated;
            Assembly myAssembly = typeof(Application).Assembly;
            string assemblyPath = myAssembly.Location;
            application.Idling += OnRevitIdling;

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

            logger.Info("<<<STARTUP>>>");
            TrudeEventSystem.Instance.Init();
            TrudeEventSystem.Instance.AddEvent(TRUDE_EVENT.MANAGER_UI_OPEN);
            TrudeEventSystem.Instance.AddEvent(TRUDE_EVENT.MANAGER_UI_CLOSE);
            TrudeEventSystem.Instance.AddEvent(TRUDE_EVENT.MANAGER_UI_MAIN_WINDOW_RMOUSE);
            TrudeEventSystem.Instance.Start();

            return Result.Succeeded;
        }

        private void OnRevitIdling(object sender, IdlingEventArgs e)
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
                    }
                }
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            SnaptrudeManagerAddin.Launcher.LaunchProcess.CleanUp();
            application.ViewActivated -= OnViewActivated;
            TrudeEventSystem.Instance.Shutdown();
            logger.Info("<<<SHUTDOWN>>>");
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

    }
}
