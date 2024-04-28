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
    public class SelectTrudeFileViewModel : ViewModelBase
    {
        ICommand StartImportNavigateCommand {  get; set; }
        ICommand IncompatibleNavigateCommand {  get; set; }
        public SelectTrudeFileViewModel(NavigationService progressViewNavigationService, NavigationService incompatibleNavigationService)
        {
            StartImportNavigateCommand = new NavigateCommand(progressViewNavigationService);
            IncompatibleNavigateCommand = new NavigateCommand(incompatibleNavigationService);
            if (progressViewNavigationService != null)
                StartImportNavigateCommand.Execute(null);
            else
                IncompatibleNavigateCommand.Execute(null);
        }
    }
}
