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
        SelectFolderError,
        RevitClosed
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
        public WarningViewModel(WarningId warningId, NavigationService primaryNavigationService = null, NavigationService secondaryNavigationService = null)
        {
            MainWindowViewModel.Instance.WhiteBackground = true;
            PrimaryCommand = new NavigateCommand(primaryNavigationService);
            SecondaryCommand = new NavigateCommand(secondaryNavigationService);
            switch (warningId)
            {
                case WarningId.SelectFolderError:
                    ShowSecondaryButton = true;
                    Title = "Something went wrong";
                    Message = "Try again to refresh and load the workspaces, or go back to the previous screen.";
                    PrimaryButtonText = "Try again";
                    SecondaryButtonText = "Go back";
                    break;
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
                default:
                    break;
            }
        }
    }
}
