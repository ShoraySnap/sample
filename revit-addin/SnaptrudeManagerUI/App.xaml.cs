using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;
using SnaptrudeManagerUI.API;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //WPFTODO: CHECKFORUPDATES
            var currentVersion = "2.2";
            var updateVersion = "2.3";

            NavigationStore navigationStore = NavigationStore.Instance;
            MainWindowViewModel.Instance.ConfigMainWindowViewModel(navigationStore, currentVersion, updateVersion, true);
            if (currentVersion != updateVersion)
                navigationStore.CurrentViewModel = ViewModelCreator.CreateUpdateAvailableViewModel();
            else
                navigationStore.CurrentViewModel = ViewModelCreator.CreateLoginViewModel();
            SnaptrudeService snaptrudeService = new SnaptrudeService();

            bool isLoggedIn = await snaptrudeService.CheckIfUserLoggedInAsync();
            Console.WriteLine($"Is user logged in: {isLoggedIn}");

            var workspaces = await snaptrudeService.GetUserWorkspacesAsync();
            foreach (var workspace in workspaces)
            {
                Console.WriteLine($"Workspace ID: {workspace["id"]}, Name: {workspace["name"]}");
            }

            bool isPaidUser = await snaptrudeService.IsPaidUserAccountAsync();
            Console.WriteLine($"Is paid user: {isPaidUser}");
        }
    }

}
