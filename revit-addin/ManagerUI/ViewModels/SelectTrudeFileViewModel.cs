using ManagerUI.Commands;
using ManagerUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class SelectTrudeFileViewModel : ViewModelBase
    {
        ICommand StartImportNavigateCommand {  get; set; }
        ICommand BackHomeNavigateCommand {  get; set; }
        ICommand SelectTrudeFileCommand {  get; set; }
        ICommand IncompatibleNavigateCommand {  get; set; }
        public SelectTrudeFileViewModel(NavigationService progressViewNavigationService, NavigationService incompatibleNavigationService, NavigationService backNavigationService)
        {
            StartImportNavigateCommand = new NavigateCommand(progressViewNavigationService);
            IncompatibleNavigateCommand = new NavigateCommand(incompatibleNavigationService);
            BackHomeNavigateCommand = new NavigateCommand(backNavigationService);
            SelectTrudeFileCommand = new RelayCommand(async (o) => await SelectTrudeFile());
            SelectTrudeFileCommand.Execute(null);
        }
        private async Task SelectTrudeFile()
        {
            await Task.Delay(5);
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Select Snaptrude File",
                Filter = "Snaptrude Files (*.trude)|*.trude",
                RestoreDirectory = true
            };
            if ((bool)openFileDialog.ShowDialog())
            {
                string sourcePath = openFileDialog.FileName;
                //TO DO: INCOMPATIBLE LOGIC
                if (sourcePath.Contains("IncompatibleTrude") && MainWindowViewModel.Instance.CurrentVersion != MainWindowViewModel.Instance.UpdateVersion) 
                    IncompatibleNavigateCommand.Execute(null);
                else
                    StartImportNavigateCommand.Execute(null);
            }
            else
            {
                BackHomeNavigateCommand.Execute(null);
            }
        }
    }
}
