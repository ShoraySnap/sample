﻿using SnaptrudeManagerUI.Services;
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
        private static string IniFilePath = @"C:\ProgramData\Snaptrude\SnaptrudeManager.ini";

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

        public static ViewModelBase CreateWarningAllVisiblePartsViewModel()
        {
            var iniFile = new IniFileUtils(IniFilePath);
            string result = iniFile.Read(WarningId.AllVisibleParts.ToString(), "Warnings");
            if (result == "Skip")
                return CreateExportViewModel();
            else
            {
                return new WarningViewModel(WarningId.AllVisibleParts,
                    new NavigationService(NavigationStore.Instance, CreateHomeViewModel),
                    new NavigationService(NavigationStore.Instance, CreateExportViewModel)
                    );
            }
        }

        public static ViewModelBase CreateWarningWillNotReconcileViewModel()
        {
            var iniFile = new IniFileUtils(IniFilePath);
            string result = iniFile.Read(WarningId.WillNotReconcile.ToString(), "Warnings");
            if (result == "Skip")
                return CreateEnterProjectUrlViewModel();
            else
            {
                return new WarningViewModel(WarningId.WillNotReconcile,
                new NavigationService(NavigationStore.Instance, CreateExportViewModel),
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

        public static ProgressViewModel CreateExportProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportToNew,
                new NavigationService(NavigationStore.Instance, CreateModelExportedViewModel),
                new NavigationService(NavigationStore.Instance, CreateHomeViewModel));
            return MainWindowViewModel.Instance.ProgressViewModel;
        }

        public static ProgressViewModel CreateExportToExistingProgressViewModel()
        {
            MainWindowViewModel.Instance.ProgressViewModel = new ProgressViewModel(
                ProgressViewModel.ProgressViewType.ExportToExisting,
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


        private static HomeViewModel CreateHomeViewModel()
        {
            return new HomeViewModel(
                new NavigationService(NavigationStore.Instance, CreateIncompatibleTrudeFileViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportLabelsViewModel),
                new NavigationService(NavigationStore.Instance, CreateImportProgressViewModel),
                new NavigationService(NavigationStore.Instance, CreateWarningAllVisiblePartsViewModel),
                new NavigationService(NavigationStore.Instance, CreateUpdateProgressViewModel));
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
                new NavigationService(NavigationStore.Instance, CreateExportProgressViewModel));
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
