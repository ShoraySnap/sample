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
                    Message = "Download complete!\nPlease close Revit to proceed with the installation.";
                    LaunchCommand = new RelayCommand((o) => App.Current.Shutdown());
                    ButtonVisible = true;
                    break;
                default:
                    break;
            }
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
