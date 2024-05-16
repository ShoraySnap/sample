using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.IO;
using TrudeCommon.Events;

namespace SnaptrudeManagerUI.ViewModels
{
    public class SelectTrudeFileViewModel : ViewModelBase
    {
        ICommand StartImportNavigateCommand { get; set; }
        ICommand BackHomeNavigateCommand { get; set; }
        ICommand SelectTrudeFileCommand { get; set; }
        ICommand IncompatibleNavigateCommand { get; set; }
        public SelectTrudeFileViewModel(NavigationService progressViewNavigationService, NavigationService incompatibleNavigationService, NavigationService backNavigationService)
        {
            StartImportNavigateCommand = new NavigateCommand(progressViewNavigationService);
            IncompatibleNavigateCommand = new NavigateCommand(incompatibleNavigationService);
            BackHomeNavigateCommand = new NavigateCommand(backNavigationService);
            SelectTrudeFileCommand = new RelayCommand(async (o) => await SelectAndParseTrudeFile());
            SelectTrudeFileCommand.Execute(null);
        }
        private async Task SelectAndParseTrudeFile()
        {
            await Task.Delay(5);
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Select Snaptrude File",
                Filter = "Snaptrude Files (*.trude)|*.trude",
                RestoreDirectory = true,
            };
            var dialogOpened = openFileDialog.ShowDialog();
            if (dialogOpened.HasValue && dialogOpened.Value)
            {
                string sourcePath = openFileDialog.FileName;
                TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT, sourcePath, App.TransferManager);
                StartImportNavigateCommand.Execute(null);
            }
            else
            {
                BackHomeNavigateCommand.Execute(null);
            }
        }
    }
}
