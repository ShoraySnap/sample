using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using NLog;
using SnaptrudeManagerAddin.Logging;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        public static Application Instance;

        //Seperate thread to run the UI
        public Thread uiThread;
        public ManualResetEventSlim waitHandle;
        DispatcherProcessingDisabled _prevDisable;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Result OnStartup(UIControlledApplication application)
        {
            LogsConfig.Initialize();
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
            string className = TypeDescriptor.GetClassName(typeof(SnaptrudeManagerAddin.SnaptrudeManager));
            PushButtonData buttonData = new PushButtonData("Export", "Snaptrude Manager", assemblyPath, className);
            PushButton button = panel.AddItem(buttonData) as PushButton;

            BitmapIcons bitmapIcons = new BitmapIcons(Assembly.GetExecutingAssembly(), "SnaptrudeManagerAddin.Icons.logo256.png", application);
            button.Image = bitmapIcons.MediumBitmap();
            button.LargeImage = bitmapIcons.LargeBitmap();
            button.ToolTip = "Export the model to Snaptrude";

            logger.Info("<<<STARTUP>>>");

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            logger.Info("<<<SHUTDOWN>>>");
            LogsConfig.Shutdown();
            application.ViewActivated -= OnViewActivated;
            return Result.Succeeded;
        }

        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            View currentView = e.CurrentActiveView;
            UpdateButtonState(currentView is View3D);
        }

        private void UpdateButtonState(bool is3DView)
        {
            //MainWindowViewModel.Instance.IsActiveView3D = is3DView;
        }

        private ImageSource GetEmbeddedImage(System.Reflection.Assembly assemb, string imageName)
        {
            System.IO.Stream file = assemb.GetManifestResourceStream(imageName);
            PngBitmapDecoder bd = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return bd.Frames[0];
        }

    }
}
