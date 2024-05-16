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
using SnaptrudeManagerUI.API;
using System.Text.RegularExpressions;
using System.Security.Policy;

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

        private Uri image;
        public Uri Image
        {
            get { return image; }
            set
            {
                image = value;
                OnPropertyChanged("Image");
            }
        }

        private string projectName;
        public string ProjectName
        {
            get { return projectName; }
            set
            {
                projectName = value;
                OnPropertyChanged("ProjectName");
            }
        }

        private async Task ValidateURL()
        {
            //WPFTODO: UPDATE THE VALUE 10
            if (uRL == "" || !ValidateUrlWithRegex(uRL))
            {
                await Task.Delay(100);
                RequestStatus = URLValidationStatus.None;
            }
            else
            {
                RequestStatus = URLValidationStatus.Validating;
                string floorkey = extractFloorkey(uRL);
                var response = await SnaptrudeRepo.ValidateURLAsync(floorkey);
                if (response != null)
                {
                    if (response.Access)
                    {
                        RequestStatus = URLValidationStatus.Validated;
                        Image = new Uri(Urls.Get("snaptrudeDjangoUrl") + "/media/" + response.ImagePath);
                        ProjectName = response.ProjectName;
                    }
                    else
                    {
                        ErrorMessage = response.Message;
                        RequestStatus = URLValidationStatus.InvalidURL;
                    }
                }
                else
                {
                    ErrorMessage = "Network error, try again.";
                    RequestStatus = URLValidationStatus.InvalidURL;
                }
            }
        }

        private string extractFloorkey(string url)
        {
            if (url.EndsWith("/"))
                return url.Substring(url.Length - 7, 6);
            return url.Substring(url.Length - 6);
        }

        private bool ValidateUrlWithRegex(string inputText)
        {
            var domain = Urls.Get("snaptrudeReactUrl");
            domain = (domain == null) ? "" : domain;
            if (domain.Substring(0, 8) == "https://" &&
                inputText.Substring(0, 8) != "https://")
            {
                inputText = "https://" + inputText;
            }
            else if (domain.Substring(0, 7) == "http://" &&
                inputText.Substring(0, 7) != "http://")
            {
                inputText = "http://" + inputText;
            }

            var pattern = new Regex("^" + domain + "/model/" + "\\w{6}/?$");
            return pattern.IsMatch(inputText);
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
            TransformCommand transformMainWindowViewModelCommand = new TransformCommand(
                new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                {
                    ((MainWindowViewModel)viewmodel).PropertyChanged += MainWindowViewModel_PropertyChanged;
                    return viewmodel;
                }));
            transformMainWindowViewModelCommand.Execute(new object());
            BackCommand = new NavigateCommand(backNavigationService);
            BeginExportCommand = new NavigateCommand(exportToExistingNavigationService);
        }
    }
}
