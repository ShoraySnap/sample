using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static SnaptrudeManagerUI.ViewModels.ProgressViewModel;

namespace SnaptrudeManagerUI.ViewModels
{
    public class ErrorViewModel : ViewModelBase
    {
        private string errorTitle;

        public string ErrorTitle
        {
            get { return errorTitle; }
            set { errorTitle = value; OnPropertyChanged("ErrorTitle"); }
        }

        public ICommand ContactUsCommand { get; }
        public ICommand BackHomeCommand { get; }
        public ErrorViewModel(ProgressViewType type, NavigationService backHomeNavigationService)
        {
            ContactUsCommand = new RelayCommand((o) => Process.Start(new ProcessStartInfo("https://help.snaptrude.com/") { UseShellExecute = true }));
            BackHomeCommand = new NavigateCommand(backHomeNavigationService);
            switch (type)
            {
                case ProgressViewType.ExportProjectNew:
                    ErrorTitle = "Export unsuccessful";
                    break;
                case ProgressViewType.ExportRFANew:
                    ErrorTitle = "Export unsuccessful";
                    break;
                case ProgressViewType.ExportRFAExisting:
                    ErrorTitle = "Export unsuccessful";
                    break;
                case ProgressViewType.Import:
                    ErrorTitle = "Import unsuccessful";
                    break;
                case ProgressViewType.Update:
                    ErrorTitle = "Update unsuccessful";
                    break;
                default:
                    break;
            }
        }
    }
}
