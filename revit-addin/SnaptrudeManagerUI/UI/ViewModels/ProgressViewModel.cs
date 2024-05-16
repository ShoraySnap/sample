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
                    TransformCommand = new TransformCommand(
                        new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                        {
                            ((MainWindowViewModel)viewmodel).TopMost = false;
                            return viewmodel;
                        }));
                    TransformCommand.Execute(new object());
                    StartProgressCommand = new RelayCommand(async (o) => await StartImport());
                    progressMessage = "Import in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                case ProgressViewType.Update:
                    TransformCommand = new TransformCommand(
                        new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                        {
                            ((MainWindowViewModel)viewmodel).WhiteBackground = false;
                            return viewmodel;
                        }));
                    TransformCommand.Execute(new object());
                    OnPropertyChanged(nameof(WhiteBackground));
                    StartProgressCommand = new RelayCommand(async (o) => await StartUpdate());
                    progressMessage = "Update in progress, please don’t close this window.";
                    StartProgressCommand.Execute(null);
                    break;
                default:
                    break;
            }

            //StartProgressCommand.Execute(null);
        }

        public void UpdateProgress(int Value, string message)
        {
            ProgressValue = Value;
            ProgressMessage = message;
        }

        public void FinishImportToRevit()
        {
            SuccessCommand.Execute(new object());
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
