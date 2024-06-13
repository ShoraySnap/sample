using SnaptrudeManagerUI.API;
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
using NLog;

namespace SnaptrudeManagerUI.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public enum ProgressViewType
        {
            ExportProjectNew,
            ExportRFANew,
            ExportRFAExisting,
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
        /// ProgressVIew for Revit ExportProjectNew
        /// </summary>
        /// <param name="successNavigationService"></param>
        /// <param name="failureNavigationService"></param>
        public ProgressViewModel(ProgressViewType progressType, NavigationService successNavigationService, NavigationService failureNavigationService)
        {
            IsProgressBarIndeterminate = false;
            SuccessCommand = new NavigateCommand(successNavigationService);
            FailureCommand = new NavigateCommand(failureNavigationService);
            switch (progressType)
            {
                case ProgressViewType.ExportProjectNew:
                    progressViewType = ProgressViewType.ExportProjectNew;
                    StartProgressCommand = new RelayCommand(async (o) => await StartExportNewProject());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    CancelCommand = new RelayCommand(new Action<object>((o) => Cancel(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT)));
                    break;
                case ProgressViewType.ExportRFANew:
                    progressViewType = ProgressViewType.ExportRFANew;
                    StartProgressCommand = new RelayCommand(async (o) => await StartExportRFANew());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    CancelCommand = new RelayCommand(new Action<object>((o) => Cancel(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT)));
                    break;
                case ProgressViewType.ExportRFAExisting:
                    progressViewType = ProgressViewType.ExportRFAExisting;
                    StartProgressCommand = new RelayCommand(async (o) => await StartExportRFAExisting());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    CancelCommand = new RelayCommand(new Action<object>((o) => Cancel(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT)));
                    break;
                case ProgressViewType.Import:
                    progressViewType = ProgressViewType.Import;
                    MainWindowViewModel.Instance.TopMost = false;
                    progressMessage = "Import in progress, please don’t close this window.";
                    CancelCommand = new RelayCommand(new Action<object>((o) => Cancel(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_IMPORT)));
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
            App.OnAbort += Abort;
        }

        public void Cancel(TRUDE_EVENT trudeEvent)
        {
            TrudeEventEmitter.EmitEvent(trudeEvent);
            UpdateProgress(0, "Rolling back changes...");
            IsProgressBarIndeterminate = true;
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

        public void Abort()
        {
            FailureCommand.Execute(new object());
        }

        public async Task StartExportNewProject()
        {
            string floorkey = await SnaptrudeRepo.CreateProjectAsync();
            if (string.IsNullOrEmpty(floorkey))
            {
                logger.Error("Project for rvt could not be created!"); //TODO: UX for handling project creation failure
                Cancel(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT);
            }
            Store.Set("floorkey", floorkey);
            Store.Save();

            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE, "", App.TransferManager);
        }

        public async Task StartExportRFAExisting()
        {
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE, "", App.TransferManager);
        }

        public async Task StartExportRFANew()
        {
            string floorkey = await SnaptrudeRepo.CreateProjectAsync();
            if (string.IsNullOrEmpty(floorkey))
            {
                logger.Error("Project for RFA could not be created!"); //TODO: UX for handling project creation failure
                Cancel(TRUDE_EVENT.MANAGER_UI_REQ_ABORT_EXPORT);
            }
            Store.Set("floorkey", floorkey);
            Store.Save();

            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE, "", App.TransferManager);
        }

        public async Task FinishExport(string floorkey)
        {
            switch(progressViewType)
            {
                case ProgressViewType.ExportProjectNew:
                    await SnaptrudeService.SetRevitImportState(floorkey, "NEW");
                    break;
                case ProgressViewType.ExportRFANew:
                    await SnaptrudeService.SetRevitImportState(floorkey, "RFA");
                    break;
                case ProgressViewType.ExportRFAExisting:
                    await SnaptrudeService.SetRevitImportState(floorkey, "RFA");
                    break;
            }
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
