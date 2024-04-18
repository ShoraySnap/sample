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
    public class LoginViewModel : ViewModelBase
    {
        public ICommand LoginCommand { get; }
        public LoginViewModel(NavigationService loginNavigationService)
        {
            LoginCommand = new NavigateCommand(loginNavigationService);
        }
    }
}
