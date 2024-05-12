using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Models;
using SnaptrudeManagerUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnaptrudeManagerUI.ViewModels
{
    public class SelectFolderViewModel : ViewModelBase
    {
        //WPFTODO: CONNECT TO DB
        public List<Folder> Database = new List<Folder>
        {
            new Folder("1", "All Workspaces", "top"),
            new Folder("2", "Personal", "personal", "1"),
            new Folder("3", "Snaptrude Team 1", "shared", "1"),
            new Folder("4", "Snaptrude Team 2", "shared", "1"),
            new Folder("4", "Snaptrude Team 2", "shared", "1"),
            new Folder("4", "Snaptrude Team 2", "shared", "1"),
            new Folder("4", "Snaptrude Team 2", "shared", "1"),
            new Folder("5", "Folder 1", "folder", "2"),
            new Folder("6", "Folder 2", "folder", "2"),
            new Folder("7", "Folder 3", "folder", "2"),
            new Folder("8", "Folder 4", "folder", "2"),
            new Folder("9", "Folder 5", "folder", "2"),
            new Folder("10", "Folder 6", "folder", "2"),
            new Folder("11", "Folder 7", "folder", "2"),
            new Folder("12", "Folder 1", "folder", "3"),
            new Folder("13", "Child Folder", "folder", "5"),
            new Folder("14", "Child Folder", "folder", "13"),
            new Folder("15", "Child Folder", "folder", "14"),
            new Folder("16", "Child Folder", "folder", "15"),
            new Folder("17", "Child Folder", "folder", "16"),
            new Folder("18", "Child Folder", "folder", "17"),
        };

        public ObservableCollection<FolderViewModel> CurrentPathFolders { get; private set; }
        public ObservableCollection<FolderViewModel> Breadcrumb { get; private set; }
        public ICommand OpenFolderCommand { get; private set; }
        public ICommand NavigateToFolderCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }

        private string selectedWorkspaceString;

        public string SelectedWorkspaceString
        {
            get { return selectedWorkspaceString; }
            set 
            {
                selectedWorkspaceString = value;
                OnPropertyChanged("SelectedWorkspaceString");
            }
        }

        private bool isWorkspaceSelected;

        public bool IsWorkspaceSelected
        {
            get { return isWorkspaceSelected; }
            set { isWorkspaceSelected = value; OnPropertyChanged("IsWorkspaceSelected"); OnPropertyChanged("ExportIsEnable"); }
        }

        public bool ExportIsEnable => ViewIs3D && IsWorkspaceSelected;

        public bool ViewIs3D => MainWindowViewModel.Instance.IsActiveView3D;
        public bool ViewIsNot3D => !ViewIs3D;

        private void MainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Instance.IsActiveView3D))
            {
                OnPropertyChanged(nameof(ViewIs3D));
                OnPropertyChanged(nameof(ViewIsNot3D));
                OnPropertyChanged(nameof(ExportIsEnable));
            }
        }
        public SelectFolderViewModel(NavigationService backNavigationService, NavigationService exportNavigationService)
        {
            TransformCommand transformMainWindowViewModelCommand = new TransformCommand(
                new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                {
                    ((MainWindowViewModel)viewmodel).PropertyChanged += MainWindowViewModel_PropertyChanged;
                    return viewmodel;
                }));
            transformMainWindowViewModelCommand.Execute(new object());
            BeginExportCommand = new NavigateCommand(exportNavigationService);
            BackCommand = new NavigateCommand(backNavigationService);
            CurrentPathFolders = new ObservableCollection<FolderViewModel>();
            Breadcrumb = new ObservableCollection<FolderViewModel>();
            OpenFolderCommand = new RelayCommand(OpenFolder);
            NavigateToFolderCommand = new RelayCommand(NavigateToFolder);
            SelectedWorkspaceString = "Select workspace to begin export";
            // Initialize with root folders
            LoadRootFolders();
        }

        private void LoadRootFolders()
        {
            Breadcrumb.Clear();
            //WPFTODO: GET Workspaces
            Folder allWorksSpacesFolder = Database.First(x => x.Id == "1");

            FolderViewModel allWorksSpacesFolderViewModel = new FolderViewModel(allWorksSpacesFolder);
            Breadcrumb.Add(allWorksSpacesFolderViewModel);
            /*await*/
            GetSubFoldersAsync(allWorksSpacesFolderViewModel);
        }

        private void GetSubFoldersAsync(FolderViewModel parentFolder)
        {
            //WPFTODO: GET subfolders
            var subFolders = Database.Where(x => x.ParentId == parentFolder.Id).ToList();
            var subFolderViewModels = new List<FolderViewModel>();
            foreach (var subFolder in subFolders)
            {
                subFolderViewModels.Add(new FolderViewModel(subFolder, parentFolder));
            }

            PopulateSubFolders(subFolderViewModels);
        }

        private void PopulateSubFolders(List<FolderViewModel> subFolders)
        {
            foreach (var subFolder in subFolders)
            {
                CurrentPathFolders.Add(subFolder);
            }
        }

        private void OpenFolder(object parameter)
        {
            if (parameter is FolderViewModel folder)
            {
                CurrentPathFolders.Clear();
                foreach (var subFolder in Database.Where(x => x.ParentId == folder.Id))
                {
                    CurrentPathFolders.Add(new FolderViewModel(subFolder, folder));
                }
                Breadcrumb.Add(folder);
            }
            SetStringAndButtonEnable();
        }

        private void NavigateToFolder(object parameter)
        {
            if (parameter is FolderViewModel folder)
            {
                // Update the list of current path folders
                CurrentPathFolders.Clear();
                GetSubFoldersAsync(folder);

                // Update the breadcrumb
                UpdateBreadcrumb(folder);
            }
            SetStringAndButtonEnable();
        }

        private void SetStringAndButtonEnable()
        {
            if (Breadcrumb.Count == 1)
            {
                SelectedWorkspaceString = "Select workspace to begin export";
                IsWorkspaceSelected = false;
            }
            else
            {
                SelectedWorkspaceString = $"Workspace: {Breadcrumb[1].Name}";
                IsWorkspaceSelected = true;
            }
        }

        private void UpdateBreadcrumb(FolderViewModel folder)
        {
            Breadcrumb.Clear();
            var currentFolder = folder;

            while (currentFolder != null)
            {
                Breadcrumb.Insert(0, currentFolder);
                currentFolder = currentFolder.ParentFolder;
            }
        }
    }
}
