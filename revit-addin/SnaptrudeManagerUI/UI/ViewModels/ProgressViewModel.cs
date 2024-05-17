﻿using SnaptrudeManagerUI.Commands;
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

namespace SnaptrudeManagerUI.ViewModels
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

        /// <summary>
        /// ProgressVIew for Revit Export
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
                case ProgressViewType.Export:
                    StartProgressCommand = new RelayCommand(async (o) => await StartExport());
                    progressMessage = "Export in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                case ProgressViewType.Import:
                    MainWindowViewModel.Instance.TopMost = false;
                    progressMessage = "Import in progress, please don’t close this window.";
                    break;
                case ProgressViewType.Update:
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

        public async Task StartExport()
        {
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(20);
            }
            SuccessCommand.Execute(new object());
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
