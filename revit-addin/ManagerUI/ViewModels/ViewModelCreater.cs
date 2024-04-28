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
                new NavigationService(NavigationStore.Instance, CreateExportProgressViewModel)
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
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }

        public static ProgressViewModel CreateExportProgressViewModel()
        {
            return new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Export,
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }
        public static ProgressViewModel CreateImportProgressViewModel()
        {
            return new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Import,
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }

        public static ProgressViewModel CreateUpdateProgressViewModel()
        {
            return new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Update,
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateAvailableViewModel));
        }


        private static HomeViewModel CreateHomeViewModel()
        {
            return new HomeViewModel(
                new NavigationService(NavigationStore.Instance, CreateSelectTrudeFileViewModel),
                new NavigationService(NavigationStore.Instance, CreateExportViewModel));
        }

        public static SelectTrudeFileViewModel CreateImportSelectTrudeFileViewModel()
        {
            return new SelectTrudeFileViewModel(
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel));
        }

        private static EnterProjectUrlViewModel CreateEnterProjectUrlViewModel()
        {
            return new EnterProjectUrlViewModel(
                new NavigationService(NavigationStore.Instance, CreateExportViewModel),
                new NavigationService(NavigationStore.Instance, CreateExportProgressViewModel));
        }
        private static ExportViewModel CreateExportViewModel()
        {
            return new ExportViewModel(
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateEnterProjectUrlViewModel));
        }

        private static SelectTrudeFileViewModel CreateSelectTrudeFileViewModel()
        {
            return new SelectTrudeFileViewModel(
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel));
        }

        private static IncompatibleTrudeViewModel CreateIncompatibleTrudeFileViewModel()
        {
            return new IncompatibleTrudeViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel));
        }
    }
}
