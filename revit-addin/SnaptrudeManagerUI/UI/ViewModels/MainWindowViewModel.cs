using SnaptrudeManagerUI.API;
using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TrudeCommon.Events;
using SnaptrudeManagerUI.UI.Helpers;
using Newtonsoft.Json;

namespace SnaptrudeManagerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private MainWindowViewModel() {
            ShowUserIcon = true;
            ShowLoader = false;
            App.OnSuccessfullLogin += OnSuccessfulLogin;
            App.OnFailedLogin += OnFailedLogin;
        }

        private static readonly object padlock = new object();
        private static MainWindowViewModel instance = null;
        public static MainWindowViewModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new MainWindowViewModel();
                    }
                    return instance;
                }
            }
        }

        public ICommand SwitchUserError;

        public ProgressViewModel ProgressViewModel;

        private NavigationStore navigationStore;

        public string ImportPath { get; set; }

        private string username;
        public string Username
        {
            get
            {
                username = Store.Get("fullname")?.ToString();
                return username;
            }
            set
            {
                username = value;
                OnPropertyChanged("Username");
            }
        }

        private bool showUserIcon;
        public bool ShowUserIcon
        {
            get { return showUserIcon; }
            set
            {
                showUserIcon = value; OnPropertyChanged("ShowUserIcon");
            }
        }

        private bool showLoader;
        public bool ShowLoader
        {
            get { return showLoader; }
            set
            {
                showLoader = value; OnPropertyChanged("ShowLoader");
            }
        }

        private string currentVersion;

        public string CurrentVersion
        {
            get { return currentVersion; }
            set { currentVersion = value; OnPropertyChanged("CurrentVersion"); }
        }

        private string updateVersion;

        public string UpdateVersion
        {
            get { return updateVersion; }
            set { updateVersion = value; OnPropertyChanged("UpdateVersion"); }
        }

        public bool HaveUpdateAvailable => CurrentVersion != UpdateVersion && WhiteBackground;

        public bool ImageBackground => !WhiteBackground;

        private bool whiteBackground;
        public bool WhiteBackground
        {
            get { return whiteBackground; }
            set
            {
                whiteBackground = value; OnPropertyChanged("ImageBackground"); OnPropertyChanged("WhiteBackground"); OnPropertyChanged("HaveUpdateAvailable");
            }
        }

        public ICommand CloseCommand { get; private set; }
        public ICommand NavigateHomeCommand { get; private set; }
        public ICommand UpdateCommand { get; private set; }
        public ICommand SwitchAccountCommand { get; private set; }
        public ICommand LogOutCommand { get; private set; }

        private bool isActiveView3D;
        public bool IsActiveView3D
        {
            get { return isActiveView3D; }
            set
            {
                isActiveView3D = value; OnPropertyChanged("IsActiveView3D");
            }
        }

        public Action CloseAction { get; set; }
        public ViewModelBase CurrentViewModel => navigationStore.CurrentViewModel;
        public bool CloseButtonVisible =>
            CurrentViewModel.GetType().Name != "ProgressViewModel";

        public bool LoginButtonVisible =>
            !ImageBackground && CurrentViewModel.GetType().Name != "ModelExportedViewModel" &&
            !ImageBackground && CurrentViewModel.GetType().Name != "ModelImportedViewModel" &&
            CurrentViewModel.GetType().Name != "ProgressViewModel";

        private bool topMost = true;
        public bool TopMost
        {
            get { return topMost; }
            set
            {
                topMost = value; OnPropertyChanged("TopMost");
            }
        }

        private bool isDocumentOpen;
        public bool IsDocumentOpen
        {
            get { return isDocumentOpen; }
            set
            {
                isDocumentOpen = value; OnPropertyChanged("IsDocumentOpen");
            }
        }



        public void ConfigMainWindowViewModel(NavigationStore navigationStore, string currentVersion, string updateVersion, bool isActiveView3D)
        {
            IsDocumentOpen = true;
            NavigateHomeCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateHomeViewModel));
            TopMost = true;
            IsActiveView3D = isActiveView3D;
            CurrentVersion = currentVersion;
            UpdateVersion = updateVersion;
            navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            this.navigationStore = navigationStore;
            CloseCommand = new RelayCommand(new Action<object>((o) =>
            {
                App.Current.Shutdown();
            }));
            UpdateCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateUpdateProgressViewModel));
            LogOutCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateLoginViewModel));
            SwitchAccountCommand = new RelayCommand(SwitchAccount);
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(LoginButtonVisible));
            OnPropertyChanged(nameof(CloseButtonVisible));
        }

        private void SwitchAccount(object parameter)
        {
            ShowUserIcon = false;
            ShowLoader = true;
            Store.Set("fullname", " Switching account");
            Username = " Switching Account";
            LoginHelper.Login(parameter);
            if (SwitchUserError != null)
            {
                var param =
                new Dictionary<string, string>{
                    {"infotext", ""},
                    {"infocolor", "#767B93"},
                    {"showinfo", "false"}
                };
                SwitchUserError.Execute(param);
            }
        }

        private void OnFailedLogin()
        {
            ShowUserIcon = true;
            ShowLoader = false;
            OnPropertyChanged("Username");

            if (SwitchUserError != null)
            {
                var param =
                new Dictionary<string, string>{
                    {"infotext", "Failed to switch account. Please try again."},
                    {"infocolor", "#D24B4E"},
                    {"showinfo", "true"}
                };
                SwitchUserError.Execute(param);
            }
        }

        private void OnSuccessfulLogin()
        {
            ShowUserIcon = true;
            ShowLoader = false;
            OnPropertyChanged("Username");


            if (SwitchUserError != null)
            {
                var param =
                new Dictionary<string, string>{
                    {"infotext", ""},
                    {"infocolor", "#767B93"},
                    {"showinfo", "false"}
                };
                SwitchUserError.Execute(param);
            }
        }
    }
}
