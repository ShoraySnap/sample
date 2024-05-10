using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //WPFTODO: CHECKFORUPDATES
            var currentVersion = "2.2";
            var updateVersion = "2.2";

            NavigationStore navigationStore = NavigationStore.Instance;
            MainWindowViewModel.Instance.ConfigMainWindowViewModel(navigationStore, currentVersion, updateVersion, true);
            if (currentVersion != updateVersion)
                navigationStore.CurrentViewModel = ViewModelCreator.CreateUpdateAvailableViewModel();
            else
                navigationStore.CurrentViewModel = ViewModelCreator.CreateLoginViewModel();
        }
    }

}
