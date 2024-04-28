using ManagerUI.Commands;
using ManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        public enum ProgressViewType 
        {
            Export,
            Import,
            Update
        }
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

        private Style _dynamicStyle = (Style)Application.Current.FindResource("Style1");
        public Style DynamicStyle
        {
            get { return _dynamicStyle; }
            set
            {
                _dynamicStyle = value;
                OnPropertyChanged("DynamicStyle");
            }

        }

        private bool logoIsVisible = true;

        public bool LogoIsVisible
        {
            get { return logoIsVisible; }
            set
            {
                logoIsVisible = value;
                OnPropertyChanged("LogoIsVisible");
            }
        }

        ICommand StartProgressCommand { get; set; }
        ICommand SuccessCommand { get; set; }
        ICommand FailureCommand { get; set; }

        /// <summary>
        /// ProgressVIew for Revit Export
        /// </summary>
        /// <param name="successNavigationService"></param>
        /// <param name="failureNavigationService"></param>
        public ProgressViewModel(ProgressViewType progressViewType, NavigationService successNavigationService, NavigationService failureNavigationService)
        {
            SuccessCommand = new NavigateCommand(successNavigationService);
            FailureCommand = new NavigateCommand(failureNavigationService);
            switch (progressViewType)
            {
                case ProgressViewType.Export:
                    StartProgressCommand = new RelayCommand(async (o) => await StartExport());
                    break;
                case ProgressViewType.Import:
                    StartProgressCommand = new RelayCommand(async (o) => await StartImport());
                    break;
                case ProgressViewType.Update:
                    LogoIsVisible = false;
                    StartProgressCommand = new RelayCommand(async (o) => await StartUpdate());
                    break;
                default:
                    break;
            }
            StartProgressCommand.Execute(new object());
        }

        private async Task StartExport()
        {
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(50);
                //if (i == 15)
                //{
                //    FailureCommand.Execute(new object());
                //    return;
                //}
            }
            SuccessCommand.Execute(new object());
        }

        private async Task StartImport()
        {
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(50);
            }
            SuccessCommand.Execute(new object());
        }

        private async Task StartUpdate()
        {
            for (int i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                await Task.Delay(50);
            }
            SuccessCommand.Execute(new object());
        }

    }
}
