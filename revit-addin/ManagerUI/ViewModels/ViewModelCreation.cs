using ManagerUI.Services;
using ManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagerUI.ViewModels
{
    public static class ViewModelCreation
    {
        public static LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }
        private static HomeViewModel CreateHomeViewModel()
        {
            return new HomeViewModel(new NavigationService(NavigationStore.Instance, CreateImportViewModel), new NavigationService(NavigationStore.Instance, CreateExportViewModel));
        }
        public static ExportViewModel CreateImportViewModel()
        {
            return new ExportViewModel();
        }
        private static ExportViewModel CreateExportViewModel()
        {
            return new ExportViewModel();
        }
    }
}
