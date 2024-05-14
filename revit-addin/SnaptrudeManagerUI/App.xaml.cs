using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;
using NLog;
using SnaptrudeManagerUI.IPC;
using TrudeCommon.Logging;
using TrudeCommon.Events;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LogsConfig.Initialize("ManagerUI");

            //WPFTODO: CHECKFORUPDATES
            var currentVersion = "2.1";
            var updateVersion = "2.1";


            NavigationStore navigationStore = NavigationStore.Instance;
            MainWindowViewModel.Instance.ConfigMainWindowViewModel(navigationStore, currentVersion, updateVersion, true);
            if (currentVersion != updateVersion)
                navigationStore.CurrentViewModel = ViewModelCreator.CreateUpdateAvailableViewModel();
            else
                navigationStore.CurrentViewModel = ViewModelCreator.CreateLoginViewModel();
            logger.Info("UI Initialized!");

            TrudeEventSystem.Instance.Init();
            TrudeEventSystem.Instance.AddEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D);
            TrudeEventSystem.Instance.AddEventHandler(TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D, () =>
            {
                LogManager.GetCurrentClassLogger().Info("View changed to 3D in Revit.");
                Application.Current.Dispatcher.Invoke(() => {
                    MainWindowViewModel.Instance.IsActiveView3D = true;
                });
            });

            TrudeEventSystem.Instance.AddEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER);
            TrudeEventSystem.Instance.AddEventHandler(TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER, () =>
            {
                LogManager.GetCurrentClassLogger().Info("View changed to not 3D in Revit.");
                Application.Current.Dispatcher.Invoke(() => {
                    MainWindowViewModel.Instance.IsActiveView3D = false;
                });
            });

            TrudeEventSystem.Instance.Start();


            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_OPEN);
        } 


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            logger.Info("UI Shutdown!");
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_CLOSE);
            LogsConfig.Shutdown();
        }
    }

}
