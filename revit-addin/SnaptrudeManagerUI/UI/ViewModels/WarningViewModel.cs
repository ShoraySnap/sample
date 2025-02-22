﻿using SnaptrudeManagerUI.Commands;
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
        SelectFolderIssue,
        TokenExpired,
        EnterProjectUrlIssue,
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
                case WarningId.StartupInternetConnectionIssue:
                    ShowSecondaryButton = false;
                    Title = "Connection lost";
                    Message = $"Please check your internet connection and try again.";
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
                    Title = "Upload to Snaptrude failed";
                    Message = $"{errorMessage}";
                    PrimaryButtonText = "Try again";
                    break;
                case WarningId.SelectFolderIssue:
                    ShowSecondaryButton = true;
                    SecondaryButtonText = "Go back";
                    Title = "Unable to load workspace";
                    Message = $"{errorMessage}";
                    PrimaryButtonText = "Try again";
                    break;
                case WarningId.EnterProjectUrlIssue:
                    ShowSecondaryButton = true;
                    SecondaryButtonText = "Go back";
                    Title = "Unable to load project";
                    Message = $"{errorMessage}";
                    PrimaryButtonText = "Try again";
                    break;
                case WarningId.TokenExpired:
                    ShowSecondaryButton = false;
                    Title = "Invalid token";
                    Message = $"{errorMessage}";
                    PrimaryButtonText = "Login";
                    break;
                default:
                    break;
            }
        }
    }
}
