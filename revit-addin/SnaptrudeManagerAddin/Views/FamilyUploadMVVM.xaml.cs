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

        public WindowViewModel WindowViewModel;
        
        public FamilyUploadMVVM()
        {
            InitializeComponent();

            WindowViewModel = new WindowViewModel();
            DataContext = WindowViewModel;
            InitializeCommands();
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
            WindowViewModel._skipAll = true;
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
            WindowViewModel.RefreshFilteredView();
        }
        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            TabItem nextTabItem = TabControl.Items.GetItemAt(0) as TabItem;
            TabControl.SelectedItem = nextTabItem;
        }
        private void Select_File_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            WindowViewModel.ChooseFile(button.DataContext as MissingFamilyViewModel);
        }
        private void Select_Folder_Click(object sender, RoutedEventArgs e)
        {
            WindowViewModel.LinkFolder();
        }
        private void Done_Button_Click(object sender, RoutedEventArgs e)
        {
            WindowViewModel._skipAll = false;
            Dispose();
        }
        
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WindowViewModel.UpdateSelectedCount();
        }
    }



    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WindowViewModel : ViewModelBase
    {

        public ListCollectionView FilteredMissingFamilies { get; set; }
        public ListCollectionView CompleteListMissingFamilyViewModels { get; set; }
        public ObservableCollection<MissingFamilyViewModel> MissingFamilyViewModels { get; set; }

        public IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> missingDoorFamiliesCount = TrudeImporter.GlobalVariables.MissingDoorFamiliesCount;
        public IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> missingWindowFamiliesCount = TrudeImporter.GlobalVariables.MissingWindowFamiliesCount;

        private bool _isAllChecked = true;
        public bool _skipAll = false;
        public string MissingFamiliesFolderPath { get; set; }

        public bool DoneButtonIsEnabled
        {
            get => TotalSelected == LinkedCount;
        }

        public bool LinkButtonIsEnabled
        {
            get => TotalSelected != 0;
        }

        public int TotalMissing
        {
            get => (missingDoorFamiliesCount.Values.Count(x => x.IsChecked) + missingWindowFamiliesCount.Values.Count(x => x.IsChecked));
        }

        private int totalSelected;
        public int TotalSelected
        {
            get { return totalSelected; }
            set 
            { 
                totalSelected = value; 
                OnPropertyChanged(nameof(TotalSelected));
                OnPropertyChanged(nameof(MissingString));
                OnPropertyChanged(nameof(LinkButtonIsEnabled));
            }
        }

        public int LinkedCount
        {
            get => MissingFamilyViewModels.Where(vm => vm.IsChecked).Count(vm => vm.IsLinked);
        }

        public string MissingString
        {
            get => $"Linked {LinkedCount} of {TotalSelected}";
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

                    // Prevent recursive update when setting IsAllChecked programmatically.
                    if (!_updatingCheckState)
                        UpdateCheckStateForAll(_isAllChecked);
                }
            }
        }
        private bool _updatingCheckState = false;

        public WindowViewModel()
        {
            MissingFamilyViewModels = new ObservableCollection<MissingFamilyViewModel>();
            FilteredMissingFamilies = new ListCollectionView(MissingFamilyViewModels);
            CompleteListMissingFamilyViewModels = new ListCollectionView(MissingFamilyViewModels);
            LoadMissingFamilies();
            ApplyFilter(FilteredMissingFamilies);
        }

        private void ApplyFilter(ICollectionView listCollectionView)
        {
            // Apply filter
            if (listCollectionView != null)
            {
                listCollectionView.Filter += (e) =>
                {
                    if (e is MissingFamilyViewModel item)
                    {
                        if (item.IsChecked) return true;
                        else return false;
                    }
                    else return false;
                };
                // Optionally refresh to immediately apply
                listCollectionView.Refresh();
            }
        }

        public void RefreshFilteredView()
        {
            foreach (var item in MissingFamilyViewModels)
            {
                if (missingDoorFamiliesCount.ContainsKey(item.FamilyName))
                {
                    missingDoorFamiliesCount[item.FamilyName] = (item.IsChecked, missingDoorFamiliesCount[item.FamilyName].NumberOfElements, "");
                }
                if (missingWindowFamiliesCount.ContainsKey(item.FamilyName))
                {
                    missingWindowFamiliesCount[item.FamilyName] = (item.IsChecked, missingWindowFamiliesCount[item.FamilyName].NumberOfElements, "");
                }
                System.Diagnostics.Debug.WriteLine("MissingFamilyViewModels: " + item.FamilyName + " " + item.IsChecked + " " + item.FamilyPath);
            }
            FilteredMissingFamilies?.Refresh();
            OnPropertyChanged(nameof(DoneButtonIsEnabled));
        }
        public void UpdateCheckStateForAll(bool isChecked)
        {
            foreach (var viewModel in MissingFamilyViewModels)
            {
                viewModel.IsChecked = isChecked;
            }
            UpdateSelectedCount();
        }

        public void UpdateSelectedCount()
        {
            TotalSelected = MissingFamilyViewModels.Count(x => x.IsChecked);

            // Check if all items are checked and update IsAllChecked accordingly.
            var areAllItemsChecked = MissingFamilyViewModels.All(vm => vm.IsChecked);
            _updatingCheckState = true; // Prevent recursion
            IsAllChecked = areAllItemsChecked;
            _updatingCheckState = false;
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
                MissingFamilyViewModels.First(x => x.FamilyName == key).FamilyPath = Path.Combine(folderPath, key + ".rfa");
                familyDict[key] = (value.IsChecked, value.NumberOfElements, Path.Combine(folderPath, key + ".rfa"));
            }
        }
        public void LinkFolder()
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
            OnPropertyChanged(nameof(MissingString));
            OnPropertyChanged(nameof(DoneButtonIsEnabled));
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

        public void ChooseFile(MissingFamilyViewModel missingFamilyViewModel)
        {
            var familyName = missingFamilyViewModel.FamilyName;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select Family File For " + familyName;
            openFileDialog.Filter = "Revit Families (*.rfa)|*.rfa";
            openFileDialog.RestoreDirectory = true;
            bool? uploadResult = openFileDialog.ShowDialog();
            if (uploadResult == true)
            {
                string sourcePath = openFileDialog.FileName;
                missingFamilyViewModel.FamilyPath = sourcePath;
                if (missingDoorFamiliesCount.ContainsKey(familyName))
                {
                    missingDoorFamiliesCount[familyName] = (true, missingDoorFamiliesCount[familyName].NumberOfElements, sourcePath);
                }
                if (missingWindowFamiliesCount.ContainsKey(familyName))
                {
                    missingWindowFamiliesCount[familyName] = (true, missingWindowFamiliesCount[familyName].NumberOfElements, sourcePath);
                }
            }
            OnPropertyChanged(nameof(MissingString));
            OnPropertyChanged(nameof(DoneButtonIsEnabled));
        }
    }

    public class MissingFamilyViewModel : ViewModelBase
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
            set { _familyPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsLinked)); }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
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

    }

}