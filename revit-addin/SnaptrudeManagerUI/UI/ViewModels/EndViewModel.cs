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
            RevitClosed
        }

        public string Message { get; }
        public bool ButtonVisible { get; }
        public ICommand LaunchCommand { get; }

        public EndViewModel(EndViewType finalViewType)
        {
            MainWindowViewModel.Instance.TopMost = true;
            switch (finalViewType)
            {
                case EndViewType.ExportedSucessfull:
                    Message = "The model was exported successfully!";
                    ButtonVisible = true;
                    LaunchCommand = new RelayCommand(new Action<object>((o) => OpenSnaptrudeModel()));
                    break;
                case EndViewType.ImportedSucessfull:
                    Message = "The model was imported successfully!";
                    ButtonVisible = false;
                    break;
                case EndViewType.RevitClosed:
                    Message = "Revit was closed. Reopen Snaptrude Manager to continue importing/exporting Revit files!";
                    ButtonVisible = false;
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
