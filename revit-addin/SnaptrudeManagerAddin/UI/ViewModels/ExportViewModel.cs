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
    public class ExportViewModel : ViewModelBase
    {
        public ICommand ExportToNewCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ExportToExistingCommand { get; }
        public ExportViewModel(NavigationService importToNewNavigationService, NavigationService backHomeNavigationService, NavigationService exportToExistingNavigationService)
        {
            ExportToNewCommand = new NavigateCommand(importToNewNavigationService);
            BackCommand = new NavigateCommand(backHomeNavigationService);
            ExportToExistingCommand = new NavigateCommand(exportToExistingNavigationService);
        }
    }
}
