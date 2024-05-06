using SnaptrudeManagerAddin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerAddin.Stores
{
    public sealed class NavigationStore
    {
        private NavigationStore() {}

        private static readonly object padlock = new object();
        private static NavigationStore instance = null;
        public static NavigationStore Instance
        {
            get
            {
                lock (padlock)
                    {
                        if (instance == null)
                        {
                            instance = new NavigationStore();
                        }
                        return instance;
                    }
            }
        }

        private ViewModelBase currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get => currentViewModel;
            set
            {
                currentViewModel = value;
                OnCurrentViewModelChanged();
            }
        }

        private void OnCurrentViewModelChanged()
        {
            CurrentViewModelChanged?.Invoke();
        }

        public event Action CurrentViewModelChanged;
    }
}
