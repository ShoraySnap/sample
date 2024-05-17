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
                while (!File.Exists("text.txt"))
                {
                    if (currentTry > limitTries)
                    {
                        break;
                    }
                    else if (currentTry < limitTries)
                    {

                    }

                    await Task.Delay(1000);

                    currentTry++;
                }

                //userCredentials = GetSavedUserCredentials();
                LoginCommand.Execute(parameter);
            });
        }
    }
}
