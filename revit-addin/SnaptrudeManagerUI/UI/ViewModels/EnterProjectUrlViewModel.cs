using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.ComponentModel;

namespace SnaptrudeManagerUI.ViewModels
{
    public enum URLValidationStatus
    {
        None,
        Validating,
        Validated,
        InvalidURL,
        PermissionDenied
    }

    public class EnterProjectUrlViewModel : ViewModelBase
    {
        public ICommand BackCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }

        private URLValidationStatus requestStatus = URLValidationStatus.None;

        public URLValidationStatus RequestStatus
        {
            get { return requestStatus; }
            set { requestStatus = value; OnPropertyChanged("RequestStatus"); OnPropertyChanged("ExportIsEnable"); }
        }

        private string errorMessage;

        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        private string uRL;

        public string URL
        {
            get { return uRL; }
            set
            {
                uRL = value;
                ValidateURL();
            }
        }

        private async Task ValidateURL()
        {
            //WPFTODO: VALIDATE URL
            if (uRL == "")
            {
                await Task.Delay(100);
                RequestStatus = URLValidationStatus.None;
            }
            else
            {
                RequestStatus = URLValidationStatus.Validating;
                await Task.Delay(500);
                switch (URL)
                {
                    case "ValidURL":
                        RequestStatus = URLValidationStatus.Validated;
                        break;
                    case "PermissionDenied":
                        ErrorMessage = "Your account doesn’t have access to this file";
                        RequestStatus = URLValidationStatus.PermissionDenied;
                        break;
                    default:
                        ErrorMessage = "Invalid URL";
                        RequestStatus = URLValidationStatus.InvalidURL;
                        break;
                }
            }
        }

        public bool ExportIsEnable => ViewIs3D && RequestStatus == URLValidationStatus.Validated;

        public bool ViewIs3D => MainWindowViewModel.Instance.IsActiveView3D;
        public bool ViewIsNot3D => !ViewIs3D;

        private void MainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Instance.IsActiveView3D))
            {
                OnPropertyChanged(nameof(ViewIs3D));
                OnPropertyChanged(nameof(ViewIsNot3D));
                OnPropertyChanged(nameof(ExportIsEnable));
            }
        }
        public EnterProjectUrlViewModel(NavigationService backNavigationService, NavigationService exportToExistingNavigationService)
        {
            MainWindowViewModel.Instance.PropertyChanged += MainWindowViewModel_PropertyChanged;
            BackCommand = new NavigateCommand(backNavigationService);
            BeginExportCommand = new NavigateCommand(exportToExistingNavigationService);
        }
    }
}
