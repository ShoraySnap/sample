using ManagerUI.Commands;
using ManagerUI.Services;
using ManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static MainWindowViewModel Instance;

        private readonly NavigationStore navigationStore;

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

        public ICommand CloseCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand SwitchAccountCommand { get; }
        public ICommand LogOutCommand { get; }
        public Action CloseAction { get; set; }

        public ViewModelBase CurrentViewModel => navigationStore.CurrentViewModel;
        public bool LoginButtonVisible => !ImageBackground && CurrentViewModel.GetType().Name != "ModelExportedViewModel" && CurrentViewModel.GetType().Name != "ProgressViewModel";

        public MainWindowViewModel(NavigationStore navigationStore, string currentVersion, string updateVersion)
        {
            CurrentVersion = currentVersion;
            UpdateVersion = updateVersion;
            Instance = this;
            navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            this.navigationStore = navigationStore;
            CloseCommand = new RelayCommand(new Action<object>((o) => MainWindow.Instance.Close()));
            UpdateCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreater.CreateUpdateProgressViewModel));
            LogOutCommand = new NavigateCommand(new NavigationService(navigationStore, ViewModelCreater.CreateLoginViewModel));
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(LoginButtonVisible));
        }
    }
}
