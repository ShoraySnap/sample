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
using TrudeCommon.Utils;
using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerUI.Stores;

namespace SnaptrudeManagerUI.ViewModels
{
    public enum URLValidationStatus
    {
        None,
        Validating,
        Validated,
        Error
    }

    public class EnterProjectUrlViewModel : ViewModelBase
    {
        private bool disposed = false;
        public ICommand BackCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }
        public ICommand ClearURLCommand { get; private set; }

        private URLValidationStatus requestStatus = URLValidationStatus.None;

        static Logger logger = LogManager.GetCurrentClassLogger();

        public URLValidationStatus RequestStatus
        {
            get { return requestStatus; }
            set { requestStatus = value; OnPropertyChanged("RequestStatus"); OnPropertyChanged("ExportIsEnabled"); }
        }

        private string floorkey;

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
                OnPropertyChanged("URL");
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
            try
            {

                //WPFTODO: UPDATE THE VALUE 10
                if (uRL == "")
                {
                    await Task.Delay(50);
                    RequestStatus = URLValidationStatus.None;
                }
                else if (!ValidateUrlWithRegex(uRL))
                {
                    RequestStatus = URLValidationStatus.Validating;
                    await Task.Delay(200);
                    ErrorMessage = "Invalid URL";
                    RequestStatus = URLValidationStatus.Error;
                }
                else
                {
                    RequestStatus = URLValidationStatus.Validating;
                    string _floorkey = extractFloorkey(uRL);
                    var response = await SnaptrudeRepo.ValidateURLAsync(_floorkey);
                    if (response != null)
                    {
                        if (response.Access)
                        {
                            RequestStatus = URLValidationStatus.Validated;
                            var presignedUrlResponse = await Uploader.GetPresignedURL("media/" + response.ImagePath, Config.GetConfigObject());
                            var presignedUrlResponseData = await presignedUrlResponse.Content.ReadAsStringAsync();
                            PreSignedURLResponse presignedURL = JsonConvert.DeserializeObject<PreSignedURLResponse>(presignedUrlResponseData);
                            Image = new Uri(presignedURL.url + presignedURL.fields["key"]);
                            ProjectName = response.ProjectName;
                            floorkey = _floorkey;
                        }
                        else
                        {
                            ErrorMessage = response.Message;
                            RequestStatus = URLValidationStatus.Error;
                        }
                    }
                    else
                    {
                        ErrorMessage = "Network error, try again.";
                        RequestStatus = URLValidationStatus.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                ErrorHandler.HandleException(ex, App.OnEnterProjectUrlIssue);
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
            if (inputText.Length < 8) return false;
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

        public bool ExportIsEnabled => ViewIs3D && RequestStatus == URLValidationStatus.Validated;

        public bool ViewIs3D => MainWindowViewModel.Instance.IsView3D;

        public bool ViewIsNot3D => !ViewIs3D;

        public EnterProjectUrlViewModel(NavigationService backNavigationService, NavigationService exportToExistingNavigationService)
        {
            BackCommand = new NavigateCommand(backNavigationService);
            BeginExportCommand = new RelayCommand((o) => { BeginExport(o, exportToExistingNavigationService); });
            ClearURLCommand = new RelayCommand((o) => { ClearUrl(); });
            App.OnActivateView2D += SetExportEnablement;
            App.OnActivateView3D += SetExportEnablement;
            App.OnEnterProjectUrlIssue += ShowOnEnterProjectUrlIssue;
            SetExportEnablement();
        }

        private void ShowOnEnterProjectUrlIssue(string errorMessage)
        {
            NavigationStore.Instance.CurrentViewModel = ViewModelCreator.CreateEnterProjectUrlWarningViewModel(errorMessage);
            Dispose();
        }

        private void SetExportEnablement()
        {
            OnPropertyChanged(nameof(ViewIs3D));
            OnPropertyChanged(nameof(ViewIsNot3D));
            OnPropertyChanged(nameof(ExportIsEnabled));
            OnPropertyChanged(nameof(ErrorMessage));
        }

        private void ClearUrl()
        {
            URL = "";
        }

        private void BeginExport(object param, NavigationService exportToExistingNavigationService)
        {
            Store.Set("floorkey", floorkey);
            Store.Save();
            var navCmd = new NavigateCommand(exportToExistingNavigationService);
            navCmd.Execute(param);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    UnsubscribeEvents();
                }
                disposed = true;
            }
        }

        private void UnsubscribeEvents()
        {
            App.OnActivateView2D -= SetExportEnablement;
            App.OnActivateView3D -= SetExportEnablement;
            App.OnEnterProjectUrlIssue -= ShowOnEnterProjectUrlIssue;
        }
        ~EnterProjectUrlViewModel()
        {
            Dispose(false);
        }
    }
}
