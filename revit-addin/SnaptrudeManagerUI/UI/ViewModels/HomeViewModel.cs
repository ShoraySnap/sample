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
        public bool UpdateAvailable => CurrentVersion != UpdateVersion && !string.IsNullOrEmpty(UpdateVersion);
        public bool UpdateNotAvailable => !UpdateAvailable;


        private bool showInfoText;
        public bool ShowInfoText
        {
            get { return showInfoText; }
            set { showInfoText = value; OnPropertyChanged(nameof(ShowInfoText)); }
        }


        private string infoText;
        public string InfoText
        {
            get { return infoText; }
            set
            {
                infoText = value; OnPropertyChanged("InfoText");
            }
        }

        private string infoColor;
        public string InfoColor
        {
            get { return infoColor; }
            set
            {
                infoColor = value; OnPropertyChanged("InfoColor");
            }
        }
        public void UpdateInfoTextBlock(object param)
        {
            var _param = param as Dictionary<string, string>;
            InfoText = _param["infotext"];
            InfoColor = _param["infocolor"];
            ShowInfoText = string.Equals(_param["showinfo"], "false") ? false : true;
        }

        public bool IsDocumentOpen => MainWindowViewModel.Instance.IsDocumentOpen;
        public bool IsView3D => MainWindowViewModel.Instance.IsView3D;
        public bool IsDocumentRvt => MainWindowViewModel.Instance.IsDocumentRvt;
        public bool IsExportButtonEnable => IsDocumentOpen && IsView3D;
        public bool IsImportButtonEnable => IsDocumentOpen && IsDocumentRvt;

        public HomeViewModel(NavigationService incompatibleTrudeNavigationService, NavigationService labelConfigNavigationService, NavigationService importNavigationService, NavigationService exportNavigationService, NavigationService updateNavigationService)
        {
            MainWindowViewModel.Instance.TopMost = true;
            MainWindowViewModel.Instance.WhiteBackground = true;
            ExportCommand = new NavigateCommand(exportNavigationService);
            ImportCommand = new NavigateCommand(importNavigationService);
            LabelConfigCommand = new NavigateCommand(labelConfigNavigationService);
            UpdateCommand = new NavigateCommand(updateNavigationService);
            SelectTrudeFileCommand = new RelayCommand(async (o) => await SelectAndParseTrudeFile());
            App.OnActivateView2D += SetExportEnablement;
            App.OnActivateView3D += SetExportEnablement;
            App.OnRvtOpened += SetImportEnablement;
            App.OnRfaOpened += SetImportEnablement;
            App.OnDocumentClosed += DisableButtons;
            UpdateButtonText();
        }

        private void SetExportEnablement()
        {
            OnPropertyChanged(nameof(IsExportButtonEnable));
            UpdateButtonText();
        }

        private void SetImportEnablement()
        {
            OnPropertyChanged(nameof(IsImportButtonEnable));
            UpdateButtonText();
        }

        private void DisableButtons()
        {
            OnPropertyChanged(nameof(IsExportButtonEnable));
            OnPropertyChanged(nameof(IsImportButtonEnable));
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            ShowInfoText = !IsDocumentOpen || !IsView3D;
            InfoColor = "#767B93";
            if (!IsDocumentOpen)
                InfoText = "Please open a Revit document to enable the commands.";
            else if (!IsView3D)
                InfoText = "Export is not supported in this view. Switch to 3D view to enable export.";
            else
                InfoText = "";
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

                if ((trudeProperties.Floors.Any() &&
                    trudeProperties.Floors.Where(f => f.RoomType != "Default").Any()) ||
                    (trudeProperties.Masses.Any() &&
                    trudeProperties.Masses.Where(f => f.RoomType != "Default").Any())
                    )
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
        protected override void Dispose(bool disposing)
        {
            bool disposed = false;
            if (!disposed)
            {
                if (disposing)
                {
                    UnsubscribeEvents();
                }
                disposed = true;
            }
        }

        private void UnsubscribeEvents()
        {
            App.OnActivateView2D -= SetExportEnablement;
            App.OnActivateView3D -= SetExportEnablement;
            App.OnRvtOpened -= SetImportEnablement;
            App.OnRfaOpened -= SetImportEnablement;
            App.OnDocumentClosed -= DisableButtons;
        }
        ~HomeViewModel()
        {
            Dispose(false);
        }
    }

    public class TrudeProperties
    {
        [JsonProperty("floors")]
        public List<FloorProperties> Floors { get; set; }
        [JsonProperty("masses")]
        public List<FloorProperties> Masses { get; set; }
    }
    public class FloorProperties
    {
        [JsonProperty("roomType")]
        public string RoomType { get; set; }
    }

    

}
