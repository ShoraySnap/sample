using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;
using SnaptrudeManagerUI.Logging;
using NLog;
using SnaptrudeManagerUI.IPC;

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
            LogsConfig.Initialize();

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

            IPCManager.Init();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            LogsConfig.Shutdown();
        }
    }

}
