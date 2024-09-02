using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerUI.ViewModels
{
    public enum WarningId
    {
        InternetConnectionIssue,
        RevitClosed,
        StartupInternetConnectionIssue,
        APIBlocked,
        UploadIssue,
    }

    public class WarningViewModel : ViewModelBase
    {
        private bool showSecondaryButton;

        public bool ShowSecondaryButton
        {
            get { return showSecondaryButton; }
            set { showSecondaryButton = value; OnPropertyChanged("ShowSecondaryButton"); }
        }

        private string message;

        public string Message
        {
            get { return message; }
            set { message = value; OnPropertyChanged("Message"); }
        }

        private string primaryButtonMessage;

        public string PrimaryButtonText
        {
            get { return primaryButtonMessage; }
            set { primaryButtonMessage = value; OnPropertyChanged("PrimaryButtonMessage"); }
        }

        private string secondaryButtonMessage;

        public string SecondaryButtonText
        {
            get { return secondaryButtonMessage; }
            set { secondaryButtonMessage = value; OnPropertyChanged("SecondaryButtonMessage"); }
        }

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; OnPropertyChanged("Title"); }
        }

        public ICommand SecondaryCommand { get; }
        public ICommand PrimaryCommand { get; }
        public WarningViewModel(WarningId warningId, NavigationService primaryNavigationService = null, NavigationService secondaryNavigationService = null, string errorMessage = null)
        {
            MainWindowViewModel.Instance.WhiteBackground = true;
            PrimaryCommand = new NavigateCommand(primaryNavigationService);
            SecondaryCommand = new NavigateCommand(secondaryNavigationService);
            switch (warningId)
            {
                case WarningId.RevitClosed:
                    ShowSecondaryButton = false;
                    Title = "Revit closed unexpectedly";
                    Message = "Relaunch Revit and open Snaptrude Manager.";
                    PrimaryButtonText = "Close";
                    PrimaryCommand = new RelayCommand(new Action<object>((o) =>
                    {
                        App.Current.Shutdown();
                    }));
                    break;
                case WarningId.InternetConnectionIssue:
                    ShowSecondaryButton = true;
                    Title = "Connection lost";
                    Message = "A network error occurred while connecting to Snaptrude. Please check your internet connection and try again.";
                    PrimaryButtonText = "Try again";
                    SecondaryButtonText = "Go back";
                    break;
                case WarningId.StartupInternetConnectionIssue:
                    ShowSecondaryButton = false;
                    Title = "Connection lost";
                    Message = $"A network error occurred while connecting to Snaptrude. Please check your internet connection and try again.\n{errorMessage}";
                    PrimaryButtonText = "Close";
                    PrimaryCommand = new RelayCommand(new Action<object>((o) =>
                    {
                        App.Current.Shutdown();
                    }));
                    break;
                case WarningId.APIBlocked:
                    ShowSecondaryButton = false;
                    Title = "Snpatrude API is blocked";
                    Message = $"Please check your firewall settings.\n{errorMessage}";
                    PrimaryButtonText = "Close";
                    PrimaryCommand = new RelayCommand(new Action<object>((o) =>
                    {
                        App.Current.Shutdown();
                    }));
                    break;
                case WarningId.UploadIssue:
                    ShowSecondaryButton = true;
                    SecondaryButtonText = "Go back";
                    Title = "Upload failed";
                    Message = $"{errorMessage}";
                    PrimaryButtonText = "Try again";
                    break;
                default:
                    break;
            }
        }
    }
}
