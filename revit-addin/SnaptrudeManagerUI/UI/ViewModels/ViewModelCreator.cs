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
                        (Func<ViewModelBase>)CreateHomeViewModel :
                        CreateExportViewModel
                        ),

                new NavigationService(NavigationStore.Instance,
                    String.Equals(Store.Get("fileType"), "rfa") ?
                    (Func<ViewModelBase>)CreateExportToRFANewProgressViewModel : CreateExportToNewProjectProgressViewModel
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
                if (String.Equals(Store.Get("fileType"), "rvt"))
                    return CreateSelectFolderViewModel();
                else
                    return CreateExportViewModel();
            }
            else
            {
                return new WarningViewModel(WarningId.AllVisibleParts,
                    new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                    new NavigationService(
                        NavigationStore.Instance,
                        String.Equals(Store.Get("fileType"), "rvt") ?
                        (Func<ViewModelBase>)CreateSelectFolderViewModel :
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
                        (Func<ViewModelBase>)CreateHomeViewModel :
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
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }
        public static ProgressViewModel CreateExportToRFAExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFAExisting,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }


        public static ProgressViewModel CreateExportToNewProjectProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportProjectNew,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateExportToExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFAExisting,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateImportProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Import,
                new NavigationService(NavigationStore.Instance, CreateModelImportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
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
                new NavigationService(NavigationStore.Instance, CreateRetryUpdateAvailableViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ViewModelBase CreateErrorViewModel()
        {
            return new ErrorViewModel(ProgressViewModel.progressViewType,
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
        }

        public static ViewModelBase CreateHomeViewModel()
        {
            if (Store.isDataValid())
            {
                return new HomeViewModel(
                new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportLabelsViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateWarningAllVisiblePartsViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel));
            }
            else
            {
                return CreateLoginViewModel();
            }
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
                (Func<ViewModelBase>)CreateExportToRFAExistingProgressViewModel :
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
