﻿using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;

namespace SnaptrudeManagerUI.ViewModels
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
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://app.snaptrude.com",
                UseShellExecute = true
            });
            App.Current.Shutdown();
        }
    }
}
