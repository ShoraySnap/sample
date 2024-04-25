using ManagerUI.Commands;
using ManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        private int progressValue;

        public int ProgressValue
        {
            get { return progressValue; }
            set 
            {
                progressValue = value; 
                OnPropertyChanged("ProgressValue"); 
            }
        }

        ICommand SuccessCommand { get; set; }
        ICommand FailureCommand { get; set; }

        public ProgressViewModel(NavigationService successNavigationService, NavigationService failureNavigationService)
        {
            SuccessCommand = new NavigateCommand(successNavigationService);
            FailureCommand = new NavigateCommand(failureNavigationService);
            Run();
        }

        public ProgressViewModel(NavigationService successNavigationService)
        {
            SuccessCommand = new NavigateCommand(successNavigationService);
            Run();
        }

        private async void Run()
        {
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(50);
                ProgressValue++;
            }
            SuccessCommand.Execute(new object());
        }
    }
}
