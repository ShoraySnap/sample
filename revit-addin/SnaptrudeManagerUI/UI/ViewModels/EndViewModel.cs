using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using SnaptrudeManagerUI.API;

namespace SnaptrudeManagerUI.ViewModels
{
    public class EndViewModel : ViewModelBase
    {
        public enum EndViewType
        {
            ExportedSucessfull,
            ImportedSucessfull,
            RevitClosed,
            CloseAndUpdate
        }

        private string message;
        public string Message 
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged(nameof(Message));
            }
        }
        public string ButtonMessage { get; set; }
        public bool ButtonVisible { get; set; }

        private bool revitInstancesRunning;

        private bool buttonEnabled;
        public bool ButtonEnabled 
        { 
            get { return buttonEnabled; }
            set
            {
                buttonEnabled = value;
                OnPropertyChanged(nameof(ButtonEnabled));
            }
        }

        public bool WhiteBackground => MainWindowViewModel.Instance.WhiteBackground;
        public ICommand LaunchCommand { get; set; }
        public EndViewModel(EndViewType finalViewType)
        {
            MainWindowViewModel.Instance.TopMost = true;
            switch (finalViewType)
            {
                case EndViewType.ExportedSucessfull:
                    ButtonMessage = "Launch Snaptrude";
                    Message = "The model was exported successfully!";
                    ButtonVisible = true;
                    LaunchCommand = new RelayCommand(new Action<object>((o) => OpenSnaptrudeModel()));
                    break;
                case EndViewType.ImportedSucessfull:
                    ButtonMessage = "Launch Snaptrude";
                    Message = "The model was imported successfully!";
                    ButtonVisible = false;
                    break;
                case EndViewType.RevitClosed:
                    Message = "Revit was closed. Reopen Snaptrude Manager to continue importing/exporting Revit files!";
                    ButtonVisible = false;
                    break;
                case EndViewType.CloseAndUpdate:
                    MainWindowViewModel.Instance.WhiteBackground = false;
                    LaunchCommand = new RelayCommand((o) => StartUpdate());
                    ButtonVisible = true;
                    CheckForRevitInstances();
                    break;
                default:
                    break;
            }
        }

        private async void CheckForRevitInstances()
        {
            if (IsRevitRunning())
            {
                ButtonEnabled = false;
                revitInstancesRunning = true;
                ButtonMessage = "Install Update";
                Message = "Setup download completed!\nPlease close all Revit windows to continue.";
                await MonitorRevitClosedAsync();
            }
            else
            {
                ButtonEnabled = true;
                ButtonMessage = "Install Update";
                Message = "Setup download completed!";
            }
        }

        private void StartUpdate()
        {
            OpenUpdateInstaller();
            App.Current.Shutdown();
        }

        private bool IsRevitRunning()
        {
            return Process.GetProcessesByName("Revit").Any();
        }

        private async Task MonitorRevitClosedAsync()
        {
            while (revitInstancesRunning)
            {
                if (!IsRevitRunning())
                {
                    revitInstancesRunning = false;
                    ButtonEnabled = true;
                    ButtonMessage = "Install Update";
                    Message = "Setup download completed!";
                }
                await Task.Delay(500);
            }
        }

        private void OpenUpdateInstaller()
        {
            string installerPath = App.Updater.DownloadedFilePath;
            string revitExecutablePath = App.RevitProcess == null ? "" : App.RevitProcessFilePath;
            string arguments = $"/SILENT /ExecutablePath=\"{revitExecutablePath}\"";
            Process process = new Process();
            process.StartInfo.FileName = installerPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.Start();
        }

        private void OpenSnaptrudeModel()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Urls.Get("snaptrudeReactUrl") + "/model/" + Store.Get("floorkey"),
                UseShellExecute = true
            });
            App.Current.Shutdown();
        }
    }
}
