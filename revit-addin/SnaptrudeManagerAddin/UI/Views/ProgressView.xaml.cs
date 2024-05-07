using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfAnimatedGif;


namespace SnaptrudeManagerAddin.Views
{
    /// <summary>
    /// Interaction logic for ExportView.xaml
    /// </summary>
    public partial class ProgressView : UserControl
    {
        public event EventHandler OnSuccessfulExport;
        public ProgressView()
        {
            WpfAnimatedGif.ImageAnimationController a;
            InitializeComponent();
        }
        //private void export(object sender, RoutedEventArgs e)
        //{
        //    OnSuccessfulExport?.Invoke(this, e);
        //}
    }
}
