using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ManagerUI.Views;

namespace ManagerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        public static ImageBrush BackgroundImage;
        public static LoginView LoginView = new LoginView();
        public static HomeView HomeView = new HomeView();
        public static ExportView ExportView = new ExportView();
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            //BackgroundImage = new ImageBrush();
            //BackgroundImage.ImageSource = new BitmapImage(new Uri("/Images/Snaptrude Manager.png", UriKind.Relative));
            //this.Background = BackgroundImage;
            // check for login logic here and set view accordingly
            ActiveItem.Content = new LoginView();
        }

        private void closeManager(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
