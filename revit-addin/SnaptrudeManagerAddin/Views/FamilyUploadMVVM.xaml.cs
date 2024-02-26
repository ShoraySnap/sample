using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace SnaptrudeManagerAddin
{
    public partial class FamilyUploadMVVM : Window, IDisposable
    {
        private bool _isAllChecked = true;
        public bool _skipAll = false;
        public ObservableCollection<MissingFamilyViewModel> MissingFamilyViewModels { get; set; } = new ObservableCollection<MissingFamilyViewModel>();
        public string MissingFamiliesFolderPath { get; set; }

        public IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> missingDoorFamiliesCount = TrudeImporter.GlobalVariables.MissingDoorFamiliesCount;
        public IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> missingWindowFamiliesCount = TrudeImporter.GlobalVariables.MissingWindowFamiliesCount;
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FamilyUploadMVVM()
        {
            DataContext = this;
            InitializeComponent();
            //InitializeCommands();
            LoadMissingFamilies();
            // Setup filtered view source from resources
            var filteredViewSource = this.Resources["FilteredMissingFamilies"] as CollectionViewSource;

            // Apply filter
            if (filteredViewSource != null)
            {
                filteredViewSource.Filter += (s, e) =>
                {
                    if (e.Item is MissingFamilyViewModel item)
                    {
                        e.Accepted = item.IsChecked;
                    }
                };

                // Optionally refresh to immediately apply
                filteredViewSource.View.Refresh();
            }
        }

        public void LoadMissingFamilies()
        {
            MissingFamilyViewModels.Clear();
            foreach (var item in missingDoorFamiliesCount)
            {
                var viewModel = new MissingFamilyViewModel
                {
                    FamilyName = item.Key,
                    FamilyPath = item.Value.path,
                    IsChecked = item.Value.IsChecked
                };

                MissingFamilyViewModels.Add(viewModel);
            }

            foreach (var item in missingWindowFamiliesCount)
            {
                var viewModel = new MissingFamilyViewModel
                {
                    FamilyName = item.Key,
                    FamilyPath = item.Value.path,
                    IsChecked = item.Value.IsChecked
                };

                MissingFamilyViewModels.Add(viewModel);
            }
            OnPropertyChanged(nameof(TotalMissing));
        }
        public int TotalMissing
        {
            get { return missingDoorFamiliesCount.Values.Count(x => x.IsChecked) + missingWindowFamiliesCount.Values.Count(x => x.IsChecked); }
        }
        public string TrudeFileName
        {
            get
            {
                return TrudeImporter.GlobalVariables.TrudeFileName;
                
            }
        }
        public bool IsAllChecked
        {
            get { return _isAllChecked; }
            set
            {
                if (_isAllChecked != value)
                {
                    _isAllChecked = value;
                    OnPropertyChanged(nameof(IsAllChecked));
                    System.Diagnostics.Debug.WriteLine("IsAllChecked: " + _isAllChecked);
                    UpdateCheckStateForAll(_isAllChecked);
                }
            }
        }
        private void UpdateCheckStateForAll(bool isChecked)
        {
            foreach (var viewModel in MissingFamilyViewModels)
            {
                viewModel.IsChecked = isChecked;
            }

            // Ensure TotalMissing is also updated.
            OnPropertyChanged(nameof(TotalMissing));
        }
        private void InitializeCommands()
        {
            this.ShowInTaskbar = true;
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void Skip_Button_Click(object sender, RoutedEventArgs e)
        {
            _skipAll = true;
            Dispose();
            Close();
        }
        private void Minimize_Button_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        public void Dispose()
        {
            this.Close();
        }
        private void Link_Button_Click(object sender, RoutedEventArgs e)
        {
            TabItem nextTabItem = TabControl.Items.GetItemAt(1) as TabItem;
            TabControl.SelectedItem = nextTabItem;

            foreach (var item in MissingFamilyViewModels)
            {

                System.Diagnostics.Debug.WriteLine("MissingFamilyViewModels: " + item.FamilyName + " " + item.IsChecked + " " + item.FamilyPath);
                if (missingDoorFamiliesCount.ContainsKey(item.FamilyName))
                {
                    missingDoorFamiliesCount[item.FamilyName] = (item.IsChecked, missingDoorFamiliesCount[item.FamilyName].NumberOfElements, "");
                }
                if (missingWindowFamiliesCount.ContainsKey(item.FamilyName))
                {
                    missingWindowFamiliesCount[item.FamilyName] = (item.IsChecked, missingWindowFamiliesCount[item.FamilyName].NumberOfElements, "");
                }
            }
            RefreshFilteredView();
        }
        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            TabItem nextTabItem = TabControl.Items.GetItemAt(0) as TabItem;
            TabControl.SelectedItem = nextTabItem;
        }
        private void Select_File_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var item = button.DataContext as MissingFamilyViewModel;
            var familyName = item.FamilyName;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select Family File For " + familyName;
            openFileDialog.Filter = "Revit Families (*.rfa)|*.rfa";
            openFileDialog.RestoreDirectory = true;
            bool? uploadResult = openFileDialog.ShowDialog();
            if (uploadResult == true)
            {
                string sourcePath = openFileDialog.FileName;
                if (missingDoorFamiliesCount.ContainsKey(familyName))
                {
                    missingDoorFamiliesCount[familyName] = (true, missingDoorFamiliesCount[familyName].NumberOfElements, sourcePath);
                }
                if (missingWindowFamiliesCount.ContainsKey(familyName))
                {
                    missingWindowFamiliesCount[familyName] = (true, missingWindowFamiliesCount[familyName].NumberOfElements, sourcePath);
                }
            }
        }   
        private void Select_Folder_Click (object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select Folder Containing RFAs";
            folderBrowserDialog.ShowNewFolderButton = false;
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                MissingFamiliesFolderPath = folderBrowserDialog.SelectedPath;
                AutomaticLinking();
            }
        }
        private void Done_Button_Click(object sender, RoutedEventArgs e)
        {
            _skipAll = false;
            Dispose();
        }
        public void RefreshFilteredView()
        {
            var filteredView = this.Resources["FilteredMissingFamilies"] as CollectionViewSource;
            filteredView?.View.Refresh();
        }

        public void AutomaticLinking()
        {
            System.Diagnostics.Debug.WriteLine("Automatic Linking");
            TryLinkFamilyFiles(missingDoorFamiliesCount);
            TryLinkFamilyFiles(missingWindowFamiliesCount);
        }

        public void TryLinkFamilyFiles(IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> familyDict)
        {
            string folderPath = MissingFamiliesFolderPath;
            List<string> keysToUpdate = new List<string>();

            foreach (var item in familyDict)
            {
                if (!item.Value.IsChecked) continue; // Skip unchecked items

                string expectedFileName = item.Key + ".rfa";
                string fullPath = Path.Combine(folderPath, expectedFileName);

                if (File.Exists(fullPath))
                {
                    keysToUpdate.Add(item.Key);
                }
            }

            // Update dictionary entries with found paths
            foreach (var key in keysToUpdate)
            {
                var value = familyDict[key];
                familyDict[key] = (value.IsChecked, value.NumberOfElements, Path.Combine(folderPath, key + ".rfa"));
            }
        }
    }

    public class MissingFamilyViewModel : INotifyPropertyChanged
    {
        private string _familyName;
        private string _familyPath;
        private bool _isChecked;
        public string FamilyName
        {
            get => _familyName;
            set { _familyName = value; OnPropertyChanged(); }
        }

        public string FamilyPath
        {
            get => _familyPath;
            set { _familyPath = value; OnPropertyChanged(); }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLinked
        {
            get => !string.IsNullOrEmpty(FamilyPath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}