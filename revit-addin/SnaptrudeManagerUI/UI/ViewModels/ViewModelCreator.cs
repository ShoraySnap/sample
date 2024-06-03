using SnaptrudeManagerUI.API;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.ViewModels
{
    public static class ViewModelCreator
    {
        public static SelectFolderViewModel CreateSelectFolderViewModel()
        {
            return new SelectFolderViewModel(
                new NavigationService(
                    NavigationStore.Instance,
                    String.Equals(Store.Get("fileType"), "rvt") ?
                        CreateHomeViewModel :
                        CreateExportViewModel
                        ),

                new NavigationService(NavigationStore.Instance,
                    String.Equals(Store.Get("fileType"), "rfa") ?
                    CreateExportToRFANewProgressViewModel : CreateExportToNewProjectProgressViewModel
                ),
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel)
                );
        }

        public static LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateAvailableViewModel)
                );
        }

        public static ViewModelBase CreateWarningAllVisiblePartsViewModel()
        {
            bool skip = NavigationStore.Get(WarningId.AllVisibleParts.ToString())?.ToString() == "False";
            if (skip)
            { 
                return String.Equals(Store.Get("fileType"), "rvt") ?
                CreateSelectFolderViewModel() :
                CreateExportViewModel();
            }
            else
            {
                return new WarningViewModel(WarningId.AllVisibleParts,
                    new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                    new NavigationService(
                        NavigationStore.Instance,
                        String.Equals(Store.Get("fileType"), "rvt") ?
                        CreateSelectFolderViewModel :
                        CreateExportViewModel
                        )
                    );
            }
        }

        public static ViewModelBase CreateWarningWillNotReconcileViewModel()
        {
            bool skip = NavigationStore.Get(WarningId.WillNotReconcile.ToString())?.ToString() == "False";
            if (skip)
                return CreateEnterProjectUrlViewModel();
            else
            {
                return new WarningViewModel(WarningId.WillNotReconcile,
                new NavigationService(
                    NavigationStore.Instance,
                String.Equals(Store.Get("fileType"), "rvt") ?
                        CreateHomeViewModel :
                        CreateExportViewModel
                        ),
                new NavigationService(NavigationStore.Instance, CreateEnterProjectUrlViewModel)
                );
            }
        }

        public static UpdateAvailableViewModel CreateUpdateAvailableViewModel()
        {
            return new UpdateAvailableViewModel(
                false,
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }

        public static UpdateAvailableViewModel CreateRetryUpdateAvailableViewModel()
        {
            return new UpdateAvailableViewModel(
                true,
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }

        public static ModelExportedViewModel CreateModelExportedViewModel()
        {
            return new ModelExportedViewModel();
        }

        public static ProgressViewModel CreateExportToRFANewProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFANew,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }
        public static ProgressViewModel CreateExportToRFAExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFAExisting,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }


        public static ProgressViewModel CreateExportToNewProjectProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportProjectNew,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateExportToExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFAExisting,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateImportProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Import,
                new NavigationService(NavigationStore.Instance, CreateModelImportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ModelImportedViewModel CreateModelImportedViewModel()
        {
            return new ModelImportedViewModel();
        }

        public static ProgressViewModel CreateUpdateProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Update,
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateRetryUpdateAvailableViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }


        public static ViewModelBase CreateHomeViewModel()
        {
            return
            Store.isDataValid() ?
             new HomeViewModel(
                new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportLabelsViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateWarningAllVisiblePartsViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel))
            : CreateLoginViewModel();
        }

        private static ImportLabelsViewModel CreateImportLabelsViewModel()
        {
            return new ImportLabelsViewModel(
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel));
        }

        private static EnterProjectUrlViewModel CreateEnterProjectUrlViewModel()
        {
            return new EnterProjectUrlViewModel(
                new NavigationService(NavigationStore.Instance, CreateExportViewModel),
                new NavigationService(NavigationStore.Instance,
                String.Equals(Store.Get("fileType"), "rfa") ?
                CreateExportToRFAExistingProgressViewModel :
                CreateExportToNewProjectProgressViewModel));
        }
        private static ExportViewModel CreateExportViewModel()
        {
            return new ExportViewModel(
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateWarningWillNotReconcileViewModel));
        }

        private static IncompatibleTrudeViewModel CreateIncompatibleTrudeFileViewModel()
        {
            return new IncompatibleTrudeViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel));
        }
    }
}
