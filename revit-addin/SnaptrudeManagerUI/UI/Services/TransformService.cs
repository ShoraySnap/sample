using SnaptrudeManagerUI.Stores;
using SnaptrudeManagerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.Services
{
    public class TransformService
    {
        private readonly ViewModelBase viewModel;
        private readonly Func<ViewModelBase, ViewModelBase> transformViewModel;

        public TransformService(ViewModelBase viewModel, Func<ViewModelBase, ViewModelBase> transformViewModel)
        {
            this.viewModel = viewModel;
            this.transformViewModel = transformViewModel;
        }

        public void Transform()
        {
            transformViewModel(viewModel);
        }
    }
}
