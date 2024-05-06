using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SnaptrudeManagerAddin.Commands;
using SnaptrudeManagerAddin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SnaptrudeManagerAddin.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        public enum ProgressViewType
        {
            Export,
            Import,
            Update
        }

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

        private bool whiteBackground = true;

        public bool WhiteBackground
        {
            get { return whiteBackground; }
            set
            {
                whiteBackground = value;
                OnPropertyChanged("WhiteBackground");
            }
        }

        public ICommand StartProgressCommand { get; set; }
        public ICommand SuccessCommand { get; set; }
        public ICommand FailureCommand { get; set; }

        /// <summary>
        /// ProgressVIew for Revit Export
        /// </summary>
        /// <param name="successNavigationService"></param>
        /// <param name="failureNavigationService"></param>
        public ProgressViewModel(ProgressViewType progressViewType, NavigationService successNavigationService, NavigationService failureNavigationService)
        {
            SuccessCommand = new NavigateCommand(successNavigationService);
            FailureCommand = new NavigateCommand(failureNavigationService);
            switch (progressViewType)
            {
                case ProgressViewType.Export:
                    StartProgressCommand = new RelayCommand(async (o) => await StartExport());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                case ProgressViewType.Import:
                    StartProgressCommand = new RelayCommand(async (o) => await StartImport());
                    progressMessage = "Import in progress, please don’t close this window.";
                    break;
                case ProgressViewType.Update:
                    MainWindowViewModel.Instance.WhiteBackground = false;
                    WhiteBackground = false;
                    StartProgressCommand = new RelayCommand(async (o) => await StartUpdate());
                    progressMessage = "Update in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                default:
                    break;
            }

            //StartProgressCommand.Execute(null);
        }

        public async Task StartExport()
        {
            MainWindowViewModel.Instance.ImportEvent.Raise();
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(20);
            }
        }

        public async Task UpdateProgressValue()
        {
            ProgressValue += 10;
            await Task.Delay(20);
        }

        private async Task StartImport()
        {
            for (int i = 0; i <= 90; i++)
            {
                ProgressValue = i;
                await Task.Delay(1000);
            }
        }

        private async Task StartUpdate()
        {
            Random random = new Random();
            int randomResult = random.Next(2);
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(10);
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
