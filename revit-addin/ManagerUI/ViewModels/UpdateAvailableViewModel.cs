using ManagerUI.Commands;
using ManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class UpdateAvailableViewModel : ViewModelBase
    {
        public ICommand SkipCommand { get; }
        public ICommand UpdateCommand { get; }

        private string updateVersion;

        public string UpdateVersion
        {
            get { return updateVersion; }
            set { updateVersion = value; OnPropertyChanged("UpdateVersion"); }
        }

        public UpdateAvailableViewModel(NavigationService updateNowNavigationService, NavigationService skipUpdateNavigationService) 
        {
            //TO DO: GET UPDATES VERSION
            UpdateVersion = "2.2";

            MainWindowViewModel.Instance.ImageBackground = true;
            SkipCommand = new NavigateCommand(skipUpdateNavigationService);
            UpdateCommand = new UpdateCommand();
        }
    }
}
