using SnaptrudeManagerAddin.Commands;
using SnaptrudeManagerAddin.Services;
using SnaptrudeManagerAddin.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerAddin.ViewModels
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
