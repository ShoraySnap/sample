using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using NLog;
using SnaptrudeManagerAddin.Logging;
using SnaptrudeManagerAddin.ViewModels;
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

        public MainWindow MainWindow;

        //Seperate thread to run the UI
        public Thread uiThread;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Result OnStartup(UIControlledApplication application)
        {
            LogsConfig.Initialize();
            MainWindow = null;
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
            MainWindowViewModel.Instance.IsActiveView3D = is3DView;
        }

        private ImageSource GetEmbeddedImage(System.Reflection.Assembly assemb, string imageName)
        {
            System.IO.Stream file = assemb.GetManifestResourceStream(imageName);
            PngBitmapDecoder bd = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return bd.Frames[0];
        }

        public void ShowUISeperateThread(UIApplication uiapp)
        {
            // If we do not have a thread started or has been terminated start a new one
            if (!(uiThread is null) && uiThread.IsAlive)
            {
                MainWindow.Show();
                return;
            }

            uiThread = new Thread(() =>
            {
                logger.Info($"Thread ID Start: {Dispatcher.CurrentDispatcher.Thread.ManagedThreadId}");
                //Set the sync context
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

                MainWindow = new MainWindow();
                MainWindow.Show();

                //Shut down the dispatcher while closing
                MainWindow.Closed += MainWindow_Closed;
                MainWindow.Show();

                if(Dispatcher.CurrentDispatcher.CheckAccess())
                    Dispatcher.Run();
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            logger.Info($"Thread ID Shutdown: {Dispatcher.CurrentDispatcher.Thread.ManagedThreadId}");
            Dispatcher.ExitAllFrames();
            MainWindow.Dispatcher.InvokeShutdown();
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
