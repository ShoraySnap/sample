using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Models;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.API;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using NLog;
using System.Windows.Data;
using System.Collections.Generic;
using System;

namespace SnaptrudeManagerUI.ViewModels
{
    public class SelectFolderViewModel : ViewModelBase
    {
        private bool disposed = false; 

        static Logger logger = LogManager.GetCurrentClassLogger();
        public ObservableCollection<FolderViewModel> CurrentPathFolders { get; private set; }
        public ListCollectionView CurrentPathFoldersView { get; private set; }
        public ObservableCollection<FolderViewModel> Breadcrumb { get; private set; }
        public ICommand OpenFolderCommand { get; private set; }
        public ICommand NavigateToFolderCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand TryAgainCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }

        private string team_id = "";
        private string folder_id = "";

        public bool IsBreadcrumbEnabled => !IsLoaderVisible;

        private bool isLoaderVisible = true;
        public bool IsLoaderVisible
        {
            get { return isLoaderVisible; }
            set { isLoaderVisible = value; OnPropertyChanged("IsLoaderVisible"); OnPropertyChanged("IsBreadcrumbEnabled"); OnPropertyChanged("ExportIsEnabled"); OnPropertyChanged("ExportIsDisbled"); }
        }

        private string infoMessage = !MainWindowViewModel.Instance.IsView3D ? "Switch to 3D view to enable export" : "Select workspace to begin export";
        public string InfoMessage
        {
            get { return infoMessage; }
            set { infoMessage = value; OnPropertyChanged("InfoMessage"); }
        }

        private bool addBreadcrumbs = true;

        private bool isWorkspaceSelected;

        public bool IsWorkspaceSelected
        {
            get { return isWorkspaceSelected; }
            set { isWorkspaceSelected = value; OnPropertyChanged("IsWorkspaceSelected"); OnPropertyChanged("ExportIsEnabled"); OnPropertyChanged("ExportIsDisabled"); }
        }

        public bool ExportIsEnabled => ViewIs3D && IsWorkspaceSelected && !IsLoaderVisible;
        public bool ExportIsDisabled => ViewIsNot3D || !IsWorkspaceSelected;

        public bool ViewIs3D => MainWindowViewModel.Instance.IsView3D;

        public bool ViewIsNot3D => !ViewIs3D;

        private FolderViewModel AllWorkspacesViewModel = new FolderViewModel(new Folder("-1", "All Workspaces", Constants.WorkspaceType.Top, "-1"));

        public SelectFolderViewModel(NavigationService backNavigationService, NavigationService exportNavigationService, NavigationService tryAgainNavigationService)
        {
            BeginExportCommand = new RelayCommand((o) => { BeginExport(o, exportNavigationService); });
            BackCommand = new NavigateCommand(backNavigationService);
            TryAgainCommand = new NavigateCommand(tryAgainNavigationService);
            CurrentPathFolders = new ObservableCollection<FolderViewModel>();
            CurrentPathFoldersView = new ListCollectionView(CurrentPathFolders);
            CurrentPathFoldersView.SortDescriptions.Add(new SortDescription(nameof(FolderViewModel.FolderType), ListSortDirection.Ascending));
            CurrentPathFoldersView.SortDescriptions.Add(new SortDescription(nameof(FolderViewModel.Name), ListSortDirection.Ascending));
            Breadcrumb = new ObservableCollection<FolderViewModel>();
            OpenFolderCommand = new RelayCommand(GetSubFoldersAsync);
            NavigateToFolderCommand = new RelayCommand(NavigateToFolder);
            
            App.OnActivateView2D += SetExportEnablement;
            App.OnActivateView3D += SetExportEnablement;
            // Initialize with root folders
            LoadRootFolders();
            SetExportEnablement();
        }

        private void SetExportEnablement()
        {
            OnPropertyChanged(nameof(ViewIs3D));
            OnPropertyChanged(nameof(ViewIsNot3D));
            OnPropertyChanged(nameof(ExportIsEnabled));
            OnPropertyChanged(nameof(ExportIsDisabled));
            updateInfoMessage();
        }

        private void BeginExport(object param, NavigationService exportNavigationService)
        {
            Store.Set("team_id", team_id);
            Store.Set("folder_id", folder_id);
            Store.Save();

            var navCmd = new NavigateCommand(exportNavigationService);
            navCmd.Execute(param);
        }

