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
    public class HomeViewModel : ViewModelBase
    {
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand UpdateCommand { get; }

        public string CurrentVersion => MainWindowViewModel.Instance.CurrentVersion;
        public string UpdateVersion => MainWindowViewModel.Instance.UpdateVersion;
        public bool UpdateAvailable => CurrentVersion != UpdateVersion;
        public HomeViewModel(NavigationService importNavigationService, NavigationService exportNavigationService, NavigationService updateNavigationService)
        {
            //MainWindowViewModel.Instance.ImageBackground = false;
            MainWindowViewModel.Instance.WhiteBackground = true;
            ExportCommand = new NavigateCommand(exportNavigationService);
            ImportCommand = new NavigateCommand(importNavigationService);
            UpdateCommand = new NavigateCommand(updateNavigationService);
        }
    }
}
