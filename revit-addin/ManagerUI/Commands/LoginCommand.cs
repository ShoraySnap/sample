using ManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagerUI.Commands
{
    public class LoginCommand : CommandBase
    {
        private readonly NavigationService navigationService;

        public LoginCommand(NavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public override void Execute(object parameter)
        {
            // TO DO: OPEN SNAPTRUDE LOGIN PAGE
            navigationService.Navigate();
        }
    }
}
