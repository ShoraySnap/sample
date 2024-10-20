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
        public ICommand LatestVersionCommand { get; set; }
        public ICommand UpdateAvailableCommand { get; }
        private string message;

        public string Message
        {
            get { return message; }
            set { message = value; OnPropertyChanged(nameof(Message)); }
        }

        public CheckingUpdateViewModel(NavigationService updateAvailableNavigationService)
        {
            MainWindowViewModel.Instance.WhiteBackground = false;
            UpdateAvailableCommand = new NavigateCommand(updateAvailableNavigationService);
            App.OnUpdateAvailable += NavigateToUpdateView;
            App.OnLatestVersion += NavigateToNextView;
            Init();
            MainWindowViewModel.Instance.IsLoaderVisible = false;
        }

        public void InvokeUpdateActions()
        {
            App.OnUpdateAvailable.Invoke();
            if (App.Updater.CriticalUpdateFound)
            {
                App.OnCriticalUpdateAvailable.Invoke();
            }
        }

        public async void Init()
        {
            Task checkCredentialsTask = CheckCredentials();
            Task<bool> checkUpdatesTask = CheckForUpdates();
            bool haveUpdates = await checkUpdatesTask;
            await checkCredentialsTask;

            if (haveUpdates)
            {
                InvokeUpdateActions();
            }
            else
            {
                App.OnLatestVersion?.Invoke();
            }
        }

        public async Task CheckCredentials()
        {
            Message = "Verifying your credentials...";
            bool isUserLoggedIn = await SnaptrudeService.CheckIfUserLoggedInAsync();
            if (isUserLoggedIn)
            {
                MainWindowViewModel.Instance.IsUserLoggedIn = true;
                LatestVersionCommand = new NavigateCommand(new NavigationService(NavigationStore.Instance, ViewModelCreator.CreateHomeViewModel));
            }
            else
            {
                LatestVersionCommand = new NavigateCommand(new NavigationService(NavigationStore.Instance, ViewModelCreator.CreateLoginViewModel));
            }
        }
        public async Task<bool> CheckForUpdates()
        {
            Message = "Checking for updates...";
            return await App.Updater.IsUpdateAvailable();
        }

        private void NavigateToNextView()
        {
            LatestVersionCommand.Execute(null);
        }

        private void NavigateToUpdateView()
        {
            UpdateAvailableCommand.Execute(null);
        }
    }
}
