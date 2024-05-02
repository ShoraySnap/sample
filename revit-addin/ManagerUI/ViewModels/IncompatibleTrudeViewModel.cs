using ManagerUI.Commands;
using ManagerUI.Services;
using ManagerUI.Stores;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{

    public class IncompatibleTrudeViewModel : ViewModelBase
    {
        public string CurrentVersion => MainWindowViewModel.Instance.CurrentVersion;

        public string UpdateVersion => MainWindowViewModel.Instance.UpdateVersion;

        public ICommand BackCommand { get; }
        public ICommand UpdateCommand { get; }
        public IncompatibleTrudeViewModel(NavigationService backHomeNavigationService, NavigationService updateNavigationService)
        {
            BackCommand = new NavigateCommand(backHomeNavigationService);
            UpdateCommand = new NavigateCommand(updateNavigationService);
        }

        
    }
}
