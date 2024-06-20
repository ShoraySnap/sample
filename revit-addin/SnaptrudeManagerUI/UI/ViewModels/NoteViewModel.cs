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
    public enum NoteId
    {
        AllVisibleParts,
        WillNotReconcile
    }

    public class NoteViewModel : ViewModelBase
    {
        private NoteId NoteId;

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
        public NoteViewModel(NoteId noteId, NavigationService backHomeNavigationService, NavigationService iUnderstandNavigationService)
        {
            NoteId = noteId;
            IUnderstandCommand = new NavigateCommand(iUnderstandNavigationService);
            BackCommand = new NavigateCommand(backHomeNavigationService);
            switch (noteId)
            {
                case NoteId.AllVisibleParts:
                    WarningMessage = "All the visible parts of the model will export to Snaptrude. Hide or remove any elements that you don’t wish to export before proceeding.";
                    break;
                case NoteId.WillNotReconcile:
                    WarningMessage = "The Revit model will directly export to Snaptrude without being reconciled with the existing model.";
                    break;
                default:
                    break;
            }
        }

        private void StoreDontShowAgainValue()
        {
            NavigationStore.Set(NoteId.ToString(), (!DontShowAgain).ToString());
        }
    }
}
