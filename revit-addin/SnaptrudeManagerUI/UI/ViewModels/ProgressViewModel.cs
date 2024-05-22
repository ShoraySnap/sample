﻿using SnaptrudeManagerUI.API;
using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TrudeCommon.Events;

namespace SnaptrudeManagerUI.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        public enum ProgressViewType
        {
            ExportToNew,
            ExportToExisting,
            Import,
            Update
        }

        public static ProgressViewType progressViewType;

        private string progressMessage;

        public string ProgressMessage
        {
            get { return progressMessage; }
            set
            {
                progressMessage = value;
                OnPropertyChanged("ProgressMessage");
            }
        }

        private int progressValue;

        public int ProgressValue
        {
            get { return progressValue; }
            set
            {
                progressValue = value;
                OnPropertyChanged("ProgressValue");
            }
        }

        private bool isProgressBarIndeterminate;

        public bool IsProgressBarIndeterminate
        {
            get { return isProgressBarIndeterminate; }
            set
            {
                isProgressBarIndeterminate = value;
                OnPropertyChanged("IsProgressBarIndeterminate");
                OnPropertyChanged("IsProgressValueVisible");
            }
        }

        public bool IsProgressValueVisible => !isProgressBarIndeterminate;

        public bool WhiteBackground => MainWindowViewModel.Instance.WhiteBackground;

        public ICommand StartProgressCommand { get; set; }
        public ICommand SuccessCommand { get; set; }
        public ICommand FailureCommand { get; set; }
        public ICommand TransformCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        /// <summary>
        /// ProgressVIew for Revit ExportToNew
        /// </summary>
        /// <param name="successNavigationService"></param>
        /// <param name="failureNavigationService"></param>
        public ProgressViewModel(ProgressViewType progressViewType, NavigationService successNavigationService, NavigationService failureNavigationService)
        {
            IsProgressBarIndeterminate = false;
            SuccessCommand = new NavigateCommand(successNavigationService);
            FailureCommand = new NavigateCommand(failureNavigationService);
            switch (progressViewType)
            {
                case ProgressViewType.ExportToNew:
                    progressViewType = ProgressViewType.ExportToNew;
                    StartProgressCommand = new RelayCommand(async (o) => await StartExportNew());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                case ProgressViewType.ExportToExisting:
                    progressViewType = ProgressViewType.ExportToNew;
                    StartProgressCommand = new RelayCommand(async (o) => await StartExportExisting());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                case ProgressViewType.Import:
                    progressViewType = ProgressViewType.Import;
                    MainWindowViewModel.Instance.TopMost = false;
                    progressMessage = "Import in progress, please don’t close this window.";
                    CancelCommand = new RelayCommand(new Action<object>((o) =>
                    {
                        TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_IMPORT);
                        UpdateProgress(0, "Rolling back changes...");
                        IsProgressBarIndeterminate = true;
                    }));
                    break;
                case ProgressViewType.Update:
                    progressViewType = ProgressViewType.Update;
                    MainWindowViewModel.Instance.WhiteBackground = false;
                    OnPropertyChanged(nameof(WhiteBackground));
                    StartProgressCommand = new RelayCommand(async (o) => await StartUpdate());
                    progressMessage = "Update in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                default:
                    break;
            }
            //StartProgressCommand.Execute(null);
            App.OnProgressUpdate += UpdateProgress;
            App.OnAbortImport += AbortImportToRevit;
        }

        public void UpdateProgress(int Value, string message)
        {
            App.Current.MainWindow.Focus();
            ProgressValue = Value;
            ProgressMessage = message;
        }

        public void FinishImportToRevit()
        {
            SuccessCommand.Execute(new object());
        }

        public void AbortImportToRevit()
        {
            FailureCommand.Execute(new object());
        }

        public async Task StartExportNew()
        {
            Store.Set("floorkey", await SnaptrudeService.CreateProjectAsync());
            Store.Save();

            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE, "NEW", App.TransferManager);
        }

        public async Task StartExportExisting()
        {
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE, "EXISTING", App.TransferManager);
        }

        private async Task StartImport()
        {
            //MainWindowViewModel.Instance.ImportEvent.Raise();
        }

        private async Task StartUpdate()
        {
            Random random = new Random();
            int randomResult = random.Next(2);
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(50);
                if (i == 15 && randomResult == 0)
                {
                    FailureCommand.Execute(new object());
                    return;
                }
            }
            MainWindowViewModel.Instance.CurrentVersion = MainWindowViewModel.Instance.UpdateVersion;
            SuccessCommand.Execute(new object());
        }

    }
}
