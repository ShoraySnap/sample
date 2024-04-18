using ManagerUI.Stores;
using ManagerUI.ViewModels;
using System.Windows;

namespace ManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create the startup window

            NavigationStore navigationStore = NavigationStore.Instance;
            navigationStore.CurrentViewModel = ViewModelCreation.CreateLoginViewModel();
            MainWindow wnd = new MainWindow
            {
                DataContext = new MainWindowViewModel(navigationStore)
            };
            // Show the window
            wnd.Show();
        }
    }
}
