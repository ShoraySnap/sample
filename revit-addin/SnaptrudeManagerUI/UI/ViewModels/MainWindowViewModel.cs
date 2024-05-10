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
    public class MainWindowViewModel : ViewModelBase
    {
        private MainWindowViewModel() { }

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
        public ProgressViewModel ProgressViewModel;

        private NavigationStore navigationStore = NavigationStore.Instance;

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

        public void ConfigMainWindowViewModel(NavigationStore navigationStore, string currentVersion, string updateVersion, bool isActiveView3D)
        {
            TopMost = true;
            IsActiveView3D = isActiveView3D;
            CurrentVersion = currentVersion;
            UpdateVersion = updateVersion;
            navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            this.navigationStore = navigationStore;
            CloseCommand = new RelayCommand(new Action<object>((o) => App.Current.Shutdown()));
            UpdateCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateUpdateProgressViewModel));
            LogOutCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreator.CreateLoginViewModel));
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(LoginButtonVisible));
        }
    }
}
