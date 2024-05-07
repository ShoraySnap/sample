using SnaptrudeManagerAddin.Commands;
using SnaptrudeManagerAddin.Services;
using SnaptrudeManagerAddin.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerAddin.ViewModels
{
    public class ModelExportedViewModel : ViewModelBase
    {
        public ICommand LaunchCommand { get; }
        public ModelExportedViewModel()
        {
            MainWindowViewModel.Instance.TopMost = true;
            LaunchCommand = new RelayCommand(new Action<object>((o) => OpenSnaptrudeModel()));
        }

        private void OpenSnaptrudeModel()
        {
            System.Diagnostics.Process.Start("https://app.snaptrude.com");
            MainWindowViewModel.Instance.CloseCommand.Execute(null);
        }
    }
}
