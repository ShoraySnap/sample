using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;
using SnaptrudeManagerUI.API;
using Microsoft.Win32;
using System.Reflection;
using Newtonsoft.Json;
using SnaptrudeManagerUI.Models;
using System.Web;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void RegisterProtocol()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + Constants.SNAPTRUDE_PROTOCOL))
            {
                key.SetValue(string.Empty, "URL:Snaptrude Manager");
                key.SetValue("URL Protocol", string.Empty);

                using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    string location = Assembly.GetExecutingAssembly().Location;
                    location.Replace(".dll", ".exe");
                    commandKey.SetValue(string.Empty, $"\"{location}\" \"%1\"");
                }
            }
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                var queryParams = HttpUtility.ParseQueryString(new Uri(e.Args[0]).Query);
                var dataEncoded = queryParams["data"];
                var data = HttpUtility.UrlDecode(dataEncoded);
                UserCredentialsModel userCredentialsModel = JsonConvert.DeserializeObject<UserCredentialsModel>(data);
            }

            base.OnStartup(e);

            //WPFTODO: CHECKFORUPDATES
            var currentVersion = "2.2";
            var updateVersion = "2.3";

            RegisterProtocol();

            NavigationStore navigationStore = NavigationStore.Instance;
            MainWindowViewModel.Instance.ConfigMainWindowViewModel(navigationStore, currentVersion, updateVersion, true);
            if (currentVersion != updateVersion)
                navigationStore.CurrentViewModel = ViewModelCreator.CreateUpdateAvailableViewModel();
            else
                navigationStore.CurrentViewModel = ViewModelCreator.CreateLoginViewModel();
            // SnaptrudeService snaptrudeService = new SnaptrudeService();

            bool isLoggedIn = await SnaptrudeService.CheckIfUserLoggedInAsync();
            Console.WriteLine($"Is user logged in: {isLoggedIn}");

            var workspaces = await SnaptrudeService.GetUserWorkspacesAsync();
            foreach (var workspace in workspaces)
            {
                System.Diagnostics.Debug.WriteLine($"Workspace ID: {workspace["id"]}, Name: {workspace["name"]}");
            }

            bool isPaidUser = await SnaptrudeService.IsPaidUserAccountAsync();
            Console.WriteLine($"Is paid user: {isPaidUser}");
        }
    }

}
