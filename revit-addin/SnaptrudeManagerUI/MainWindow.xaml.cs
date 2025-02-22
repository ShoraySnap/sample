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
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SnaptrudeManagerUI.Stores;
using SnaptrudeManagerUI.ViewModels;
using TrudeCommon.Events;
using SnaptrudeManagerUI.API;
using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = MainWindowViewModel.Instance;
            InitializeComponent();

            this.Title = "Snaptrude Manager";
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();

            if (e.ChangedButton == MouseButton.Right)
            {
                TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_MAIN_WINDOW_RMOUSE);

            }
        }
    }
}
