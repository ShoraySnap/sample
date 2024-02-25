using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SnaptrudeManagerAddin
{
    public partial class FamilyUploadMVVM : Window, IDisposable
    {
        private bool _isAllChecked = true;
        public ObservableCollection<MissingFamilyViewModel> MissingFamilyViewModels { get; set; } = new ObservableCollection<MissingFamilyViewModel>();

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

            // Initialize your ViewModel collection here based on actual missing families from Revit.
            LoadMissingFamilies();
        }

        public void LoadMissingFamilies()
        {
            MissingFamilyViewModels.Clear();
            foreach (var item in missingDoorFamiliesCount)
            {
                MissingFamilyViewModels.Add(new MissingFamilyViewModel
                {
                    FamilyName = item.Key,
                    FamilyPath = item.Value.path,
                    IsChecked = item.Value.IsChecked
                });
            }

            foreach (var item in missingWindowFamiliesCount)
            {
                MissingFamilyViewModels.Add(new MissingFamilyViewModel
                {
                    FamilyName = item.Key,
                    FamilyPath = item.Value.path,
                    IsChecked = item.Value.IsChecked
                });
            }
            OnPropertyChanged(nameof(TotalMissing));
        }

        //Update the Number of missing families
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
        
        public void EvaluateIsAllChecked()
        {
            _isAllChecked = MissingFamilyViewModels.All(x => x.IsChecked);
            OnPropertyChanged(nameof(IsAllChecked));
        }

        private void UpdateCheckStateForAll(bool isChecked)
        {
            foreach (var key in missingDoorFamiliesCount.Keys.ToList())
            {
                var item = missingDoorFamiliesCount[key];
                missingDoorFamiliesCount[key] = (isChecked, item.NumberOfElements, item.path);
            }

            foreach (var key in missingWindowFamiliesCount.Keys.ToList())
            {
                var item = missingWindowFamiliesCount[key];
                missingWindowFamiliesCount[key] = (isChecked, item.NumberOfElements, item.path);
            }

            foreach (var viewModel in MissingFamilyViewModels)
            {
                viewModel.IsChecked = isChecked;
            }

            // Ensure TotalMissing is also updated.
            OnPropertyChanged(nameof(TotalMissing));
        }

        public void IndividualCheckboxChanged()
        {
            var allDoorsAreSameState = missingDoorFamiliesCount.Values.All(x => x.IsChecked == _isAllChecked);
            var allWindowsAreSameState = missingWindowFamiliesCount.Values.All(x => x.IsChecked == _isAllChecked);

            if (!allDoorsAreSameState || !allWindowsAreSameState)
            {
                _isAllChecked = false;
                OnPropertyChanged(nameof(IsAllChecked));
            }
            else if (allDoorsAreSameState && allWindowsAreSameState && !_isAllChecked) // If everything is manually checked but top isn't updated yet.
            {
                _isAllChecked = true;
                OnPropertyChanged(nameof(IsAllChecked));
            }
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
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
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
        }

        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            TabItem nextTabItem = TabControl.Items.GetItemAt(0) as TabItem;
            TabControl.SelectedItem = nextTabItem;
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
                _isChecked = value; 
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine("IsChecked: " + _isChecked);
                //FamilyUploadMVVM.EvaluateIsAllChecked();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}