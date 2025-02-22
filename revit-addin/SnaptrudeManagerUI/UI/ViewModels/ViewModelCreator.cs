﻿using SnaptrudeManagerUI.API;
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
        public static CheckingUpdateViewModel CreateCheckingUpdateViewModel()
        {
            return new CheckingUpdateViewModel(
                new NavigationService(NavigationStore.Instance, CreateUpdateAvailableViewModel));
        }

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
                ))
                ;
        }

        public static LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateAvailableViewModel)
                );
        }

        public static ViewModelBase CreateNoteAllVisiblePartsViewModel()
        {
            bool skip = NavigationStore.Get(NoteId.AllVisibleParts.ToString())?.ToString() == "False";
            if (skip)
            {
                if (String.Equals(Store.Get("fileType"), "rvt"))
                    return CreateSelectFolderViewModel();
                else
                    return CreateExportViewModel();
            }
            else
            {
                return new NoteViewModel(NoteId.AllVisibleParts,
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

        public static ViewModelBase CreateNoteWillNotReconcileViewModel()
        {
            bool skip = NavigationStore.Get(NoteId.WillNotReconcile.ToString())?.ToString() == "False";
            if (skip)
                return CreateEnterProjectUrlViewModel();
            else
            {
                return new NoteViewModel(NoteId.WillNotReconcile,
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

        public static ViewModelBase CreateUploadIssueViewModel(string errorMessage)
        {
            return new WarningViewModel(WarningId.UploadIssue,
                new NavigationService(NavigationStore.Instance, CreateRetryUploadProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                errorMessage
                );
        }

        public static ViewModelBase CreateSelectFolderWarningViewModel(string errorMessage)
        {
            return new WarningViewModel(WarningId.SelectFolderIssue,
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                errorMessage
                );
        }

        public static ViewModelBase CreateEnterProjectUrlWarningViewModel(string errorMessage)
        {
            return new WarningViewModel(WarningId.SelectFolderIssue,
                new NavigationService(NavigationStore.Instance, CreateEnterProjectUrlViewModel),
                new NavigationService(NavigationStore.Instance, CreateExportViewModel),
                errorMessage
                );
        }

        public static ViewModelBase CreateTokenExpiredWarningViewModel(string errorMessage)
        {
            return new WarningViewModel(WarningId.TokenExpired,
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel), 
                null,
                errorMessage
                );
        }
        public static ViewModelBase CreateStartupInternetIssueWarningViewModel()
        {
            return new WarningViewModel(WarningId.StartupInternetConnectionIssue);
        }

        public static ViewModelBase CreateAPIBlockedViewModel(string errorMessage)
        {
            return new WarningViewModel(WarningId.APIBlocked, null, null, errorMessage);
        }

        public static ViewModelBase CreateRevitClosedWarningViewModel()
        {
            return new WarningViewModel(WarningId.RevitClosed,
                new NavigationService(NavigationStore.Instance, CreateSelectFolderViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel)
                );
        }

        public static UpdateAvailableViewModel CreateUpdateAvailableViewModel()
        {
            return new UpdateAvailableViewModel(
                false,
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateLoginViewModel));
        }

        public static UpdateAvailableViewModel CreateRetryUpdateAvailableViewModel()
        {
            return new UpdateAvailableViewModel(
                true,
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateLoginViewModel));
        }

        public static EndViewModel CreateDownloadFinishedViewModel()
        {
            return new EndViewModel(EndViewModel.EndViewType.CloseAndUpdate);
        }

        public static EndViewModel CreateModelExportedViewModel()
        {
            return new EndViewModel(EndViewModel.EndViewType.ExportedSucessfull);
        }

        public static ProgressViewModel CreateRetryUploadProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                App.RetryUploadProgressType,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, null),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                true);
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateExportToRFANewProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFANew,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                false);
            return MainWindowViewModel.Instance.ProgressViewModel;
        }
        public static ProgressViewModel CreateExportToRFAExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFAExisting,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                false);
            return MainWindowViewModel.Instance.ProgressViewModel;
        }


        public static ProgressViewModel CreateExportToNewProjectProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportProjectNew,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                false);
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateExportToExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportRFAExisting,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                false);
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateImportProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Import,
                new NavigationService(NavigationStore.Instance, CreateModelImportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateErrorViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                false);
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static EndViewModel CreateModelImportedViewModel()
        {
            return new EndViewModel(EndViewModel.EndViewType.ImportedSucessfull);
        }

        public static ProgressViewModel CreateUpdateProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.Update,
                new NavigationService(NavigationStore.Instance, CreateDownloadFinishedViewModel),
                new NavigationService(NavigationStore.Instance, CreateRetryUpdateAvailableViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                false);
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
                new NavigationService(NavigationStore.Instance, CreateNoteAllVisiblePartsViewModel),
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
                new NavigationService(NavigationStore.Instance, CreateNoteWillNotReconcileViewModel));
        }

        private static IncompatibleTrudeViewModel CreateIncompatibleTrudeFileViewModel()
        {
            return new IncompatibleTrudeViewModel(
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel));
        }
    }
}