        private async void LoadRootFolders()
        {
            Breadcrumb.Clear();
            GetSubFoldersAsync(AllWorkspacesViewModel);
        }

        private async void GetSubFoldersAsync(object parameter)
        {
            try
            {
                if (parameter is FolderViewModel parentFolder)
                {
                    parentFolder.Selected = true;
                    IsLoaderVisible = true;

                    List<Folder> subFolders = new List<Folder>();
                    var subFolderViewModels = new List<FolderViewModel>();
                    if (parentFolder.FolderType == Constants.WorkspaceType.Top)
                    {
                        subFolders = await SnaptrudeRepo.GetUserWorkspacesAsync();
                        subFolderViewModels = new List<FolderViewModel>();
                        foreach (var subFolder in subFolders)
                        {
                            subFolderViewModels.Add(new FolderViewModel(subFolder));
                        }
                    }
                    else
                    {
                        CurrentPathFolders.Clear();
                        subFolders = await SnaptrudeRepo.GetSubFoldersAsync(parentFolder);
                        subFolderViewModels = new List<FolderViewModel>();
                        foreach (var subFolder in subFolders)
                        {
                            subFolderViewModels.Add(new FolderViewModel(subFolder));
                        }
                        team_id = parentFolder.TeamId;
                        folder_id = parentFolder.FolderType == Constants.WorkspaceType.Folder ? parentFolder.Id : "root";
                    }
                    PopulateSubFolders(subFolderViewModels);
                    if (addBreadcrumbs == true)
                    {
                        parentFolder.Name = parentFolder.Name.Replace("_", "__"); //https://www.charlespetzold.com/blog/2006/01/061004.html
                        Breadcrumb.Add(parentFolder);
                        setExportButton();
                    }
                    addBreadcrumbs = true;
                    IsLoaderVisible = false;
                    updateInfoMessage();
                }
                SetSelectedFolder();
            }
            catch (Exception ex)
            {
                if (parameter is FolderViewModel parentFolder)
                {
                    logger.Error(parentFolder.Id + " " + parentFolder.Name + " " + parentFolder.FolderType.ToString() + " " + parentFolder.TeamId);
                }
                logger.Error(ex.Message);
                TryAgainCommand.Execute(null);
            }
        }

        private void PopulateSubFolders(List<FolderViewModel> subFolders)
        {
            foreach (var subFolder in subFolders)
            {
                CurrentPathFolders.Add(subFolder);
            }
        }


        private void NavigateToFolder(object parameter)
        {
            if (parameter is FolderViewModel folder)
            {
                // Update the list of current path folders
                CurrentPathFolders.Clear();
                UpdateBreadcrumb(folder);
                addBreadcrumbs = false;
                GetSubFoldersAsync(folder);
                // Update the breadcrumb
            }
            setExportButton();
        }

        private void setExportButton()
        {
            IsWorkspaceSelected = Breadcrumb.Count > 1;
            OnPropertyChanged(nameof(ExportIsEnabled));
        }

        private void SetSelectedFolder()
        {
            for (int i = 0; i < Breadcrumb.Count; i++)
            {
                Breadcrumb[i].Selected = i == Breadcrumb.Count - 1;
            }
        }

        private void UpdateBreadcrumb(FolderViewModel folder)
        {
            for (int i = Breadcrumb.Count - 1; i >= 0; i--)
            {
                var topBreadcrumb = Breadcrumb[i];
                if (topBreadcrumb.Id == folder.Id && topBreadcrumb.FolderType == folder.FolderType)
                {
                    break;
                }
                Breadcrumb.RemoveAt(i);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    UnsubscribeEvents();
                }
                disposed = true;
            }
        }

        private void UnsubscribeEvents()
        {
            App.OnActivateView2D -= SetExportEnablement;
            App.OnActivateView3D -= SetExportEnablement;
        }
        ~SelectFolderViewModel()
        {
            Dispose(false);
        }

        private void updateInfoMessage()
        {
            InfoMessage =
                !MainWindowViewModel.Instance.IsView3D ?
                "Switch to 3D view to enable export" :
                Breadcrumb.Count < 2 ?
                "Select workspace to begin export":
                "";
        }
    }
}
