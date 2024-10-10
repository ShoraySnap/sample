using SnaptrudeManagerUI.API;
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

        public UpdateAvailableViewModel(bool retry, NavigationService updateNowNavigationService, NavigationService skipHomeNavigationService, NavigationService skipLoginNavigationService) 
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

            TransformCommand transformMainWindowViewModelCommand = new TransformCommand(
                new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                {
                    ((MainWindowViewModel)viewmodel).WhiteBackground = false;
                    return viewmodel;
                }));
            transformMainWindowViewModelCommand.Execute(new object());
            if (MainWindowViewModel.Instance.IsUserLoggedIn)
                SkipCommand = new NavigateCommand(skipHomeNavigationService);
            else
                SkipCommand = new NavigateCommand(skipLoginNavigationService);
            UpdateCommand = new NavigateCommand(updateNowNavigationService);
            App.OnCriticalUpdateAvailable += SetCriticalUpdateMessage;
        }

        private void SetCriticalUpdateMessage()
        {
            Message = $"A critical update has been identified, please update to{UpdateVersion}.\nFailing to update may cause errors during Import and Export.";
        }
    }
}
