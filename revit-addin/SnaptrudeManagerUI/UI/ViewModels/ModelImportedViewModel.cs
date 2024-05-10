using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerUI.ViewModels
{
    public class ModelImportedViewModel : ViewModelBase
    {
        public ModelImportedViewModel()
        {
            MainWindowViewModel.Instance.TopMost = true;
        }
    }
}
