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
    public enum WarningId
    {
        AllVisibleParts = 0,
        WillNotReconcile = 1
    }

    public class WarningViewModel : ViewModelBase
    {
        private readonly string IniFilePath = @"C:\ProgramData\Snaptrude\SnaptrudeManager.ini";

        private WarningId WarningId;

        private bool dontShowAgain;

        public bool DontShowAgain
        {
            get { return dontShowAgain; }
            set { dontShowAgain = value; OnPropertyChanged("DontShowAgain"); StoreDontShowAgainValue(); }
        }

        private string warningMessage;

        public string WarningMessage
        {
            get { return warningMessage; }
            set { warningMessage = value; OnPropertyChanged("WarningMessage"); }
        }

        public ICommand BackCommand { get; }
        public ICommand IUnderstandCommand { get; }
        public WarningViewModel(WarningId warningId, NavigationService backHomeNavigationService, NavigationService iUnderstandNavigationService)
        {
            WarningId = warningId;
            IUnderstandCommand = new NavigateCommand(iUnderstandNavigationService);
            BackCommand = new NavigateCommand(backHomeNavigationService);
            switch (warningId)
            {
                case WarningId.AllVisibleParts:
                    WarningMessage = "All the visible parts of the model will export to Snaptrude. Hide or remove any elements that you don’t wish to export before proceeding.";
                    break;
                case WarningId.WillNotReconcile:
                    WarningMessage = "The Revit model will directly export to Snaptrude without being reconciled with the existing model.";
                    break;
                default:
                    break;
            }
        }

        private void StoreDontShowAgainValue()
        {
            var iniFile = new IniFileUtils(IniFilePath);
            iniFile.Write(WarningId.ToString(), DontShowAgain ? "Skip" : "Show", "Warnings");
        }
    }
}
