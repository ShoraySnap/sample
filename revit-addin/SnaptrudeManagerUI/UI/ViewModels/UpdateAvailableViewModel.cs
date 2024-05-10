using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerUI.ViewModels
{
    public class UpdateAvailableViewModel : ViewModelBase
    {
        public ICommand SkipCommand { get; }
        public ICommand UpdateCommand { get; }

        private string message;

        public string Message
        {
            get { return message; }
            set { message = value; OnPropertyChanged("Message"); }
        }

        private string updateVersion;

        public string UpdateVersion
        {
            get { return updateVersion; }
            set { updateVersion = value; OnPropertyChanged("UpdateVersion"); }
        }

        private string updateButtonText;

        public string UpdateButtonText
        {
            get { return updateButtonText; }
            set { updateButtonText = value; OnPropertyChanged("UpdateButtonText"); }
        }

        public UpdateAvailableViewModel(bool retry, NavigationService updateNowNavigationService, NavigationService skipUpdateNavigationService) 
        {
            if (retry)
            {
                UpdateButtonText = "Retry";
                UpdateVersion = "";
                Message = $"Something went wrong, please try again.\r\nContact us if the issue persists.";
            }
            else
            {
                UpdateButtonText = "Update";
                UpdateVersion = $" v{MainWindowViewModel.Instance.UpdateVersion}";
                Message = $"Version {MainWindowViewModel.Instance.UpdateVersion} is ready to install. Update Snaptrude Manager to continue collaborating seamlessly with Snaptrude.";
            }

            MainWindowViewModel.Instance.WhiteBackground = false;
            SkipCommand = new NavigateCommand(skipUpdateNavigationService);
            UpdateCommand = new NavigateCommand(updateNowNavigationService);
        }
    }
}
