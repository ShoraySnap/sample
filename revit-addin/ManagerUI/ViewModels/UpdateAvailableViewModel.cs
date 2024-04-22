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
    public class UpdateAvailableViewModel : ViewModelBase
    {
        public ICommand SkipCommand { get; }
        public ICommand UpdateCommand { get; }
        public UpdateAvailableViewModel(NavigationService updateNowNavigationService, NavigationService skipUpdateNavigationService) 
        {
            SkipCommand = new NavigateCommand(skipUpdateNavigationService);
            UpdateCommand = new Upd(updateNowNavigationService);
        }
    }
}
