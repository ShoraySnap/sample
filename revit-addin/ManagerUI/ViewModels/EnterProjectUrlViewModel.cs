using ManagerUI.Commands;
using ManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace ManagerUI.ViewModels
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
            set { requestStatus = value; OnPropertyChanged("RequestStatus"); }
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

        public EnterProjectUrlViewModel(NavigationService backNavigationService, NavigationService exportToExistingNavigationService)
        {
            BackCommand = new NavigateCommand(backNavigationService);
            BeginExportCommand = new NavigateCommand(exportToExistingNavigationService);
        }
    }
}
