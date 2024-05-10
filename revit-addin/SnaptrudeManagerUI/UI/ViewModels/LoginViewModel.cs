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
    public class LoginViewModel : ViewModelBase
    {
        public ICommand LoginCommand { get; }
        public LoginViewModel(NavigationService homeNavigationService, NavigationService updateAvailableNavigationService)
        {
            MainWindowViewModel.Instance.WhiteBackground = false;
            LoginCommand = new NavigateCommand(homeNavigationService);
        }

        private bool openSnaptrudeLoginPage()
        {
            return true;
        }
    }
}
