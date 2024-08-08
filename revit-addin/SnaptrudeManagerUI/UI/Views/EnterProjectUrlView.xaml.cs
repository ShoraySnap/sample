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

namespace SnaptrudeManagerUI.Views
{
    /// <summary>
    /// Interaction logic for WarningView.xaml
    /// </summary>
    public partial class EnterProjectUrlView : UserControl
    {
        public EnterProjectUrlView()
        {
            InitializeComponent();
        }

        private void ClearValue_Button_Click(object sender, RoutedEventArgs e)
        {
            URLTextBox.Focus();
        }

        private void EnterProjectUrlView_Loaded(object sender, RoutedEventArgs e)
        {
            URLTextBox.Focus();
        }
    }
}
