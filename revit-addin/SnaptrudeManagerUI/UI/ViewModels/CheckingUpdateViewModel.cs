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
using SnaptrudeManagerUI.UI.Helpers;
using System.Windows;

namespace SnaptrudeManagerUI.ViewModels
{
    public class CheckingUpdateViewModel : ViewModelBase
    {
        public ICommand LoginCommand { get; }
        public ICommand UpdateAvailableCommand { get; }
        public CheckingUpdateViewModel(NavigationService loginNavigationService, NavigationService updateAvailableNavigationService)
        {
            MainWindowViewModel.Instance.WhiteBackground = false;
            LoginCommand = new NavigateCommand(loginNavigationService);
            UpdateAvailableCommand = new NavigateCommand(updateAvailableNavigationService);
            App.OnUpdateAvailable += NavigateToUpdateView;
            App.OnLatestVersion += NavigateToLoginView;
        }

        private void NavigateToLoginView()
        {
            LoginCommand.Execute(null);
        }

        private void NavigateToUpdateView()
        {
            UpdateAvailableCommand.Execute(null);
        }
    }
}
