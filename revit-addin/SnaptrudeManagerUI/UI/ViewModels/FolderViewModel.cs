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
        public string Name { get; }
        public FolderViewModel ParentFolder { get; }
        public Constants.WorkspaceType FolderType { get; }
        public string Id { get; }

        public FolderViewModel(Folder folder, FolderViewModel parent = null)
        {
            Id = folder.Id;
            Name = folder.Name;
            ParentFolder = parent;
            FolderType = folder.FolderType;
        }
    }
}
