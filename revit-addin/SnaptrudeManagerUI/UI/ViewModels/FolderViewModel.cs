using SnaptrudeManagerUI.API;
using SnaptrudeManagerUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SnaptrudeManagerUI.ViewModels
{
    public class FolderViewModel : ViewModelBase
    {
        public string Name { get; set; }
        public Constants.WorkspaceType FolderType { get; }
        public string Id { get; }
        public string TeamId { get; }
        private bool selected;
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }

        public FolderViewModel(Folder folder)
        {
            Id = folder.Id;
            Name = folder.Name;
            FolderType = folder.FolderType;
            TeamId = folder.TeamId;
            Selected = false;
        }
    }
}
