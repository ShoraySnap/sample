using SnaptrudeManagerUI.Stores;
using SnaptrudeManagerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.UI.Services
{
    public class TransformService
    {
        private readonly NavigationStore navigationStore;
        private readonly Func<ViewModelBase, ViewModelBase> transformViewModel;

        public TransformService(NavigationStore navigationStore, Func<ViewModelBase, ViewModelBase> transformViewModel)
        {
            this.navigationStore = navigationStore;
            this.transformViewModel = transformViewModel;
        }

        public void Transform()
        {
            navigationStore.CurrentViewModel = transformViewModel(navigationStore.CurrentViewModel);
        }
    }
}
