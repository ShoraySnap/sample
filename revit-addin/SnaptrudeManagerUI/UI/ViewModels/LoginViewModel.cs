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
using TrudeCommon.Events;
using System.Runtime.InteropServices;

namespace SnaptrudeManagerUI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        public ICommand LoginCommand { get; }
        public ICommand AuthCommand { get; private set; }

        private string message { get; set; }
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        private bool showButton { get; set; }
        public bool ShowButton
        {
            get { return showButton; }
            set
            {
                showButton = value;
                OnPropertyChanged("ShowButton");
            }
        }

        private bool showLoader { get; set; }
        public bool ShowLoader
        {
            get { return showLoader; }
            set
            {
                showLoader = value;
                OnPropertyChanged("ShowLoader");
            }
        }

        public LoginViewModel(NavigationService homeNavigationService)
        {
            MainWindowViewModel.Instance.WhiteBackground = false;
            LoginCommand = new NavigateCommand(homeNavigationService);
            AuthCommand = new RelayCommand(ExecuteAuth);
            App.OnSuccessfullLogin += OnSuccessfullLogin;
            App.OnFailedLogin += OnFailedLogin;
            Message = "Login to your Snaptrude account and start importing/exporting Revit files";
            ShowButton = true;
            ShowLoader = false;
        }

        private void OnSuccessfullLogin()
        {
            if (App.RevitProcess != null)
            {
                SetForegroundWindow(App.RevitProcess.MainWindowHandle);
            }
            LoginCommand.Execute(new object());
        }

        private void ExecuteAuth(object parameter)
        {
            ShowButton = false;
            ShowLoader = true;
            Message = "Redirecting to browser. Please ensure your pop-up blocker is disabled.";
            LoginHelper.Login(parameter);
        }
        private void OnFailedLogin()
        {
            ShowButton = true;
            ShowLoader = false;
            Message = "Login attempt failed. Please try again.";
        }

    }
}
