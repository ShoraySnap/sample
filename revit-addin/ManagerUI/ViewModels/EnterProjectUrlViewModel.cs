using ManagerUI.Commands;
using ManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class EnterProjectUrlViewModel : ViewModelBase
    {
        public ICommand BackCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }
        public EnterProjectUrlViewModel(NavigationService backNavigationService, NavigationService exportToExistingNavigationService)
        {
            BackCommand = new NavigateCommand(backNavigationService);
            BeginExportCommand = new NavigateCommand(exportToExistingNavigationService);
        }
    }
}
