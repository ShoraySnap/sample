using ManagerUI.Services;
using ManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagerUI.ViewModels
{
    public static class ViewModelCreater
    {
        public static SelectFolderViewModel CreateSelectFolderViewModel()
        {
            return new SelectFolderViewModel(
                new NavigationService(NavigationStore.Instance, CreateExportViewModel),
                new NavigationService(NavigationStore.Instance, CreateExportViewModel)
                );
        }

        public static LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateAvailableViewModel)
                );
        }
        
        public static UpdateAvailableViewModel CreateUpdateAvailableViewModel()
        {
            return new UpdateAvailableViewModel(
                new NavigationService(NavigationStore.Instance, CreateUpdateNowViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }
        
        public static UpdateNowViewModel CreateUpdateNowViewModel()
        {
            return new UpdateNowViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }
        
        private static HomeViewModel CreateHomeViewModel()
        {
            return new HomeViewModel(
                new NavigationService(NavigationStore.Instance, CreateSelectTrudeFileViewModel), 
                new NavigationService(NavigationStore.Instance, CreateExportViewModel));
        }
        
        public static SelectTrudeFileViewModel CreateImportSelectTrudeFileViewModel()
        {
            return new SelectTrudeFileViewModel(new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel));
        }


        private static ExportViewModel CreateExportViewModel()
        {
            return new ExportViewModel(
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel));
        }
        
        private static SelectTrudeFileViewModel CreateSelectTrudeFileViewModel()
        {
            return new SelectTrudeFileViewModel(
                new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel));
        }

        private static IncompatibleTrudeViewModel CreateIncompatibleTrudeFileViewModel()
        {
            return new IncompatibleTrudeViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateNowViewModel));
        }
    }
}
