﻿using SnaptrudeManagerUI.API;
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
using NLog;
using System.Reflection;

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
        private static Logger logger = LogManager.GetCurrentClassLogger();
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

        public ProgressViewModel ProgressViewModel;

        private NavigationStore navigationStore;
        
        public string ImportPath { get; set; }

        private string username;
        public string Username
        {
            get
            {
                string fullName = Store.Get("fullname")?.ToString();
                string[] names = fullName.Split(' ');
                username = names.Count() > 1 ? $"{names[0]} {names.Last().First()}." : fullName;
                
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

        private string projectFileName;
        public string ProjectFileName
        {
            get { return projectFileName; }
            set
            {
                projectFileName = value; 
                OnPropertyChanged("ProjectFileName");
                OnPropertyChanged("IsProjectFileNameVisible");
            }
        }

        public bool IsProjectFileNameVisible => WhiteBackground && ProjectFileName != "" && CurrentViewModel?.GetType().Name != "WarningViewModel";

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
                whiteBackground = value; 
                OnPropertyChanged("ImageBackground"); 
                OnPropertyChanged("WhiteBackground"); 
                OnPropertyChanged("HaveUpdateAvailable");
                OnPropertyChanged("IsProjectFileNameVisible");
            }
        }

        public ICommand CloseCommand { get; private set; }
        public ICommand NavigateHomeCommand { get; private set; }
        public ICommand UpdateCommand { get; private set; }
        public ICommand LogOutCommand { get; private set; }
        public ICommand BackToLoginCommand { get; private set; }
        public bool IsUserLoggedIn { get; set; }
        public ICommand RevitClosedCommand { get; private set; }


        public bool IsView3D;
        public bool IsDocumentOpen;
        public bool IsDocumentRvt;

        public ViewModelBase CurrentViewModel => navigationStore?.CurrentViewModel;
        public bool CloseButtonVisible =>
            CurrentViewModel?.GetType().Name != "ProgressViewModel";

        public bool LoginButtonVisible =>
            CurrentViewModel != null &&
            !ImageBackground && CurrentViewModel?.GetType().Name != "EndViewModel" &&
            CurrentViewModel?.GetType().Name != "ProgressViewModel" &&
            CurrentViewModel?.GetType().Name != "WarningViewModel" &&
            CurrentViewModel?.GetType().Name != "ProgressViewModel" &&
            CurrentViewModel?.GetType().Name != "CheckingUpdateViewModel";

        private bool topMost = true;
        private bool disposed;

        public bool TopMost
        {
            get { return topMost; }
            set
            {
                topMost = value; OnPropertyChanged("TopMost");
            }
        }

        private bool isLoaderVisible;

        public bool IsLoaderVisible
        {
            get { return isLoaderVisible; }
            set { isLoaderVisible = value; OnPropertyChanged(nameof(IsLoaderVisible)); }
        }

        public async void ConfigMainWindowViewModel(NavigationStore navigationStore, bool isView3D, bool isDocumentRvt, bool isDocumentOpen, string fileName)
        {
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
            WhiteBackground = false;
            IsLoaderVisible = false;
            ProjectFileName = fileName + (isDocumentOpen ? (isDocumentRvt ? ".rvt" : ".rfa") : "");
            IsDocumentOpen = isDocumentOpen;
            NavigateHomeCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateHomeViewModel));
            TopMost = true;
            IsView3D = isView3D;
            IsDocumentRvt = isDocumentRvt;
            navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            this.navigationStore = navigationStore;
            CloseCommand = new RelayCommand(new Action<object>((o) =>
            {
                App.Current.Shutdown();
            }));
            UpdateCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateUpdateProgressViewModel));
            RevitClosedCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateRevitClosedWarningViewModel));
            LogOutCommand = new RelayCommand(LogoutAccount);
            BackToLoginCommand =  new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateLoginViewModel));
            App.OnActivateView2D += Set2DView;
            App.OnActivateView3D += Set3DView;
            App.OnRvtOpened += SetProjectRvt;
            App.OnRfaOpened += SetProjectRfa;
            App.OnDocumentClosed += HandleDocumentClosed;
            App.OnDocumentChanged += NavigateBackHome;
            App.OnRevitClosed += GotoRevitCloseEndView;
            App.OnUpdateAvailable += SetUpdateVersion;
            App.OnLoginNotFound += SetLoginNotFound;
        }

        public void SetLoginNotFound()
        {
            IsUserLoggedIn = false;
        }

        private void SetUpdateVersion()
        {
            UpdateVersion = App.Updater.UpdateVersion.Substring(0,5);
        }

        private void GotoRevitCloseEndView()
        {
            if (WhiteBackground)
            {
            RevitClosedCommand.Execute(null);
        }
        }

        private void HandleDocumentClosed()
        {
            IsDocumentOpen = false;
            ProjectFileName = "";
        }

        private void Set2DView()
        {
            IsDocumentOpen = true;
            IsView3D = false;
        }

        private void Set3DView()
        {
            IsDocumentOpen = true;
            IsView3D = true;
        }

        private void SetProjectRvt()
        {
            IsDocumentRvt = true;
        }

        private void SetProjectRfa()
        {
            IsDocumentRvt = false;
        }

        private void NavigateBackHome()
        {
            if (WhiteBackground && !(CurrentViewModel is ProgressViewModel))
            {
                NavigateHomeCommand.Execute(null);
            }
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(LoginButtonVisible));
            OnPropertyChanged(nameof(CloseButtonVisible));
            OnPropertyChanged(nameof(IsProjectFileNameVisible));
        }

        private void LogoutAccount(object parameter)
        {
            ShowUserIcon = false;
            ShowLoader = true;
            if (Store.isDataValid())
            {
                logger.Info("Logging out current user...");
            }
            else
            {
                logger.Warn("Invalid config for current user when logging out...");
            }
            Store.Reset();
            BackToLoginCommand.Execute(parameter);
        }

        private void OnFailedLogin()
        {
            ShowUserIcon = true;
            ShowLoader = false;
            OnPropertyChanged("Username");
        }

        private void OnSuccessfulLogin()
        {
            ShowUserIcon = true;
            ShowLoader = false;
            OnPropertyChanged("Username");
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
            App.OnActivateView2D -= Set2DView;
            App.OnActivateView3D -= Set3DView;
            App.OnRvtOpened -= SetProjectRvt;
            App.OnRfaOpened -= SetProjectRfa;
            App.OnDocumentClosed -= HandleDocumentClosed;
            App.OnDocumentChanged -= NavigateBackHome;
        }
        ~MainWindowViewModel()
        {
            Dispose(false);
        }
    }
}
