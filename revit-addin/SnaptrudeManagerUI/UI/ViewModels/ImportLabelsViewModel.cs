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
    public class ImportLabelsViewModel : ViewModelBase
    {
        public ICommand StartImportNavigateCommand { get; set; }
        public ICommand ImportWithLabelsCommand { get; set; }
        public ICommand ImportWithoutLabelsCommand { get; set; }
        public ImportLabelsViewModel(NavigationService progressViewNavigationService)
        {
            StartImportNavigateCommand = new NavigateCommand(progressViewNavigationService);
            ImportWithLabelsCommand = new RelayCommand(async (o) => await StartImport(true));
            ImportWithoutLabelsCommand = new RelayCommand(async (o) => await StartImport(false));
        }
        private async Task StartImport(bool withLabels)
        {
            await Task.Delay(5);
            string sourcePath = MainWindowViewModel.Instance.ImportPath;
            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT, $"{sourcePath};{withLabels}", App.TransferManager);
            StartImportNavigateCommand.Execute(null);
        }
    }
}
