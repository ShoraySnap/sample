using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerUI.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand UpdateCommand { get; }

        public string CurrentVersion => MainWindowViewModel.Instance.CurrentVersion;
        public string UpdateVersion => MainWindowViewModel.Instance.UpdateVersion;
        public bool UpdateAvailable => CurrentVersion != UpdateVersion;
        public bool ViewIs3D => MainWindowViewModel.Instance.IsActiveView3D;
        public bool ViewIsNot3D => !ViewIs3D;

        private void MainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Instance.IsActiveView3D))
            {
                OnPropertyChanged(nameof(ViewIs3D));
                OnPropertyChanged(nameof(ViewIsNot3D));
            }
        }

        public HomeViewModel(NavigationService importNavigationService, NavigationService exportNavigationService, NavigationService updateNavigationService)
        {
            MainWindowViewModel.Instance.TopMost = true;
            MainWindowViewModel.Instance.PropertyChanged += MainWindowViewModel_PropertyChanged;
            MainWindowViewModel.Instance.WhiteBackground = true;
            ExportCommand = new NavigateCommand(exportNavigationService);
            ImportCommand = new NavigateCommand(importNavigationService);
            UpdateCommand = new NavigateCommand(updateNavigationService);
        }


    }
}
