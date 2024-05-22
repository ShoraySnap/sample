using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TrudeCommon.Events;

namespace SnaptrudeManagerUI.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand LabelConfigCommand { get; }
        public ICommand SelectTrudeFileCommand { get; }
        public ICommand UpdateCommand { get; }

        public string CurrentVersion => MainWindowViewModel.Instance.CurrentVersion;
        public string UpdateVersion => MainWindowViewModel.Instance.UpdateVersion;
        public bool UpdateAvailable => CurrentVersion != UpdateVersion;
        public bool ViewIs3D => MainWindowViewModel.Instance.IsActiveView3D;
        public bool ViewIsNot3D => !ViewIs3D;

        private void MainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Instance.IsActiveView3D))
            {
                OnPropertyChanged(nameof(ViewIs3D));
                OnPropertyChanged(nameof(ViewIsNot3D));
            }
        }

        public HomeViewModel(NavigationService incompatibleTrudeNavigationService, NavigationService labelConfigNavigationService, NavigationService importNavigationService, NavigationService exportNavigationService, NavigationService updateNavigationService)
        {
            MainWindowViewModel.Instance.TopMost = true;
            MainWindowViewModel.Instance.PropertyChanged += MainWindowViewModel_PropertyChanged;
            MainWindowViewModel.Instance.WhiteBackground = true;
            ExportCommand = new NavigateCommand(exportNavigationService);
            ImportCommand = new NavigateCommand(importNavigationService);
            LabelConfigCommand = new NavigateCommand(labelConfigNavigationService);
            UpdateCommand = new NavigateCommand(updateNavigationService);
            SelectTrudeFileCommand = new RelayCommand(async (o) => await SelectAndParseTrudeFile());
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

                JsonSerializer serializer = new JsonSerializer()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                };
                JObject trudeData = JObject.Parse(File.ReadAllText(sourcePath));
                TrudeProperties trudeProperties = trudeData.ToObject<TrudeProperties>(serializer);

                if (trudeProperties.Floors.Any() && trudeProperties.Floors.Where(f => f.RoomType != "Default").Any())
                {
                    MainWindowViewModel.Instance.ImportPath = sourcePath;
                    LabelConfigCommand.Execute(null);
                }
                else
                {
                    TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.MANAGER_UI_REQ_IMPORT_TO_REVIT, $"{sourcePath};false", App.TransferManager);
                    ImportCommand.Execute(null);
                }
            }
        }
    }

    public class TrudeProperties
    {
        [JsonProperty("floors")]
        public List<FloorProperties> Floors { get; set; }
    }
    public class FloorProperties
    {
        [JsonProperty("roomType")]
        public string RoomType { get; set; }
    }

}
