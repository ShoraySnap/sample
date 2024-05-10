using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using SnaptrudeManagerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.Commands
{
    public class NavigateCommand : CommandBase
    {
        private readonly NavigationService navigationService;

        public NavigateCommand(NavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public override void Execute(object parameter)
        {
            navigationService.Navigate();
        }
    }
}
