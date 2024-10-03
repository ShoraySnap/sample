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

        public UpdateAvailableViewModel(bool retry, NavigationService updateNowNavigationService, NavigationService skipUpdateNavigationService, bool isShutdownUpdate) 
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
                Message = $"Snaptrude Manager v{MainWindowViewModel.Instance.UpdateVersion} is available.\nPlease update to get the latest features.";
            }

            TransformCommand transformMainWindowViewModelCommand = new TransformCommand(
                new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                {
                    ((MainWindowViewModel)viewmodel).WhiteBackground = false;
                    return viewmodel;
                }));
            transformMainWindowViewModelCommand.Execute(new object());

            if (isShutdownUpdate)
                SkipCommand = new RelayCommand((o) => App.Current.Shutdown());
            if (!isShutdownUpdate)
                SkipCommand = new NavigateCommand(skipUpdateNavigationService);
            UpdateCommand = new NavigateCommand(updateNowNavigationService);
        }
    }
}
