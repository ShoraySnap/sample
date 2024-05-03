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


            //WPFTODO: CHECKFORUPDATES
            NavigationStore navigationStore = NavigationStore.Instance;

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(navigationStore, "2.1.1", "2.2.1");

            // Create initial ViewModel
            navigationStore.CurrentViewModel = ViewModelCreater.CreateUpdateAvailableViewModel();

            MainWindow wnd = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            // Show the window
            wnd.Show();
        }
    }
}
