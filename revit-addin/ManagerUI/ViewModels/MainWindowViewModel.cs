using ManagerUI.Commands;
using ManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static MainWindowViewModel Instance;

        private readonly NavigationStore navigationStore;

        private bool imageBackground;
        public bool ImageBackground
        {
            get { return imageBackground; }
            set 
            {
                imageBackground = value; OnPropertyChanged("ImageBackground"); 
            }
        }

        public ICommand CloseCommand { get; }

        public ViewModelBase CurrentViewModel => navigationStore.CurrentViewModel;

        public MainWindowViewModel(NavigationStore navigationStore)
        {
            Instance = this;
            navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            this.navigationStore = navigationStore;
            CloseCommand = new CloseCommand();
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }
    }
}
