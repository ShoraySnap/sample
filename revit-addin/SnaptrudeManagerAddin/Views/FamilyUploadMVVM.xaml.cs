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
        public ObservableCollection<MissingFamilyViewModel> MissingFamilyViewModels { get; set; } = new ObservableCollection<MissingFamilyViewModel>();
        public FamilyUploadMVVM()
        {
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 2", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 3", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 4", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 5", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 6", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 7", IsChecked = true });
            MissingFamilyViewModels.Add(new MissingFamilyViewModel() { FamilyName = "Teste 8", IsChecked = true });
            InitializeCommands();
            InitializeComponent();
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
        private string familyName;
        public string FamilyName
        {
            get { return familyName; }
            set
            {
                familyName = value;
                OnPropertyChanged();
            }
        }

        private string familyPath;
        public string FamilyPath
        {
            get { return familyPath; }
            set
            {
                familyPath = value;
                OnPropertyChanged();
            }
        }

        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged();
            }
        }

        public string MissingFamilyString = "Missing from model";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}