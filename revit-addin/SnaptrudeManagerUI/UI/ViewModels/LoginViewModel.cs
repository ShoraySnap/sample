using SnaptrudeManagerUI.API;
using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Models;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;

namespace SnaptrudeManagerUI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        public ICommand LoginCommand { get; }
        public ICommand AuthCommand { get; private set; }
        public LoginViewModel(NavigationService homeNavigationService, NavigationService updateAvailableNavigationService)
        {
            MainWindowViewModel.Instance.WhiteBackground = false;
            LoginCommand = new NavigateCommand(homeNavigationService);
            AuthCommand = new RelayCommand(Login);
            App.OnSuccessfullLogin += OnSuccessfullLogin;
        }

        private void OnSuccessfullLogin()
        {
            LoginCommand.Execute(new object());
        }

        private async void Login(object parameter)
        {
            UserCredentialsModel userCredentials = null;

            await Task.Run(async () =>
            {
                var ps = new ProcessStartInfo(Urls.Get("snaptrudeReactUrl") + "/login?externalAuth=revit")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);

                int currentTry = 0;
                int limitTries = 60;
                while (currentTry < limitTries)
                {
                    if (File.Exists(Constants.AUTH_FILE))
                        break;

                    await Task.Delay(1000);
                    currentTry++;
                }
                if (File.Exists(Constants.AUTH_FILE))
                {
                    string data = File.ReadAllText(Constants.AUTH_FILE);
                    Dictionary<string, string> userCredentialsModel = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                    Store.SetAllAndSave(userCredentialsModel);
                    File.Delete(Constants.AUTH_FILE);
                    LoginCommand.Execute(parameter);
                }
            });
        }
    }
}
