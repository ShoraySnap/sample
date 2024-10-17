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

        public string Message { get; }
        public string ButtonMessage { get; }
        public bool ButtonVisible { get; }
        public bool WhiteBackground => MainWindowViewModel.Instance.WhiteBackground;
        public ICommand LaunchCommand { get; }
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
                    ButtonMessage = "Install Update";
                    Message = "Setup download completed!";
                    LaunchCommand = new RelayCommand((o) => StartUpdate());
                    ButtonVisible = true;
                    break;
                default:
                    break;
            }
        }

        private void StartUpdate()
        {
            OpenUpdateInstaller();
            App.Current.Shutdown();
        }

        private void OpenUpdateInstaller()
        {
            string installerPath = App.Updater.DownloadedFilePath;
            string revitExecutablePath = App.RevitProcess.MainModule.FileName;
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
